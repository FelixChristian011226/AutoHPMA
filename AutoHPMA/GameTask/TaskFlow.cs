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

namespace AutoHPMA.GameTask
{
    public enum TaskFlowState
    {
        Waiting,
        Gathering,
        Preparing,
        Answering,
        Gapping
    }

    public struct Point
    {
        public int x;
        public int y;
    }

    public class TaskFlow
    {
        private static LogWindow _logWindow;
        private static Bitmap gather,tip,over,club1,club2;
        private static Bitmap[] options = new Bitmap[4];
        private static Bitmap time0,time6,time10,time15,time16,time17,time18,time20;
        private static TaskFlow _taskflow;
        private static Boolean _nextQuestion = false;
        private TaskFlowState _currentState = TaskFlowState.Waiting;
        private readonly SemaphoreSlim workAsyncLock = new SemaphoreSlim(1, 1);

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

        public void Init(LogWindow logWindow)
        {
            _logWindow = logWindow;
            gather = new Bitmap("Assets/Captures/gather.png");
            tip = new Bitmap("Assets/Captures/tip.png");
            over = new Bitmap("Assets/Captures/over.png");
            club1 = new Bitmap("Assets/Captures/club1.png");
            club2 = new Bitmap("Assets/Captures/club2.png");
            options[0] = new Bitmap("Assets/Captures/Option/A.png");
            options[1] = new Bitmap("Assets/Captures/Option/B.png");
            options[2] = new Bitmap("Assets/Captures/Option/C.png");
            options[3] = new Bitmap("Assets/Captures/Option/D.png");
            time0 = new Bitmap("Assets/Captures/Time/0.png");
            time6 = new Bitmap("Assets/Captures/Time/6.png");
            time10 = new Bitmap("Assets/Captures/Time/10.png");
            time15 = new Bitmap("Assets/Captures/Time/15.png");
            time16 = new Bitmap("Assets/Captures/Time/16.png");
            time17 = new Bitmap("Assets/Captures/Time/17.png");
            time18 = new Bitmap("Assets/Captures/Time/18.png");
            time20 = new Bitmap("Assets/Captures/Time/20.png");

        }

        public async Task WorkAsync(nint hwnd, Bitmap bmp)
        {
            Bitmap croppedBmp;
            double similarity;
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();

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
                        bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                        //string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                        //Directory.CreateDirectory(folderPath);
                        //ImageProcessingHelper.SaveBitmapAs(bmp, folderPath, "gather1.png", ImageFormat.Png);


                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1505, 1388, 1598 - 1505, 1474 - 1388);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(gather, croppedBmp));
                        if (similarity > 0.9)
                        {
                            _logWindow?.AddLogMessage("INF", "定位到社团集结，准备开始！");
                            //await ClickAtPositionAsync(1550, 1420);
                            WindowInteractionHelper.SendMouseClick(hwnd, 1550, 1330);
                            await Task.Delay(2000);
                            //await ClickAtPositionAsync(2100, 417);
                            WindowInteractionHelper.SendMouseClick(hwnd, 2100, 327);
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(2100, 417);
                            WindowInteractionHelper.SendMouseClick(hwnd, 2100, 327);
                            roundIndex++;
                            _logWindow?.AddLogMessage("INF", "-----第" + roundIndex + "轮答题-----");
                            _currentState = TaskFlowState.Preparing;
                        }
                        else
                        {
                            _currentState = TaskFlowState.Gapping;
                        }
                        break;

