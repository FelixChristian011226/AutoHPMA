using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    private Mat ui_teaming, ui_gaming, ui_endding;
    private Mat teaming_start;
    private Mat gaming_round1, gaming_round2, gaming_round3, gaming_round4, gaming_round5;
    private Mat gaming_forward, gaming_return, gaming_candy, gaming_monster;

    private int round = 0, prev_round = 0, step = 1;
    private int _maxStep = 12;

    // 状态检测规则
    private StateRule<AutoSweetAdventureState>[] _stateRules = null!;

    public AutoSweetAdventure(ILogger<AutoSweetAdventure> logger, nint displayHwnd, nint gameHwnd)
        : base(logger, displayHwnd, gameHwnd)
    {
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
        InitStateRules();
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow?.AddLayer("Match");
        _maskWindow?.AddLayer("Click");
        _maskWindow?.AddLayer("Round");
    }

    private void InitStateRules()
    {
        _stateRules = new StateRule<AutoSweetAdventureState>[]
        {
            new(new[] { ui_teaming }, AutoSweetAdventureState.Teaming, "甜蜜冒险-组队中"),
            new(new[] { ui_gaming }, AutoSweetAdventureState.Gaming, "甜蜜冒险-游戏中"),
            new(new[] { ui_endding }, AutoSweetAdventureState.Endding, "甜蜜冒险-结算中"),
        };
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

    public override async void Start()
    {
        _state = AutoSweetAdventureState.Unknown;
        await RunTaskAsync("甜蜜冒险");
    }

    protected override async Task ExecuteLoopAsync()
    {
        _state = FindStateByRules(_stateRules, AutoSweetAdventureState.Unknown, "甘蜜冒险-未知状态");
        
        // 进入有效状态时重置等待标志
        if (_state != AutoSweetAdventureState.Unknown)
            _waited = false;
        
        switch (_state)
        {
            case AutoSweetAdventureState.Unknown:
                if (!_waited)
                {
                    await Task.Delay(5000, _cts.Token);
                    _waited = true;
                    return;
                }
                _waited = false;
                await Task.Delay(1000, _cts.Token);
                break;

            case AutoSweetAdventureState.Teaming:
                var startResult = Find(teaming_start);
                if (startResult.Success)
                {
                    ClickMatchCenter(startResult);
                    await Task.Delay(3000, _cts.Token);
                }
                await Task.Delay(1000, _cts.Token);
                break;

            case AutoSweetAdventureState.Gaming:
                _maskWindow?.ShowLayer("Round");
                round = FindRound();
                if (round > prev_round)
                {
                    _logger.LogInformation("当前回合数：[Yellow]{Round}[/Yellow]。", round);
                    step = 1;
                    prev_round = round;
                }
                if (step < _maxStep)
                {
                    var forwardResult = Find(gaming_forward, new MatchOptions { Threshold = 0.96 });
                    if (forwardResult.Success)
                    {
                        ClickMatchCenter(forwardResult);
                        step++;
                        _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：前进。", step);
                        await Task.Delay(1000, _cts.Token);
                        return;
                    }
                    
                    var candyResult = Find(gaming_candy, new MatchOptions { Threshold = 0.96 });
                    if (candyResult.Success)
                    {
                        ClickMatchCenter(candyResult);
                        step++;
                        _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：预测糖果。", step);
                        await Task.Delay(1000, _cts.Token);
                        return;
                    }
                }
                else
                {
                    var returnResult = Find(gaming_return, new MatchOptions { Threshold = 0.96 });
                    if (returnResult.Success)
                    {
                        ClickMatchCenter(returnResult);
                        step++;
                        _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：返回。", step);
                        await Task.Delay(1000, _cts.Token);
                        return;
                    }
                    
                    var monsterResult = Find(gaming_monster, new MatchOptions { Threshold = 0.96 });
                    if (monsterResult.Success)
                    {
                        ClickMatchCenter(monsterResult);
                        step++;
                        _logger.LogInformation("第[Yellow]{Step}[/Yellow]步：预测怪物。", step);
                        await Task.Delay(1000, _cts.Token);
                        return;
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

    private int FindRound()
    {
        // 使用模板数组简化重复代码
        var roundTemplates = new[] 
        { 
            (gaming_round1, 1), 
            (gaming_round2, 2), 
            (gaming_round3, 3), 
            (gaming_round4, 4), 
            (gaming_round5, 5) 
        };

        foreach (var (template, roundNum) in roundTemplates)
        {
            var result = Find(template);
            if (result.Success)
            {
                ShowMatchRects(result, "Round");
                return roundNum;
            }
        }

        return -1;
    }

    public override bool SetParameters(Dictionary<string, object> parameters)
    {
        // 甜蜜冒险目前没有需要设置的参数
        return true;
    }
}
