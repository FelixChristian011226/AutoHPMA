using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.DataHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.GameTask.Permanent;

public enum AutoClubQuizState
{
    Outside,
    Map,
    ClubScene,
    ChatFrame,
    Events,
    Wait,
    Quiz,
    Over,
    Victory,
}

public class AutoClubQuiz : BaseGameTask
{
    #region 字段

    private static ExcelHelper excelHelper;
    private static PaddleOCRHelper paddleOCRHelper;

    private AutoClubQuizState _state = AutoClubQuizState.Outside;

    // 模板图像
    private Mat close_quiz_info, close_club_rank;
    private Mat map_castle_symbol, map_club_symbol, map_club_enter, map_return;
    private Mat ui_club_symbol, ui_badge;
    private Mat chat_mail, chat_whisper, chat_club, chat_club_quiz, chat_college, chat_college_help;
    private Mat badge_club_shop, badge_enter, badge_enter_mask;
    private Mat quiz_wait, quiz_leave, quiz_over, quiz_victory;
    private Mat quiz_option_a, quiz_option_b, quiz_option_c, quiz_option_d, quiz_option_mask;
    private Mat quiz_time0, quiz_time20;

    // 状态字段
    private string? excelPath;
    private char bestOption;
    private string? q, a, b, c, d, answer, i;
    private bool _optionLocated = false, _questionLocated = false;
    private bool _waited = false, _quiz_over = true;
    private Dictionary<char, Rect> optionRects = new();
    private Rect question_rect;
    private Rect index_rect;
    private int detect_gap = 200;
    private enum GatherRefreshMode { ChatBox, Badge }
    private GatherRefreshMode _gatherRefreshMode = GatherRefreshMode.Badge;
    private int _answerDelay = 0;
    private bool _joinOthers = true;
    private int roundIndex = 1;

    #endregion

    public AutoClubQuiz(ILogger<AutoClubQuiz> logger, nint displayHwnd, nint gameHwnd)
        : base(logger, displayHwnd, gameHwnd)
    {
        excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/ClubQuiz", "club_question_bank.xlsx");
        excelHelper = new ExcelHelper(excelPath);
        paddleOCRHelper = new PaddleOCRHelper();
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow?.AddLayer("Match");
        _maskWindow?.AddLayer("Option");
        _maskWindow?.AddLayer("Question");
        _maskWindow?.AddLayer("Time");
    }

    public void LoadAssets()
    {
        string folder = "Assets/ClubQuiz/Image/";

        close_club_rank = Cv2.ImRead(folder + "close_club_rank.png", ImreadModes.Color);
        close_quiz_info = Cv2.ImRead(folder + "close_quiz_info.png", ImreadModes.Color);
        map_castle_symbol = Cv2.ImRead(folder + "map_castle_symbol.png", ImreadModes.Color);
        map_club_symbol = Cv2.ImRead(folder + "map_club_symbol.png", ImreadModes.Color);
        map_club_enter = Cv2.ImRead(folder + "map_club_enter.png", ImreadModes.Color);
        map_return = Cv2.ImRead(folder + "map_return.png", ImreadModes.Color);
        ui_club_symbol = Cv2.ImRead(folder + "ui_club_symbol.png", ImreadModes.Color);
        ui_badge = Cv2.ImRead(folder + "ui_badge.png", ImreadModes.Color);
        chat_mail = Cv2.ImRead(folder + "chat_mail.png", ImreadModes.Color);
        chat_whisper = Cv2.ImRead(folder + "chat_whisper.png", ImreadModes.Color);
        chat_club = Cv2.ImRead(folder + "chat_club.png", ImreadModes.Color);
        chat_club_quiz = Cv2.ImRead(folder + "chat_club_quiz.png", ImreadModes.Color);
        chat_college = Cv2.ImRead(folder + "chat_college.png", ImreadModes.Color);
        chat_college_help = Cv2.ImRead(folder + "chat_college_help.png", ImreadModes.Color);
        badge_club_shop = Cv2.ImRead(folder + "badge_club_shop.png", ImreadModes.Color);
        badge_enter = Cv2.ImRead(folder + "badge_enter.png", ImreadModes.Color);
        badge_enter_mask = Cv2.ImRead(folder + "badge_enter_mask.png", ImreadModes.Color);
        quiz_wait = Cv2.ImRead(folder + "quiz_wait.png", ImreadModes.Color);
        quiz_leave = Cv2.ImRead(folder + "quiz_leave.png", ImreadModes.Color);
        quiz_option_a = Cv2.ImRead(folder + "quiz_option_a.png", ImreadModes.Color);
        quiz_option_b = Cv2.ImRead(folder + "quiz_option_b.png", ImreadModes.Color);
        quiz_option_c = Cv2.ImRead(folder + "quiz_option_c.png", ImreadModes.Color);
        quiz_option_d = Cv2.ImRead(folder + "quiz_option_d.png", ImreadModes.Color);
        quiz_option_mask = Cv2.ImRead(folder + "quiz_option_mask.png", ImreadModes.Color);
        quiz_time0 = Cv2.ImRead(folder + "quiz_time0.png", ImreadModes.Color);
        quiz_time20 = Cv2.ImRead(folder + "quiz_time20.png", ImreadModes.Color);
        quiz_over = Cv2.ImRead(folder + "quiz_over.png", ImreadModes.Color);
        quiz_victory = Cv2.ImRead(folder + "quiz_victory.png", ImreadModes.Color);
    }

