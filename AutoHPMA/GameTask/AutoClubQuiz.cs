using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Vanara.PInvoke;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.IO;
using static AutoHPMA.Helpers.WindowInteractionHelper;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Services;

namespace AutoHPMA.GameTask;

public enum AutoClubQuizState
{
    Gathering,
    Preparing,
    Answering,
}

public class AutoClubQuiz
{
    private static LogWindow _logWindow => AppContextService.Instance.LogWindow;
    private static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;
    private static ExcelHelper excelHelper;
    private static PaddleOCRHelper paddleOCRHelper;

    private AutoClubQuizState _state = AutoClubQuizState.Gathering;

    private static Mat gather, channel, join, close, time18, time20, over, end, gothmog, option_a, option_b, option_c, option_d;
    private static Mat? captureMat;

    private IntPtr _displayHwnd, _gameHwnd;
    private int offsetX, offsetY;
    private double scale;

    private bool _textLocated = false;
    private int question_x, question_y, question_w, question_h;
    private int answer_a_x, answer_a_y;
    private int answer_b_x, answer_b_y;
    private int answer_c_x, answer_c_y;
    private int answer_d_x, answer_d_y;
    private OpenCvSharp.Rect answer_a, answer_b, answer_c, answer_d, question;
    private int answer_w, answer_h;
    private int answer_offset_x, answer_offset_y;
    private string? q, a, b, c, d, answer;
    private string? excelPath;
    private char bestOption;

    private int _answerDelay = 0;

