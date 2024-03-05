using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoHPMA.Config;

[Serializable]
//public partial class LogWindowConfig : ObservableObject
//{
//    //遮罩窗口启用
//    [ObservableProperty] private bool _logWindowEnabled = true;
//    [ObservableProperty] private int _logWindowLeft = 0;
//    [ObservableProperty] private int _logWindowTop = 0;
//}

public class LogWindowConfig : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _logWindowEnabled = true;
    private int _logWindowLeft = 0;
    private int _logWindowTop = 0;

    public bool LogWindowEnabled
    {
        get => _logWindowEnabled;
        set
        {
            if (_logWindowEnabled != value)
            {
                _logWindowEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogWindowEnabled)));
            }
        }
    }

    public int LogWindowLeft
    {
        get => _logWindowLeft;
        set
        {
            if (_logWindowLeft != value)
            {
                _logWindowLeft = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogWindowLeft)));
            }
        }
    }

    public int LogWindowTop
    {
        get => _logWindowTop;
        set
        {
            if (_logWindowTop != value)
            {
                _logWindowTop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogWindowTop)));
            }
        }
    }

}
