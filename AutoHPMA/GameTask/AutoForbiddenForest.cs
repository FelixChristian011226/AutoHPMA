using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.FileSystemGlobbing;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.GameTask;

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

public class AutoForbiddenForest
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;

    private static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

    private IntPtr _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;
    private AutoForbiddenForestState _state = AutoForbiddenForestState.Outside;

    private Mat? captureMat;
    private Mat ui_explore, ui_loading, ui_clock, ui_statistics;
    private Mat team_auto, team_start, team_confirm, team_ready;
    private Mat fight_auto;
    private Mat over_thumb;

    private List<Rect> detect_rects = new List<Rect>();

    private bool _waited = false;

    private int _autoForbiddenForestTimes;
    private AutoForbiddenForestOption _autoForbiddenForestOption = AutoForbiddenForestOption.Leader;

    private int round = 0;

    private CancellationTokenSource _cts;

    public AutoForbiddenForest(IntPtr _displayHwnd, IntPtr _gameHwnd)
    {
        this._displayHwnd = _displayHwnd;
        this._gameHwnd = _gameHwnd;
        _cts = new CancellationTokenSource();
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow.AddLayer("Match");
        _maskWindow.AddLayer("Click");
        _maskWindow.AddLayer("MultiClick");
    }

    private void LoadAssets()
    {
        string image_folder = "Assets/ForbiddenForest/Image/";

        ui_explore = Cv2.ImRead(image_folder + "ui_explore.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_explore, ui_explore, ColorConversionCodes.BGR2GRAY);
        ui_loading = Cv2.ImRead(image_folder + "ui_loading.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_loading, ui_loading, ColorConversionCodes.BGR2GRAY);
        ui_clock = Cv2.ImRead(image_folder + "ui_clock.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_clock, ui_clock, ColorConversionCodes.BGR2GRAY);
        ui_statistics = Cv2.ImRead(image_folder + "ui_statistics.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_statistics, ui_statistics, ColorConversionCodes.BGR2GRAY);
        team_auto = Cv2.ImRead(image_folder + "team_auto.png", ImreadModes.Unchanged);
        Cv2.CvtColor(team_auto, team_auto, ColorConversionCodes.BGR2GRAY);
        team_start = Cv2.ImRead(image_folder + "team_start.png", ImreadModes.Unchanged);
        Cv2.CvtColor(team_start, team_start, ColorConversionCodes.BGR2GRAY);
        team_confirm = Cv2.ImRead(image_folder + "team_confirm.png", ImreadModes.Unchanged);
        Cv2.CvtColor(team_confirm, team_confirm, ColorConversionCodes.BGR2GRAY);
        team_ready = Cv2.ImRead(image_folder + "team_ready.png", ImreadModes.Unchanged);
        Cv2.CvtColor(team_ready, team_ready, ColorConversionCodes.BGR2GRAY);
        fight_auto = Cv2.ImRead(image_folder + "fight_auto.png", ImreadModes.Unchanged);
        Cv2.CvtColor(fight_auto, fight_auto, ColorConversionCodes.BGR2GRAY);
        over_thumb = Cv2.ImRead(image_folder + "over_thumb.png", ImreadModes.Unchanged);
        Cv2.CvtColor(over_thumb, over_thumb, ColorConversionCodes.BGR2GRAY);


    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public async void Start()
    {
        _state = AutoForbiddenForestState.Outside;
        _logWindow?.SetGameState("禁林");
        _logWindow?.AddLogMessage("INF", "[Aquamarine]---自动禁林任务已启动---[/Aquamarine]");
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                GC.Collect();
                if (round > _autoForbiddenForestTimes)
                {
                    _logWindow?.AddLogMessage("INF", "[Aquamarine]---自动禁林任务已终止---[/Aquamarine]");
                    break;
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
                        if (FindAndClick(ref team_auto))
                        {
                            _logWindow?.AddLogMessage("DBG", "点击自动战斗按钮。");
                        }
                        await Task.Delay(1000, _cts.Token);
                        switch (_autoForbiddenForestOption)
                        {
                            case AutoForbiddenForestOption.Leader:
                                if (FindAndClick(ref team_start))
                                {
                                    _logWindow?.AddLogMessage("DBG", "点击开始。");
                                }
                                await Task.Delay(1500, _cts.Token);
                                if (FindAndClick(ref team_confirm))
                                {
                                    _logWindow?.AddLogMessage("DBG", "点击是。");
                                }
                                break;
                            case AutoForbiddenForestOption.Member:
                                if (FindAndClick(ref team_ready))
                                {
                                    _logWindow?.AddLogMessage("DBG", "点击准备。");
                                }
                                await Task.Delay(1000, _cts.Token);
                                break;

                        }
                        break;

                    case AutoForbiddenForestState.Loading:
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoForbiddenForestState.Fighting:
                        FindAndClick(ref fight_auto);
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoForbiddenForestState.Summary:
                        _logWindow?.AddLogMessage("DBG", "检测到点赞页面");
                        await Task.Delay(3000, _cts.Token);
                        await FindAndClickMultiAsync(over_thumb);
                        await Task.Delay(1500, _cts.Token);
                        SendSpace(_gameHwnd);
                        _logWindow?.AddLogMessage("INF", "第[Yellow]" + ++round + "/" + _autoForbiddenForestTimes + "[/Yellow]次禁林任务完成。");
                        await Task.Delay(2000, _cts.Token);
                        break;

                }
            }
        }
        catch (TaskCanceledException)
        {
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---自动禁林任务已终止---[/Aquamarine]");
        }
        catch (Exception ex)
        {
            _logWindow?.AddLogMessage("ERR", "发生异常：" + ex.Message);
        }
        finally
        {
            _maskWindow.ClearAllLayers();
            _logWindow?.SetGameState("空闲");
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    public void FindState()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);

        if( FindMatch(ref ui_explore))
        {
            _state = AutoForbiddenForestState.Teaming;
            _logWindow?.SetGameState("禁林-组队中");
            _waited = false;
            return;
        }

        if( FindMatch(ref ui_loading))
        {
            _state = AutoForbiddenForestState.Loading;
            _logWindow?.SetGameState("禁林-加载中");
            _waited = false;
            return;
        }

        if( FindMatch(ref ui_clock))
        {
            _state = AutoForbiddenForestState.Fighting;
            _logWindow?.SetGameState("禁林-战斗中");
            _waited = false;
            return;
        }

        if( FindMatch(ref ui_statistics))
        {
            _state = AutoForbiddenForestState.Summary;
            _logWindow?.SetGameState("禁林-结算中");
            _waited = false;
            return;
        }

        _state = AutoForbiddenForestState.Outside;
        return;

    }
    private bool FindMatch(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
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
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
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

    private bool FindAndClickWithMask(ref Mat mat, ref Mat mask, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(mask.Width, mask.Height));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        Cv2.BitwiseAnd(captureMat, mask, captureMat);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
        detect_rects.Clear();
        detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        _maskWindow?.SetLayerRects("Match", detect_rects);
        SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
        return true;
    }

    private async Task<bool> FindAndClickMultiAsync(Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);

        var matchrects = MatchTemplateHelper.MatchOnePicForOnePic(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        if(matchrects.Count == 0)
        {
            return false;
        }
        detect_rects.Clear();
        foreach (var rect in matchrects)
        {
            detect_rects.Add(new Rect((int)(rect.X * scale), (int)(rect.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        }
        _maskWindow?.SetLayerRects("MultiClick", detect_rects);
        foreach (var rect in matchrects)
        {
            SendMouseClick(_gameHwnd,
                (uint)(rect.X * scale - offsetX + mat.Width / 2.0 * scale),
                (uint)(rect.Y * scale - offsetY + mat.Height / 2.0 * scale));
            //_logWindow?.AddLogMessage("DBG", "第" + ++index + "次点赞：(" + rect.X + "," + rect.Y + ")");
            await Task.Delay(1000);
        }
        _maskWindow?.ClearLayer("MultiClick");
        return true;
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

    public bool SetAutoForbiddenForestTimes(int AutoForbiddenForestTimes)
    {
        _autoForbiddenForestTimes = AutoForbiddenForestTimes;
        return true;
    }

    public bool SetTeamPosition(string SelectedTeamPosition)
    {
        if (SelectedTeamPosition == "Leader")
        {
            _autoForbiddenForestOption = AutoForbiddenForestOption.Leader;
        }
        else if (SelectedTeamPosition == "Member")
        {
            _autoForbiddenForestOption = AutoForbiddenForestOption.Member;
        }
        return true;
    }

    #endregion


}
