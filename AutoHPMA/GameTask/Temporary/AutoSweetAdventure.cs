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

public class AutoSweetAdventure : BaseGameTask
{
    private AutoSweetAdventureState _state = AutoSweetAdventureState.Unknown;

    private Mat? captureMat;
    private Mat ui_teaming, ui_gaming, ui_endding;
    private Mat teaming_start;
    private Mat gaming_round1, gaming_round2, gaming_round3, gaming_round4, gaming_round5;
    private Mat gaming_forward, gaming_return, gaming_candy, gaming_monster;

    private bool _waited = false;
    private bool _refreshed = true;
    private int round=0, prev_round=0, step=1;
    private int _maxStep = 12;

    public event EventHandler? TaskCompleted;

    public AutoSweetAdventure(ILogger<AutoSweetAdventure> logger, nint displayHwnd, nint gameHwnd)
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
        _maskWindow?.AddLayer("Round");
    }

    private void LoadAssets()
    {
        string image_folder = "Assets/SweetAdventure/Image/";

        ui_teaming = Cv2.ImRead(image_folder + "ui_teaming.png", ImreadModes.Color);
        ui_gaming = Cv2.ImRead(image_folder + "ui_gaming.png", ImreadModes.Color);
        ui_endding = Cv2.ImRead(image_folder + "ui_endding.png", ImreadModes.Color);
        teaming_start = Cv2.ImRead(image_folder + "teaming_start.png", ImreadModes.Color);
        gaming_round1 = Cv2.ImRead(image_folder + "gaming_round1.png", ImreadModes.Color);
        gaming_round2 = Cv2.ImRead(image_folder + "gaming_round2.png", ImreadModes.Color);
        gaming_round3 = Cv2.ImRead(image_folder + "gaming_round3.png", ImreadModes.Color);
        gaming_round4 = Cv2.ImRead(image_folder + "gaming_round4.png", ImreadModes.Color);
        gaming_round5 = Cv2.ImRead(image_folder + "gaming_round5.png", ImreadModes.Color);
        gaming_forward = Cv2.ImRead(image_folder + "gaming_forward.png", ImreadModes.Color);
        gaming_return = Cv2.ImRead(image_folder + "gaming_return.png", ImreadModes.Color);
        gaming_candy = Cv2.ImRead(image_folder + "gaming_candy.png", ImreadModes.Color);
        gaming_monster = Cv2.ImRead(image_folder + "gaming_monster.png", ImreadModes.Color);
    }

    public override void Stop()
    {
        base.Stop();
    }

    public override async void Start()
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
                        _maskWindow?.ShowLayer("Round");
                        round = FindRound();
                        if(round > prev_round)
                        {
                            _logger.LogInformation("当前回合数：[Yellow]{Round}[/Yellow]。", round);
                            step = 1;
                            prev_round = round;
                        }
                        if (step < _maxStep)
                        {
                            if (FindAndClick(ref gaming_forward, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：前进。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                            if (FindAndClick(ref gaming_candy, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：预测糖果。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                        }
                        else
                        {
                            if(FindAndClick(ref gaming_return, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：返回。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                            if(FindAndClick(ref gaming_monster, 0.96))
                            {
                                step++;
                                _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：预测怪物。", step);
                                await Task.Delay(1000, _cts.Token);
                                continue;
                            }
                        }
                        await Task.Delay(1000, _cts.Token);
                        _maskWindow?.HideLayer("Round");
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
            _maskWindow?.ClearAllLayers();
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
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
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


    public override bool SetParameters(Dictionary<string, object> parameters)
    {
        // 甜蜜冒险目前没有需要设置的参数
        return true;
    }
}