    private int questionIndex = 0, roundIndex = 0;

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
    }


    public void LoadAssets()
    {
        string image_folder = "Assets/ClubQuiz/Image/";
        gather = Cv2.ImRead(image_folder+"gather.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gather, gather, ColorConversionCodes.BGR2GRAY);
        channel = Cv2.ImRead(image_folder+"channel.png", ImreadModes.Unchanged);
        Cv2.CvtColor(channel, channel, ColorConversionCodes.BGR2GRAY);
        join = Cv2.ImRead(image_folder + "join.png", ImreadModes.Unchanged);
        Cv2.CvtColor(join, join, ColorConversionCodes.BGR2GRAY);
        close = Cv2.ImRead(image_folder + "close.png", ImreadModes.Unchanged);
        Cv2.CvtColor(close, close, ColorConversionCodes.BGR2GRAY);
        time18 = Cv2.ImRead(image_folder + "time18.png", ImreadModes.Unchanged);
        Cv2.CvtColor(time18, time18, ColorConversionCodes.BGR2GRAY);
        time20 = Cv2.ImRead(image_folder + "time20.png", ImreadModes.Unchanged);
        Cv2.CvtColor(time20, time20, ColorConversionCodes.BGR2GRAY);
        over = Cv2.ImRead(image_folder + "over.png", ImreadModes.Unchanged);
        Cv2.CvtColor(over, over, ColorConversionCodes.BGR2GRAY);
        end = Cv2.ImRead(image_folder + "end.png", ImreadModes.Unchanged);
        Cv2.CvtColor(end, end, ColorConversionCodes.BGR2GRAY);
        gothmog = Cv2.ImRead(image_folder + "gothmog.png", ImreadModes.Unchanged);
        Cv2.CvtColor(gothmog, gothmog, ColorConversionCodes.BGR2GRAY);
        option_a = Cv2.ImRead(image_folder + "option_a.png", ImreadModes.Unchanged);
        Cv2.CvtColor(option_a, option_a, ColorConversionCodes.BGR2GRAY);
        option_b = Cv2.ImRead(image_folder + "option_b.png", ImreadModes.Unchanged);
        Cv2.CvtColor(option_b, option_b, ColorConversionCodes.BGR2GRAY);
        option_c = Cv2.ImRead(image_folder + "option_c.png", ImreadModes.Unchanged);
        Cv2.CvtColor(option_c, option_c, ColorConversionCodes.BGR2GRAY);
        option_d = Cv2.ImRead(image_folder + "option_d.png", ImreadModes.Unchanged);
        Cv2.CvtColor(option_d, option_d, ColorConversionCodes.BGR2GRAY);

    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public async void Start()
    {
        _state = AutoClubQuizState.Gathering;

        while (!_cts.Token.IsCancellationRequested)
        {
            GC.Collect();
            switch (_state)
            {
                case AutoClubQuizState.Gathering:
                    await Task.Delay(1000);
                    if (!FindAndClick(ref gather))  //找不到集结图标，重复开关聊天框刷新状态
                    {
                        _logWindow?.AddLogMessage("INF", "等待下一场答题...");
                        for (int i = 15; i > 0; i--)
                        {
                            _logWindow?.AddLogMessage("INF", "还剩[Yellow]" + i + "[/Yellow]秒...");
                            await Task.Delay(1000);
                            _logWindow?.DeleteLastLogMessage();
                        }
                        _logWindow?.DeleteLastLogMessage();
                        SendEnter(_gameHwnd);
                        await Task.Delay(1500);
                        FindAndClick(ref channel);
                        await Task.Delay(1500);
                        SendESC(_gameHwnd);
                        continue;
                    }

                    await Task.Delay(1000);
                    if (!FindAndClick(ref join))
                    {
                        SendESC(_gameHwnd);
                        continue;
                    }

                    _state = AutoClubQuizState.Preparing;

                    break;

                case AutoClubQuizState.Preparing:
                    await Task.Delay(1000);

                    if (FindMatch(ref time20))
                    {
                        roundIndex++;
                        _logWindow?.AddLogMessage("INF", "第[Yellow]" + roundIndex + "[/Yellow]轮答题开始");
                        _state = AutoClubQuizState.Answering;
                        continue;
                    }

                    if (!FindAndClick(ref close))
                    {
                        continue;
                    }
                    roundIndex++;
                    _logWindow?.AddLogMessage("INF", "第[Yellow]" + roundIndex + "[/Yellow]轮答题开始");
                    _state = AutoClubQuizState.Answering;

                    break;

                case AutoClubQuizState.Answering:
                    await Task.Delay(500);

                    if (_textLocated == false)
                    {
                        questionIndex++;
                        await Task.Delay(2000);
                        LocateText();
                        await Task.Delay(100);
                        if (_textLocated == true)       //初始化位置后不需要判断20秒，直接执行。
                        {
                            await Task.Run(() => RecogniseText());
                            //RecogniseText();
                            q = TextMatchHelper.FilterChineseAndPunctuation(q);
                            PrintText();
                            answer = excelHelper.GetBestMatchingAnswer(q);
                            bestOption = TextMatchHelper.FindBestOption(answer, a, b, c, d);
                            _logWindow?.AddLogMessage("INF", "第[Yellow]" + questionIndex + "[/Yellow]题，选：[Lime]" + bestOption + "[/Lime]。");
                            await Task.Delay(_answerDelay*1000);
                            ClickOption();
                        }
                        continue;
                    }

                    if (FindAndClick(ref over))
                    {
                        await Task.Delay(3000);
                        SendESC(_gameHwnd);
                        _state = AutoClubQuizState.Gathering;
                        questionIndex = 0;
                    }

                    if (FindMatch(ref time20))
                    {
                        questionIndex++;
                        await Task.Delay(100);
                        await Task.Run(() => RecogniseText());
                        //RecogniseText();
                        q = TextMatchHelper.FilterChineseAndPunctuation(q);
                        PrintText();
                        answer = excelHelper.GetBestMatchingAnswer(q);
                        //_logWindow?.AddLogMessage("DBG", "最佳答案是：" + answer);
                        bestOption = TextMatchHelper.FindBestOption(answer, a, b, c, d);
                        _logWindow?.AddLogMessage("INF", "第[Yellow]" + questionIndex + "[/Yellow]题，选：[Lime]" + bestOption + "[/Lime]。");
                        ClickOption();

                        continue;
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

    private void LocateText()
    {
        OpenCvSharp.Point matchpoint;
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGR2GRAY);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, gothmog, TemplateMatchModes.CCoeffNormed, null, 0.9);
        if (matchpoint == default)
        {
            return;
        }
        question_x = (int)(matchpoint.X);
        question_y = (int)(matchpoint.Y - gothmog.Height / 2);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, option_a, TemplateMatchModes.CCoeffNormed, null, 0.9);
        answer_a_x = (int)(matchpoint.X + option_a.Width);
        answer_a_y = (int)(matchpoint.Y);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, option_b, TemplateMatchModes.CCoeffNormed, null, 0.9);
        answer_b_x = (int)(matchpoint.X + option_b.Width);
        answer_b_y = (int)(matchpoint.Y);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, option_c, TemplateMatchModes.CCoeffNormed, null, 0.9);
        answer_c_x = (int)(matchpoint.X + option_c.Width);
        answer_c_y = (int)(matchpoint.Y);
        matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, option_d, TemplateMatchModes.CCoeffNormed, null, 0.9);
        answer_d_x = (int)(matchpoint.X + option_d.Width);
        answer_d_y = (int)(matchpoint.Y);

        answer_w = answer_b_x - answer_a_x - option_b.Width * 2;
        answer_h = option_a.Height;

        question_w = answer_w * 2;
        question_h = gothmog.Height * 2;

        answer_offset_x = -option_a.Width / 2;
        answer_offset_y = option_a.Height / 2;

        question = new OpenCvSharp.Rect(question_x, question_y, question_w, question_h);
        answer_a = new OpenCvSharp.Rect(answer_a_x, answer_a_y, answer_w, answer_h);
        answer_b = new OpenCvSharp.Rect(answer_b_x, answer_b_y, answer_w, answer_h);
        answer_c = new OpenCvSharp.Rect(answer_c_x, answer_c_y, answer_w, answer_h);
        answer_d = new OpenCvSharp.Rect(answer_d_x, answer_d_y, answer_w, answer_h);

        _logWindow?.AddLogMessage("DBG", "答题框位置初始化完成！");
        _logWindow?.AddLogMessage("DBG", "问题框坐标: (" + question_x + "," + question_y + ")");
        _logWindow?.AddLogMessage("DBG", "问题框宽高: (" + question_w + "," + question_h + ")");
        _logWindow?.AddLogMessage("DBG", "选项A坐标: (" + answer_a_x + "," + answer_a_y + ")");
        _logWindow?.AddLogMessage("DBG", "选项B坐标: (" + answer_b_x + "," + answer_b_y + ")");
        _logWindow?.AddLogMessage("DBG", "选项C坐标: (" + answer_c_x + "," + answer_c_y + ")");
        _logWindow?.AddLogMessage("DBG", "选项D坐标: (" + answer_d_x + "," + answer_d_y + ")");
        _logWindow?.AddLogMessage("DBG", "选项框宽高: (" + answer_w + "," + answer_h + ")");
        _textLocated = true;
    }

    public void RecogniseText()
    {
        captureMat = _capture.Capture();
        Cv2.Resize(captureMat, captureMat, new OpenCvSharp.Size(captureMat.Width / scale, captureMat.Height / scale));
        Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

        Mat questionMat = new Mat(captureMat, question);
        Mat answeraMat = new Mat(captureMat, answer_a);
        Mat answerbMat = new Mat(captureMat, answer_b);
        Mat answercMat = new Mat(captureMat, answer_c);
        Mat answerdMat = new Mat(captureMat, answer_d);

        q = paddleOCRHelper.Ocr(questionMat);
        a = paddleOCRHelper.Ocr(answeraMat);
        b = paddleOCRHelper.Ocr(answerbMat);
        c = paddleOCRHelper.Ocr(answercMat);
        d = paddleOCRHelper.Ocr(answerdMat);

    }

    private void PrintText()
    {
        _logWindow?.AddLogMessage("DBG", "问题：" + q);
        _logWindow?.AddLogMessage("DBG", "选项A：" + a);
        _logWindow?.AddLogMessage("DBG", "选项B：" + b);
        _logWindow?.AddLogMessage("DBG", "选项C：" + c);
        _logWindow?.AddLogMessage("DBG", "选项D：" + d);
    }

    private void CalOffset()
    {
        int left, top, width, height;
        int leftMumu, topMumu;
        GetWindowPositionAndSize(_displayHwnd, out leftMumu, out topMumu, out width, out height);
        GetWindowPositionAndSize(_gameHwnd, out left, out top, out width, out height);
        offsetX = left - leftMumu;
        offsetY = top - topMumu;
        scale = width/1280.0;
    }

    private void ClickOption()
    {
        switch (bestOption)
        {
            case 'A':
                SendMouseClick(_gameHwnd, (uint)(answer_a_x * scale - offsetX + answer_offset_x * scale), (uint)(answer_a_y * scale - offsetY + answer_offset_y * scale));
                break;
            case 'B':
                SendMouseClick(_gameHwnd, (uint)(answer_b_x * scale - offsetX + answer_offset_x * scale), (uint)(answer_b_y * scale - offsetY + answer_offset_y * scale));
                break;
            case 'C':
                SendMouseClick(_gameHwnd, (uint)(answer_c_x * scale - offsetX + answer_offset_x * scale), (uint)(answer_c_y * scale - offsetY + answer_offset_y * scale));
                break;
            case 'D':
                SendMouseClick(_gameHwnd, (uint)(answer_d_x * scale - offsetX + answer_offset_x * scale), (uint)(answer_d_y * scale - offsetY + answer_offset_y * scale));
                break;
        }
    }

    public int SetAnswerDelay(int  answer_delay)
    {
        if (answer_delay < 0)
        {
            _logWindow?.AddLogMessage("ERR", "答题延迟不能小于0");
            return -1;
        }
        _answerDelay = answer_delay;
        _logWindow?.AddLogMessage("DBG", "答题延迟设置为：" + _answerDelay);
        return 0;
    }

}
