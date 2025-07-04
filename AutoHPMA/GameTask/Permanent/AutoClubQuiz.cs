﻿using AutoHPMA.Helpers;
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Vanara.PInvoke;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using static Vanara.PInvoke.User32;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;
using AutoHPMA.GameTask;

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
    private static ExcelHelper excelHelper;
    private static PaddleOCRHelper paddleOCRHelper;

    private AutoClubQuizState _state = AutoClubQuizState.Outside;
    private static Mat? captureMat;
    private Mat close_quiz_info, close_club_rank;
    private Mat map_castle_symbol, map_club_symbol, map_club_enter, map_return;
    private Mat ui_club_symbol, ui_badge;
    private Mat chat_mail, chat_whisper, chat_club, chat_club_quiz, chat_college, chat_college_help;
    private Mat badge_club_shop, badge_enter, badge_enter_mask;
    private Mat quiz_wait, quiz_leave, quiz_over, quiz_victory;
    private Mat quiz_option_a, quiz_option_b, quiz_option_c, quiz_option_d, quiz_option_mask;
    private Mat quiz_time0, quiz_time20;

    private List<Rect> time_rects = new List<Rect>();
    private string? excelPath;
    private char bestOption;
    private string? q, a, b, c, d, answer, i;
    private bool _optionLocated = false, _questionLocated = false;
    private bool _waited = false, _quiz_over = true;
    private Rect option_a_rect, option_b_rect, option_c_rect, option_d_rect;
    private Rect question_rect;
    private Rect index_rect;
    private int detect_gap = 200;
    private enum GatherRefreshMode { ChatBox, Badge }
    private GatherRefreshMode _gatherRefreshMode=GatherRefreshMode.Badge;
    private int _answerDelay = 0;
    private bool _joinOthers = true;
    private int roundIndex = 1;

    public event EventHandler? TaskCompleted;

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
        string image_folder = "Assets/ClubQuiz/Image/";

        close_club_rank = Cv2.ImRead(image_folder + "close_club_rank.png", ImreadModes.Color);
        close_quiz_info = Cv2.ImRead(image_folder + "close_quiz_info.png", ImreadModes.Color);
        map_castle_symbol = Cv2.ImRead(image_folder+"map_castle_symbol.png", ImreadModes.Color);
        map_club_symbol = Cv2.ImRead(image_folder + "map_club_symbol.png", ImreadModes.Color);
        map_club_enter = Cv2.ImRead(image_folder + "map_club_enter.png", ImreadModes.Color);
        map_return = Cv2.ImRead(image_folder + "map_return.png", ImreadModes.Color);
        ui_club_symbol = Cv2.ImRead(image_folder + "ui_club_symbol.png", ImreadModes.Color);
        ui_badge = Cv2.ImRead(image_folder + "ui_badge.png", ImreadModes.Color);
        chat_mail = Cv2.ImRead(image_folder + "chat_mail.png", ImreadModes.Color);
        chat_whisper = Cv2.ImRead(image_folder + "chat_whisper.png", ImreadModes.Color);
        chat_club = Cv2.ImRead(image_folder + "chat_club.png", ImreadModes.Color);
        chat_club_quiz = Cv2.ImRead(image_folder + "chat_club_quiz.png", ImreadModes.Color);
        chat_college = Cv2.ImRead(image_folder + "chat_college.png", ImreadModes.Color);
        chat_college_help = Cv2.ImRead(image_folder + "chat_college_help.png", ImreadModes.Color);
        badge_club_shop = Cv2.ImRead(image_folder + "badge_club_shop.png", ImreadModes.Color);
        badge_enter = Cv2.ImRead(image_folder + "badge_enter.png", ImreadModes.Color);
        badge_enter_mask = Cv2.ImRead(image_folder + "badge_enter_mask.png", ImreadModes.Color);
        quiz_wait = Cv2.ImRead(image_folder + "quiz_wait.png", ImreadModes.Color);
        quiz_leave = Cv2.ImRead(image_folder + "quiz_leave.png", ImreadModes.Color);
        quiz_option_a = Cv2.ImRead(image_folder + "quiz_option_a.png", ImreadModes.Color);
        quiz_option_b = Cv2.ImRead(image_folder + "quiz_option_b.png", ImreadModes.Color);
        quiz_option_c = Cv2.ImRead(image_folder + "quiz_option_c.png", ImreadModes.Color);
        quiz_option_d = Cv2.ImRead(image_folder + "quiz_option_d.png", ImreadModes.Color);
        quiz_option_mask = Cv2.ImRead(image_folder + "quiz_option_mask.png", ImreadModes.Color);
        quiz_time0 = Cv2.ImRead(image_folder + "quiz_time0.png", ImreadModes.Color);
        quiz_time20 = Cv2.ImRead(image_folder + "quiz_time20.png", ImreadModes.Color);
        quiz_over = Cv2.ImRead(image_folder + "quiz_over.png", ImreadModes.Color);
        quiz_victory = Cv2.ImRead(image_folder + "quiz_victory.png", ImreadModes.Color);
    }

    public override void Stop()
    {
        base.Stop();
    }

    public override async void Start()
    {
        _state = AutoClubQuizState.Outside;
        _logWindow?.SetGameState("社团答题");
        _logger.LogInformation("[Aquamarine]---社团答题任务已启动---[/Aquamarine]");
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                System.GC.Collect();
                await CloseDialogs();
                FindState();
                switch (_state)
                {
                    case AutoClubQuizState.Outside:
                        if (!_waited)
                        {
                            _logWindow?.SetGameState("社团答题-等待加载");
                            await Task.Delay(3000, _cts.Token);
                            _waited = true;
                            break;
                        }
                        _waited = false;
                        SendESC(_gameHwnd);
                        await Task.Delay(2000, _cts.Token);
                        SendKey(_gameHwnd, 0x4D);
                        await Task.Delay(2000, _cts.Token);
                        break;

                    case AutoClubQuizState.Map:
                        FindAndClick(ref map_castle_symbol);
                        await Task.Delay(1000, _cts.Token);
                        FindAndClick(ref map_club_symbol);
                        await Task.Delay(1000, _cts.Token);
                        FindAndClick(ref map_club_enter);
                        await Task.Delay(1000, _cts.Token);
                        SendESC(_gameHwnd);
                        await Task.Delay(2000, _cts.Token);
                        break;

                    case AutoClubQuizState.ClubScene:
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
                                FindAndClick(ref ui_badge);
                                await Task.Delay(3000, _cts.Token);
                                _gatherRefreshMode = GatherRefreshMode.ChatBox;
                                break;
                        }
                        break;

                    case AutoClubQuizState.ChatFrame:
                        if (FindAndClick(ref chat_club, 0.88))       //点击展开社团频道
                            await Task.Delay(2000, _cts.Token);
                        if (FindAndClick(ref chat_club_quiz, 0.98))  //点击前往活动面板
                            await Task.Delay(2000, _cts.Token);

                        if (_joinOthers)
                        {
                            if (FindAndClick(ref chat_college_help, 0.88))  // case 1: 学院聊天频道已经展开，点击学院互助
                            {
                                await Task.Delay(1500, _cts.Token);
                                if (FindAndClick(ref chat_club_quiz, 0.98))     //case 1.1: 有社团答题，点击前往活动
                                {
                                    await Task.Delay(2000, _cts.Token);
                                    if (FindMatch(ref chat_club_quiz, 0.98))
                                    {
                                        SendESC(_gameHwnd);
                                        await Task.Delay(1500, _cts.Token);
                                    }
                                    continue;
                                }
                                SendESC(_gameHwnd);                             //case 1.2: 没有社团答题，关闭聊天框
                                await Task.Delay(1500, _cts.Token);
                                continue;
                            }
                            if (FindAndClick(ref chat_college, 0.88))       // case 2: 学院聊天频道未展开，先点击学院
                            {
                                await Task.Delay(1500, _cts.Token);
                                if (FindAndClick(ref chat_club_quiz, 0.98))     // case 2.1: 直接进入学院互助频道，点前往活动
                                {
                                    await Task.Delay(2000, _cts.Token);
                                    if (FindMatch(ref chat_club_quiz, 0.98))
                                    {
                                        SendESC(_gameHwnd);
                                        await Task.Delay(1500, _cts.Token);
                                    }
                                    continue;
                                }
                                if (FindAndClick(ref chat_college_help))        // case 2.2: 在学院聊天频道，先点击学院互助
                                {
                                    await Task.Delay(1500, _cts.Token);
                                    if (FindAndClick(ref chat_club_quiz, 0.98))     //case 3.2.1: 有社团答题，点击前往活动
                                    {
                                        await Task.Delay(2000, _cts.Token);
                                        if (FindMatch(ref chat_club_quiz, 0.98))
                                        {
                                            SendESC(_gameHwnd);
                                            await Task.Delay(1500, _cts.Token);
                                        }
                                        continue;
                                    }
                                    SendESC(_gameHwnd);                             //case 3.2.2: 没有社团答题，关闭聊天框
                                    await Task.Delay(1500, _cts.Token);
                                    continue;
                                }
                            }
                        }

                        SendESC(_gameHwnd);     //所有操作完成后，关闭聊天框
                        await Task.Delay(1500, _cts.Token);
                        break;

                    case AutoClubQuizState.Events:
                        if (!FindAndClickWithMask(ref badge_enter, ref badge_enter_mask))
                        {
                            SendESC(_gameHwnd);
                            await Task.Delay(1500, _cts.Token);
                        }
                        await Task.Delay(1500, _cts.Token);
                        break;

                    case AutoClubQuizState.Wait:
                        await Task.Delay(1000, _cts.Token);
                        break;

                    case AutoClubQuizState.Quiz:
                        _maskWindow?.ShowLayer("Option");
                        if (_quiz_over)
                        {
                            _logger.LogInformation("第[Yellow]{roundIndex}[/Yellow]轮答题开始", roundIndex);
                            _quiz_over = false;
                        }
                        if (_optionLocated == false)
                        {
                            if (!LocateOption())
                            {
                                _logger.LogWarning("未定位到选项框位置！即将重新尝试定位。");
                                await Task.Delay(1000, _cts.Token);
                            }
                            continue;
                        }
                        if (_questionLocated == false)
                        {
                            if (!LocateQuestion())
                            {
                                _logger.LogWarning("未定位到选项框位置！即将重新尝试定位。");
                                await Task.Delay(1000, _cts.Token);
                            }
                            continue;
                        }

                        if (FindTime20AndIndex())
                        {
                            await Task.Delay(500, _cts.Token);
                            LocateQuestion();
                            await Task.Delay(100, _cts.Token);
                            await AcquireAnswerAsync();
                            continue;
                        }

                        await Task.Delay(detect_gap, _cts.Token);
                        _maskWindow?.HideLayer("Option");
                        break;

                    case AutoClubQuizState.Over:
                        _maskWindow?.ClearLayer("Time");
                        _maskWindow?.ClearLayer("Question");
                        await Task.Delay(1000, _cts.Token);
                        FindAndClick(ref quiz_over);
                        await Task.Delay(2000, _cts.Token);
                        break;

                    case AutoClubQuizState.Victory:
                        await Task.Delay(1000, _cts.Token);
                        roundIndex++;
                        _quiz_over = true;
                        FindScore();
                        SendESC(_gameHwnd);
                        await Task.Delay(1000, _cts.Token);
                        break;
                }

                continue;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[Aquamarine]---社团答题任务已终止---[/Aquamarine]");
        }
        catch (Exception ex)
        {
            _logger.LogError("发生异常：{ex}", ex.Message);
        }
        finally
        {
            _maskWindow?.ClearAllLayers();
            _logWindow?.SetGameState("空闲");
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    private async Task AcquireAnswerAsync()
    {
        await Task.Run(() => RecogniseText());
        q = TextMatchHelper.FilterChineseAndPunctuation(q);
        PrintText();
        answer = excelHelper.GetBestMatchingAnswer(q);
        bestOption = TextMatchHelper.FindBestOption(answer, a, b, c, d);
        await Task.Delay(_answerDelay * 1000, _cts.Token);
        i = Regex.Match(i, @"\d+/\d+").Value;   //正则匹配去掉多余符号
        _logger.LogInformation("进度：[Yellow]{i}[/Yellow]。答案：[Lime]{bestOption}[/Lime]。", i, bestOption);
        ClickOption();
    }

    private bool LocateOption()
    {
        Point matchpoint;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_a, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.85);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return false;
        }
        option_a_rect = new Rect(matchpoint.X, matchpoint.Y, quiz_option_a.Width, quiz_option_a.Height);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_b, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.85);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return false;
        }
        option_b_rect = new Rect(matchpoint.X, matchpoint.Y, quiz_option_b.Width, quiz_option_b.Height);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_c, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.85);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return false;
        }
        option_c_rect = new Rect(matchpoint.X, matchpoint.Y, quiz_option_c.Width, quiz_option_c.Height);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_d, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.85);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return false;
        }
        option_d_rect = new Rect(matchpoint.X, matchpoint.Y, quiz_option_d.Width, quiz_option_d.Height);

        _maskWindow?.SetLayerRects("Option", new List<Rect>
        {
            ScaleRect(option_a_rect, scale),
            ScaleRect(option_b_rect, scale),
            ScaleRect(option_c_rect, scale),
            ScaleRect(option_d_rect, scale)
        });

        _optionLocated = true;

        return true;
    }

    private bool LocateQuestion()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Mat captureMat_binary = ContourDetectHelper.Binarize(captureMat, 200);
        question_rect = ContourDetectHelper.DetectApproxRectangle(captureMat_binary);
        if (question_rect == default)
        {
            return false;
        }

        _questionLocated = true;
        _maskWindow?.SetLayerRects("Question", new List<Rect> { ScaleRect(question_rect, scale) });

        return true;
    }

    public void RecogniseText()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        Mat questionMat = new Mat(captureMat, question_rect);
        Mat answeraMat = new Mat(captureMat, option_a_rect);
        Mat answerbMat = new Mat(captureMat, option_b_rect);
        Mat answercMat = new Mat(captureMat, option_c_rect);
        Mat answerdMat = new Mat(captureMat, option_d_rect);
        Mat indexMat = new Mat(captureMat, index_rect);

        q = paddleOCRHelper.Ocr(questionMat);
        a = paddleOCRHelper.Ocr(answeraMat);
        b = paddleOCRHelper.Ocr(answerbMat);
        c = paddleOCRHelper.Ocr(answercMat);
        d = paddleOCRHelper.Ocr(answerdMat);
        i = paddleOCRHelper.Ocr(indexMat);
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
        switch (bestOption)
        {
            case 'A':
                SendMouseClick(_gameHwnd, (uint)(option_a_rect.X * scale - offsetX + quiz_option_mask.Width / 4 * scale), (uint)(option_a_rect.Y * scale - offsetY + quiz_option_mask.Height / 2 * scale));
                break;
            case 'B':
                SendMouseClick(_gameHwnd, (uint)(option_b_rect.X * scale - offsetX + quiz_option_mask.Width / 4 * scale), (uint)(option_b_rect.Y * scale - offsetY + quiz_option_mask.Height / 2 * scale));
                break;
            case 'C':
                SendMouseClick(_gameHwnd, (uint)(option_c_rect.X * scale - offsetX + quiz_option_mask.Width / 4 * scale), (uint)(option_c_rect.Y * scale - offsetY + quiz_option_mask.Height / 2 * scale));
                break;
            case 'D':
                SendMouseClick(_gameHwnd, (uint)(option_d_rect.X * scale - offsetX + quiz_option_mask.Width / 4 * scale), (uint)(option_d_rect.Y * scale - offsetY + quiz_option_mask.Height / 2 * scale));
                break;
        }
    }

    public async Task CloseDialogs()
    {
        if(FindAndClick(ref close_quiz_info))
            await Task.Delay(1000, _cts.Token);
        if(FindAndClick(ref close_club_rank))
            await Task.Delay(1000, _cts.Token);
    }

    public void FindState()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        if(FindMatch(ref ui_club_symbol))
        {
            _state = AutoClubQuizState.ClubScene;
            _logWindow?.SetGameState("社团答题-等待中");
            _waited = false;
            return;
        }

        if(FindMatch(ref map_return))
        {
            _state = AutoClubQuizState.Map;
            _logWindow?.SetGameState("社团答题-地图");
            _waited = false;
            return;
        }

        if(FindMatch(ref chat_mail) || FindMatch(ref chat_whisper))
        {
            _state = AutoClubQuizState.ChatFrame;
            _logWindow?.SetGameState("社团答题-聊天框");
            _waited = false;
            return;
        }

        if (FindMatch(ref badge_club_shop))
        {
            _state = AutoClubQuizState.Events;
            _logWindow?.SetGameState("社团答题-活动选择");
            _waited = false;
            return;
        }

        if (FindMatch(ref quiz_wait))
        {
            _state = AutoClubQuizState.Wait;
            _logWindow?.SetGameState("社团答题-集结中");
            _waited = false;
            return;
        }

        if (FindMatch(ref quiz_leave))
        {
            _state = AutoClubQuizState.Quiz;
            _logWindow?.SetGameState("社团答题-活动中");
            _waited = false;
            return;
        }

        if (FindMatch(ref quiz_over))
        {
            _state = AutoClubQuizState.Over;
            _logWindow?.SetGameState("社团答题-已结束");
            _waited = false;
            return;
        }

        if (FindMatch(ref quiz_victory))
        {
            _state = AutoClubQuizState.Victory;
            _logWindow?.SetGameState("社团答题-结算中");
            _waited = false;
            return;
        }

        _state = AutoClubQuizState.Outside;
        _logWindow?.SetGameState("社团答题-未进入场景");
        return;
    }

    private void FindScore()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        string ocrText = paddleOCRHelper.Ocr(captureMat);
        var match = Regex.Match(ocrText,
            @"\+(\d+)\s*社团贡献\s*(\(\d+\/\d+\))",
            RegexOptions.Singleline
        );

        if (!match.Success)
        {
            _logger.LogWarning("无法识别社团贡献分数，请检查OCR设置或截图质量。");
        }
        int addScore = int.Parse(match.Groups[1].Value);
        string weekTotal = match.Groups[2].Value;

        _logger.LogInformation("本次社团贡献：[Yellow]+{addScore}[/Yellow]。", addScore);
        _logger.LogInformation("本周社团贡献：[Yellow]{weekTotal}[/Yellow]。", weekTotal);
        ToastNotificationHelper.ShowToastWithImage("答题结束", "本次社团贡献：+" + addScore + "。\n" + "本周社团贡献：" + weekTotal + "。", captureMat);
    }

    private bool FindTime20AndIndex()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_time20, TemplateMatchModes.CCoeffNormed, null, 0.9);
        if (matchpoint == default)
        {
            return false;
        }
        time_rects.Clear();
        time_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(quiz_time20.Width * scale), (int)(quiz_time20.Height * scale)));
        _maskWindow?.SetLayerRects("Time", time_rects);
        index_rect = new Rect(matchpoint.X, matchpoint.Y + quiz_time20.Height, quiz_time20.Width, quiz_time20.Height);
        return true;
    }




    #region SetParameter

    public override bool SetParameters(Dictionary<string, object> parameters)
    {
        try
        {
            if (parameters.ContainsKey("AnswerDelay"))
            {
                var answerDelay = Convert.ToInt32(parameters["AnswerDelay"]);
                if (answerDelay < 0)
                {
                    _logger.LogWarning("答题延迟不能小于0。已设置为默认值。");
                    return false;
                }
                _answerDelay = answerDelay;
                _logger.LogDebug("答题延迟设置为：{AnswerDelay}秒", _answerDelay);
            }

            if (parameters.ContainsKey("JoinOthers"))
            {
                _joinOthers = Convert.ToBoolean(parameters["JoinOthers"]);
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
