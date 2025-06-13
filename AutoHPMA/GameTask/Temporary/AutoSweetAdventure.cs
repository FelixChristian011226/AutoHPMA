using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.GameTask.Temporary;
public enum AutoSweetAdventureState
{
    Unknown,
    Teaming,
    Gaming,
    Endding,
}

public class AutoSweetAdventure : IGameTask
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

    private readonly ILogger<AutoSweetAdventure> _logger;
    private IntPtr _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;
    private AutoSweetAdventureState _state = AutoSweetAdventureState.Unknown;

    private Mat? captureMat;
    private Mat ui_teaming, ui_gaming, ui_endding;
    private Mat teaming_start;
    private Mat gaming_round1, gaming_round2, gaming_round3, gaming_round4, gaming_round5;
    private Mat gaming_forward, gaming_return, gaming_candy, gaming_monster;


    private List<Rect> detect_rects = new List<Rect>();

    private bool _waited = false;
    private bool _refreshed = true;
    private int round=0, prev_round=0, step=1;
    private int _maxStep = 12;

    private CancellationTokenSource _cts;

    public event EventHandler? TaskCompleted;

    public AutoSweetAdventure(ILogger<AutoSweetAdventure> logger, IntPtr _displayHwnd, IntPtr _gameHwnd)
    {
        this._logger = logger;
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
        _maskWindow.AddLayer("Round");
    }

    private void LoadAssets()
    {
        string image_folder = "Assets/SweetAdventure/Image/";

        ui_teaming = Cv2.ImRead(image_folder + "ui_teaming.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_teaming, ui_teaming, ColorConversionCodes.BGR2GRAY);
        ui_gaming = Cv2.ImRead(image_folder + "ui_gaming.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_gaming, ui_gaming, ColorConversionCodes.BGR2GRAY);
        ui_endding = Cv2.ImRead(image_folder + "ui_endding.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_endding, ui_endding, ColorConversionCodes.BGR2GRAY);
        teaming_start = Cv2.ImRead(image_folder + "teaming_start.png", ImreadModes.Unchanged);
        Cv2.CvtColor(teaming_start, teaming_start, ColorConversionCodes.BGR2GRAY);
        gaming_round1 = Cv2.ImRead(image_folder + "gaming_round1.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gaming_round1, gaming_round1, ColorConversionCodes.BGR2GRAY);
        gaming_round2 = Cv2.ImRead(image_folder + "gaming_round2.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gaming_round2, gaming_round2, ColorConversionCodes.BGR2GRAY);
        gaming_round3 = Cv2.ImRead(image_folder + "gaming_round3.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gaming_round3, gaming_round3, ColorConversionCodes.BGR2GRAY);
        gaming_round4 = Cv2.ImRead(image_folder + "gaming_round4.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gaming_round4, gaming_round4, ColorConversionCodes.BGR2GRAY);
        gaming_round5 = Cv2.ImRead(image_folder + "gaming_round5.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gaming_round5, gaming_round5, ColorConversionCodes.BGR2GRAY);
        gaming_forward = Cv2.ImRead(image_folder + "gaming_forward.png", ImreadModes.Unchanged);
        //Cv2.CvtColor(gaming_forward, gaming_forward, ColorConversionCodes.BGR2GRAY);
        gaming_return = Cv2.ImRead(image_folder + "gaming_return.png", ImreadModes.Unchanged);
        //Cv2.CvtColor(gaming_return, gaming_return, ColorConversionCodes.BGR2GRAY);
        gaming_candy = Cv2.ImRead(image_folder + "gaming_candy.png", ImreadModes.Unchanged);
        //Cv2.CvtColor(gaming_candy, gaming_candy, ColorConversionCodes.BGR2GRAY);
        gaming_monster = Cv2.ImRead(image_folder + "gaming_monster.png", ImreadModes.Unchanged);
        //Cv2.CvtColor(gaming_monster, gaming_monster, ColorConversionCodes.BGR2GRAY);

    }

    public void Stop()
    {
        _cts.Cancel();
        TaskCompleted?.Invoke(this, EventArgs.Empty);
    }

    public async void Start()
    {
        _state = AutoSweetAdventureState.Unknown;
        _logWindow?.SetGameState("甜蜜冒险");
        _logger.LogInformation("[Aquamarine]---甜蜜冒险任务已启动---[/Aquamarine]");
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                GC.Collect();
                FindState();
                switch (_state)
                {
                    case AutoSweetAdventureState.Unknown:
                        if (!_waited)
                        {
                            await Task.Delay(5000, _cts.Token);
                            _waited = true;
                            break;
                        }
                        _logWindow?.SetGameState("甜蜜冒险-未知状态");
                        _waited = false;
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoSweetAdventureState.Teaming:
                        if(FindAndClick(ref teaming_start))
                        {
                            await Task.Delay(3000, _cts.Token);
                        }
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoSweetAdventureState.Gaming:
                        _maskWindow.ShowLayer("Round");
                        round = FindRound();
                        if(round > prev_round)
                        {
                            _logger.LogInformation("当前回合数：[Yellow]{Round}[/Yellow]。", round);
                            step = 1;
                            prev_round = round;
                        }
                        if (step < _maxStep)
                        {
                            if (FindAndClickColored(ref gaming_forward, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：前进。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                            if (FindAndClickColored(ref gaming_candy, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：预测糖果。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                        }
                        else
                        {
                            if(FindAndClickColored(ref gaming_return, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：返回。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                            if(FindAndClickColored(ref gaming_monster, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：预测怪物。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                        }
                        await Task.Delay(1000, _cts.Token);
                        _maskWindow.HideLayer("Round");
                        break;

                    case AutoSweetAdventureState.Endding:
                        _maskWindow?.ClearLayer("Round");
                        _maskWindow?.HideLayer("Round");
                        round = 0;
                        prev_round = 0;
                        step = 1;
                        _logger.LogInformation("游戏结束，正在结算中...");
                        SendSpace(_gameHwnd);
                        await Task.Delay(2000, _cts.Token);
                        break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[Aquamarine]---甜蜜冒险任务已终止---[/Aquamarine]");
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

    private void FindState()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);

        if (FindMatch(ref ui_teaming))
        {
            _state = AutoSweetAdventureState.Teaming;
            _logWindow?.SetGameState("甜蜜冒险-组队中");
            _waited = false;
            return;
        }

        if (FindMatch(ref ui_gaming))
        {
            _state = AutoSweetAdventureState.Gaming;
            _logWindow?.SetGameState("甜蜜冒险-游戏中");
            _waited = false;
            return;
        }

        if (FindMatch(ref ui_endding))
        {
            _state = AutoSweetAdventureState.Endding;
            _logWindow?.SetGameState("甜蜜冒险-结算中");
            _waited = false;
            return;
        }

        _state = AutoSweetAdventureState.Unknown;
        return;

    }


    private int FindRound()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        double threshold = 0.9;

        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, gaming_round1, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint != default)
        {
            _maskWindow?.SetLayerRects("Round", new List<Rect> { new Rect ((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(gaming_round1.Width * scale), (int)(gaming_round1.Height * scale)) });
            return 1;
        }
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, gaming_round2, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint != default)
        {
            _maskWindow?.SetLayerRects("Round", new List<Rect> { new Rect ((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(gaming_round2.Width * scale), (int)(gaming_round2.Height * scale)) });
            return 2;
        }
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, gaming_round3, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint != default)
        {
            _maskWindow?.SetLayerRects("Round", new List<Rect> { new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(gaming_round3.Width * scale), (int)(gaming_round3.Height * scale)) });
            return 3;
        }
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, gaming_round4, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint != default)
        {
            _maskWindow?.SetLayerRects("Round", new List<Rect> { new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(gaming_round4.Width * scale), (int)(gaming_round4.Height * scale)) });
            return 4;
        }
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, gaming_round5, TemplateMatchModes.CCoeffNormed, null, threshold);
        if (matchpoint != default)
        {
            _maskWindow?.SetLayerRects("Round", new List<Rect> { new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(gaming_round5.Width * scale), (int)(gaming_round5.Height * scale)) });
            return 5;
        }
        //_logWindow?.AddLogMessage("WRN", "未定位到回合数！");
        return -1;

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

    private bool FindAndClickColored(ref Mat mat, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
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

    public bool SetParameters(Dictionary<string, object> parameters)
    {
        // 甜蜜冒险目前没有需要设置的参数
        return true;
    }
}
