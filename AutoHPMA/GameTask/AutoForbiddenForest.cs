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
using System.Threading.Tasks;

using static AutoHPMA.Helpers.WindowInteractionHelper;

namespace AutoHPMA.GameTask;

public enum AutoForbiddenForestState
{
    Preparing,
    Fighting,
}

public class AutoForbiddenForest
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

    private AutoForbiddenForestState _state = AutoForbiddenForestState.Preparing;

    private Mat auto, ready, loading, thumb;
    private Mat? captureMat;

    private IntPtr _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;

    private int _autoForbiddenForestTimes;

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
                    if (FindAndClick(ref ready))
                    {
                        _logWindow?.AddLogMessage("DBG", "点击准备按钮。");
                    }
                    await Task.Delay(1000);
                    break;
                case AutoForbiddenForestState.Fighting:
                    await Task.Delay(2000);
                    if (FindAndClickMulti(ref thumb))
                    {
                        _logWindow?.AddLogMessage("DBG", "点赞。");
                        await Task.Delay(1000);
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

    private bool FindAndClickMulti(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        var matchpoints = MatchTemplateHelper.MatchTemplateMulti(captureMat, mat, null, threshold);
        if (matchpoints.Count == 0)
        {
            return false;
        }
        for(int i = 0; i < matchpoints.Count; i++)
        {
            SendMouseClick(_gameHwnd, (uint)(matchpoints[i].X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoints[i].Y * scale - offsetY + mat.Height / 2.0 * scale));
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
