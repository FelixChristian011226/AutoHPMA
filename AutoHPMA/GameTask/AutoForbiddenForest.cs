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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static AutoHPMA.Helpers.WindowInteractionHelper;

namespace AutoHPMA.GameTask;

public enum AutoForbiddenForestState
{
    Preparing,
    Fighting,
}

public enum AutoForbiddenForestOption
{
    Leader,
    Member,
}

public class AutoForbiddenForest
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

    private AutoForbiddenForestState _state = AutoForbiddenForestState.Preparing;

    private Mat auto, auto_fight, start, confirm, ready, loading, thumb;
    private Mat? captureMat;

    private IntPtr _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;

    private int _autoForbiddenForestTimes;
    private AutoForbiddenForestOption _autoForbiddenForestOption = AutoForbiddenForestOption.Leader;

    private int index = 0;

    private CancellationTokenSource _cts;

    public AutoForbiddenForest(IntPtr _displayHwnd, IntPtr _gameHwnd)
    {
        this._displayHwnd = _displayHwnd;
        this._gameHwnd = _gameHwnd;
        _cts = new CancellationTokenSource();
        LoadAssets();
        CalOffset();
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public async void Start()
    {
        _state = AutoForbiddenForestState.Preparing;

        while (!_cts.Token.IsCancellationRequested)
        {
            GC.Collect();
            if (index > _autoForbiddenForestTimes)
                break;
            switch (_state)
            {
                case AutoForbiddenForestState.Preparing:
                    if(FindMatch(ref loading))
                    {
                        _logWindow?.AddLogMessage("DBG", "检测到加载页面");
                        _state = AutoForbiddenForestState.Fighting;
                    }

                    if (FindAndClick(ref auto))
                    {
                        _logWindow?.AddLogMessage("DBG", "点击自动战斗按钮。");
                    }
                    await Task.Delay(1000);
                    switch (_autoForbiddenForestOption)
                    {
                        case AutoForbiddenForestOption.Leader:
                            if (FindAndClick(ref start))
                            {
                                _logWindow?.AddLogMessage("DBG", "点击开始。");
                            }
                            await Task.Delay(1500);
                            if (FindAndClick(ref confirm))
                            {
                                _logWindow?.AddLogMessage("DBG", "点击是。");
                            }
                            break;
                        case AutoForbiddenForestOption.Member:
                            if (FindAndClick(ref ready))
                            {
                                _logWindow?.AddLogMessage("DBG", "点击准备。");
                            }
                            await Task.Delay(1000);
                            break;

                    }
                    break;
                case AutoForbiddenForestState.Fighting:
                    await Task.Delay(1000);
                    FindAndClick(ref auto_fight);
                    if(FindMatch(ref thumb))
                    {
                        _logWindow?.AddLogMessage("DBG", "检测到点赞页面");
                        await Task.Delay(4000);
                        await FindAndClickMultiAsync(thumb);
                        await Task.Delay(2000);
                        SendSpace(_gameHwnd);
                        _state = AutoForbiddenForestState.Preparing;
                        _logWindow?.AddLogMessage("INF", "第[Yellow]" + ++index + "[/Yellow]次禁林任务完成。");
                    }

                    break;

            }
        }
    }

    private bool FindMatch(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
        return true;
    }

    private bool FindAndClick(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
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
        int index = 0;
        foreach (var rect in matchrects)
        {
            SendMouseClick(_gameHwnd,
                (uint)(rect.X * scale - offsetX + mat.Width / 2.0 * scale),
                (uint)(rect.Y * scale - offsetY + mat.Height / 2.0 * scale));
            _logWindow?.AddLogMessage("DBG", "第" + ++index + "次点赞：(" + rect.X + "," + rect.Y + ")");
            await Task.Delay(1000);
        }
        return true;
    }


    private bool FindAndClickWithMask(ref Mat mat, ref Mat mask, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        Cv2.BitwiseAnd(captureMat, mask, captureMat);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
        SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
        return true;
    }

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

    private void LoadAssets()
    {
        string image_folder = "Assets/ForbiddenForest/Image/";
        auto = Cv2.ImRead(image_folder + "auto.png", ImreadModes.Unchanged);
        Cv2.CvtColor(auto, auto, ColorConversionCodes.BGR2GRAY);
        ready = Cv2.ImRead(image_folder + "ready.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ready, ready, ColorConversionCodes.BGR2GRAY);
        loading = Cv2.ImRead(image_folder + "loading.png", ImreadModes.Unchanged);
        Cv2.CvtColor(loading, loading, ColorConversionCodes.BGR2GRAY);
        thumb = Cv2.ImRead(image_folder + "thumb.png", ImreadModes.Unchanged);
        Cv2.CvtColor(thumb, thumb, ColorConversionCodes.BGR2GRAY);
        auto_fight = Cv2.ImRead(image_folder + "auto_fight.png", ImreadModes.Unchanged);
        Cv2.CvtColor(auto_fight, auto_fight, ColorConversionCodes.BGR2GRAY);
        start = Cv2.ImRead(image_folder + "start.png", ImreadModes.Unchanged);
        Cv2.CvtColor(start, start, ColorConversionCodes.BGR2GRAY);
        confirm = Cv2.ImRead(image_folder + "confirm.png", ImreadModes.Unchanged);
        Cv2.CvtColor(confirm, confirm, ColorConversionCodes.BGR2GRAY);

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
}
