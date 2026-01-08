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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AutoHPMA.Helpers.WindowInteractionHelper;
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

public class AutoCooking : BaseGameTask
{
    #region 字段

    private readonly CookingConfigService _cookingConfigService;
    private AutoCookingState _state = AutoCookingState.Unknown;

    // 模板图像
    private Mat bin, board;  // 不需要进度条的厨具
    private Dictionary<string, Mat> kitchenwares = new();  // 需要进度条的厨具
    private Dictionary<string, Mat> kitchenwareRings = new();  // 厨具进度条
    private Dictionary<string, Mat> ingredients = new();
    private Dictionary<string, Mat> condiments = new();
    private Mat order, red_order;
    private Dictionary<string, Mat> dishImages = new();

    // 任务参数
    private int _autoCookingTimes;
    private string _autoCookingDish;
    private int _autoCookingGap = 100;
    private DishConfig? _currentDishConfig;

    // 状态追踪
    private bool initialized = false;
    private int round = 0;

    // 定位结果
    private Dictionary<string, Rect> kitchenwareRects = new();
    private Dictionary<string, Point> kitchenwareCenters = new();
    private Dictionary<string, Rect> ingredientRects = new();
    private Dictionary<string, Point> ingredientCenters = new();
    private Dictionary<string, Rect> condimentRects = new();
    private Dictionary<string, Point> condimentCenters = new();
    private Point next_order;

    // 烹饪状态
    private Dictionary<string, (CookingStatus status, double progress)> kitchenwareStatus = new();
    private HashSet<int> completedSteps = new();
    private HashSet<int> preCookedSteps = new();
    private Dictionary<string, int> condimentCounts = new();

    // OCR
    private static PaddleOCRHelper? paddleOCRHelper;
    private static TesseractOCRHelper? tesseractOCRHelper;
    private string _autoCookingSelectedOCR = "Tesseract";

    #endregion

    // 状态检测规则
    private StateRule<AutoCookingState>[] _stateRules = null!;

    public AutoCooking(ILogger<AutoCooking> logger, CookingConfigService cookingConfigService, nint displayHwnd, nint gameHwnd)
        : base(logger, displayHwnd, gameHwnd)
    {
        _cookingConfigService = cookingConfigService;
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
        InitStateRules();
    }

    private void InitStateRules()
    {
        _stateRules = new StateRule<AutoCookingState>[]
        {
            new(new[] { GetImage("ui_clock") }, AutoCookingState.Cooking, "烹饪-烹饪中"),
            new(new[] { GetImage("ui_shop") }, AutoCookingState.Workbench, "烹饪-工作台"),
            new(new[] { GetImage("ui_challenge") }, AutoCookingState.Challenge, "烹饪-订单挑战"),
            new(new[] { GetImage("ui_continue1"), GetImage("ui_continue2") }, AutoCookingState.Summary, "烹饪-结算中"),
        };
    }

    public override async void Start()
    {
        _state = AutoCookingState.Unknown;
        await RunTaskAsync("自动烹饪");
    }

