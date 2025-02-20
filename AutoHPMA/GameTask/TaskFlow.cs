using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.IO;
using Vanara.PInvoke;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.Marshalling;

namespace AutoHPMA.GameTask
{
    public enum TaskFlowState
    {
        Waiting,
        Gathering,
        Preparing,
        Answering,
        Gapping,
        Stopping
    }

    public struct Point
    {
        public int x;
        public int y;
    }

    public class TaskFlow
    {
        private static LogWindow _logWindow;
        private static JObject config;
        private static Bitmap gather,tip,over,club1,club2;
        private static Bitmap[] options = new Bitmap[4];
        private static Bitmap time0,time6,time10,time15,time16,time17,time18,time20;
        private static TaskFlow _taskflow;
        private static Boolean _nextQuestion = false;
        private TaskFlowState _currentState = TaskFlowState.Waiting;
        private readonly SemaphoreSlim workAsyncLock = new SemaphoreSlim(1, 1);

        int launch_option;

        int questionIndex = 0;
        int roundIndex = 0;
        char option = 'X';


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        public void Reset()
        {
            _currentState = TaskFlowState.Waiting;
        }

        public void Stop()
        {
            _currentState = TaskFlowState.Stopping;
        }

        public void Init(LogWindow logWindow, int op)
        {
            String address = "";
            string json;
            _logWindow = logWindow;
            launch_option = op;

            switch (op)
            {
                case 0:
                    address = "Assets/Captures/Official/";
                    json = File.ReadAllText("Config/official_config.json");
                    config = JObject.Parse(json);
                    break;
                case 1:
                    address = "Assets/Captures/Mumu/";
                    json = File.ReadAllText("Config/mumu_config.json");
                    config = JObject.Parse(json);
                    break;
                case 2:
                    address = "Assets/Captures/Mumu1080/";
                    json = File.ReadAllText("Config/mumu1080_config.json");
                    config = JObject.Parse(json);
                    break;
            }

            gather = new Bitmap(address+"gather.png");
            tip = new Bitmap(address+"tip.png");
            over = new Bitmap(address+"over.png");
            club1 = new Bitmap(address+"club1.png");
            club2 = new Bitmap(address+"club2.png");
            options[0] = new Bitmap(address+"Option/A.png");
            options[1] = new Bitmap(address+"Option/B.png");
            options[2] = new Bitmap(address+"Option/C.png");
            options[3] = new Bitmap(address+"Option/D.png");
            time0 = new Bitmap(address+"Time/0.png");
            time6 = new Bitmap(address+"Time/6.png");
            time10 = new Bitmap(address+"Time/10.png");
            time15 = new Bitmap(address+"Time/15.png");
            time16 = new Bitmap(address+"Time/16.png");
            time17 = new Bitmap(address+"Time/17.png");
            time18 = new Bitmap(address+"Time/18.png");
            time20 = new Bitmap(address+"Time/20.png");

        }