    public override async void Start()
    {
        _state = AutoClubQuizState.Outside;
        await RunTaskAsync("社团答题");
    }

    protected override async Task ExecuteLoopAsync()
    {
        await CloseDialogs();
        FindState();
        
        switch (_state)
        {
            case AutoClubQuizState.Outside:
                await HandleOutsideState();
                break;

            case AutoClubQuizState.Map:
                await HandleMapState();
                break;

            case AutoClubQuizState.ClubScene:
                await HandleClubSceneState();
                break;

            case AutoClubQuizState.ChatFrame:
                await HandleChatFrameState();
                break;

            case AutoClubQuizState.Events:
                await HandleEventsState();
                break;

            case AutoClubQuizState.Wait:
                await Task.Delay(1000, _cts.Token);
                break;

            case AutoClubQuizState.Quiz:
                await HandleQuizState();
                break;

            case AutoClubQuizState.Over:
                await HandleOverState();
                break;

            case AutoClubQuizState.Victory:
                await HandleVictoryState();
                break;
        }
    }

    #region 状态处理方法

    private async Task HandleOutsideState()
    {
        if (!_waited)
        {
            _logWindow?.SetGameState("社团答题-等待加载");
            await Task.Delay(3000, _cts.Token);
            _waited = true;
            return;
        }
        _waited = false;
        SendESC(_gameHwnd);
        await Task.Delay(2000, _cts.Token);
        SendKey(_gameHwnd, 0x4D);
        await Task.Delay(2000, _cts.Token);
    }

    private async Task HandleMapState()
    {
        TryClickTemplate(map_castle_symbol);
        await Task.Delay(1000, _cts.Token);
        TryClickTemplate(map_club_symbol);
        await Task.Delay(1000, _cts.Token);
        TryClickTemplate(map_club_enter);
        await Task.Delay(1000, _cts.Token);
        SendESC(_gameHwnd);
        await Task.Delay(2000, _cts.Token);
    }

    private async Task HandleClubSceneState()
    {
        _logger.LogInformation("等待下一场答题...");
        for (int i = 5; i > 0; i--)
        {
            _logger.LogInformation("还剩[Yellow]{Count}[/Yellow]秒...", i);
            await Task.Delay(1000, _cts.Token);
            _logWindow?.DeleteLastLogMessage();
        }
        _logWindow?.DeleteLastLogMessage();

        switch (_gatherRefreshMode)
        {
            case GatherRefreshMode.ChatBox:
                SendEnter(_gameHwnd);
                await Task.Delay(2000, _cts.Token);
                _gatherRefreshMode = GatherRefreshMode.Badge;
                break;
            case GatherRefreshMode.Badge:
                TryClickTemplate(ui_badge);
                await Task.Delay(3000, _cts.Token);
                _gatherRefreshMode = GatherRefreshMode.ChatBox;
                break;
        }
    }

    private async Task HandleChatFrameState()
    {
        // 点击展开社团频道
        if (TryClickTemplate(chat_club, 0.88))
            await Task.Delay(2000, _cts.Token);
        // 点击前往活动面板
        if (TryClickTemplate(chat_club_quiz, 0.98))
            await Task.Delay(2000, _cts.Token);

        if (_joinOthers)
        {
            await TryJoinOthersQuiz();
        }

        // 关闭聊天框
        SendESC(_gameHwnd);
        await Task.Delay(1500, _cts.Token);
    }

    private async Task TryJoinOthersQuiz()
    {
        // 尝试在学院互助频道找到社团答题
        if (TryClickTemplate(chat_college_help, 0.88) || TryClickTemplate(chat_college, 0.88))
        {
            await Task.Delay(1500, _cts.Token);
            
            // 如果是学院频道，先点学院互助
            if (TryClickTemplate(chat_college_help))
                await Task.Delay(1500, _cts.Token);
            
            // 尝试点击前往活动
            if (TryClickTemplate(chat_club_quiz, 0.98))
            {
                await Task.Delay(2000, _cts.Token);
                if (Find(chat_club_quiz, new MatchOptions { Threshold = 0.98 }).Success)
                {
                    SendESC(_gameHwnd);
                    await Task.Delay(1500, _cts.Token);
                }
            }
        }
    }

