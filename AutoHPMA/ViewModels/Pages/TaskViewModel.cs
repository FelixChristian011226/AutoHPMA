using AutoHPMA.GameTask;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Messages;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class TaskViewModel : ObservableObject, INavigationAware
    {

        #region Observable Properties
        [ObservableProperty] 
        private Visibility _autoClubQuizStartButtonVisibility = Visibility.Visible;
        [ObservableProperty] 
        private Visibility _autoClubQuizStopButtonVisibility = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _autoForbiddenForestStartButtonVisibility = Visibility.Visible;
        [ObservableProperty]
        private Visibility _autoForbiddenForestStopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStartTriggerCommand))]
        private bool _autoClubQuizStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStopTriggerCommand))]
        private bool _autoClubQuizStopButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoForbiddenForestStartTriggerCommand))]
        private bool _autoForbiddenForestStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoForbiddenForestStopTriggerCommand))]
        private bool _autoForbiddenForestStopButtonEnabled = true;

        [ObservableProperty]
        private int _answerDelay = 0;

        [ObservableProperty]
        private ObservableCollection<string> _gatherRefreshModes =
            [
                "ChatBox",
                "Badge"
            ];
        [ObservableProperty]
        private string _selectedGatherRefreshMode = "Badge";

        [ObservableProperty]
        private int _autoForbiddenForestTimes = 30;

        [ObservableProperty]
        private ObservableCollection<string> _teamPositions =
            [
                "Leader",
                "Member"
            ];
        [ObservableProperty]
        private string _selectedTeamPosition = "Leader";

        #endregion

        private IntPtr _displayHwnd => AppContextService.Instance.DisplayHwnd;
        private IntPtr _gameHwnd => AppContextService.Instance.GameHwnd;
        private LogWindow? _logWindow => AppContextService.Instance.LogWindow;
        private WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;


        private AutoClubQuiz? _autoClubQuiz;
        private AutoForbiddenForest? _autoForbiddenForest;
        private AppContextService appContextService;

        public TaskViewModel()
        {
            // 获取单例实例
            appContextService = AppContextService.Instance;
            // 订阅属性变化通知
            appContextService.PropertyChanged += AppContextService_PropertyChanged;

            // 初始化时从设置中加载数据
            AnswerDelay = Properties.Settings.Default.AnswerDelay;
            SelectedGatherRefreshMode = Properties.Settings.Default.SelectedGatherRefreshMode;

            AutoForbiddenForestTimes = Properties.Settings.Default.AutoForbiddenForestTimes;
            SelectedTeamPosition = Properties.Settings.Default.SelectedTeamPosition;




        }

        private void AppContextService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppContextService.LogWindow) ||
                e.PropertyName == nameof(AppContextService.DisplayHwnd) ||
                e.PropertyName == nameof(AppContextService.GameHwnd) ||
                e.PropertyName == nameof(AppContextService.Capture))
            {
                // 当共享数据有更新时执行相应操作
                // CheckRequiredParameters();
            }
        }

        private bool CanAutoClubQuizStartTrigger() => AutoClubQuizStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStartTrigger))]
        private void OnAutoClubQuizStartTrigger()
        {

            if (_gameHwnd == IntPtr.Zero || _displayHwnd == IntPtr.Zero || _capture == null || _logWindow == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 错误",
                    Content = "任务启动失败。请先启动截图器!",
                };
                var result = uiMessageBox.ShowDialogAsync();
                return;
            }
            else
            {
                var snackbarInfo = new SnackbarInfo
                {
                    Title = "启动成功",
                    Message = "自动社团答题已启动。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                    Duration = TimeSpan.FromSeconds(3)
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
            }

            // 保存设置
            Properties.Settings.Default.AnswerDelay = AnswerDelay;
            Properties.Settings.Default.SelectedGatherRefreshMode = SelectedGatherRefreshMode;
            Properties.Settings.Default.Save();

            AutoClubQuizStartButtonVisibility = Visibility.Collapsed;
            AutoClubQuizStopButtonVisibility = Visibility.Visible;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---社团答题任务已启动---[/Aquamarine]");

            _autoClubQuiz = new AutoClubQuiz(_displayHwnd, _gameHwnd);
            _autoClubQuiz.SetAnswerDelay(AnswerDelay);
            _autoClubQuiz.SetGatherRefreshMode(SelectedGatherRefreshMode);
            _autoClubQuiz.Start();

        }

        private bool CanAutoClubQuizStopTrigger() => AutoClubQuizStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStopTrigger))]
        private void OnAutoClubQuizStopTrigger()
        {
            AutoClubQuizStartButtonVisibility = Visibility.Visible;
            AutoClubQuizStopButtonVisibility = Visibility.Collapsed;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---社团答题任务已终止---[/Aquamarine]");

            _autoClubQuiz?.Stop();
            _autoClubQuiz = null;
            GC.Collect();
        }

        private bool CanAutoForbiddenForestStartTrigger() => AutoForbiddenForestStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoForbiddenForestStartTrigger))]
        private void OnAutoForbiddenForestStartTrigger()
        {

            if (_gameHwnd == IntPtr.Zero || _displayHwnd == IntPtr.Zero || _capture == null || _logWindow == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 错误",
                    Content = "任务启动失败。请先启动截图器!",
                };
                var result = uiMessageBox.ShowDialogAsync();
                return;
            }
            else
            {
                var snackbarInfo = new SnackbarInfo
                {
                    Title = "启动成功",
                    Message = "自动禁林已启动。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                    Duration = TimeSpan.FromSeconds(3)
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
            }

            // 保存设置
            Properties.Settings.Default.AutoForbiddenForestTimes = AutoForbiddenForestTimes;
            Properties.Settings.Default.SelectedTeamPosition = SelectedTeamPosition;
            Properties.Settings.Default.Save();

            AutoForbiddenForestStartButtonVisibility = Visibility.Collapsed;
            AutoForbiddenForestStopButtonVisibility = Visibility.Visible;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---禁林任务已启动---[/Aquamarine]");

            _autoForbiddenForest = new AutoForbiddenForest(_displayHwnd, _gameHwnd);
            _autoForbiddenForest.SetAutoForbiddenForestTimes(AutoForbiddenForestTimes);
            _autoForbiddenForest.SetTeamPosition(SelectedTeamPosition);
            _autoForbiddenForest.Start();

        }

        private bool CanAutoForbiddenForestStopTrigger() => AutoForbiddenForestStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoForbiddenForestStopTrigger))]
        private void OnAutoForbiddenForestStopTrigger()
        {
            AutoForbiddenForestStartButtonVisibility = Visibility.Visible;
            AutoForbiddenForestStopButtonVisibility = Visibility.Collapsed;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---禁林任务已终止---[/Aquamarine]");

            _autoForbiddenForest?.Stop();
            _autoForbiddenForest = null;
            GC.Collect();
        }

        [RelayCommand]
        private void OnOpenQuestionBank(object sender)
        {
            var questionBankPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/ClubQuiz");
            if (!Directory.Exists(questionBankPath))
            {
                Directory.CreateDirectory(questionBankPath);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = questionBankPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}
