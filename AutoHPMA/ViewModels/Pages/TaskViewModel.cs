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
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Microsoft.Extensions.Logging;

namespace AutoHPMA.ViewModels.Pages
{
    /// <summary>
    /// 任务类型枚举
    /// </summary>
    public enum TaskType
    {
        None,
        AutoClubQuiz,
        AutoForbiddenForest,
        AutoCooking,
        AutoSweetAdventure
    }

    public partial class TaskViewModel : ObservableObject, INavigationAware
    {
        private readonly AppSettings _settings;
        private readonly ILogger<TaskViewModel> _logger;
        private readonly CookingConfigService _cookingConfigService;

        #region Observable Properties
        [ObservableProperty]
        private TaskType _currentTaskType = TaskType.None;

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
        private string _autoCookingSelectedDish = "海鱼黄金焗饭";

        [ObservableProperty]
        private string _autoCookingSelectedOCR = "Tesseract";

        public ObservableCollection<string> OCRs { get; } = new ObservableCollection<string> { "Tesseract", "PaddleOCR" };

        #endregion

        #region 智能按钮状态计算属性
        /// <summary>
        /// 自动社团答题按钮状态
        /// </summary>
        public Visibility AutoClubQuizStartButtonVisibility => 
            CurrentTaskType == TaskType.AutoClubQuiz ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AutoClubQuizStopButtonVisibility => 
            CurrentTaskType == TaskType.AutoClubQuiz ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 自动禁林按钮状态
        /// </summary>
        public Visibility AutoForbiddenForestStartButtonVisibility => 
            CurrentTaskType == TaskType.AutoForbiddenForest ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AutoForbiddenForestStopButtonVisibility => 
            CurrentTaskType == TaskType.AutoForbiddenForest ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 自动烹饪按钮状态
        /// </summary>
        public Visibility AutoCookingStartButtonVisibility => 
            CurrentTaskType == TaskType.AutoCooking ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AutoCookingStopButtonVisibility => 
            CurrentTaskType == TaskType.AutoCooking ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 甜蜜冒险按钮状态
        /// </summary>
        public Visibility AutoSweetAdventureStartButtonVisibility => 
            CurrentTaskType == TaskType.AutoSweetAdventure ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AutoSweetAdventureStopButtonVisibility => 
            CurrentTaskType == TaskType.AutoSweetAdventure ? Visibility.Visible : Visibility.Collapsed;

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

            // 注册停止所有任务的消息接收器
            WeakReferenceMessenger.Default.Register<StopAllTasksMessage>(this, (r, message) =>
            {
                StopAllRunningTasks();
            });

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

        /// <summary>
        /// 停止所有正在运行的任务
        /// </summary>
        private void StopAllRunningTasks()
        {
            if (_currentTask != null)
            {
                _logger.LogInformation("收到停止信号，正在停止当前任务...");
                _currentTask.Stop();
                _currentTask = null;
                
                // 确保在UI线程上执行属性变化通知
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentTaskType = TaskType.None;
                });
                
                GC.Collect();
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
        private bool CheckTaskRunningStatus()
        {
            if (_currentTask != null)
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

        [RelayCommand]
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

            CurrentTaskType = TaskType.AutoClubQuiz;

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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentTaskType = TaskType.None;
                    _currentTask = null;
                });
            };
            _currentTask.Start();
        }

        [RelayCommand]
        private void OnAutoClubQuizStopTrigger()
        {
            _currentTask?.Stop();
            _currentTask = null;
            CurrentTaskType = TaskType.None;

            GC.Collect();
        }
        #endregion

        #region 自动禁林
        [RelayCommand]
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

            CurrentTaskType = TaskType.AutoForbiddenForest;

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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentTaskType = TaskType.None;
                    _currentTask = null;
                });
            };
            _currentTask.Start();
        }

        [RelayCommand]
        private void OnAutoForbiddenForestStopTrigger()
        {
            _currentTask?.Stop();
            _currentTask = null;
            CurrentTaskType = TaskType.None;

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
        [RelayCommand]
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

            CurrentTaskType = TaskType.AutoCooking;

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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentTaskType = TaskType.None;
                    _currentTask = null;
                });
            };
            _currentTask.Start();
        }

        [RelayCommand]
        private void OnAutoCookingStopTrigger()
        {
            _currentTask?.Stop();
            _currentTask = null;
            CurrentTaskType = TaskType.None;

            GC.Collect();
        }
        #endregion

        #region 自动甜蜜冒险
        [RelayCommand]
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

            CurrentTaskType = TaskType.AutoSweetAdventure;

            var logger = App.GetLogger<AutoSweetAdventure>();
            _currentTask = new AutoSweetAdventure(logger, _displayHwnd, _gameHwnd);
            _currentTask.Start();
        }

        [RelayCommand]
        private void OnAutoSweetAdventureStopTrigger()
        {
            _currentTask?.Stop();
            _currentTask = null;
            CurrentTaskType = TaskType.None;

            GC.Collect();
        }
        #endregion

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            // 取消消息注册，避免内存泄漏
            //WeakReferenceMessenger.Default.Unregister<StopAllTasksMessage>(this);
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

        partial void OnCurrentTaskTypeChanged(TaskType value)
        {
            // 通知UI更新所有按钮状态
            OnPropertyChanged(nameof(AutoClubQuizStartButtonVisibility));
            OnPropertyChanged(nameof(AutoClubQuizStopButtonVisibility));
            OnPropertyChanged(nameof(AutoForbiddenForestStartButtonVisibility));
            OnPropertyChanged(nameof(AutoForbiddenForestStopButtonVisibility));
            OnPropertyChanged(nameof(AutoCookingStartButtonVisibility));
            OnPropertyChanged(nameof(AutoCookingStopButtonVisibility));
            OnPropertyChanged(nameof(AutoSweetAdventureStartButtonVisibility));
            OnPropertyChanged(nameof(AutoSweetAdventureStopButtonVisibility));
        }

    }
}
