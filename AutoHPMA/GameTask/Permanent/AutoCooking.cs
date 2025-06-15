using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Models.Cooking;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.GameTask.Permanent;

public enum AutoCookingState
{
    Unknown,
    Workbench,
    Challenge,
    Cooking,
    Summary,
}

public enum CookingStatus
{
    Idle,       // 空闲
    Cooking,    // 烹饪中
    Cooked,     // 已完成
    Overcooked  // 糊了
}

public enum Dishes
{
    FishRice,       // 黄金海鱼焗饭
    RoastedPig,     // 果香烤乳猪
    MushroomRisotto // 奶油蘑菇炖饭
}

public class AutoCooking : IGameTask
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

    private readonly ILogger<AutoCooking> _logger;
    private readonly CookingConfigService _cookingConfigService;
    private nint _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;
    private AutoCookingState _state = AutoCookingState.Unknown;

    private Mat? captureMat;
    private Mat bin, board, oven, pot;
    private Mat oven_ring, pot_ring;
    private Dictionary<string, Mat> ingredients = new();
    private Dictionary<string, Mat> condiments = new();
    private Mat order, red_order;

    private Mat ui_shop, ui_challenge, ui_clock, ui_continue1, ui_continue2;
    private Mat click_challenge, click_start;
    private Dictionary<string, Mat> dishImages = new();

    private List<Rect> detect_rects = new List<Rect>();
    private List<Rect> order_rects = new List<Rect>();

    private int _autoCookingTimes;
    private string _autoCookingDish = "黄金海鱼焗饭";
    private DishConfig? _currentDishConfig;

    private bool initialized = false;
    private int round = 0;
    private bool _waited = false;

    private Dictionary<string, Rect> kitchenwareRects = new();
    private Dictionary<string, Point> kitchenwareCenters = new();
    private Dictionary<string, Rect> ingredientRects = new();
    private Dictionary<string, Point> ingredientCenters = new();
    private Dictionary<string, Rect> condimentRects = new();
    private Dictionary<string, Point> condimentCenters = new();
    private Point next_order;

    private Dictionary<string, (CookingStatus status, double progress)> kitchenwareStatus = new();

    private Dictionary<string, int> condimentCounts = new();
    private static PaddleOCRHelper paddleOCRHelper;

    private CancellationTokenSource _cts;
    public event EventHandler? TaskCompleted;

    public AutoCooking(ILogger<AutoCooking> logger, CookingConfigService cookingConfigService, nint _displayHwnd, nint _gameHwnd)
    {
        _logger = logger;
        _cookingConfigService = cookingConfigService;
        this._displayHwnd = _displayHwnd;
        this._gameHwnd = _gameHwnd;
        _cts = new CancellationTokenSource();
        paddleOCRHelper = new PaddleOCRHelper();
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
    }

    public void Stop()
    {
        _cts.Cancel();
        TaskCompleted?.Invoke(this, EventArgs.Empty);
    }

    public async void Start()
    {
        _state = AutoCookingState.Unknown;
        _logWindow?.SetGameState("自动烹饪");
        _logger.LogInformation("[Aquamarine]---自动烹饪任务已启动---[/Aquamarine]");
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                GC.Collect();

                if (round >= _autoCookingTimes)
                {
                    Stop();
                    _logger.LogInformation("[Aquamarine]---自动烹饪任务已终止---[/Aquamarine]");
                    continue;
                }
                FindState();
                switch (_state)
                {
                    case AutoCookingState.Unknown:
                        if (!_waited)
                        {
                            await Task.Delay(2000, _cts.Token);
                            _waited = true;
                            break;
                        }
                        _logger.LogInformation("状态未知，请手动进入烹饪界面。");
                        _waited = false;
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoCookingState.Workbench:
                        FindAndClick(ref click_challenge);
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoCookingState.Challenge:
                        ChooseDish();
                        await Task.Delay(1500, _cts.Token);
                        FindAndClick(ref click_start);
                        await Task.Delay(2000, _cts.Token);
                        break;

                    case AutoCookingState.Cooking:
                        if (!initialized)
                        {
                            Initialize();
                            await Task.Delay(500, _cts.Token);
                            continue;
                        }

                        // 更新所有厨具状态
                        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
                        {
                            if (kitchenware == "oven" || kitchenware == "pot")
                            {
                                kitchenwareStatus[kitchenware] = GetKitchenwareStatus(kitchenware);
                            }
                        }
                        UpdateKitchenwareStatus();

                        // 检查是否有厨具糊了
                        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
                        {
                            if (kitchenwareStatus.TryGetValue(kitchenware, out var status) && status.status == CookingStatus.Overcooked)
                            {
                                // 把糊了的厨具和食材都扔进垃圾桶
                                var kitchenwareCenter = kitchenwareCenters[kitchenware];
                                var binCenter = kitchenwareCenters["bin"];
                                DragMove(ref kitchenwareCenter, ref binCenter, 100);
                                kitchenwareCenters[kitchenware] = kitchenwareCenter;
                                kitchenwareCenters["bin"] = binCenter;

                                var boardCenter = kitchenwareCenters["board"];
                                DragMove(ref boardCenter, ref binCenter, 100);
                                kitchenwareCenters["board"] = boardCenter;
                                kitchenwareCenters["bin"] = binCenter;

                                await Task.Delay(500, _cts.Token);
                                continue;
                            }
                        }

                        // 检查是否所有厨具都为空
                        bool allIdle = true;
                        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
                        {
                            if (kitchenwareStatus.TryGetValue(kitchenware, out var status) && status.status != CookingStatus.Idle)
                            {
                                allIdle = false;
                                break;
                            }
                        }

                        if (allIdle)
                        {
                            // 根据配置执行烹饪步骤
                            foreach (var step in _currentDishConfig.CookingSteps)
                            {
                                var ingredientCenter = ingredientCenters[step.Ingredient];
                                var kitchenwareCenter = kitchenwareCenters[step.TargetKitchenware];
                                DragMove(ref ingredientCenter, ref kitchenwareCenter, 100);
                                ingredientCenters[step.Ingredient] = ingredientCenter;
                                kitchenwareCenters[step.TargetKitchenware] = kitchenwareCenter;
                                await Task.Delay(500, _cts.Token);
                            }
                            continue;
                        }

                        // 检查是否所有厨具都烹饪完成
                        bool allCooked = true;
                        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
                        {
                            if (kitchenwareStatus.TryGetValue(kitchenware, out var status) && status.status != CookingStatus.Cooked)
                            {
                                allCooked = false;
                                break;
                            }
                        }

                        if (allCooked)
                        {
                            LocateOrders();

                            // 移出烹饪好的食材
                            foreach (var step in _currentDishConfig.CookingSteps)
                            {
                                var kitchenwareCenter = kitchenwareCenters[step.TargetKitchenware];
                                var boardCenter = kitchenwareCenters["board"];
                                DragMove(ref kitchenwareCenter, ref boardCenter, 100);
                                kitchenwareCenters[step.TargetKitchenware] = kitchenwareCenter;
                                kitchenwareCenters["board"] = boardCenter;
                                if (CheckOver()) continue;
                            }

                            // 继续补充食材
                            foreach (var step in _currentDishConfig.CookingSteps)
                            {
                                var ingredientCenter = ingredientCenters[step.Ingredient];
                                var kitchenwareCenter = kitchenwareCenters[step.TargetKitchenware];
                                DragMove(ref ingredientCenter, ref kitchenwareCenter, 100);
                                ingredientCenters[step.Ingredient] = ingredientCenter;
                                kitchenwareCenters[step.TargetKitchenware] = kitchenwareCenter;
                                if (CheckOver()) continue;
                            }

                            // 补充调料
                            foreach (var condiment in _currentDishConfig.RequiredCondiments)
                            {
                                if (condimentCounts.TryGetValue(condiment, out var count))
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var condimentCenter = condimentCenters[condiment];
                                        var boardCenter = kitchenwareCenters["board"];
                                        DragMove(ref condimentCenter, ref boardCenter, 100);
                                        condimentCenters[condiment] = condimentCenter;
                                        kitchenwareCenters["board"] = boardCenter;
                                        if (CheckOver()) continue;
                                    }
                                }
                            }

                            // 提交订单
                            var finalBoardCenter = kitchenwareCenters["board"];
                            DragMove(ref finalBoardCenter, ref next_order, 100);
                            kitchenwareCenters["board"] = finalBoardCenter;

                            await Task.Delay(500, _cts.Token);
                            continue;
                        }
                        await Task.Delay(500, _cts.Token);
                        break;

                    case AutoCookingState.Summary:
                        round++;
                        _logger.LogInformation("第 {round} 轮烹饪完成。", round);
                        _maskWindow.ClearLayer("Kitchenware");
                        _maskWindow.ClearLayer("Condiments");
                        _maskWindow.ClearLayer("Ingredients");
                        _maskWindow.ClearLayer("Orders");
                        await Task.Delay(1000, _cts.Token);
                        SendSpace(_gameHwnd);
                        await Task.Delay(2000, _cts.Token);
                        SendSpace(_gameHwnd);
                        await Task.Delay(3000, _cts.Token);
                        break;

                    default:

                        break;


                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[Aquamarine]---自动烹饪任务已终止---[/Aquamarine]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生异常");
        }
        finally
        {
            _maskWindow.ClearAllLayers();
            _logWindow?.SetGameState("空闲");
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    private bool ChooseDish()
    {
        if (_currentDishConfig == null) return false;

        double threshold = 0.9;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        if (dishImages.TryGetValue(_currentDishConfig.ImagePath, out var dishImage))
        {
            using (var dishImageBGR = dishImage.Clone())
            {
                Cv2.CvtColor(dishImageBGR, dishImageBGR, ColorConversionCodes.BGRA2BGR);
                var maskMat = MatchTemplateHelper.GenerateMask(dishImage);
                var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, dishImageBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
                if (matchpoint == default)
                {
                    return false;
                }
                detect_rects.Clear();
                detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(dishImage.Width * scale), (int)(dishImage.Height * scale)));
                _maskWindow?.SetLayerRects("Click", detect_rects);
                SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + dishImage.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + dishImage.Height / 2.0 * scale));
            }
            return true;
        }
        return false;
    }

    private bool CheckOver()
    {
        FindState();
        if(_state == AutoCookingState.Summary)
        {
            return true;
        }
        return false;
    }

    private void FindState()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        //Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        if (FindMatch(ref ui_shop))
        {
            _state = AutoCookingState.Workbench;
            _logWindow?.SetGameState("烹饪-工作台");
            _waited = false;
            return;
        }

        if (FindMatch(ref ui_challenge))
        {
            _state = AutoCookingState.Challenge;
            _logWindow?.SetGameState("烹饪-订单挑战");
            _waited = false;
            return;
        }

        if (FindMatch(ref ui_clock))
        {
            _state = AutoCookingState.Cooking;
            _logWindow?.SetGameState("烹饪-烹饪中");
            _waited = false;
            return;
        }

        if (FindMatch(ref ui_continue1) || FindMatch(ref ui_continue2))
        {
            _state = AutoCookingState.Summary;
            _logWindow?.SetGameState("烹饪-结算中");
            _waited = false;
            return;
        }

        _state = AutoCookingState.Unknown;
        return;

    }

    private bool FindMatch(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
        _maskWindow?.ClearLayer("Click");
        detect_rects.Clear();
        detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        _maskWindow?.SetLayerRects("Match", detect_rects);
        return true;

    }

    private bool FindAndClick(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
        detect_rects.Clear();
        detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        _maskWindow?.SetLayerRects("Click", detect_rects);
        SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
        return true;
    }

    private bool Initialize()
    {
        if (_currentDishConfig == null)
        {
            _logger.LogError("当前菜品配置为空");
            return false;
        }

        _logger.LogInformation("开始初始化菜品：{DishName}", _currentDishConfig.Name);
        _logger.LogInformation("需要定位的厨具：{Kitchenware}", string.Join(", ", _currentDishConfig.RequiredKitchenware));
        _logger.LogInformation("需要定位的食材：{Ingredients}", string.Join(", ", _currentDishConfig.RequiredIngredients));
        _logger.LogInformation("需要定位的调料：{Condiments}", string.Join(", ", _currentDishConfig.RequiredCondiments));

        if (!LocateKitchenWare())
        {
            _logger.LogError("厨具定位失败");
            return false;
        }
        _logger.LogInformation("厨具定位成功");

        if (!LocateIngredients())
        {
            _logger.LogError("食材定位失败");
            return false;
        }
        _logger.LogInformation("食材定位成功");

        if (!LocateCondiment())
        {
            _logger.LogError("调料定位失败");
            return false;
        }
        _logger.LogInformation("调料定位成功");

        _logger.LogInformation("自动烹饪初始化完成");
        initialized = true;
        return true;
    }

    #region Locate Assets

    private bool LocateKitchenWare()
    {
        if (_currentDishConfig == null) return false;

        double threshold = 0.85;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
        {
            _logger.LogDebug("正在定位厨具：{Kitchenware}", kitchenware);
            var mat = GetKitchenwareMat(kitchenware);
            var matMask = MatchTemplateHelper.GenerateMask(mat);
            var matBGR = mat.Clone();
            Cv2.CvtColor(matBGR, matBGR, ColorConversionCodes.BGRA2BGR);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, matBGR, TemplateMatchModes.CCoeffNormed, matMask, threshold);
            if (matchpoint == default)
            {
                _logger.LogError("厨具 {Kitchenware} 定位失败", kitchenware);
                return false;
            }
            kitchenwareRects[kitchenware] = new Rect(matchpoint.X, matchpoint.Y, mat.Width, mat.Height);
            kitchenwareCenters[kitchenware] = new Point(matchpoint.X + mat.Width / 2, matchpoint.Y + mat.Height / 2);
            _logger.LogDebug("厨具 {Kitchenware} 定位成功，位置：({X}, {Y})", kitchenware, matchpoint.X, matchpoint.Y);
        }

        return true;
    }

    private bool LocateIngredients()
    {
        if (_currentDishConfig == null) return false;

        double threshold = 0.85;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        foreach (var ingredient in _currentDishConfig.RequiredIngredients)
        {
            _logger.LogDebug("正在定位食材：{Ingredient}", ingredient);
            if (!ingredients.ContainsKey(ingredient))
            {
                _logger.LogError("食材 {Ingredient} 的图片未加载", ingredient);
                return false;
            }
            var mat = ingredients[ingredient];
            var matMask = MatchTemplateHelper.GenerateMask(mat);
            var matBGR = mat.Clone();
            Cv2.CvtColor(matBGR, matBGR, ColorConversionCodes.BGRA2BGR);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, matBGR, TemplateMatchModes.SqDiffNormed, matMask, threshold);
            if (matchpoint == default)
            {
                _logger.LogError("食材 {Ingredient} 定位失败", ingredient);
                return false;
            }
            ingredientRects[ingredient] = new Rect(matchpoint.X, matchpoint.Y, ingredients[ingredient].Width, ingredients[ingredient].Height);
            ingredientCenters[ingredient] = new Point(matchpoint.X + ingredients[ingredient].Width / 2, matchpoint.Y + ingredients[ingredient].Height / 2);
            _logger.LogDebug("食材 {Ingredient} 定位成功，位置：({X}, {Y})", ingredient, matchpoint.X, matchpoint.Y);
        }

        return true;
    }

    private bool LocateCondiment()
    {
        if (_currentDishConfig == null) return false;

        double threshold = 0.85;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        foreach (var condiment in _currentDishConfig.RequiredCondiments)
        {
            _logger.LogDebug("正在定位调料：{Condiment}", condiment);
            if (!condiments.ContainsKey(condiment))
            {
                _logger.LogError("调料 {Condiment} 的图片未加载", condiment);
                return false;
            }
            var mat = condiments[condiment];
            var matMask = MatchTemplateHelper.GenerateMask(mat);
            var matBGR = mat.Clone();
            Cv2.CvtColor(matBGR, matBGR, ColorConversionCodes.BGRA2BGR);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, matBGR, TemplateMatchModes.SqDiffNormed, matMask, threshold);
            if (matchpoint == default)
            {
                _logger.LogError("调料 {Condiment} 定位失败", condiment);
                return false;
            }
            condimentRects[condiment] = new Rect(matchpoint.X, matchpoint.Y, condiments[condiment].Width, condiments[condiment].Height);
            condimentCenters[condiment] = new Point(matchpoint.X + condiments[condiment].Width / 2, matchpoint.Y + condiments[condiment].Height / 2);
            _logger.LogDebug("调料 {Condiment} 定位成功，位置：({X}, {Y})", condiment, matchpoint.X, matchpoint.Y);
        }

        return true;
    }

    private Mat GetKitchenwareMat(string kitchenware)
    {
        return kitchenware switch
        {
            "bin" => bin,
            "board" => board,
            "oven" => oven,
            "pot" => pot,
            _ => throw new ArgumentException($"未知的厨具：{kitchenware}")
        };
    }

    private bool LocateOrders()
    {
        if (_currentDishConfig == null) return false;

        double threshold = 0.9;
        next_order = default;
        detect_rects.Clear();
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        Mat maskMat;
        Rect selectedOrderRect = default;

        using (var red_orderBGR = red_order.Clone())
        {
            Cv2.CvtColor(red_orderBGR, red_orderBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(red_order);
            var matches = MatchTemplateHelper.MatchOnePicForOnePic(
                captureMat,
                red_orderBGR,
                TemplateMatchModes.SqDiffNormed,
                maskMat,
                threshold
            );

            if(matches.Count != 0)
            {
                selectedOrderRect = matches[0];
                next_order = new Point(matches[0].X + order.Width / 2, matches[0].Y + order.Height / 2);
                foreach (var rect in matches)
                {
                    detect_rects.Add(ScaleRect(rect, scale));
                }
            }
        }

        using (var orderBGR = order.Clone())
        {
            Cv2.CvtColor(orderBGR, orderBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(order);
            var matches = MatchTemplateHelper.MatchOnePicForOnePic(
                captureMat,
                orderBGR,
                TemplateMatchModes.SqDiffNormed,
                maskMat,
                threshold
            );

            if (matches.Count != 0)
            {
                if(next_order == default)
                {
                    int min_X = matches[0].X;
                    // 选择X最小的match
                    foreach (var rect in matches)
                    {
                        if(rect.X <= min_X)
                        {
                            selectedOrderRect = rect;
                            next_order.X = rect.X + order.Width/2;
                            next_order.Y = rect.Y + order.Height/2;
                        }
                    }
                }
                foreach (var rect in matches)
                {
                    detect_rects.Add(ScaleRect(rect, scale));
                }
            }
        }

        // 只对最终选择的订单进行调料数量识别
        if (selectedOrderRect != default)
        {
            // 识别每个调料的数量
            foreach (var condiment in _currentDishConfig.RequiredCondiments)
            {
                if (_currentDishConfig.CondimentPositions.TryGetValue(condiment, out var position))
                {
                    var condimentRect = new Rect(
                        selectedOrderRect.X + selectedOrderRect.Width * (position - 1) / 4,
                        selectedOrderRect.Y + selectedOrderRect.Height + 50,
                        selectedOrderRect.Width / 4,
                        40
                    );
                    var condimentMat = new Mat(captureMat, condimentRect);
                    var condimentText = paddleOCRHelper.Ocr(condimentMat);
                    if (int.TryParse(condimentText, out int count))
                    {
                        condimentCounts[condiment] = count;
                    }
                }
            }
        }

        _maskWindow.SetLayerRects("Orders", detect_rects, new Dictionary<Rect, string> { { selectedOrderRect, "目标订单" } });
        return next_order != default;
    }

    #endregion

    private bool DragMove(ref Point start, ref Point end, int duration = 500)
    {
        SendMouseDragWithNoise(
            _gameHwnd,
            (uint)(start.X * scale - offsetX),
            (uint)(start.Y * scale - offsetY),
            (uint)(end.X * scale - offsetX),
            (uint)(end.Y * scale - offsetY),
            duration
        );
        return true;
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow.AddLayer("Kitchenware");
        _maskWindow.AddLayer("Condiments");
        _maskWindow.AddLayer("Ingredients");
        _maskWindow.AddLayer("Orders");

        _maskWindow.AddLayer("Match");
        _maskWindow.AddLayer("Click");
    }

    public void LoadAssets()
    {
        string image_folder = "Assets/Cooking/Image/";
        _logger.LogInformation("开始加载图片资源");
        
        //UI
        ui_shop = Cv2.ImRead(image_folder + "ui_shop.png");
        ui_challenge = Cv2.ImRead(image_folder + "ui_challenge.png");
        ui_clock = Cv2.ImRead(image_folder + "ui_clock.png");
        ui_continue1 = Cv2.ImRead(image_folder + "ui_continue1.png");
        ui_continue2 = Cv2.ImRead(image_folder + "ui_continue2.png");

        //Click
        click_challenge = Cv2.ImRead(image_folder + "click_challenge.png");
        click_start = Cv2.ImRead(image_folder + "click_start.png");

        //Dishes
        dishImages["Dishes/海鱼黄金焗饭.png"] = Cv2.ImRead(image_folder + "Dishes/海鱼黄金焗饭.png", ImreadModes.Unchanged);
        dishImages["Dishes/果香烤乳猪.png"] = Cv2.ImRead(image_folder + "Dishes/果香烤乳猪.png", ImreadModes.Unchanged);
        _logger.LogDebug("已加载菜品图片：海鱼黄金焗饭，果香烤乳猪。");

        //Kitchenware
        bin = Cv2.ImRead(image_folder + "Kitchenware/bin.png");
        board = Cv2.ImRead(image_folder + "Kitchenware/board.png");
        oven = Cv2.ImRead(image_folder + "Kitchenware/oven.png", ImreadModes.Unchanged);
        pot = Cv2.ImRead(image_folder + "Kitchenware/pot.png", ImreadModes.Unchanged);
        oven_ring = Cv2.ImRead(image_folder + "Kitchenware/oven_ring.png");
        pot_ring = Cv2.ImRead(image_folder + "Kitchenware/pot_ring.png");
        _logger.LogDebug("已加载厨具图片：bin, board, oven, pot");

        //Condiments
        condiments["cream"] = Cv2.ImRead(image_folder + "Condiment/cream.png", ImreadModes.Unchanged);
        condiments["onion"] = Cv2.ImRead(image_folder + "Condiment/onion.png", ImreadModes.Unchanged);
        condiments["lemon"] = Cv2.ImRead(image_folder + "Condiment/lemon.png", ImreadModes.Unchanged);
        condiments["coconut"] = Cv2.ImRead(image_folder + "Condiment/coconut.png", ImreadModes.Unchanged);
        _logger.LogDebug("已加载调料图片：cream, onion, lemon, coconut");

        //Ingredients
        ingredients["rice"] = Cv2.ImRead(image_folder + "Ingredients/rice.png", ImreadModes.Unchanged);
        ingredients["fish"] = Cv2.ImRead(image_folder + "Ingredients/fish.png", ImreadModes.Unchanged);
        ingredients["pork"] = Cv2.ImRead(image_folder + "Ingredients/pork.png", ImreadModes.Unchanged);
        _logger.LogDebug("已加载食材图片：rice, fish, pork");

        //Orders
        order = Cv2.ImRead(image_folder + "Order/order.png", ImreadModes.Unchanged);
        red_order = Cv2.ImRead(image_folder + "Order/red_order.png", ImreadModes.Unchanged);
        _logger.LogDebug("已加载订单图片：order, red_order");

        _logger.LogInformation("图片资源加载完成");
    }

    private void CalOffset()
    {
        int left, top, width, height;
        int leftMumu, topMumu;
        GetWindowPositionAndSize(_displayHwnd, out leftMumu, out topMumu, out width, out height);
        GetWindowPositionAndSize(_gameHwnd, out left, out top, out width, out height);
        offsetX = left - leftMumu;
        offsetY = top - topMumu;
        scale = width / 1280.0;
    }
    private Rect ScaleRect(Rect rect, double scale)
    {
        return new Rect(
            (int)(rect.X * scale),
            (int)(rect.Y * scale),
            (int)(rect.Width * scale),
            (int)(rect.Height * scale)
        );
    }

    private (CookingStatus status, double progress) GetKitchenwareStatus(string kitchenware)
    {
        if (_currentDishConfig == null) return (CookingStatus.Idle, 0);

        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        var rect = kitchenwareRects[kitchenware];
        var region = new Mat(captureMat, rect);
        var mask = kitchenware == "oven" ? oven_ring : pot_ring;

        // 检查是否在烹饪中（黄色）
        double cookingPercentage = ColorFilterHelper.CalculateColorMatchPercentage(region, mask, "ffd700", 5);
        if (cookingPercentage > 0)
        {
            return (CookingStatus.Cooking, cookingPercentage);
        }

        // 检查是否完成（ed5432颜色）
        double completedPercentage = ColorFilterHelper.CalculateColorMatchPercentage(region, mask, "ed5432", 5);
        if (completedPercentage > 0)
        {
            if (completedPercentage > 95)
                return (CookingStatus.Overcooked, completedPercentage);
            return (CookingStatus.Cooked, completedPercentage);
        }

        return (CookingStatus.Idle, 0);
    }

    private void UpdateKitchenwareStatus()
    {
        var textContents = new Dictionary<Rect, string>();
        
        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
        {
            if (kitchenwareStatus.TryGetValue(kitchenware, out var status))
            {
                string text = status.status switch
                {
                    CookingStatus.Idle => "空闲",
                    CookingStatus.Cooking => $"烹饪中：{status.progress:F1}%",
                    CookingStatus.Cooked => $"已完成：{status.progress:F1}%",
                    CookingStatus.Overcooked => "糊了！",
                    _ => "未知状态"
                };
                textContents[kitchenwareRects[kitchenware]] = text;
            }
        }

        var rects = _currentDishConfig.RequiredKitchenware.Select(k => ScaleRect(kitchenwareRects[k], scale)).ToList();
        _maskWindow?.SetLayerRects("Kitchenware", rects, textContents);
    }

    #region SetParameter

    public bool SetParameters(Dictionary<string, object> parameters)
    {
        try
        {
            if (parameters.ContainsKey("Times"))
            {
                var times = Convert.ToInt32(parameters["Times"]);
                if (times < 0)
                {
                    _logger.LogWarning("烹饪次数必须大于等于0。已设置为默认值。");
                    return false;
                }
                _autoCookingTimes = times;
                _logger.LogDebug("烹饪次数设置为：{Times}次", _autoCookingTimes);
            }

            if (parameters.ContainsKey("Dish"))
            {
                var dish = parameters["Dish"].ToString();
                if (dish != null)
                {
                    _autoCookingDish = dish;
                    _currentDishConfig = _cookingConfigService.GetDishConfig(dish);
                    if (_currentDishConfig == null)
                    {
                        _logger.LogWarning("未找到菜品配置：{Dish}。已设置为默认值。", dish);
                        return false;
                    }
                    _logger.LogDebug("菜品设置为：{Dish}", _autoCookingDish);
                }
                else
                {
                    _logger.LogWarning("无效的菜品选择。已设置为默认值。");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("设置参数时发生错误：{Message}", ex.Message);
            return false;
        }
    }

    #endregion

}