    private async Task HandleEventsState()
    {
        var enterResult = Find(badge_enter, new MatchOptions { Mask = badge_enter_mask });
        if (!enterResult.Success)
        {
            SendESC(_gameHwnd);
        }
        else
        {
            ClickMatchCenter(enterResult);
        }
        await Task.Delay(1500, _cts.Token);
    }

    private async Task HandleQuizState()
    {
        _maskWindow?.ShowLayer("Option");
        
        if (_quiz_over)
        {
            _logger.LogInformation("第[Yellow]{roundIndex}[/Yellow]轮答题开始", roundIndex);
            _quiz_over = false;
        }
        
        if (!_optionLocated && !LocateOptions())
        {
            _logger.LogWarning("未定位到选项框位置！即将重新尝试定位。");
            await Task.Delay(1000, _cts.Token);
            return;
        }
        
        if (!_questionLocated && !LocateQuestion())
        {
            _logger.LogWarning("未定位到问题框位置！即将重新尝试定位。");
            await Task.Delay(1000, _cts.Token);
            return;
        }

        if (FindTime20AndIndex())
        {
            await Task.Delay(500, _cts.Token);
            LocateQuestion();
            await Task.Delay(100, _cts.Token);
            await AcquireAnswerAsync();
            return;
        }

        await Task.Delay(detect_gap, _cts.Token);
        _maskWindow?.HideLayer("Option");
    }

    private async Task HandleOverState()
    {
        _maskWindow?.ClearLayer("Time");
        _maskWindow?.ClearLayer("Question");
        await Task.Delay(1000, _cts.Token);
        TryClickTemplate(quiz_over);
        await Task.Delay(2000, _cts.Token);
    }

    private async Task HandleVictoryState()
    {
        await Task.Delay(1000, _cts.Token);
        roundIndex++;
        _quiz_over = true;
        FindScore();
        SendESC(_gameHwnd);
        await Task.Delay(1000, _cts.Token);
    }

    #endregion

    #region 辅助方法

    private async Task AcquireAnswerAsync()
    {
        await Task.Run(() => RecogniseText());
        q = TextMatchHelper.FilterChineseAndPunctuation(q);
        PrintText();
        answer = excelHelper.GetBestMatchingAnswer(q);
        bestOption = TextMatchHelper.FindBestOption(answer, a, b, c, d);
        await Task.Delay(_answerDelay * 1000, _cts.Token);
        i = Regex.Match(i, @"\d+/\d+").Value;
        _logger.LogInformation("进度：[Yellow]{i}[/Yellow]。答案：[Lime]{bestOption}[/Lime]。", i, bestOption);
        ClickOption();
    }

    /// <summary>
    /// 定位所有选项位置（使用循环简化重复代码）
    /// </summary>
    private bool LocateOptions()
    {
        var captureMat = CaptureAndPreprocess();
        
        var optionTemplates = new (char key, Mat template)[]
        {
            ('A', quiz_option_a),
            ('B', quiz_option_b),
            ('C', quiz_option_c),
            ('D', quiz_option_d)
        };

        optionRects.Clear();

        foreach (var (key, template) in optionTemplates)
        {
            var matchPoint = MatchTemplateHelper.MatchTemplate(
                captureMat, template, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.85);
            
            if (matchPoint == default || !IsValidPoint(matchPoint, captureMat))
                return false;
            
            optionRects[key] = new Rect(matchPoint.X, matchPoint.Y, template.Width, template.Height);
        }

        // 显示定位结果
        var scaledRects = optionRects.Values.Select(r => ScaleRect(r, scale)).ToList();
        _maskWindow?.SetLayerRects("Option", scaledRects);

        _optionLocated = true;
        return true;
    }

    private bool IsValidPoint(Point p, Mat mat) =>
        p.X >= 0 && p.Y >= 0 && p.X < mat.Width && p.Y < mat.Height;

    private bool LocateQuestion()
    {
        var captureMat = CaptureAndPreprocess();
        Mat captureMat_binary = ContourDetectHelper.Binarize(captureMat, 200);
        question_rect = ContourDetectHelper.DetectApproxRectangle(captureMat_binary);
        
        if (question_rect == default)
            return false;

        _questionLocated = true;
        _maskWindow?.SetLayerRects("Question", new List<Rect> { ScaleRect(question_rect, scale) });
        return true;
    }

