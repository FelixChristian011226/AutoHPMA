using AutoHPMA.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows;

namespace AutoHPMA.Views.Pages;

public partial class LogPage : INavigableView<LogViewModel>
{
    public LogViewModel ViewModel { get; }

    public LogPage(LogViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        // 订阅 ViewModel 的滚动请求事件
        ViewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
        Unloaded += (_, _) => ViewModel.ScrollToBottomRequested -= OnScrollToBottomRequested;
    }

    private void OnScrollToBottomRequested()
    {
        Dispatcher.BeginInvoke(() =>
        {
            LogScrollViewer.ScrollToBottom();
        });
    }
}
