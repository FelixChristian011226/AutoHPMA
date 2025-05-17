using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
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

namespace AutoHPMA.GameTask;

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

    Gathering,
    Preparing,
    Answering,
}

public class AutoClubQuiz
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;
    private static ExcelHelper excelHelper;
    private static PaddleOCRHelper paddleOCRHelper;

    private IntPtr _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;
    private AutoClubQuizState _state = AutoClubQuizState.Outside;

    private static Mat? captureMat;
    private Mat map_castle_symbol, map_club_symbol, map_club_enter, map_return;
    private Mat ui_club_symbol, ui_badge;
    private Mat chat_mail, chat_whisper, chat_club, chat_club_quiz, chat_college, chat_college_help;
    private Mat badge_club_shop, badge_enter, badge_enter_mask;
    private Mat quiz_wait, quiz_reward_close, quiz_leave, quiz_over, quiz_victory;
    private Mat quiz_option_a, quiz_option_b, quiz_option_c, quiz_option_d, quiz_option_mask;
    private Mat quiz_time0, quiz_time20;

    private List<Rect> detect_rects = new List<Rect>();
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

    private enum GatherRefreshMode
    {
        ChatBox,
        Badge,
    }

    private GatherRefreshMode _gatherRefreshMode=GatherRefreshMode.Badge;
    private int _answerDelay = 0;
    private bool _joinOthers = true;

    private int roundIndex = 1;

    private CancellationTokenSource _cts;

    public AutoClubQuiz(IntPtr _displayHwnd, IntPtr _gameHwnd)
    {
        this._displayHwnd = _displayHwnd;
        this._gameHwnd = _gameHwnd;
        _cts = new CancellationTokenSource();
        excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/ClubQuiz", "club_question_bank.xlsx");
        excelHelper = new ExcelHelper(excelPath);
        paddleOCRHelper = new PaddleOCRHelper();
        LoadAssets();
        CalOffset();
        AddLayersForMaskWindow();
    }

    private void AddLayersForMaskWindow()
    {
        _maskWindow.AddLayer("Match");
        _maskWindow.AddLayer("Option");
        _maskWindow.AddLayer("Question");
        _maskWindow.AddLayer("Time");
    }

    public void LoadAssets()
    {
        string image_folder = "Assets/ClubQuiz/Image/";

        map_castle_symbol = Cv2.ImRead(image_folder+"map_castle_symbol.png", ImreadModes.Unchanged);
        Cv2.CvtColor(map_castle_symbol, map_castle_symbol, ColorConversionCodes.BGR2GRAY);
        map_club_symbol = Cv2.ImRead(image_folder + "map_club_symbol.png", ImreadModes.Unchanged);
        Cv2.CvtColor(map_club_symbol, map_club_symbol, ColorConversionCodes.BGR2GRAY);
        map_club_enter = Cv2.ImRead(image_folder + "map_club_enter.png", ImreadModes.Unchanged);
        Cv2.CvtColor(map_club_enter, map_club_enter, ColorConversionCodes.BGR2GRAY);
        map_return = Cv2.ImRead(image_folder + "map_return.png", ImreadModes.Unchanged);
        Cv2.CvtColor(map_return, map_return, ColorConversionCodes.BGR2GRAY);
        ui_club_symbol = Cv2.ImRead(image_folder + "ui_club_symbol.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_club_symbol, ui_club_symbol, ColorConversionCodes.BGR2GRAY);
        ui_badge = Cv2.ImRead(image_folder + "ui_badge.png", ImreadModes.Unchanged);
        Cv2.CvtColor(ui_badge, ui_badge, ColorConversionCodes.BGR2GRAY);
        chat_mail = Cv2.ImRead(image_folder + "chat_mail.png", ImreadModes.Unchanged);
        Cv2.CvtColor(chat_mail, chat_mail, ColorConversionCodes.BGR2GRAY);
        chat_whisper = Cv2.ImRead(image_folder + "chat_whisper.png", ImreadModes.Unchanged);
        Cv2.CvtColor(chat_whisper, chat_whisper, ColorConversionCodes.BGR2GRAY);
        chat_club = Cv2.ImRead(image_folder + "chat_club.png", ImreadModes.Unchanged);
        Cv2.CvtColor(chat_club, chat_club, ColorConversionCodes.BGR2GRAY);
        chat_club_quiz = Cv2.ImRead(image_folder + "chat_club_quiz.png", ImreadModes.Unchanged);
        Cv2.CvtColor(chat_club_quiz, chat_club_quiz, ColorConversionCodes.BGR2GRAY);
        chat_college = Cv2.ImRead(image_folder + "chat_college.png", ImreadModes.Unchanged);
        Cv2.CvtColor(chat_college, chat_college, ColorConversionCodes.BGR2GRAY);
        chat_college_help = Cv2.ImRead(image_folder + "chat_college_help.png", ImreadModes.Unchanged);
        Cv2.CvtColor(chat_college_help, chat_college_help, ColorConversionCodes.BGR2GRAY);
        badge_club_shop = Cv2.ImRead(image_folder + "badge_club_shop.png", ImreadModes.Unchanged);
        Cv2.CvtColor(badge_club_shop, badge_club_shop, ColorConversionCodes.BGR2GRAY);
        badge_enter = Cv2.ImRead(image_folder + "badge_enter.png", ImreadModes.Unchanged);
        Cv2.CvtColor(badge_enter, badge_enter, ColorConversionCodes.BGR2GRAY);
        badge_enter_mask = Cv2.ImRead(image_folder + "badge_enter_mask.png", ImreadModes.Unchanged);
        Cv2.CvtColor(badge_enter_mask, badge_enter_mask, ColorConversionCodes.BGR2GRAY);
        quiz_wait = Cv2.ImRead(image_folder + "quiz_wait.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_wait, quiz_wait, ColorConversionCodes.BGR2GRAY);
        quiz_reward_close = Cv2.ImRead(image_folder + "quiz_reward_close.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_reward_close, quiz_reward_close, ColorConversionCodes.BGR2GRAY);
        quiz_leave = Cv2.ImRead(image_folder + "quiz_leave.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_leave, quiz_leave, ColorConversionCodes.BGR2GRAY);
        quiz_option_a = Cv2.ImRead(image_folder + "quiz_option_a.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_option_a, quiz_option_a, ColorConversionCodes.BGR2GRAY);
        quiz_option_b = Cv2.ImRead(image_folder + "quiz_option_b.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_option_b, quiz_option_b, ColorConversionCodes.BGR2GRAY);
        quiz_option_c = Cv2.ImRead(image_folder + "quiz_option_c.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_option_c, quiz_option_c, ColorConversionCodes.BGR2GRAY);
        quiz_option_d = Cv2.ImRead(image_folder + "quiz_option_d.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_option_d, quiz_option_d, ColorConversionCodes.BGR2GRAY);
        quiz_option_mask = Cv2.ImRead(image_folder + "quiz_option_mask.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_option_mask, quiz_option_mask, ColorConversionCodes.BGR2GRAY);
        quiz_time0 = Cv2.ImRead(image_folder + "quiz_time0.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_time0, quiz_time0, ColorConversionCodes.BGR2GRAY);
        quiz_time20 = Cv2.ImRead(image_folder + "quiz_time20.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_time20, quiz_time20, ColorConversionCodes.BGR2GRAY);
        quiz_over = Cv2.ImRead(image_folder + "quiz_over.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_over, quiz_over, ColorConversionCodes.BGR2GRAY);
        quiz_victory = Cv2.ImRead(image_folder + "quiz_victory.png", ImreadModes.Unchanged);
        Cv2.CvtColor(quiz_victory, quiz_victory, ColorConversionCodes.BGR2GRAY);


    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public async void Start()
    {
        _state = AutoClubQuizState.Gathering;
        _logWindow?.SetGameState("社团答题");

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
                        await Task.Delay(3000);
                        _waited = true;
                        break;
                    }
                    _waited = false;
                    SendESC(_gameHwnd);
                    await Task.Delay(2000);
                    SendKey(_gameHwnd, 0x4D);
                    await Task.Delay(2000);
                    break;

                case AutoClubQuizState.Map:
                    FindAndClick(ref map_castle_symbol);
                    await Task.Delay(1000);
                    FindAndClick(ref map_club_symbol);
                    await Task.Delay(1000);
                    FindAndClick(ref map_club_enter);
                    await Task.Delay(1000);
                    SendESC(_gameHwnd);
                    await Task.Delay(2000);
                    break;

                case AutoClubQuizState.ClubScene:
                    _logWindow?.AddLogMessage("INF", "等待下一场答题...");
                    for (int i = 5; i > 0; i--)
                    {
                        _logWindow?.AddLogMessage("INF", "还剩[Yellow]" + i + "[/Yellow]秒...");
                        await Task.Delay(1000);
                        _logWindow?.DeleteLastLogMessage();
                    }
                    _logWindow?.DeleteLastLogMessage();

                    switch (_gatherRefreshMode)
                    {
                        case GatherRefreshMode.ChatBox:
                            SendEnter(_gameHwnd);
                            await Task.Delay(2000);
                            _gatherRefreshMode = GatherRefreshMode.Badge;
                            break;
                        case GatherRefreshMode.Badge:
                            FindAndClick(ref ui_badge);
                            await Task.Delay(3000);
                            _gatherRefreshMode = GatherRefreshMode.ChatBox;
                            break;

                    }
                    break;

                case AutoClubQuizState.ChatFrame:
                    if (FindAndClick(ref chat_club, 0.88))       //点击展开社团频道
                        await Task.Delay(2000);
                    if (FindAndClick(ref chat_club_quiz, 0.98))  //点击前往活动面板
                        await Task.Delay(2000);

                    if (_joinOthers)
                    {
                        if (FindAndClick(ref chat_college_help, 0.88))  // case 1: 学院聊天频道已经展开，点击学院互助
                        {
                            await Task.Delay(1500);
                            if (FindAndClick(ref chat_club_quiz, 0.98))     //case 1.1: 有社团答题，点击前往活动
                            {
                                await Task.Delay(2000);
                                if (FindMatch(ref chat_club_quiz, 0.98))
                                {
                                    SendESC(_gameHwnd);
                                    await Task.Delay(1500);
                                }
                                continue;
                            }
                            SendESC(_gameHwnd);                             //case 1.2: 没有社团答题，关闭聊天框
                            await Task.Delay(1500);
                            continue;
                        }
                        if (FindAndClick(ref chat_college, 0.88))       // case 2: 学院聊天频道未展开，先点击学院
                        {
                            await Task.Delay(1500);
                            if (FindAndClick(ref chat_club_quiz, 0.98))     // case 2.1: 直接进入学院互助频道，点前往活动
                            {
                                await Task.Delay(2000);
                                if (FindMatch(ref chat_club_quiz, 0.98))
                                {
                                    SendESC(_gameHwnd);
                                    await Task.Delay(1500);
                                }
                                continue;
                            }
                            if (FindAndClick(ref chat_college_help))        // case 2.2: 在学院聊天频道，先点击学院互助
                            {
                                await Task.Delay(1500);
                                if (FindAndClick(ref chat_club_quiz, 0.98))     //case 3.2.1: 有社团答题，点击前往活动
                                {
                                    await Task.Delay(2000);
                                    if (FindMatch(ref chat_club_quiz, 0.98))
                                    {
                                        SendESC(_gameHwnd);
                                        await Task.Delay(1500);
                                    }
                                    continue;
                                }
                                SendESC(_gameHwnd);                             //case 3.2.2: 没有社团答题，关闭聊天框
                                await Task.Delay(1500);
                                continue;
                            }
                        }
                    }

                    SendESC(_gameHwnd);     //所有操作完成后，关闭聊天框
                    await Task.Delay(1500);
                    break;

                case AutoClubQuizState.Events:
                    if (!FindAndClickWithMask(ref badge_enter, ref badge_enter_mask))
                    {
                        SendESC(_gameHwnd);
                        await Task.Delay(1500);
                    }
                    await Task.Delay(1500);
                    break;

                case AutoClubQuizState.Wait:
                    await Task.Delay(1000);
                    break;

                case AutoClubQuizState.Quiz:
                    _maskWindow.ShowLayer("Option");
                    if(_quiz_over)
                    {
                        _logWindow?.AddLogMessage("INF", "第[Yellow]" + roundIndex + "[/Yellow]轮答题开始");
                        _quiz_over = false;
                    }
                    if (_optionLocated == false)
                    {
                        LocateOption();
                        continue;
                    }
                    if (_questionLocated == false)
                    {
                        LocateQuestion();
                        continue;
                    }

                    if (FindTime20AndIndex())
                    {
                        await Task.Delay(500);
                        LocateQuestion();
                        await Task.Delay(100);
                        await AcquireAnswerAsync();
                        continue;
                    }

                    await Task.Delay(detect_gap);
                    _maskWindow.HideLayer("Option");
                    break;

                case AutoClubQuizState.Over:
                    _maskWindow.ClearLayer("Time");
                    _maskWindow.ClearLayer("Question");
                    await Task.Delay(1000);
                    FindAndClick(ref quiz_over);
                    await Task.Delay(2000);
                    break;

                case AutoClubQuizState.Victory:
                    await Task.Delay(1000);
                    roundIndex++;
                    _quiz_over = true;
                    FindScore();
                    SendESC(_gameHwnd);
                    await Task.Delay(1000);
                    break;

            }


            continue;
        }
    }

    private async Task AcquireAnswerAsync()
    {
        await Task.Run(() => RecogniseText());
        q = TextMatchHelper.FilterChineseAndPunctuation(q);
        PrintText();
        answer = excelHelper.GetBestMatchingAnswer(q);
        bestOption = TextMatchHelper.FindBestOption(answer, a, b, c, d);
        await Task.Delay(_answerDelay * 1000);
        i = Regex.Match(i, @"\d+/\d+").Value;   //正则匹配去掉多余符号
        _logWindow?.AddLogMessage("INF", "进度：[Yellow]" + i + "[/Yellow]。答案：[Lime]" + bestOption + "[/Lime]。");
        ClickOption();
    }

    private void LocateOption()
    {
        OpenCvSharp.Point matchpoint;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_a, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.9);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return;
        }
        option_a_rect = new OpenCvSharp.Rect(matchpoint.X, matchpoint.Y, quiz_option_a.Width, quiz_option_a.Height);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_b, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.9);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return;
        }
        option_b_rect = new OpenCvSharp.Rect(matchpoint.X, matchpoint.Y, quiz_option_b.Width, quiz_option_b.Height);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_c, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.9);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return;
        }
        option_c_rect = new OpenCvSharp.Rect(matchpoint.X, matchpoint.Y, quiz_option_c.Width, quiz_option_c.Height);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_option_d, TemplateMatchModes.CCoeffNormed, quiz_option_mask, 0.9);
        if (matchpoint == default || matchpoint.X < 0 || matchpoint.Y < 0 || matchpoint.X > captureMat.Width || matchpoint.Y > captureMat.Height)
        {
            return;
        }
        option_d_rect = new OpenCvSharp.Rect(matchpoint.X, matchpoint.Y, quiz_option_d.Width, quiz_option_d.Height);

        _maskWindow?.SetLayerRects("Option", new List<OpenCvSharp.Rect> { option_a_rect, option_b_rect, option_c_rect, option_d_rect });

        _optionLocated = true;

    }

    private void LocateQuestion()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Mat captureMat_binary = RectangleDetectHelper.Binarize(captureMat, 200);
        question_rect = RectangleDetectHelper.DetectApproxRectangle(captureMat_binary);
        if (question_rect == default)
        {
            return;
        }
        else
        {
            _questionLocated = true;
            _maskWindow.SetLayerRects("Question", new List<OpenCvSharp.Rect> { question_rect });
        }
    }

    public void RecogniseText()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
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
        _logWindow?.AddLogMessage("DBG", "问题：" + q);
        _logWindow?.AddLogMessage("DBG", "选项A：" + a);
        _logWindow?.AddLogMessage("DBG", "选项B：" + b);
        _logWindow?.AddLogMessage("DBG", "选项C：" + c);
        _logWindow?.AddLogMessage("DBG", "选项D：" + d);
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
        if(FindAndClick(ref quiz_reward_close))
            await Task.Delay(1000);
    }

    public void FindState()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);

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
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
        string ocrText = paddleOCRHelper.Ocr(captureMat);
        var match = Regex.Match(ocrText,
            @"\+(\d+)\s*社团贡献\s*(\(\d+\/\d+\))",
            RegexOptions.Singleline
        );

        if (!match.Success)
        {
            _logWindow?.AddLogMessage("Err", "无法识别社团贡献分数，请检查OCR设置或截图质量。");
        }
        int addScore = int.Parse(match.Groups[1].Value);
        string weekTotal = match.Groups[2].Value;

        _logWindow?.AddLogMessage("INF", "本次社团贡献：[Yellow]+" + addScore + "[/Yellow]。");
        _logWindow?.AddLogMessage("INF", "本周社团贡献：[Yellow]" + weekTotal + "[/Yellow]。");

    }

    private bool FindTime20AndIndex()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, quiz_time20, TemplateMatchModes.CCoeffNormed, null, 0.9);
        if (matchpoint == default)
        {
            return false;
        }
        time_rects.Clear();
        time_rects.Add(new OpenCvSharp.Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(quiz_time20.Width * scale), (int)(quiz_time20.Height * scale)));
        _maskWindow?.SetLayerRects("Time", time_rects);
        index_rect = new OpenCvSharp.Rect((int)(matchpoint.X), (int)(matchpoint.Y + quiz_time20.Height), (int)(quiz_time20.Width), (int)(quiz_time20.Height));
        return true;
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
        detect_rects.Clear();
        detect_rects.Add(new OpenCvSharp.Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        _maskWindow?.SetLayerRects("Match", detect_rects);
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
        detect_rects.Clear();
        detect_rects.Add(new OpenCvSharp.Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        _maskWindow?.SetLayerRects("Match", detect_rects);
        SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
        return true;
    }

    private bool FindAndClickWithMask(ref Mat mat, ref Mat mask, double threshold = 0.9)
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(mask.Width, mask.Height));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        Cv2.BitwiseAnd(captureMat, mask, captureMat);
        var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
        //_logWindow?.AddLogMessage("DBG", "Matchpoint: ("  + matchpoint.X + "," + matchpoint.Y + ")");
        if (matchpoint == default)
        {
            return false;
        }
        detect_rects.Clear();
        detect_rects.Add(new OpenCvSharp.Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
        _maskWindow?.SetLayerRects("Match", detect_rects);
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

    #region SetParameter

    public bool SetAnswerDelay(int answer_delay)
    {
        if (answer_delay < 0)
        {
            _logWindow?.AddLogMessage("ERR", "答题延迟不能小于0。已设置为默认值。");
            return false;
        }
        _answerDelay = answer_delay;
        _logWindow?.AddLogMessage("DBG", "答题延迟设置为：" + _answerDelay);
        return true;
    }

    public bool SetGatherRefreshMode(string mode)
    {
        if (mode == "ChatBox")
        {
            _gatherRefreshMode = GatherRefreshMode.ChatBox;
            _logWindow?.AddLogMessage("DBG", "集结刷新模式设置为：聊天框");
            return true;
        }
        else if (mode == "Badge")
        {
            _gatherRefreshMode = GatherRefreshMode.Badge;
            _logWindow?.AddLogMessage("DBG", "集结刷新模式设置为：徽章");
            return true;
        }
        else
        {
            _logWindow?.AddLogMessage("ERR", "集结刷新模式设置失败。");
            return false;
        }
    }

    #endregion
}
