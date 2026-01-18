using AutoHPMA.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using Wpf.Ui.Abstractions.Controls;

namespace AutoHPMA.Views.Pages
{
    /// <summary>
    /// TestPage.xaml 的交互逻辑
    /// </summary>
    public partial class TestPage : INavigableView<TestViewModel>
    {

        public TestViewModel ViewModel { get; set; }
        public TestPage(TestViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        /// <summary>
        /// 显示放大的图像
        /// </summary>
        private void ShowZoomedImage(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Source != null)
            {
                ZoomedImage.Source = image.Source;
                ImageViewerPopup.IsOpen = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 关闭图像查看弹窗
        /// </summary>
        private void ImageViewerOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ImageViewerPopup.IsOpen = false;
            ZoomedImage.Source = null;
        }

        /// <summary>
        /// 点击颜色预览打开调色盘
        /// </summary>
        private void ColorPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.OpenColorPickerCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 将 DataGrid 的 AlternationIndex 转换为从1开始的序号
    /// </summary>
    public class IndexConverter : IValueConverter
    {
        public static readonly IndexConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
                return (index + 1).ToString();
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
