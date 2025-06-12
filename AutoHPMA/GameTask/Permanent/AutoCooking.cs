using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
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

    private List<Rect> detect_rects = new List<Rect>();

    private int _autoCookingTimes;
    private int round = 0;

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
                _logger.LogInformation("自动烹饪进行中");
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

    private bool LocateKitchenWare()
    {
        return true;
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow.AddLayer("Match");
        _maskWindow.AddLayer("Kitchenware");
    }

    public void LoadAssets()
    {
        string image_folder = "Assets/Cooking/Image/";

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