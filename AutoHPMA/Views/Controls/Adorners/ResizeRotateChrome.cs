using System.Windows;
using System.Windows.Controls;

namespace AutoHPMA.Views.Controls.Adorners;

public class ResizeRotateChrome : Control
{
    static ResizeRotateChrome()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeRotateChrome), new FrameworkPropertyMetadata(typeof(ResizeRotateChrome)));
    }
}
