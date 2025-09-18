using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoHPMA.Models
{
    /// <summary>
    /// 点击动作参数模型
    /// </summary>
    public partial class ClickActionModel : ObservableObject
    {
        [ObservableProperty]
        private int _x = 200;
        
        [ObservableProperty]
        private int _y = 200;
        
        [ObservableProperty]
        private int _interval = 500;
        
        [ObservableProperty]
        private int _times = 1;
        
        [ObservableProperty]
        private string _description = "点击动作";
        
        public ClickActionModel() { }
        
        public ClickActionModel(int x, int y, int interval = 500, int times = 1, string description = "点击动作")
        {
            X = x;
            Y = y;
            Interval = interval;
            Times = times;
            Description = description;
        }
    }
    
    /// <summary>
    /// 拖拽动作参数模型
    /// </summary>
    public partial class DragActionModel : ObservableObject
    {
        [ObservableProperty]
        private int _startX = 200;
        
        [ObservableProperty]
        private int _startY = 200;
        
        [ObservableProperty]
        private int _endX = 400;
        
        [ObservableProperty]
        private int _endY = 400;
        
        [ObservableProperty]
        private int _duration = 500;
        
        [ObservableProperty]
        private string _description = "拖拽动作";
        
        public DragActionModel() { }
        
        public DragActionModel(int startX, int startY, int endX, int endY, int duration = 500, string description = "拖拽动作")
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            Duration = duration;
            Description = description;
        }
    }
    
    /// <summary>
/// 长按动作模型
/// </summary>
public partial class LongPressActionModel : ObservableObject
{
    [ObservableProperty]
    private int _x;

    [ObservableProperty]
    private int _y;

    [ObservableProperty]
    private int _duration = 1000; // 默认长按1秒

    [ObservableProperty]
    private int _interval = 500; // 默认间隔500毫秒

    [ObservableProperty]
    private int _times = 1; // 默认执行1次

    [ObservableProperty]
    private string _description = "";

    public LongPressActionModel() { }

    public LongPressActionModel(int x, int y, int duration, int interval, int times, string description)
    {
        X = x;
        Y = y;
        Duration = duration;
        Interval = interval;
        Times = times;
        Description = description;
    }
}
}