    protected override async Task ExecuteLoopAsync()
    {
        if (round >= _autoCookingTimes)
        {
            ToastNotificationHelper.ShowToast("烹饪任务完成", $"已完成 {round} 轮烹饪任务。");
            Stop();
            return;
        }
        
        _state = FindStateByRules(_stateRules, AutoCookingState.Unknown, "烹饪-未知状态");
        
        // 进入有效状态时重置等待标志
        if (_state != AutoCookingState.Unknown)
            _waited = false;
        
        switch (_state)
        {
            case AutoCookingState.Unknown:
                if (!_waited)
                {
                    await Task.Delay(2000, _cts.Token);
                    _waited = true;
                    return;
                }
                _waited = false;
                _logger.LogInformation("状态未知，请手动进入烹饪界面。");
                await Task.Delay(1000, _cts.Token);
                break;

            case AutoCookingState.Workbench:
                TryClickTemplate(GetImage("click_challenge"));
                await Task.Delay(1000, _cts.Token);
                break;

            case AutoCookingState.Challenge:
                ChooseDish();
                await Task.Delay(1500, _cts.Token);
                TryClickTemplate(GetImage("click_start"));
                await Task.Delay(2000, _cts.Token);
                break;

            case AutoCookingState.Cooking:
                if (!initialized)
                {
                    Initialize();
                    await Task.Delay(500, _cts.Token);
                    return;
                }

                // 更新所有厨具状态
                foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
                {
                    if (kitchenware == "bin" || kitchenware == "board") continue;
                    kitchenwareStatus[kitchenware] = GetKitchenwareStatus(kitchenware);
                }
                UpdateKitchenwareStatusDisplay();

                // 检查是否有厨具糊了
                foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
                {
                    if (kitchenwareStatus.TryGetValue(kitchenware, out var status) && status.status == CookingStatus.Overcooked)
                    {
                        DiscardAllFood();
                        await Task.Delay(_autoCookingGap, _cts.Token);
                        return;
                    }
                }

                // 执行烹饪循环
                ExecuteCookingCycle();
                await Task.Delay(_autoCookingGap, _cts.Token);
                break;

            case AutoCookingState.Summary:
                round++;
                _logger.LogInformation("第 {round} 轮烹饪完成。", round);
                ClearCookingLayers();
                await Task.Delay(3000, _cts.Token);
                SendSpace(_gameHwnd);
                await Task.Delay(3000, _cts.Token);
                SendSpace(_gameHwnd);
                await Task.Delay(3000, _cts.Token);
                break;
        }
    }

    #region 辅助方法

    private void ClearCookingLayers()
    {
        _maskWindow?.ClearLayer("Kitchenware");
        _maskWindow?.ClearLayer("Condiments");
        _maskWindow?.ClearLayer("Ingredients");
        _maskWindow?.ClearLayer("Orders");
    }

    #endregion

    #region 状态检测

    private bool CheckOver() => !Find(GetImage("ui_clock")).Success;

    #endregion

    #region 菜品选择

    private bool ChooseDish()
    {
        if (_currentDishConfig == null) return false;

        if (dishImages.TryGetValue(_currentDishConfig.ImagePath, out var dishImage))
        {
            var result = Find(dishImage, new MatchOptions { UseAlphaMask = true });
            if (result.Success)
            {
                ShowMatchRects(result, "Click");
                ClickMatchCenter(result);
                return true;
            }
        }
        return false;
    }

    #endregion

    #region 烹饪逻辑

    private bool ExecuteCookingCycle()
    {
        if (_currentDishConfig == null) return false;

        if (IsAllStepsCompleted())
        {
            return ExecuteFinalStage();
        }

        return ExecuteNextCookingStep();
    }