        public async Task WorkAsync(nint hwnd, nint _targetHwnd)
        {
            Bitmap bmp,croppedBmp;
            double similarity;
            int x, y, w, h;
            uint clickX, clickY;

            await workAsyncLock.WaitAsync();

            try
            {
                switch (_currentState)
                {
                    case TaskFlowState.Waiting:
                        // 执行等待状态的逻辑...
                        _currentState = TaskFlowState.Gathering;
                        break;

                    case TaskFlowState.Gathering:
                        // 执行集结状态的逻辑...
                        bmp = Capture(_targetHwnd, launch_option);
                        //string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                        //Directory.CreateDirectory(folderPath);
                        //ImageProcessingHelper.SaveBitmapAs(bmp, folderPath, "gather1.png", ImageFormat.Png);

                        x = (int)config["Gathering"]["gather_pic"]["x"];
                        y = (int)config["Gathering"]["gather_pic"]["y"];
                        w = (int)config["Gathering"]["gather_pic"]["w"];
                        h = (int)config["Gathering"]["gather_pic"]["h"];
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(gather, croppedBmp));
                        if (similarity > 0.9)
                        {
                            _logWindow?.AddLogMessage("INF", "定位到社团集结，准备开始！");
                            clickX = (uint)config["Gathering"]["gather_click1"]["x"];
                            clickY = (uint)config["Gathering"]["gather_click1"]["y"];
                            //await ClickAtPositionAsync(1550, 1420);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            await Task.Delay(2000);
                            clickX = (uint)config["Gathering"]["gather_click2"]["x"];
                            clickY = (uint)config["Gathering"]["gather_click2"]["y"];
                            //await ClickAtPositionAsync(2100, 417);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(2100, 417);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            roundIndex++;
                            _logWindow?.AddLogMessage("INF", "-----第" + roundIndex + "轮答题-----");
                            _currentState = TaskFlowState.Preparing;
                        }
                        else
                        {
                            _currentState = TaskFlowState.Gapping;
                        }
                        bmp.Dispose();
                        croppedBmp.Dispose();
                        break;

                    case TaskFlowState.Preparing:
                        // 执行准备状态的逻辑...
                        bmp = Capture(_targetHwnd, launch_option);
                        x = (int)config["Preparing"]["tip_pic"]["x"];
                        y = (int)config["Preparing"]["tip_pic"]["y"];
                        w = (int)config["Preparing"]["tip_pic"]["w"];
                        h = (int)config["Preparing"]["tip_pic"]["h"];
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(tip, croppedBmp));
                        if (similarity > 0.9)
                        {
                            _logWindow?.AddLogMessage("INF", "定位到活动目标，准备答题！");
                            clickX = (uint)config["Preparing"]["tip_click1"]["x"];
                            clickY = (uint)config["Preparing"]["tip_click1"]["y"];
                            //await ClickAtPositionAsync(1997, 403);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(1997, 403);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            
                            _currentState = TaskFlowState.Answering;
                        }
                        if (FindTime(bmp))
                            _currentState = TaskFlowState.Answering;
                        bmp.Dispose();
                        croppedBmp.Dispose();
                        break;

                    case TaskFlowState.Answering:
                        // 执行答题状态的逻辑...
                        bmp = Capture(_targetHwnd, launch_option);
                        x = (int)config["Answering"]["over_pic"]["x"];
                        y = (int)config["Answering"]["over_pic"]["y"];
                        w = (int)config["Answering"]["over_pic"]["w"];
                        h = (int)config["Answering"]["over_pic"]["h"];
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);   //答题结束检验
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(over, croppedBmp));
                        if (similarity > 0.9)
                        {
                            _logWindow?.AddLogMessage("INF", "-----答题结束-----");
                            await Task.Delay(1000);
                            clickX = (uint)config["Answering"]["over_click1"]["x"];
                            clickY = (uint)config["Answering"]["over_click1"]["y"];
                            //await ClickAtPositionAsync(1287, 1377);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            await Task.Delay(3000);
                            clickX = (uint)config["Answering"]["over_click2"]["x"];
                            clickY = (uint)config["Answering"]["over_click2"]["y"];
                            //await ClickAtPositionAsync(750, 900);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(750, 900);
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                            _currentState = TaskFlowState.Gapping;
                        }
                        x = (int)config["Answering"]["time0_pic"]["x"];
                        y = (int)config["Answering"]["time0_pic"]["y"];
                        w = (int)config["Answering"]["time0_pic"]["w"];
                        h = (int)config["Answering"]["time0_pic"]["h"];
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time0, croppedBmp));
                        if (_nextQuestion && similarity > 0.9)
                        {
                            questionIndex++;
                            _logWindow?.AddLogMessage("INF", "第" + questionIndex + "题，选" + option + "。");
                            _nextQuestion = false;
                        }

                        if (!FindTime(bmp))
                            break;

                        _nextQuestion = true;
                        switch (FindAnswer(bmp))
                        {
                            case 0:
                                clickX = (uint)config["Answering"]["option_A"]["x"];
                                clickY = (uint)config["Answering"]["option_A"]["y"];
                                //await ClickAtPositionAsync(1000, 1230);
                                WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                                option = 'A';
                                break;
                            case 1:
                                clickX = (uint)config["Answering"]["option_B"]["x"];
                                clickY = (uint)config["Answering"]["option_B"]["y"];
                                //await ClickAtPositionAsync(2300, 1230);
                                WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                                option = 'B';
                                break;
                            case 2:
                                clickX = (uint)config["Answering"]["option_C"]["x"];
                                clickY = (uint)config["Answering"]["option_C"]["y"];
                                //await ClickAtPositionAsync(1000, 1415);
                                WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                                option = 'C';
                                break;
                            case 3:
                                clickX = (uint)config["Answering"]["option_D"]["x"];
                                clickY = (uint)config["Answering"]["option_D"]["y"];
                                //await ClickAtPositionAsync(2300, 1415);
                                WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                                option = 'D';
                                break;
                            default:
                                option = 'X';
                                break;
                        }
                        _currentState = TaskFlowState.Answering;
                        bmp.Dispose();
                        croppedBmp.Dispose();
                        break;

                    case TaskFlowState.Gapping:
                        // 执行间隔状态的逻辑...
                        questionIndex = 0;
                        _logWindow?.AddLogMessage("INF", "等待下一场答题...");
                        for (int i = 15; i > 0; i--)
                        {
                            _logWindow?.AddLogMessage("INF", "还剩" + i + "秒...");
                            await Task.Delay(1000);
                            _logWindow?.DeleteLastLogMessage();
                        }
                        clickX = (uint)config["Gapping"]["gap_click1"]["x"];
                        clickY = (uint)config["Gapping"]["gap_click1"]["y"];
                        WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);   //打开聊天框
                        await Task.Delay(1000);
                        clickX = (uint)config["Gapping"]["gap_click2"]["x"];
                        clickY = (uint)config["Gapping"]["gap_click2"]["y"];
                        WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);    //关闭输入框
                        await Task.Delay(2000);

                        bmp = Capture(_targetHwnd, launch_option);

                        // 判断社团聊天窗位置并点击展开
                        x = (int)config["Gapping"]["club1_pic"]["x"];
                        y = (int)config["Gapping"]["club1_pic"]["y"];
                        w = (int)config["Gapping"]["club1_pic"]["w"];
                        h = (int)config["Gapping"]["club1_pic"]["h"];
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(club1, croppedBmp));  
                        if (similarity > 0.9)
                        {
                            clickX = (uint)config["Gapping"]["club1_click"]["x"];
                            clickY = (uint)config["Gapping"]["club1_click"]["y"];
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                        }
                        x = (int)config["Gapping"]["club2_pic"]["x"];
                        y = (int)config["Gapping"]["club2_pic"]["y"];
                        w = (int)config["Gapping"]["club2_pic"]["w"];
                        h = (int)config["Gapping"]["club2_pic"]["h"];
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(club2, croppedBmp));
                        if (similarity>0.9)
                        {
                            clickX = (uint)config["Gapping"]["club2_click"]["x"];
                            clickY = (uint)config["Gapping"]["club2_click"]["y"];
                            WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);
                        }

                        await Task.Delay(1000);
                        clickX = (uint)config["Gapping"]["close_click"]["x"];
                        clickY = (uint)config["Gapping"]["close_click"]["y"];
                        WindowInteractionHelper.SendMouseClick(hwnd, clickX, clickY);       //关闭聊天框
                        await Task.Delay(1000);
                        _currentState = TaskFlowState.Gathering;
                        bmp.Dispose();
                        croppedBmp.Dispose();
                        break;

                    case TaskFlowState.Stopping:
                        // 执行停止状态的逻辑...
                        _logWindow?.AddLogMessage("INF", "任务已停止！");
                        break;

                }
            }
            finally
            {
                workAsyncLock.Release();
            }

            
        }

        public static int FindAnswer(Bitmap bmp)
        {
            Bitmap[] croppedBmp = new Bitmap[4];
            double[] similarity = new double[4];
            int x, y, w, h;
            
            x = (int)config["Answering"]["A_pic"]["x"];
            y = (int)config["Answering"]["A_pic"]["y"];
            w = (int)config["Answering"]["A_pic"]["w"];
            h = (int)config["Answering"]["A_pic"]["h"];
            croppedBmp[0] = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
            similarity[0] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[0], croppedBmp[0]));
            croppedBmp[0].Dispose();
            
            x = (int)config["Answering"]["B_pic"]["x"];
            y = (int)config["Answering"]["B_pic"]["y"];
            w = (int)config["Answering"]["B_pic"]["w"];
            h = (int)config["Answering"]["B_pic"]["h"];
            croppedBmp[1] = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
            similarity[1] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[1], croppedBmp[1]));
            croppedBmp[1].Dispose();

            x = (int)config["Answering"]["C_pic"]["x"];
            y = (int)config["Answering"]["C_pic"]["y"];
            w = (int)config["Answering"]["C_pic"]["w"];
            h = (int)config["Answering"]["C_pic"]["h"];
            croppedBmp[2] = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
            similarity[2] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[2], croppedBmp[2]));
            croppedBmp[2].Dispose();

            x = (int)config["Answering"]["D_pic"]["x"];
            y = (int)config["Answering"]["D_pic"]["y"];
            w = (int)config["Answering"]["D_pic"]["w"];
            h = (int)config["Answering"]["D_pic"]["h"];
            croppedBmp[3] = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
            similarity[3] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[3], croppedBmp[3]));
            croppedBmp[3].Dispose();

            //找到最小相似度下标
            int index = Array.IndexOf(similarity, similarity.Min());
            return index;
        }

        public static Boolean FindTime(Bitmap bmp)
        {
            double[] similarity = new double[7];
            int x, y, w, h;
            x = (int)config["Answering"]["time0_pic"]["x"];
            y = (int)config["Answering"]["time0_pic"]["y"];
            w = (int)config["Answering"]["time0_pic"]["w"];
            h = (int)config["Answering"]["time0_pic"]["h"];
            Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, x, y, w, h);
            similarity[0] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time6, croppedBmp));
            similarity[1] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time10, croppedBmp));
            similarity[2] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time15, croppedBmp));
            similarity[3] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time16, croppedBmp));
            similarity[4] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time17, croppedBmp));
            similarity[5] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time18, croppedBmp));
            similarity[6] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(time20, croppedBmp));
            for (int i = 0; i < 7; i++)
            {
                if (similarity[i] > 0.9)
                {
                    return true;
                }
            }
            croppedBmp.Dispose();
            return false;
        }

        public static Bitmap Capture(nint hwnd, int op)
        {
            switch (op)
            {
                case 0:
                    return BitBltCaptureHelper.Capture(hwnd);
                case 1:
                    return ScreenCaptureHelper.CaptureWindow(hwnd);
                case 2:
                    return BitBltCaptureHelper.Capture(hwnd);
            }
            return ScreenCaptureHelper.CaptureWindow(hwnd);

        }

        public static TaskFlow Instance()
        {
            if (_taskflow == null)
            {
                _taskflow = new TaskFlow();
            }
            return _taskflow;
        }

    }
}