                    case TaskFlowState.Preparing:
                        // 执行准备状态的逻辑...
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1951, 373, 2042 - 1951, 442 - 373);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(tip, croppedBmp));
                        if (similarity > 0.9)
                        {
                            _logWindow?.AddLogMessage("INF", "定位到活动目标，准备答题！");
                            //await ClickAtPositionAsync(1997, 403);
                            WindowInteractionHelper.SendMouseClick(hwnd, 1997, 313);
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(1997, 403);
                            WindowInteractionHelper.SendMouseClick(hwnd, 1997, 313);
                            
                            _currentState = TaskFlowState.Answering;
                        }
                        if (FindTime(bmp))
                            _currentState = TaskFlowState.Answering;
                        break;

                    case TaskFlowState.Answering:
                        // 执行答题状态的逻辑...
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1106, 1452, 1473 - 1106, 1492 - 1452);   //答题结束检验
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(over, croppedBmp));
                        if (similarity > 0.9)
                        {
                            _logWindow?.AddLogMessage("INF", "-----答题结束-----");
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(1287, 1377);
                            WindowInteractionHelper.SendMouseClick(hwnd, 1287, 1287);
                            await Task.Delay(3000);
                            //await ClickAtPositionAsync(750, 900);
                            WindowInteractionHelper.SendMouseClick(hwnd, 750, 810);
                            await Task.Delay(1000);
                            //await ClickAtPositionAsync(750, 900);
                            WindowInteractionHelper.SendMouseClick(hwnd, 750, 810);
                            _currentState = TaskFlowState.Gapping;
                        }

                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1227, 125, 1328 - 1227, 187 - 125);
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
                                //await ClickAtPositionAsync(1000, 1230);
                                WindowInteractionHelper.SendMouseClick(hwnd, 1000, 1140);
                                option = 'A';
                                break;
                            case 1:
                                //await ClickAtPositionAsync(2300, 1230);
                                WindowInteractionHelper.SendMouseClick(hwnd, 2300, 1140);
                                option = 'B';
                                break;
                            case 2:
                                //await ClickAtPositionAsync(1000, 1415);
                                WindowInteractionHelper.SendMouseClick(hwnd, 1000, 1325);
                                option = 'C';
                                break;
                            case 3:
                                //await ClickAtPositionAsync(2300, 1415);
                                WindowInteractionHelper.SendMouseClick(hwnd, 2300, 1325);
                                option = 'D';
                                break;
                            default:
                                option = 'X';
                                break;
                        }
                        _currentState = TaskFlowState.Answering;
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
                        WindowInteractionHelper.SendMouseClick(hwnd, 1111, 1335);   //打开聊天框
                        await Task.Delay(1000);
                        WindowInteractionHelper.SendMouseClick(hwnd, 1500, 800);    //关闭输入框
                        await Task.Delay(1000);

                        bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);

                        // 判断社团聊天窗位置并点击展开
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 538, 787, 90, 45);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(club1, croppedBmp));
                        if (similarity > 0.9)
                        {
                            WindowInteractionHelper.SendMouseClick(hwnd, 580, 814-80);
                        }
                        croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 538, 1096, 90, 45);
                        similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(club2, croppedBmp));
                        if (similarity>0.9)
                        {
                            WindowInteractionHelper.SendMouseClick(hwnd, 580, 1123-80);
                        }

                        await Task.Delay(1000);
                        WindowInteractionHelper.SendMouseClick(hwnd, 92, 76);       //关闭聊天框
                        await Task.Delay(1000);
                        _currentState = TaskFlowState.Gathering;
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
            //A 1032 1148 1250 1232     218 84
            croppedBmp[0] = ImageProcessingHelper.CropBitmap(bmp, 1032, 1148, 1250 - 1032, 1232 - 1148);
            similarity[0] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[0], croppedBmp[0]));
            //B 2282 1148 2500 1232     218 84
            croppedBmp[1] = ImageProcessingHelper.CropBitmap(bmp, 2282, 1148, 2500 - 2282, 1232 - 1148);
            similarity[1] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[1], croppedBmp[1]));
            //C 1032 1326 1250 1410     218 84
            croppedBmp[2] = ImageProcessingHelper.CropBitmap(bmp, 1032, 1326, 1250 - 1032, 1410 - 1326);
            similarity[2] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[2], croppedBmp[2]));
            //D 2282 1326 2500 1410     218 84
            croppedBmp[3] = ImageProcessingHelper.CropBitmap(bmp, 2282, 1326, 2500 - 2282, 1410 - 1326);
            similarity[3] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(options[3], croppedBmp[3]));
            //找到最小相似度下标
            int index = Array.IndexOf(similarity, similarity.Min());
            return index;
        }

        public static Boolean FindTime(Bitmap bmp)
        {
            double[] similarity = new double[7];
            Bitmap croppedBmp;
            croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1227, 125, 1328 - 1227, 187 - 125);
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
            return false;
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
