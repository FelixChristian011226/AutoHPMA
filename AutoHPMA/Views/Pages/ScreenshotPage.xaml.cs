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
        private readonly ScreenshotViewModel _viewModel;
        public ScreenshotPage()
        {
            InitializeComponent();
            _viewModel = new ScreenshotViewModel();
            DataContext = _viewModel;
        }

    }
}
