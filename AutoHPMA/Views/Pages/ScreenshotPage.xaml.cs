using AutoHPMA.ViewModels.Pages;
using AutoHPMA.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoHPMA.Views.Pages
{
    /// <summary>
    /// ScreenshotPage.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenshotPage : Page
    {
        private LogWindow _logWindow;
        private readonly ScreenshotViewModel _viewModel;
        public ScreenshotPage(LogWindow logWindow)
        {
            DataContext = _viewModel;
            _logWindow = logWindow;
            _viewModel = new ScreenshotViewModel(logWindow);
            InitializeComponent();
        }
        private void AddToLog_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(InputTextBox.Text);
            if (_logWindow != null && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                _logWindow.AddLogMessage("INF", InputTextBox.Text);
            }
        }

        private void TestScreenshot_Click(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri("/Screenshots/test.png", UriKind.Relative);
            this.TestScreenshotImage.Source = new BitmapImage(new Uri("pack://application:,,,/Screenshots/test.png"));
        }
    }
}
