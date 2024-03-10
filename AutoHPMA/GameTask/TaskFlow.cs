using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoHPMA.GameTask
{
    public enum TaskFlowState
    {
        Waiting,
        Gathering,
        Preparing,
        Answering
    }

    public class TaskFlow
    {
        private static LogWindow _logWindow;
        private static Bitmap gather,tip;
        private static TaskFlow _taskflow;
        private TaskFlowState _currentState = TaskFlowState.Waiting;


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        public void Init(LogWindow logWindow)
        {
            _logWindow = logWindow;
            gather = new Bitmap("Assets/Captures/gather.png");
            tip = new Bitmap("Assets/Captures/tip.png");
        }

        //public void Work(nint hwnd, Bitmap bmp)
        //{
        //    Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1505, 1388, 1598 - 1505, 1474 - 1388);
        //    double similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(gather, croppedBmp));
        //    if(similarity > 0.9)
        //    {
        //        _logWindow?.AddLogMessage("INF", "定位到社团集结，准备开始！");
        //        ClickAtPosition(1550, 1420);

        //    }
        //}

        public async Task WorkAsync(nint hwnd, Bitmap bmp)
        {
            Bitmap croppedBmp;
            double similarity;
            switch (_currentState)
            {
                case TaskFlowState.Waiting:
                    // 执行等待状态的逻辑...
                    _currentState = TaskFlowState.Gathering;
                    break;

                case TaskFlowState.Gathering:
                    // 执行集结状态的逻辑...
                    croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1505, 1388, 1598 - 1505, 1474 - 1388);
                    similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(gather, croppedBmp));
                    if (similarity > 0.9)
                    {
                        _logWindow?.AddLogMessage("INF", "定位到社团集结，准备开始！");
                        await ClickAtPositionAsync(1550, 1420);
                        await Task.Delay(2000);
                        await ClickAtPositionAsync(2100, 417);
                        await Task.Delay(1000);
                        await ClickAtPositionAsync(2100, 417);
                        await Task.Delay(1000);
                        await ClickAtPositionAsync(2100, 417);

                        // 假设此时的下一个状态是准备
                        _currentState = TaskFlowState.Preparing;
                    }
                    break;

                case TaskFlowState.Preparing:
                    // 执行准备状态的逻辑...
                    croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1951, 373, 2042 - 1951, 442 - 373);
                    similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(tip, croppedBmp));
                    if (similarity > 0.9)
                    {
                        _logWindow?.AddLogMessage("INF", "定位到活动目标，准备答题！");
                        await ClickAtPositionAsync(1997, 403);
                        await Task.Delay(1000);
                        await ClickAtPositionAsync(1997, 403);

                        _currentState = TaskFlowState.Answering;
                    }
                    break;

                case TaskFlowState.Answering:
                    // 执行答题状态的逻辑...
                    // 答题结束后可能会返回等待状态或其他状态
                    _currentState = TaskFlowState.Waiting;
                    break;
            }
        }

        public static TaskFlow Instance()
        {
            if (_taskflow == null)
            {
                _taskflow = new TaskFlow();
            }
            return _taskflow;
        }

        //private void ClickAtPosition(int x, int y)
        //{
        //    // 移动鼠标到指定位置
        //    SetCursorPos(x, y);
        //    // 模拟鼠标按下和释放
        //    mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
        //    mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        //}

        private async Task ClickAtPositionAsync(int x, int y)
        {
            // 移动鼠标到指定位置
            SetCursorPos(x, y);
            // 模拟鼠标按下和释放
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            await Task.Delay(100); // 添加一个小的延时确保鼠标按下和释放操作分开
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }

    }
}
