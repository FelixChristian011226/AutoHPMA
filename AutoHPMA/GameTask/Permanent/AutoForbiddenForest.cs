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

public enum AutoForbiddenForestState
{
    Outside,
    Teaming,
    Loading,
    Fighting,
    Summary,
}

public enum AutoForbiddenForestOption
{
    Leader,
    Member,
}

public class AutoForbiddenForest : BaseGameTask
{
    private AutoForbiddenForestState _state = AutoForbiddenForestState.Outside;

    private Mat ui_explore, ui_loading, ui_clock, ui_statistics;
    private Mat team_auto, team_start, team_confirm, team_ready;
    private Mat fight_auto;
    private Mat over_thumb;

    private bool _waited = false;

    private int _autoForbiddenForestTimes;
    private AutoForbiddenForestOption _autoForbiddenForestOption = AutoForbiddenForestOption.Leader;

    private int round = 0;

    public AutoForbiddenForest(ILogger<AutoForbiddenForest> logger, nint displayHwnd, nint gameHwnd)
        : base(logger, displayHwnd, gameHwnd)
    {
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow?.AddLayer("Match");
        _maskWindow?.AddLayer("Click");
        _maskWindow?.AddLayer("MultiClick");
    }

    private void LoadAssets()
    {
        string image_folder = "Assets/ForbiddenForest/Image/";

        ui_explore = Cv2.ImRead(image_folder + "ui_explore.png", ImreadModes.Color);
        ui_loading = Cv2.ImRead(image_folder + "ui_loading.png", ImreadModes.Color);
        ui_clock = Cv2.ImRead(image_folder + "ui_clock.png", ImreadModes.Color);
        ui_statistics = Cv2.ImRead(image_folder + "ui_statistics.png", ImreadModes.Color);
        team_auto = Cv2.ImRead(image_folder + "team_auto.png", ImreadModes.Color);
        team_start = Cv2.ImRead(image_folder + "team_start.png", ImreadModes.Color);
        team_confirm = Cv2.ImRead(image_folder + "team_confirm.png", ImreadModes.Color);
        team_ready = Cv2.ImRead(image_folder + "team_ready.png", ImreadModes.Color);
        fight_auto = Cv2.ImRead(image_folder + "fight_auto.png", ImreadModes.Unchanged);
        over_thumb = Cv2.ImRead(image_folder + "over_thumb.png", ImreadModes.Color);
    }

    public override void Stop()
    {
        base.Stop();
    }

