using AutoHPMA.ViewModels.Pages;
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
using Wpf.Ui.Abstractions.Controls;

namespace AutoHPMA.Views.Pages
{
    /// <summary>
    /// TaskPage.xaml 的交互逻辑
    /// </summary>
    public partial class TaskPage : INavigableView<TaskViewModel>
    {
        public TaskViewModel ViewModel { get; set; }

        public TaskPage(TaskViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
