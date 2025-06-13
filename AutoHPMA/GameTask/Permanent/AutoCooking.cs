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
using System.Threading.Tasks;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.GameTask.Permanent;

public enum AutoCookingState
{
    Outside,
    Map,
    CookingScene,
    Cooking,
    Summary,
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
    private AutoCookingState _state = AutoCookingState.Outside;

    private Mat? captureMat;
    private Mat oven, pot;
    private Mat rice, fish;

    private List<Rect> detect_rects = new List<Rect>();

    private bool initialized = false;
    private int _autoCookingTimes;
    private int round = 0;

    private Rect oven_rect, pot_rect;
    private Rect rice_rect, fish_rect;

    private CancellationTokenSource _cts;
    public event EventHandler? TaskCompleted;

    public AutoCooking(ILogger<AutoCooking> logger, nint _displayHwnd, nint _gameHwnd)
    {
        _logger = logger;
        this._displayHwnd = _displayHwnd;
        this._gameHwnd = _gameHwnd;
        _cts = new CancellationTokenSource();
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
        _state = AutoCookingState.Outside;
        _logWindow?.SetGameState("自动烹饪");
        _logger.LogInformation("[Aquamarine]---自动烹饪任务已启动---[/Aquamarine]");
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                System.GC.Collect();
                await Task.Delay(500, _cts.Token);
                if(!initialized)
                {
                    Initialize();
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

    private bool Initialize()
    {
        if (!LocateKitchenWare())
        {
            //_logger.LogDebug("未定位到厨房用具，即将开始重新定位。");
            return false;
        }
        else
        {
            _maskWindow?.SetLayerRects("Kitchenware", new List<Rect>{ ScaleRect(oven_rect, scale), ScaleRect(pot_rect, scale) });
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

        _logger.LogInformation("自动烹饪初始化完成。");
        initialized = true;
        return true;
    }

    private bool LocateKitchenWare()
    {
        double threshold = 0.9;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        Mat maskMat;
        using (var ovenBGR = oven.Clone())
        {
            Cv2.CvtColor(ovenBGR, ovenBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(oven);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, ovenBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
            if(matchpoint == default)
            {
                //_logger.LogDebug("未定位到烤箱，即将重试。");
                return false;
            }
            oven_rect = new Rect(matchpoint.X, matchpoint.Y, oven.Width, oven.Height);
        }

        using (var potBGR = pot.Clone())
        {
            Cv2.CvtColor(potBGR, potBGR, ColorConversionCodes.BGRA2BGR);
            maskMat = MatchTemplateHelper.GenerateMask(pot);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, potBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
            if(matchpoint == default) 
            {
                //_logger.LogDebug("未定位到锅，即将重试。");
                return false;
            }
            pot_rect = new Rect(matchpoint.X, matchpoint.Y, pot.Width, pot.Height);
        }
        return true;
    }

    private bool LocateIngredients()
    {
        double threshold = 0.9;
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
        }
        return true;
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow.AddLayer("Match");
        _maskWindow.AddLayer("Kitchenware");
        _maskWindow.AddLayer("Condiments");
        _maskWindow.AddLayer("Ingredients");
    }

    public void LoadAssets()
    {
        string image_folder = "Assets/Cooking/Image/";
        //Kitchenware
        oven = Cv2.ImRead(image_folder + "Kitchenware/oven.png", ImreadModes.Unchanged);
        pot = Cv2.ImRead(image_folder + "Kitchenware/pot.png", ImreadModes.Unchanged);
        //Condiments

        //Ingredients
        rice = Cv2.ImRead(image_folder + "Ingredients/rice.png", ImreadModes.Unchanged);
        fish = Cv2.ImRead(image_folder + "Ingredients/fish.png", ImreadModes.Unchanged);

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

    #region SetParameter

    public bool SetParameters(Dictionary<string, object> parameters)
    {
        try
        {
            if (parameters.ContainsKey("Times"))
            {
                var times = Convert.ToInt32(parameters["Times"]);
                if (times <= 0)
                {
                    _logger.LogWarning("烹饪次数必须大于0。已设置为默认值。");
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