    private bool IsAllStepsCompleted()
    {
        if (completedSteps.Count < _currentDishConfig.CookingSteps.Count)
            return false;

        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
        {
            if (kitchenware == "bin" || kitchenware == "board") continue;

            if (kitchenwareStatus.TryGetValue(kitchenware, out var status))
            {
                if (status.status == CookingStatus.Cooking || status.status == CookingStatus.Cooked)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool ExecuteNextCookingStep()
    {
        for (int i = 0; i < _currentDishConfig.CookingSteps.Count; i++)
        {
            var step = _currentDishConfig.CookingSteps[i];
            var targetKitchenware = step.TargetKitchenware;

            if (!kitchenwareStatus.TryGetValue(targetKitchenware, out var status))
                continue;

            switch (status.status)
            {
                case CookingStatus.Idle:
                    if (!completedSteps.Contains(i) && !preCookedSteps.Contains(i))
                    {
                        PlaceIngredientInKitchenware(step.Ingredient, targetKitchenware);
                        completedSteps.Add(i);
                        return false;
                    }
                    break;

                case CookingStatus.Cooked:
                    MoveFromKitchenwareToBoard(targetKitchenware);
                    if (preCookedSteps.Contains(i))
                    {
                        preCookedSteps.Remove(i);
                        completedSteps.Add(i);
                    }
                    return false;

                case CookingStatus.Cooking:
                case CookingStatus.Overcooked:
                    continue;
            }
        }

        return false;
    }

    private bool ExecuteFinalStage()
    {
        completedSteps.Clear();

        DefaultOrder();
        if (CheckOver()) return true;

        StartPreCooking();

        Seasoning();
        if (CheckOver()) return true;

        SubmitOrder();
        return true;
    }

    private void PlaceIngredientInKitchenware(string ingredient, string kitchenware)
    {
        if (CheckOver()) return;

        var ingredientCenter = ingredientCenters[ingredient];
        var kitchenwareCenter = kitchenwareCenters[kitchenware];

        _logger.LogDebug("将食材 {Ingredient} 放入厨具 {Kitchenware}", ingredient, kitchenware);
        DragMove(ref ingredientCenter, ref kitchenwareCenter, 100);
    }

    private void MoveFromKitchenwareToBoard(string kitchenware)
    {
        if (CheckOver()) return;

        var kitchenwareCenter = kitchenwareCenters[kitchenware];
        var boardCenter = kitchenwareCenters["board"];

        _logger.LogDebug("将食物从厨具 {Kitchenware} 移到砧板", kitchenware);
        DragMove(ref kitchenwareCenter, ref boardCenter, 100);
    }

    private void StartPreCooking()
    {
        if (_currentDishConfig == null) return;

        _logger.LogDebug("开始预烹饪检查...");

        int preCookedCount = 0;
        const int maxPreCookItems = 2;
        var usedKitchenware = new HashSet<string>();

        for (int i = 0; i < _currentDishConfig.CookingSteps.Count && preCookedCount < maxPreCookItems; i++)
        {
            var step = _currentDishConfig.CookingSteps[i];
            var targetKitchenware = step.TargetKitchenware;

            if (preCookedSteps.Contains(i) || usedKitchenware.Contains(targetKitchenware))
                continue;

            if (kitchenwareStatus.TryGetValue(targetKitchenware, out var status) && status.status == CookingStatus.Idle)
            {
                _logger.LogInformation("预烹饪：将食材 {Ingredient} 放入厨具 {Kitchenware}", step.Ingredient, targetKitchenware);
                PlaceIngredientInKitchenware(step.Ingredient, targetKitchenware);
                preCookedSteps.Add(i);
                usedKitchenware.Add(targetKitchenware);
                preCookedCount++;
            }
        }

        if (preCookedCount > 0)
        {
            _logger.LogInformation("预烹饪完成，已放入 {Count} 个食材", preCookedCount);
        }
    }

    private void Seasoning()
    {
        foreach (var condiment in _currentDishConfig.RequiredCondiments)
        {
            if (condimentCounts.TryGetValue(condiment, out var count))
            {
                for (int i = 0; i < count; i++)
                {
                    var condimentCenter = condimentCenters[condiment];
                    var boardCenter = kitchenwareCenters["board"];
                    if (CheckOver()) return;
                    DragMove(ref condimentCenter, ref boardCenter, 100);
                }
            }
        }
    }

    private void SubmitOrder()
    {
        var finalBoardCenter = kitchenwareCenters["board"];
        if (CheckOver()) return;
        DragMove(ref finalBoardCenter, ref next_order, 100);
    }

    private void DiscardAllFood()
    {
        var binCenter = kitchenwareCenters["bin"];

        foreach (var kitchenware in _currentDishConfig.RequiredKitchenware)
        {
            if (kitchenware == "bin" || kitchenware == "board") continue;

            var kitchenwareCenter = kitchenwareCenters[kitchenware];
            if (CheckOver()) return;
            DragMove(ref kitchenwareCenter, ref binCenter, 100);
        }

        var boardCenter = kitchenwareCenters["board"];
        if (CheckOver()) return;
        DragMove(ref boardCenter, ref binCenter, 100);
    }

    #endregion

    #region 初始化与定位

    private bool Initialize()
    {
        if (_currentDishConfig == null)
        {
            _logger.LogError("当前菜品配置为空");
            return false;
        }

        _logger.LogInformation("开始初始化菜品：{DishName}", _currentDishConfig.Name);

        if (!LocateItems(_currentDishConfig.RequiredKitchenware, GetKitchenwareMat, kitchenwareRects, kitchenwareCenters, "厨具"))
            return false;

        if (!LocateItems(_currentDishConfig.RequiredIngredients, k => ingredients[k], ingredientRects, ingredientCenters, "食材", TemplateMatchModes.SqDiffNormed))
            return false;

        if (!LocateItems(_currentDishConfig.RequiredCondiments, k => condiments[k], condimentRects, condimentCenters, "调料", TemplateMatchModes.SqDiffNormed))
            return false;

        // 显示食材和调料
        var ingredientRectsList = _currentDishConfig.RequiredIngredients.Select(i => ScaleRect(ingredientRects[i], scale)).ToList();
        var condimentRectsList = _currentDishConfig.RequiredCondiments.Select(c => ScaleRect(condimentRects[c], scale)).ToList();
        _maskWindow?.SetLayerRects("Ingredients", ingredientRectsList);
        _maskWindow?.SetLayerRects("Condiments", condimentRectsList);

        completedSteps.Clear();
        initialized = true;
        _logger.LogInformation("菜品初始化完成");
        return true;
    }

    /// <summary>
    /// 通用定位方法
    /// </summary>
    private bool LocateItems(
        List<string> items,
        Func<string, Mat> getTemplate,
        Dictionary<string, Rect> rectsDict,
        Dictionary<string, Point> centersDict,
        string itemType,
        TemplateMatchModes matchMode = TemplateMatchModes.CCoeffNormed)
    {
        var captureMat = CaptureAndPreprocess();

        foreach (var item in items)
        {
            _logger.LogDebug("正在定位{Type}：{Item}", itemType, item);
            
            Mat template;
            try
            {
                template = getTemplate(item);
            }
            catch
            {
                _logger.LogError("{Type} {Item} 的图片未加载", itemType, item);
                return false;
            }

            var result = FindInSource(captureMat, template, new MatchOptions 
            { 
                UseAlphaMask = true, 
                MatchMode = matchMode 
            });

            if (!result.Success)
            {
                _logger.LogError("{Type} {Item} 定位失败", itemType, item);
                return false;
            }

            rectsDict[item] = new Rect(result.Location.X, result.Location.Y, template.Width, template.Height);
            centersDict[item] = new Point(result.Location.X + template.Width / 2, result.Location.Y + template.Height / 2);
            _logger.LogDebug("{Type} {Item} 定位成功", itemType, item);
        }

        _logger.LogInformation("{Type}定位成功", itemType);
        return true;
    }


    private Mat GetKitchenwareMat(string kitchenware)
    {
        return kitchenware switch
        {
            "bin" => bin,
            "board" => board,
            _ => kitchenwares.ContainsKey(kitchenware)
                ? kitchenwares[kitchenware]
                : throw new ArgumentException($"未知的厨具：{kitchenware}")
        };
    }

    private bool DefaultOrder()
    {
        next_order = new Point(400, 130);
        foreach (var condiment in _currentDishConfig.RequiredCondiments)
        {
            condimentCounts[condiment] = 1;
        }
        return true;
    }

    #endregion

    #region 厨具状态

    private (CookingStatus status, double progress) GetKitchenwareStatus(string kitchenware)
    {
        if (_currentDishConfig == null) return (CookingStatus.Idle, 0);

        var captureMat = CaptureAndPreprocess();
        var rect = kitchenwareRects[kitchenware];
        var region = new Mat(captureMat, rect);
        var mask = kitchenwareRings[kitchenware];

        double completedPercentage = ColorFilterHelper.CalculateColorMatchPercentage(region, mask, "ed5432", 5);
        if (completedPercentage > 0)
        {
            if (completedPercentage > 95)
                return (CookingStatus.Overcooked, completedPercentage);
            return (CookingStatus.Cooked, completedPercentage);
        }

        double cookingPercentage = ColorFilterHelper.CalculateColorMatchPercentage(region, mask, "f6b622", 5);
        if (cookingPercentage > 0)
        {
            return (CookingStatus.Cooking, cookingPercentage);
        }

        return (CookingStatus.Idle, 0);
    }

    private void UpdateKitchenwareStatusDisplay()
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

    #endregion

    #region 资源加载

    private void AddLayersForMaskWindow()
    {
        _maskWindow?.AddLayer("Kitchenware");
        _maskWindow?.AddLayer("Condiments");
        _maskWindow?.AddLayer("Ingredients");
        _maskWindow?.AddLayer("Orders");
        _maskWindow?.AddLayer("Match");
        _maskWindow?.AddLayer("Click");
    }

    public void LoadAssets()
    {
        string image_folder = "Assets/Cooking/Image/";
        _logger.LogInformation("开始加载图片资源");

        // 加载根目录的UI图片到_images字典
        LoadImagesFromDirectory(image_folder);

        // Dishes
        var dishDir = Path.Combine(image_folder, "Dishes");
        foreach (var file in Directory.GetFiles(dishDir, "*.png"))
        {
            var fileName = Path.GetFileName(file);
            dishImages["Dishes/" + fileName] = Cv2.ImRead(file, ImreadModes.Unchanged);
        }

        // Section
        bin = Cv2.ImRead(image_folder + "Section/bin.png");
        board = Cv2.ImRead(image_folder + "Section/board.png");

        // Kitchenware
        var kitchenwareDir = Path.Combine(image_folder, "Kitchenware");
        foreach (var file in Directory.GetFiles(kitchenwareDir, "*.png"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.EndsWith("_ring"))
            {
                kitchenwareRings[fileName.Replace("_ring", "")] = Cv2.ImRead(file);
            }
            else
            {
                kitchenwares[fileName] = Cv2.ImRead(file, ImreadModes.Unchanged);
            }
        }

        // Condiments
        var condimentDir = Path.Combine(image_folder, "Condiment");
        foreach (var file in Directory.GetFiles(condimentDir, "*.png"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            condiments[fileName] = Cv2.ImRead(file, ImreadModes.Unchanged);
        }

        // Ingredients
        var ingredientDir = Path.Combine(image_folder, "Ingredients");
        foreach (var file in Directory.GetFiles(ingredientDir, "*.png"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            ingredients[fileName] = Cv2.ImRead(file, ImreadModes.Unchanged);
        }

        // Orders
        order = Cv2.ImRead(image_folder + "Order/order.png", ImreadModes.Unchanged);
        red_order = Cv2.ImRead(image_folder + "Order/red_order.png", ImreadModes.Unchanged);

        _logger.LogInformation("图片资源加载完成");
    }

    #endregion

    #region 参数设置

    public override bool SetParameters(Dictionary<string, object> parameters)
    {
        try
        {
            if (TryGetParameter(parameters, "Times", out int times))
            {
                if (times < 0)
                {
                    _logger.LogWarning("烹饪次数必须大于等于0。已设置为默认值。");
                    return false;
                }
                _autoCookingTimes = times;
                _logger.LogDebug("烹饪次数设置为：{Times}次", _autoCookingTimes);
            }

            if (TryGetParameter(parameters, "Dish", out string dish))
            {
                if (string.IsNullOrEmpty(dish))
                {
                    _logger.LogWarning("无效的菜品选择。已设置为默认值。");
                    return false;
                }
                
                _autoCookingDish = dish;
                _currentDishConfig = _cookingConfigService.GetDishConfig(dish);
                if (_currentDishConfig == null)
                {
                    _logger.LogWarning("未找到菜品配置：{Dish}。已设置为默认值。", dish);
                    return false;
                }
                _logger.LogDebug("菜品设置为：{Dish}", _autoCookingDish);
            }

            if (TryGetParameter(parameters, "OCR", out string ocr))
            {
                if (string.IsNullOrEmpty(ocr))
                {
                    _logger.LogWarning("无效的OCR选择。已设置为默认值。");
                    return false;
                }
                
                _autoCookingSelectedOCR = ocr;
                InitializeOCR();
                _logger.LogDebug("OCR引擎设置为：{OCR}", _autoCookingSelectedOCR);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("设置参数时发生错误：{Message}", ex.Message);
            return false;
        }
    }

    private void InitializeOCR()
    {
        paddleOCRHelper = null;
        tesseractOCRHelper = null;

        if (_autoCookingSelectedOCR == "PaddleOCR")
        {
            paddleOCRHelper = new PaddleOCRHelper();
        }
        else
        {
            tesseractOCRHelper = new TesseractOCRHelper();
        }
    }

    private string OcrText(Mat mat)
    {
        if (_autoCookingSelectedOCR == "PaddleOCR" && paddleOCRHelper != null)
        {
            return paddleOCRHelper.Ocr(mat);
        }
        else if (tesseractOCRHelper != null)
        {
            using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
            return TesseractOCRHelper.TesseractTextRecognition(TesseractOCRHelper.PreprocessImage(bitmap));
        }
        return string.Empty;
    }

    #endregion
}