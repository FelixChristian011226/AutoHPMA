using AutoHPMA.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows;
using System.Windows.Threading;

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
        
        // 页面加载后滚动到底部
        Loaded += (_, _) => ScrollToBottom();
    }

    private void OnScrollToBottomRequested()
    {
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        // 使用 ContextIdle 优先级确保 UI 完全渲染后再滚动
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (LogListBox.Items.Count > 0)
            {
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
            }
        });
    }
}
