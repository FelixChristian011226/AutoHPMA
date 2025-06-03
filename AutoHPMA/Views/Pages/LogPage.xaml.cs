using AutoHPMA.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace AutoHPMA.Views.Pages
{
    public partial class LogPage : INavigableView<LogViewModel>
    {
        public LogViewModel ViewModel { get; }

        public LogPage(LogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
} 