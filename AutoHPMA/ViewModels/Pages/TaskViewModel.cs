using AutoHPMA.GameTask;
using AutoHPMA.GameTask.Permanent;
using AutoHPMA.GameTask.Temporary;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Messages;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using AutoHPMA.Config;
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
using Microsoft.Extensions.Logging;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class TaskViewModel : ObservableObject, INavigationAware
    {
        private readonly AppSettings _settings;
        private readonly ILogger<TaskViewModel> _logger;
        private readonly CookingConfigService _cookingConfigService;
        private bool _isAnyTaskRunning = false;

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
        private Visibility _autoSweetAdventureStartButtonVisibility = Visibility.Visible;
        [ObservableProperty]
        private Visibility _autoSweetAdventureStopButtonVisibility = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _autoCookingStartButtonVisibility = Visibility.Visible;
        [ObservableProperty]
        private Visibility _autoCookingStopButtonVisibility = Visibility.Collapsed;

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
        [NotifyCanExecuteChangedFor(nameof(AutoSweetAdventureStartTriggerCommand))]
        private bool _autoSweetAdventureStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoSweetAdventureStopTriggerCommand))]
        private bool _autoSweetAdventureStopButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoCookingStartTriggerCommand))]
        private bool _autoCookingStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoCookingStopTriggerCommand))]
        private bool _autoCookingStopButtonEnabled = true;

        [ObservableProperty]
        private int _answerDelay = 0;

        [ObservableProperty]
        private bool _joinOthers = false;

        [ObservableProperty]
        private int _autoForbiddenForestTimes = 30;
        [ObservableProperty]
        private int _autoCookingTimes = 2;

        [ObservableProperty]
        private ObservableCollection<string> _teamPositions =
            [
                "Leader",
                "Member"
            ];
        [ObservableProperty]
        private string _selectedTeamPosition = "Leader";

        [ObservableProperty]
        private ObservableCollection<string> _dishes = new();
        [ObservableProperty]
        private string _autoCookingSelectedDish = "黄金海鱼焗饭";

        [ObservableProperty]
        private string _autoCookingSelectedOCR = "Tesseract";

        public ObservableCollection<string> OCRs { get; } = new ObservableCollection<string> { "Tesseract", "PaddleOCR" };

        #endregion

        private IntPtr _displayHwnd => AppContextService.Instance.DisplayHwnd;
        private IntPtr _gameHwnd => AppContextService.Instance.GameHwnd;
        private LogWindow? _logWindow => AppContextService.Instance.LogWindow;
        private WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;


        private IGameTask? _currentTask;
        private AppContextService appContextService;

        public TaskViewModel(AppSettings settings, ILogger<TaskViewModel> logger, CookingConfigService cookingConfigService)
        {
            _settings = settings;
            _logger = logger;
            _cookingConfigService = cookingConfigService;
            
            // 获取单例实例
            appContextService = AppContextService.Instance;
            // 订阅属性变化通知
            appContextService.PropertyChanged += AppContextService_PropertyChanged;

            // 初始化时从设置中加载数据
            AnswerDelay = _settings.AnswerDelay;
            JoinOthers = _settings.JoinOthers;
            AutoForbiddenForestTimes = _settings.AutoForbiddenForestTimes;
            SelectedTeamPosition = _settings.SelectedTeamPosition;
            AutoCookingTimes = _settings.AutoCookingTimes;
            AutoCookingSelectedDish = _settings.AutoCookingSelectedDish;
            AutoCookingSelectedOCR = _settings.AutoCookingSelectedOCR;

            // 加载菜品列表
            LoadDishes();
        }

        private void LoadDishes()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/Cooking/Config");
            if (Directory.Exists(configPath))
            {
                var configFiles = Directory.GetFiles(configPath, "*.json");
                foreach (var file in configFiles)
                {
                    var json = File.ReadAllText(file);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Models.Cooking.DishConfig>(json);
                    if (config != null)
                    {
                        Dishes.Add(config.Name);
                    }
                }
            }
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

        #region 自动社团答题
        private bool CanAutoClubQuizStartTrigger() => AutoClubQuizStartButtonEnabled;

        private bool CheckTaskRunningStatus()
        {
            if (_isAnyTaskRunning)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 提示",
                    Content = "已有其他任务正在运行，请先停止当前任务！",
                };
                var result = uiMessageBox.ShowDialogAsync();
                return true;
            }
            return false;
        }

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStartTrigger))]
        private void OnAutoClubQuizStartTrigger()
        {
            if (CheckTaskRunningStatus()) return;

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

            _isAnyTaskRunning = true;
            AutoClubQuizStartButtonVisibility = Visibility.Collapsed;
            AutoClubQuizStopButtonVisibility = Visibility.Visible;

            var logger = App.GetLogger<AutoClubQuiz>();
            _currentTask = new AutoClubQuiz(logger, _displayHwnd, _gameHwnd);
            _currentTask.SetParameters(new Dictionary<string, object>
            {
                { "AnswerDelay", AnswerDelay },
                { "JoinOthers", JoinOthers }
            });

            // 订阅任务完成事件，当任务完成时更新按钮状态
            _currentTask.TaskCompleted += (sender, e) =>
            {
                AutoClubQuizStartButtonVisibility = Visibility.Visible;
                AutoClubQuizStopButtonVisibility = Visibility.Collapsed;
                _currentTask = null;
                _isAnyTaskRunning = false;
            };
            _currentTask.Start();
        }

        private bool CanAutoClubQuizStopTrigger() => AutoClubQuizStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStopTrigger))]
        private void OnAutoClubQuizStopTrigger()
        {
            AutoClubQuizStartButtonVisibility = Visibility.Visible;
            AutoClubQuizStopButtonVisibility = Visibility.Collapsed;

            _currentTask?.Stop();
            _currentTask = null;
            _isAnyTaskRunning = false;

            GC.Collect();
        }
        #endregion

        #region 自动禁林
        private bool CanAutoForbiddenForestStartTrigger() => AutoForbiddenForestStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoForbiddenForestStartTrigger))]
        private void OnAutoForbiddenForestStartTrigger()
        {
            if (CheckTaskRunningStatus()) return;

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

            _isAnyTaskRunning = true;
            AutoForbiddenForestStartButtonVisibility = Visibility.Collapsed;
            AutoForbiddenForestStopButtonVisibility = Visibility.Visible;

            var logger = App.GetLogger<AutoForbiddenForest>();
            _currentTask = new AutoForbiddenForest(logger, _displayHwnd, _gameHwnd);
            _currentTask.SetParameters(new Dictionary<string, object>
            {
                { "Times", AutoForbiddenForestTimes },
                { "TeamPosition", SelectedTeamPosition }
            });

            // 订阅任务完成事件，当任务完成时更新按钮状态
            _currentTask.TaskCompleted += (sender, e) =>
            {
                AutoForbiddenForestStartButtonVisibility = Visibility.Visible;
                AutoForbiddenForestStopButtonVisibility = Visibility.Collapsed;
                _currentTask = null;
                _isAnyTaskRunning = false;
            };
            _currentTask.Start();
        }

        private bool CanAutoForbiddenForestStopTrigger() => AutoForbiddenForestStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoForbiddenForestStopTrigger))]
        private void OnAutoForbiddenForestStopTrigger()
        {
            AutoForbiddenForestStartButtonVisibility = Visibility.Visible;
            AutoForbiddenForestStopButtonVisibility = Visibility.Collapsed;

            _currentTask?.Stop();
            _currentTask = null;
            _isAnyTaskRunning = false;

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
        #endregion

        #region 自动烹饪
        private bool CanAutoCookingStartTrigger() => AutoCookingStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoCookingStartTrigger))]
        private void OnAutoCookingStartTrigger()
        {
            if (CheckTaskRunningStatus()) return;

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
                    Message = "自动烹饪已启动。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                    Duration = TimeSpan.FromSeconds(3)
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
            }

            _isAnyTaskRunning = true;
            AutoCookingStartButtonVisibility = Visibility.Collapsed;
            AutoCookingStopButtonVisibility = Visibility.Visible;

            var logger = App.GetLogger<AutoCooking>();
            _currentTask = new AutoCooking(logger, _cookingConfigService, _displayHwnd, _gameHwnd);
            _currentTask.SetParameters(new Dictionary<string, object>
            {
                { "Times", AutoCookingTimes },
                { "Dish", AutoCookingSelectedDish },
                { "OCR", AutoCookingSelectedOCR }
            });

            // 订阅任务完成事件，当任务完成时更新按钮状态
            _currentTask.TaskCompleted += (sender, e) =>
            {
                AutoCookingStartButtonVisibility = Visibility.Visible;
                AutoCookingStopButtonVisibility = Visibility.Collapsed;
                _currentTask = null;
                _isAnyTaskRunning = false;
            };
            _currentTask.Start();
        }

        private bool CanAutoCookingStopTrigger() => AutoCookingStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoCookingStopTrigger))]
        private void OnAutoCookingStopTrigger()
        {
            AutoCookingStartButtonVisibility = Visibility.Visible;
            AutoCookingStopButtonVisibility = Visibility.Collapsed;

            _currentTask?.Stop();
            _currentTask = null;
            _isAnyTaskRunning = false;

            GC.Collect();
        }
        #endregion

        #region 自动甜蜜冒险
        private bool CanAutoSweetAdventureStartTrigger() => AutoSweetAdventureStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoSweetAdventureStartTrigger))]
        private void OnAutoSweetAdventureStartTrigger()
        {
            if (CheckTaskRunningStatus()) return;

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
                    Message = "甜蜜冒险已启动。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                    Duration = TimeSpan.FromSeconds(3)
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
            }

            _isAnyTaskRunning = true;
            AutoSweetAdventureStartButtonVisibility = Visibility.Collapsed;
            AutoSweetAdventureStopButtonVisibility = Visibility.Visible;

            var logger = App.GetLogger<AutoSweetAdventure>();
            _currentTask = new AutoSweetAdventure(logger, _displayHwnd, _gameHwnd);
            _currentTask.Start();
        }

        private bool CanAutoSweetAdventureStopTrigger() => AutoSweetAdventureStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoSweetAdventureStopTrigger))]
        private void OnAutoSweetAdventureStopTrigger()
        {
            AutoSweetAdventureStartButtonVisibility = Visibility.Visible;
            AutoSweetAdventureStopButtonVisibility = Visibility.Collapsed;

            _currentTask?.Stop();
            _currentTask = null;
            _isAnyTaskRunning = false;

            GC.Collect();
        }
        #endregion

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }

        partial void OnAnswerDelayChanged(int value)
        {
            _settings.AnswerDelay = value;
            _settings.Save();
        }

        partial void OnJoinOthersChanged(bool value)
        {
            _settings.JoinOthers = value;
            _settings.Save();
        }

        partial void OnAutoForbiddenForestTimesChanged(int value)
        {
            _settings.AutoForbiddenForestTimes = value;
            _settings.Save();
        }

        partial void OnSelectedTeamPositionChanged(string value)
        {
            _settings.SelectedTeamPosition = value;
            _settings.Save();
        }

        partial void OnAutoCookingTimesChanged(int value)
        {
            _settings.AutoCookingTimes = value;
            _settings.Save();
        }

        partial void OnAutoCookingSelectedDishChanged(string value)
        {
            _settings.AutoCookingSelectedDish = value;
            _settings.Save();
        }

        partial void OnAutoCookingSelectedOCRChanged(string value)
        {
            _settings.AutoCookingSelectedOCR = value;
            _settings.Save();
        }

        private string GetDishEnumValue(string dishName)
        {
            return dishName switch
            {
                "黄金海鱼焗饭" => "FishRice",
                "果香烤乳猪" => "RoastedPig",
                "奶油蘑菇炖饭" => "MushroomRisotto",
                _ => "FishRice"
            };
        }
    }
}
