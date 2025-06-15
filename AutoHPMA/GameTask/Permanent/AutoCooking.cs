using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Helpers.RecognizeHelper;
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
    FishRice, // 海鱼黄金焗饭
}

public class AutoCooking : IGameTask
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

    private readonly ILogger<AutoCooking> _logger;
    private nint _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;
    private AutoCookingState _state = AutoCookingState.Unknown;

    private Mat? captureMat;
    private Mat bin, board, oven, pot;
    private Mat rice, fish;
    private Mat cream, onion;
    private Mat order, red_order;
    private Mat oven_ring, pot_ring;

    private Mat ui_shop, ui_challenge, ui_clock, ui_continue1, ui_continue2;
    private Mat click_challenge, click_start;
    private Mat fishrice;

    private List<Rect> detect_rects = new List<Rect>();
    private List<Rect> order_rects = new List<Rect>();

    private int _autoCookingTimes;
    private Dishes _autoCookingDish = Dishes.FishRice;

    private bool initialized = false;
    private int round = 0;
    private bool _waited = false;

    private Rect bin_rect, board_rect, oven_rect, pot_rect;
    private Rect rice_rect, fish_rect;
    private Rect cream_rect, onion_rect;

    private Point bin_center, board_center, oven_center, pot_center; 
    private Point rice_center, fish_center;
    private Point cream_center, onion_center;
    private Point next_order;

    private (CookingStatus status, double progress) _ovenStatus = (CookingStatus.Idle, 0);
    private (CookingStatus status, double progress) _potStatus = (CookingStatus.Idle, 0);

    private int _creamCount = 0;
    private int _onionCount = 0;
    private static PaddleOCRHelper paddleOCRHelper;

    private CancellationTokenSource _cts;
    public event EventHandler? TaskCompleted;

    public AutoCooking(ILogger<AutoCooking> logger, nint _displayHwnd, nint _gameHwnd)
    {
        _logger = logger;
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
                        await Task.Delay(500, _cts.Token);
                        if (!initialized)
                        {
                            Initialize();
                            continue;
                        }

                        _ovenStatus = GetOvenStatus();
                        _potStatus = GetPotStatus();
                        UpdateKitchenwareStatus();

                        //厨具糊了
                        if (_ovenStatus.status == CookingStatus.Overcooked)
                        {
                            DragMove(ref oven_center, ref bin_center, 100);
                            DragMove(ref board_center, ref bin_center, 100);
                        }
                        if (_potStatus.status == CookingStatus.Overcooked)
                        {
                            DragMove(ref pot_center, ref bin_center, 100);
                            DragMove(ref board_center, ref bin_center, 100);
                        }

                        //厨具都为空
                        if (_ovenStatus.status == CookingStatus.Idle && _potStatus.status == CookingStatus.Idle)
                        {
                            DragMove(ref fish_center, ref oven_center, 100);
                            DragMove(ref rice_center, ref pot_center, 100);
                            continue;
                        }

                        //厨具都烹饪完成
                        if (_ovenStatus.status == CookingStatus.Cooked && _potStatus.status == CookingStatus.Cooked)
                        {
                            LocateOrders();
                            DragMove(ref oven_center, ref board_center, 100);
                            DragMove(ref pot_center, ref board_center, 100);
                            //继续补充食材
                            DragMove(ref fish_center, ref oven_center, 100);
                            DragMove(ref rice_center, ref pot_center, 100);
                            //补充调料
                            for (int i = 0; i < _creamCount; i++)
                            {
                                DragMove(ref cream_center, ref board_center, 100);
                            }
                            for (int i = 0; i < _onionCount; i++)
                            {
                                DragMove(ref onion_center, ref board_center, 100);
                            }
                            //提交订单
                            DragMove(ref board_center, ref next_order, 100);
                        }
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
        double threshold = 0.9;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        Mat maskMat;

        switch(_autoCookingDish)
        {
            case Dishes.FishRice:
                using (var fishriceBGR = fishrice.Clone())
                {
                    Cv2.CvtColor(fishriceBGR, fishriceBGR, ColorConversionCodes.BGRA2BGR);
                    maskMat = MatchTemplateHelper.GenerateMask(fishrice);
                    var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, fishriceBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
                    if (matchpoint == default)
                    {
                        return false;
                    }
                    detect_rects.Clear();
                    detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(fishrice.Width * scale), (int)(fishrice.Height * scale)));
                    _maskWindow?.SetLayerRects("Click", detect_rects);
                    SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + fishrice.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + fishrice.Height / 2.0 * scale));
                }
                break;
        }

        return true;
    }

    public void FindState()
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
        if (!LocateKitchenWare())
        {
            //_logger.LogDebug("未定位到厨房用具，即将开始重新定位。");
            return false;
        }
        else
        {
            //_maskWindow?.SetLayerRects("Kitchenware", new List<Rect>{ ScaleRect(oven_rect, scale), ScaleRect(pot_rect, scale) });
        }
        if(!LocateIngredients())
        {
            //_logger.LogDebug("未定位到食材，即将开始重新定位。");
            return false;
        }
        else
        {
            _maskWindow?.SetLayerRects("Ingredients", new List<Rect>{ ScaleRect(rice_rect, scale), ScaleRect(fish_rect, scale) });
        }
        if(!LocateCondiment())
        {
            //_logger.LogDebug("未定位到调料，即将开始重新定位。");
            return false;
        }
        else
        {
            _maskWindow?.SetLayerRects("Condiments", new List<Rect>{ ScaleRect(cream_rect, scale), ScaleRect(onion_rect, scale) });
        }

        _logger.LogInformation("自动烹饪初始化完成。");
        initialized = true;
        return true;
    }

    #region Locate Assets

    private bool LocateKitchenWare()
    {
        double threshold = 0.85;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, bin, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint == default)
        {
            return false;
        }
        else
        {
            bin_rect = new Rect(matchpoint.X, matchpoint.Y, bin.Width, bin.Height);
            bin_center = new Point(matchpoint.X + bin.Width / 2, matchpoint.Y + bin.Height / 2);
        }

        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, board, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint == default)
        {
            return false;
        }
        else
        {
            board_rect = new Rect(matchpoint.X, matchpoint.Y, board.Width, board.Height);
            board_center = new Point(matchpoint.X + board.Width/2, matchpoint.Y + board.Height/2);
        }

        Mat maskMat;
        using (var ovenBGR = oven.Clone())
        {
            Cv2.CvtColor(ovenBGR, ovenBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(oven);
            matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, ovenBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
            if(matchpoint == default)
            {
                //_logger.LogDebug("未定位到烤箱，即将重试。");
                return false;
            }
            oven_rect = new Rect(matchpoint.X, matchpoint.Y, oven.Width, oven.Height);
            oven_center = new Point(matchpoint.X + oven.Width / 2, matchpoint.Y + oven.Height / 2);
        }

        using (var potBGR = pot.Clone())
        {
            Cv2.CvtColor(potBGR, potBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(pot);
            matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, potBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
            if(matchpoint == default) 
            {
                //_logger.LogDebug("未定位到锅，即将重试。");
                return false;
            }
            pot_rect = new Rect(matchpoint.X, matchpoint.Y, pot.Width, pot.Height);
            pot_center = new Point(matchpoint.X + pot.Width / 2, matchpoint.Y + pot.Height / 2);
        }

        return true;
    }

    private bool LocateIngredients()
    {
        double threshold = 0.85;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        Mat maskMat;
        using (var riceBGR = rice.Clone())
        {
            Cv2.CvtColor(riceBGR, riceBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(rice);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, riceBGR, TemplateMatchModes.SqDiffNormed, maskMat, threshold);
            if(matchpoint == default)
            {
                //_logger.LogDebug("未定位到米饭，即将重试。");
                return false;
            }
            rice_rect = new Rect(matchpoint.X, matchpoint.Y, rice.Width, rice.Height);
            rice_center = new Point(matchpoint.X + rice.Width / 2, matchpoint.Y + rice.Height / 2);
        }
        using (var fishBGR = fish.Clone())
        {
            Cv2.CvtColor(fishBGR, fishBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(fish);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, fishBGR, TemplateMatchModes.SqDiffNormed, maskMat, threshold);
            if(matchpoint == default)
            {
                //_logger.LogDebug("未定位到鱼，即将重试。");
                return false;
            }
            fish_rect = new Rect(matchpoint.X, matchpoint.Y, fish.Width, fish.Height);
            fish_center = new Point(matchpoint.X + fish.Width / 2, matchpoint.Y + fish.Height / 2);
        }
        return true;
    }

    private bool LocateCondiment()
    {
        double threshold = 0.85;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        Mat maskMat;
        using (var creamBGR = cream.Clone())
        {
            Cv2.CvtColor(creamBGR, creamBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(cream);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, creamBGR, TemplateMatchModes.SqDiffNormed, maskMat, threshold);
            if (matchpoint == default)
            {
                //_logger.LogDebug("未定位到奶油，即将重试。");
                return false;
            }
            cream_rect = new Rect(matchpoint.X, matchpoint.Y, cream.Width, cream.Height);
            cream_center = new Point(matchpoint.X + cream.Width / 2, matchpoint.Y + cream.Height / 2);
        }
        using (var onionBGR = onion.Clone())
        {
            Cv2.CvtColor(onionBGR, onionBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(onion);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, onionBGR, TemplateMatchModes.SqDiffNormed, maskMat, threshold);
            if (matchpoint == default)
            {
                //_logger.LogDebug("未定位到洋葱，即将重试。");
                return false;
            }
            onion_rect = new Rect(matchpoint.X, matchpoint.Y, onion.Width, onion.Height);
            onion_center = new Point(matchpoint.X + onion.Width / 2, matchpoint.Y + onion.Height / 2);
        }
        return true;
    }

    private bool LocateOrders()
    {
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
            // 识别奶油数量
            var creamRect = new Rect(selectedOrderRect.X + selectedOrderRect.Width/2, selectedOrderRect.Y + selectedOrderRect.Height + 50, selectedOrderRect.Width/4, 40);
            var creamMat = new Mat(captureMat, creamRect);
            var creamText = paddleOCRHelper.Ocr(creamMat);
            if (int.TryParse(creamText, out int creamCount))
            {
                _creamCount = creamCount;
            }

            // 识别洋葱数量
            var onionRect = new Rect(selectedOrderRect.X + selectedOrderRect.Width*3/4, selectedOrderRect.Y + selectedOrderRect.Height + 50, selectedOrderRect.Width/4, 40);
            var onionMat = new Mat(captureMat, onionRect);
            var onionText = paddleOCRHelper.Ocr(onionMat);
            if (int.TryParse(onionText, out int onionCount))
            {
                _onionCount = onionCount;
            }

            //_logger.LogDebug("识别到订单：奶油数量 {creamCount}，洋葱数量 {onionCount}", _creamCount, _onionCount);
        }

        _maskWindow.SetLayerRects("Orders", detect_rects, new Dictionary<Rect, string> { { selectedOrderRect, "目标订单" } });
        if (next_order != default)
            return true;
        return false;
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
        fishrice = Cv2.ImRead(image_folder + "Dishes/海鱼黄金焗饭.png", ImreadModes.Unchanged);

        //Kitchenware
        bin = Cv2.ImRead(image_folder + "Kitchenware/bin.png");
        board = Cv2.ImRead(image_folder + "Kitchenware/board.png");
        oven = Cv2.ImRead(image_folder + "Kitchenware/oven.png", ImreadModes.Unchanged);
        pot = Cv2.ImRead(image_folder + "Kitchenware/pot.png", ImreadModes.Unchanged);
        oven_ring = Cv2.ImRead(image_folder + "Kitchenware/oven_ring.png");
        pot_ring = Cv2.ImRead(image_folder + "Kitchenware/pot_ring.png");

        //Condiments
        cream = Cv2.ImRead(image_folder + "Condiment/cream.png", ImreadModes .Unchanged);
        onion = Cv2.ImRead(image_folder + "Condiment/onion.png", ImreadModes.Unchanged);

        //Ingredients
        rice = Cv2.ImRead(image_folder + "Ingredients/rice.png", ImreadModes.Unchanged);
        fish = Cv2.ImRead(image_folder + "Ingredients/fish.png", ImreadModes.Unchanged);

        //Orders
        order = Cv2.ImRead(image_folder + "Order/order.png", ImreadModes.Unchanged);
        red_order = Cv2.ImRead(image_folder + "Order/red_order.png", ImreadModes.Unchanged);
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

    private (CookingStatus status, double progress) GetOvenStatus()
    {
        captureMat = _capture.Capture();
        using var ovenRegion = new Mat(captureMat, oven_rect);
        using var mask = oven_ring.Clone();
        Cv2.Resize(mask, mask, new Size(oven_rect.Width, oven_rect.Height));

        // 检查烹饪进度（f6b622颜色）
        double cookingPercentage = ColorFilterHelper.CalculateColorMatchPercentage(ovenRegion, mask, "f6b622", 5);
        if (cookingPercentage > 0)
        {
            return (CookingStatus.Cooking, cookingPercentage);
        }

        // 检查是否完成（ed5432颜色）
        double completedPercentage = ColorFilterHelper.CalculateColorMatchPercentage(ovenRegion, mask, "ed5432", 5);
        if (completedPercentage > 0)
        {
            if (completedPercentage > 95)
                return (CookingStatus.Overcooked, completedPercentage);
            return (CookingStatus.Cooked, completedPercentage);
        }

        return (CookingStatus.Idle, 0);
    }

    private (CookingStatus status, double progress) GetPotStatus()
    {
        captureMat = _capture.Capture();
        using var potRegion = new Mat(captureMat, pot_rect);
        using var mask = pot_ring.Clone();
        Cv2.Resize(mask, mask, new Size(pot_rect.Width, pot_rect.Height));

        // 检查烹饪进度（f6b622颜色）
        double cookingPercentage = ColorFilterHelper.CalculateColorMatchPercentage(potRegion, mask, "f6b622", 5);
        if (cookingPercentage > 0)
        {
            return (CookingStatus.Cooking, cookingPercentage);
        }

        // 检查是否完成（ed5432颜色）
        double completedPercentage = ColorFilterHelper.CalculateColorMatchPercentage(potRegion, mask, "ed5432", 5);
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
        
        // 更新烤箱状态
        string ovenText = _ovenStatus.status switch
        {
            CookingStatus.Idle => "空闲",
            CookingStatus.Cooking => $"烹饪中：{_ovenStatus.progress:F1}%",
            CookingStatus.Cooked => $"已完成：{_ovenStatus.progress:F1}%",
            CookingStatus.Overcooked => "糊了！",
            _ => "未知状态"
        };
        textContents[oven_rect] = ovenText;

        // 更新锅状态
        string potText = _potStatus.status switch
        {
            CookingStatus.Idle => "空闲",
            CookingStatus.Cooking => $"烹饪中：{_potStatus.progress:F1}%",
            CookingStatus.Cooked => $"已完成：{_potStatus.progress:F1}%",
            CookingStatus.Overcooked => "糊了！",
            _ => "未知状态"
        };
        textContents[pot_rect] = potText;

        _maskWindow?.SetLayerRects("Kitchenware", new List<Rect>{ ScaleRect(bin_rect, scale), ScaleRect(board_rect, scale), ScaleRect(oven_rect, scale), ScaleRect(pot_rect, scale) }, textContents);
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