    public void RecogniseText()
    {
        var captureMat = CaptureAndPreprocess();

        q = paddleOCRHelper.Ocr(new Mat(captureMat, question_rect));
        a = paddleOCRHelper.Ocr(new Mat(captureMat, optionRects['A']));
        b = paddleOCRHelper.Ocr(new Mat(captureMat, optionRects['B']));
        c = paddleOCRHelper.Ocr(new Mat(captureMat, optionRects['C']));
        d = paddleOCRHelper.Ocr(new Mat(captureMat, optionRects['D']));
        i = paddleOCRHelper.Ocr(new Mat(captureMat, index_rect));
    }

    private void PrintText()
    {
        _logger.LogDebug("问题：{q}", q);
        _logger.LogDebug("选项A：{a}", a);
        _logger.LogDebug("选项B：{b}", b);
        _logger.LogDebug("选项C：{c}", c);
        _logger.LogDebug("选项D：{d}", d);
    }

    private void ClickOption()
    {
        var targetRect = optionRects.GetValueOrDefault(bestOption, optionRects['A']);
        var centerX = targetRect.X + quiz_option_mask.Width / 4;
        var centerY = targetRect.Y + quiz_option_mask.Height / 2;
        Click(new Point(centerX, centerY));
    }

    public async Task CloseDialogs()
    {
        if (TryClickTemplate(close_quiz_info))
            await Task.Delay(1000, _cts.Token);
        if (TryClickTemplate(close_club_rank))
            await Task.Delay(1000, _cts.Token);
    }

    /// <summary>
    /// 查找当前状态（使用模式匹配简化）
    /// </summary>
    public void FindState()
    {
        // 按优先级定义状态检测规则
        var stateRules = new (Mat[] templates, AutoClubQuizState state, string displayName)[]
        {
            (new[] { ui_club_symbol }, AutoClubQuizState.ClubScene, "社团答题-等待中"),
            (new[] { map_return }, AutoClubQuizState.Map, "社团答题-地图"),
            (new[] { chat_mail, chat_whisper }, AutoClubQuizState.ChatFrame, "社团答题-聊天框"),
            (new[] { badge_club_shop }, AutoClubQuizState.Events, "社团答题-活动选择"),
            (new[] { quiz_wait }, AutoClubQuizState.Wait, "社团答题-集结中"),
            (new[] { quiz_leave }, AutoClubQuizState.Quiz, "社团答题-活动中"),
            (new[] { quiz_over }, AutoClubQuizState.Over, "社团答题-已结束"),
            (new[] { quiz_victory }, AutoClubQuizState.Victory, "社团答题-结算中"),
        };

        foreach (var (templates, state, displayName) in stateRules)
        {
            if (templates.Any(t => Find(t).Success))
            {
                _state = state;
                _logWindow?.SetGameState(displayName);
                _waited = false;
                return;
            }
        }

        _state = AutoClubQuizState.Outside;
        _logWindow?.SetGameState("社团答题-未进入场景");
    }

    private void FindScore()
    {
        var captureMat = CaptureAndPreprocess();
        string ocrText = paddleOCRHelper.Ocr(captureMat);
        var match = Regex.Match(ocrText, @"\+(\d+)\s*社团贡献\s*\((\d+\/\d+)\)", RegexOptions.Singleline);

        if (!match.Success)
        {
            _logger.LogWarning("无法识别社团贡献分数，请检查OCR设置或截图质量。");
            return;
        }
        
        int addScore = int.Parse(match.Groups[1].Value);
        string weekTotal = match.Groups[2].Value;

        _logger.LogInformation("本次社团贡献：[Yellow]+{addScore}[/Yellow]。", addScore);
        _logger.LogInformation("本周社团贡献：[Yellow]{weekTotal}[/Yellow]。", weekTotal);
        ToastNotificationHelper.ShowToastWithImage("答题结束", $"本次社团贡献：+{addScore}。\n本周社团贡献：{weekTotal}。", captureMat);
    }

    private bool FindTime20AndIndex()
    {
        var result = Find(quiz_time20);
        if (!result.Success) return false;

        _maskWindow?.SetLayerRects("Time", result.Rects);
        index_rect = new Rect(result.Location.X, result.Location.Y + quiz_time20.Height, quiz_time20.Width, quiz_time20.Height);
        return true;
    }

    #endregion

    #region 参数设置

    public override bool SetParameters(Dictionary<string, object> parameters)
    {
        try
        {
            if (parameters.TryGetValue("AnswerDelay", out var delayObj))
            {
                var answerDelay = Convert.ToInt32(delayObj);
                if (answerDelay < 0)
                {
                    _logger.LogWarning("答题延迟不能小于0。已设置为默认值。");
                    return false;
                }
                _answerDelay = answerDelay;
                _logger.LogDebug("答题延迟设置为：{AnswerDelay}秒", _answerDelay);
            }

            if (parameters.TryGetValue("JoinOthers", out var joinObj))
            {
                _joinOthers = Convert.ToBoolean(joinObj);
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