    public override async void Start()
    {
        _state = AutoForbiddenForestState.Outside;
        _logWindow?.SetGameState("禁林");
        _logger.LogInformation("[Aquamarine]---自动禁林任务已启动---[/Aquamarine]");
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                GC.Collect();
                if (round >= _autoForbiddenForestTimes)
                {
                    ToastNotificationHelper.ShowToast("禁林任务完成", $"已完成 {round} 轮禁林任务。");
                    Stop();
                    _logger.LogInformation("[Aquamarine]---自动禁林任务已终止---[/Aquamarine]");
                    continue;
                }
                FindState();
                switch (_state)
                {
                    case AutoForbiddenForestState.Outside:
                        if (!_waited)
                        {
                            await Task.Delay(5000, _cts.Token);
                            _waited = true;
                            break;
                        }
                        _logWindow?.SetGameState("禁林-未知状态");
                        _waited = false;
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoForbiddenForestState.Teaming:
                        var autoResult = Find(team_auto);
                        if (autoResult.Success)
                        {
                            ClickMatchCenter(autoResult);
                            _logger.LogDebug("点击自动战斗按钮。");
                        }
                        await Task.Delay(1000, _cts.Token);
                        
                        switch (_autoForbiddenForestOption)
                        {
                            case AutoForbiddenForestOption.Leader:
                                var startResult = Find(team_start);
                                if (startResult.Success)
                                {
                                    ClickMatchCenter(startResult);
                                    _logger.LogDebug("点击开始。");
                                }
                                await Task.Delay(1500, _cts.Token);
                                
                                var confirmResult = Find(team_confirm);
                                if (confirmResult.Success)
                                {
                                    ClickMatchCenter(confirmResult);
                                    _logger.LogDebug("点击是。");
                                }
                                break;
                                
                            case AutoForbiddenForestOption.Member:
                                var readyResult = Find(team_ready);
                                if (readyResult.Success)
                                {
                                    ClickMatchCenter(readyResult);
                                    _logger.LogDebug("点击准备。");
                                }
                                await Task.Delay(1000, _cts.Token);
                                break;
                        }
                        break;

                    case AutoForbiddenForestState.Loading:
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoForbiddenForestState.Fighting:
                        var fightResult = Find(fight_auto, new MatchOptions 
                        { 
                            UseAlphaMask = true, 
                            Threshold = 0.8 
                        });
                        if (fightResult.Success)
                        {
                            ClickMatchCenter(fightResult);
                        }
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoForbiddenForestState.Summary:
                        _logger.LogDebug("检测到点赞页面");
                        await Task.Delay(3000, _cts.Token);
                        
                        var thumbResult = Find(over_thumb, new MatchOptions { FindMultiple = true });
                        if (thumbResult.Success)
                        {
                            ShowMatchRects(thumbResult, "MultiClick");
                            await ClickMultiMatchCentersAsync(thumbResult);
                            ClearMatchRects("MultiClick");
                        }
                        
                        await Task.Delay(1500, _cts.Token);
                        SendSpace(_gameHwnd);
                        _logger.LogInformation("第[Yellow]{Round}[/Yellow]/[Yellow]{Total}[/Yellow]次禁林任务完成。", ++round, _autoForbiddenForestTimes);
                        await Task.Delay(2000, _cts.Token);
                        break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[Aquamarine]---自动禁林任务已终止---[/Aquamarine]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生异常");
        }
        finally
        {
            _maskWindow?.ClearAllLayers();
            _logWindow?.SetGameState("空闲");
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    public void FindState()
    {
        if (Find(ui_explore).Success)
        {
            _state = AutoForbiddenForestState.Teaming;
            _logWindow?.SetGameState("禁林-组队中");
            _waited = false;
            return;
        }

        if (Find(ui_loading).Success)
        {
            _state = AutoForbiddenForestState.Loading;
            _logWindow?.SetGameState("禁林-加载中");
            _waited = false;
            return;
        }

        if (Find(ui_clock).Success)
        {
            _state = AutoForbiddenForestState.Fighting;
            _logWindow?.SetGameState("禁林-战斗中");
            _waited = false;
            return;
        }

        if (Find(ui_statistics).Success)
        {
            _state = AutoForbiddenForestState.Summary;
            _logWindow?.SetGameState("禁林-结算中");
            _waited = false;
            return;
        }

        _state = AutoForbiddenForestState.Outside;
        _logWindow?.SetGameState("禁林-未知状态");
        return;
    }

    public override bool SetParameters(Dictionary<string, object> parameters)
    {
        try
        {
            if (parameters.TryGetValue("Times", out var timesObj))
            {
                var times = Convert.ToInt32(timesObj);
                if (times < 0)
                {
                    _logger.LogWarning("禁林次数必须大于等于0。已设置为默认值。");
                    return false;
                }
                _autoForbiddenForestTimes = times;
                _logger.LogDebug("禁林次数设置为：{Times}次", _autoForbiddenForestTimes);
            }

            if (parameters.TryGetValue("TeamPosition", out var positionObj))
            {
                _autoForbiddenForestOption = positionObj?.ToString() switch
                {
                    "Leader" => AutoForbiddenForestOption.Leader,
                    "Member" => AutoForbiddenForestOption.Member,
                    _ => throw new ArgumentException("无效的队伍位置")
                };
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("设置参数时发生错误：{Message}", ex.Message);
            return false;
        }
    }
}
