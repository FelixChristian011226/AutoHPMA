using System;
using System.IO;
using System.Threading.Tasks;
using AutoHPMA.Config;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using AutoHPMA.ViewModels.Pages;
using Microsoft.Extensions.Logging;
using AutoHPMA.Services;
using Wpf.Ui.Controls;
using OpenCvSharp.Extensions;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class HotkeySettingsViewModel : ObservableObject
    {
        private readonly AppSettings _settings;
        private readonly ILogger<HotkeySettingsViewModel> _logger;

        [ObservableProperty]
        private ObservableCollection<HotkeyBinding> _hotkeyBindings;

        [ObservableProperty]
        private HotkeyBinding _selectedBinding;

        [ObservableProperty]
        private bool _isWaitingForKey;

        private HotkeyManager? _hotkeyManager;
        private Dictionary<string, int> _hotkeyIds = new();

        public HotkeySettingsViewModel(AppSettings settings, ILogger<HotkeySettingsViewModel> logger)
        {
            _settings = settings;
            _logger = logger;
            _hotkeyBindings = new ObservableCollection<HotkeyBinding>();
            LoadHotkeyBindings();
        }

        public void SetHotkeyManager(HotkeyManager manager)
        {
            _hotkeyManager = manager;
            _hotkeyManager.HotkeyPressed += (s, e) => ExecuteHotkeyAction(GetActionNameByHotkey(e.Modifiers, e.Key));
            RegisterAllHotkeys();
        }

        private void LoadHotkeyBindings()
        {
            // 主要程序
            HotkeyBindings.Add(new HotkeyBinding { Name = "AutoHPMA", Description = "启动或终止AutoHPMA", Modifiers = ModifierKeys.None, Key = Key.None, Group = "主要程序" });
            // 基础功能
            HotkeyBindings.Add(new HotkeyBinding { Name = "截图", Description = "截图当前游戏页面", Modifiers = ModifierKeys.None, Key = Key.None, Group = "基础功能" });

            // 任务启动
            
            HotkeyBindings.Add(new HotkeyBinding { Name = "社团答题", Description = "开始/停止 社团答题", Modifiers = ModifierKeys.None, Key = Key.None, Group = "任务启动" });
            HotkeyBindings.Add(new HotkeyBinding { Name = "禁林探索", Description = "开始/停止 禁林探索", Modifiers = ModifierKeys.None, Key = Key.None, Group = "任务启动" });
            HotkeyBindings.Add(new HotkeyBinding { Name = "自动烹饪", Description = "开始/停止 自动烹饪", Modifiers = ModifierKeys.None, Key = Key.None, Group = "任务启动" });
            HotkeyBindings.Add(new HotkeyBinding { Name = "甜蜜冒险", Description = "开始/停止 甜蜜冒险", Modifiers = ModifierKeys.None, Key = Key.None, Group = "任务启动" });

            // 从设置中加载保存的热键
            foreach (var binding in HotkeyBindings)
            {
                if (_settings.HotkeyBindings.TryGetValue(binding.Name, out var hotkeyStr))
                {
                    ParseHotkeyString(hotkeyStr, out var mod, out var key);
                    binding.Modifiers = mod;
                    binding.Key = key;
                    _logger.LogDebug($"Loaded hotkey binding: {binding.Name} -> {mod}+{key}");
                }
            }
        }

        public void ChangeHotkey(HotkeyBinding binding)
        {
            SelectedBinding = binding;
            IsWaitingForKey = true;
        }

        public void OnKeyDown(Key key, ModifierKeys modifiers)
        {
            _logger.LogDebug($"OnKeyDown called with key={key}, modifiers={modifiers}");
            if (!IsWaitingForKey || SelectedBinding == null)
                return;

            if (key == Key.Escape)
            {
                SelectedBinding.Key = Key.None;
                SelectedBinding.Modifiers = ModifierKeys.None;
                IsWaitingForKey = false;
                _settings.HotkeyBindings[SelectedBinding.Name] = "";
                _settings.Save();
                RegisterAllHotkeys();
                _logger.LogDebug($"Cancelled hotkey binding for: {SelectedBinding.Name}");
                return;
            }

            if (HotkeyBindings.Any(b => b != SelectedBinding && b.Key == key && b.Modifiers == modifiers))
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 热键冲突",
                    Content = "该热键已被其他功能使用，请选择其他按键。",
                };
                _ = uiMessageBox.ShowDialogAsync();
                return;
            }

            SelectedBinding.Key = key;
            SelectedBinding.Modifiers = modifiers;
            IsWaitingForKey = false;
            _settings.HotkeyBindings[SelectedBinding.Name] = HotkeyToString(modifiers, key);
            _settings.Save();
            RegisterAllHotkeys();
            _logger.LogInformation($"Set hotkey binding: {SelectedBinding.Name} -> {HotkeyToString(modifiers, key)}");
        }

        private void RegisterAllHotkeys()
        {
            if (_hotkeyManager == null) return;
            foreach (var id in _hotkeyIds.Values)
                _hotkeyManager.UnregisterHotKey(id);
            _hotkeyIds.Clear();
            foreach (var binding in HotkeyBindings)
            {
                if (binding.Key != Key.None)
                {
                    try
                    {
                        int id = _hotkeyManager.RegisterHotKey(binding.Modifiers, binding.Key);
                        _hotkeyIds[binding.Name] = id;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"注册热键失败: {binding.Name}");
                    }
                }
            }
        }

        private string HotkeyToString(ModifierKeys mod, Key key)
        {
            string s = "";
            if ((mod & ModifierKeys.Control) != 0) s += "Ctrl+";
            if ((mod & ModifierKeys.Alt) != 0) s += "Alt+";
            if ((mod & ModifierKeys.Shift) != 0) s += "Shift+";
            if ((mod & ModifierKeys.Windows) != 0) s += "Win+";
            s += key.ToString();
            return s;
        }

        private void ParseHotkeyString(string str, out ModifierKeys mod, out Key key)
        {
            mod = ModifierKeys.None;
            key = Key.None;
            if (string.IsNullOrEmpty(str)) return;
            var parts = str.Split('+');
            foreach (var p in parts)
            {
                if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) mod |= ModifierKeys.Control;
                else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mod |= ModifierKeys.Alt;
                else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mod |= ModifierKeys.Shift;
                else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase)) mod |= ModifierKeys.Windows;
                else if (Enum.TryParse<Key>(p, out var k)) key = k;
            }
        }

        private string GetActionNameByHotkey(ModifierKeys mod, Key key)
        {
            var binding = HotkeyBindings.FirstOrDefault(b => b.Key == key && b.Modifiers == mod);
            return binding?.Name ?? string.Empty;
        }

        public void ExecuteHotkeyAction(string actionName)
        {
            switch (actionName)
            {
                case "截图":
                {
                    var gameHwnd = AppContextService.Instance.GameHwnd;
                    var capture = AppContextService.Instance.Capture;
                    
                    if (capture == null)
                    {
                        var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "⚠️ 错误",
                            Content = "截图失败。请先启动截图器!",
                        };
                        _ = uiMessageBox.ShowDialogAsync();
                        break;
                    }
                    
                    try
                    {
                        Task.Delay(100).Wait();
                        using (var frame = capture.Capture())
                        {
                            if (frame != null)
                            {
                                var bitmap = frame.ToBitmap();
                                
                                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                                Directory.CreateDirectory(folderPath);
                                
                                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                string filename = $"screenshot_{timestamp}.png";
                                string fullPath = Path.Combine(folderPath, filename);
                                
                                bitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                                bitmap.Dispose();
                                
                                _logger.LogInformation($"截图已保存到: {fullPath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "截图保存失败");
                    }
                    break;
                }
                case "AutoHPMA":
                {
                    var dashboardVM = App.Services.GetService(typeof(DashboardViewModel)) as DashboardViewModel;
                    if (dashboardVM != null)
                    {
                        // 判断当前状态，切换启动/终止
                        if (dashboardVM.StartButtonVisibility == Visibility.Visible && dashboardVM.StartButtonEnabled)
                        {
                            var startCmd = dashboardVM.GetType().GetMethod("OnStartTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            startCmd?.Invoke(dashboardVM, null);
                        }
                        else if (dashboardVM.StopButtonVisibility == Visibility.Visible && dashboardVM.StopButtonEnabled)
                        {
                            var stopCmd = dashboardVM.GetType().GetMethod("OnStopTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            stopCmd?.Invoke(dashboardVM, null);
                        }
                    }
                    break;
                }
                case "社团答题":
                {
                    var taskVM = App.Services.GetService(typeof(TaskViewModel)) as TaskViewModel;
                    if (taskVM != null)
                    {
                        if (taskVM.AutoClubQuizStartButtonVisibility == Visibility.Visible)
                        {
                            var startCmd = taskVM.GetType().GetMethod("OnAutoClubQuizStartTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            startCmd?.Invoke(taskVM, null);
                        }
                        else if (taskVM.AutoClubQuizStopButtonVisibility == Visibility.Visible)
                        {
                            var stopCmd = taskVM.GetType().GetMethod("OnAutoClubQuizStopTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            stopCmd?.Invoke(taskVM, null);
                        }
                    }
                    break;
                }
                case "禁林探索":
                {
                    var taskVM = App.Services.GetService(typeof(TaskViewModel)) as TaskViewModel;
                    if (taskVM != null)
                    {
                        if (taskVM.AutoForbiddenForestStartButtonVisibility == Visibility.Visible)
                        {
                            var startCmd = taskVM.GetType().GetMethod("OnAutoForbiddenForestStartTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            startCmd?.Invoke(taskVM, null);
                        }
                        else if (taskVM.AutoForbiddenForestStopButtonVisibility == Visibility.Visible)
                        {
                            var stopCmd = taskVM.GetType().GetMethod("OnAutoForbiddenForestStopTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            stopCmd?.Invoke(taskVM, null);
                        }
                    }
                    break;
                }
                case "自动烹饪":
                {
                    var taskVM = App.Services.GetService(typeof(TaskViewModel)) as TaskViewModel;
                    if (taskVM != null)
                    {
                        if (taskVM.AutoCookingStartButtonVisibility == Visibility.Visible)
                        {
                            var startCmd = taskVM.GetType().GetMethod("OnAutoCookingStartTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            startCmd?.Invoke(taskVM, null);
                        }
                        else if (taskVM.AutoCookingStopButtonVisibility == Visibility.Visible)
                        {
                            var stopCmd = taskVM.GetType().GetMethod("OnAutoCookingStopTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            stopCmd?.Invoke(taskVM, null);
                        }
                    }
                    break;
                }
                case "甜蜜冒险":
                {
                    var taskVM = App.Services.GetService(typeof(TaskViewModel)) as TaskViewModel;
                    if (taskVM != null)
                    {
                        if (taskVM.AutoSweetAdventureStartButtonVisibility == Visibility.Visible)
                        {
                            var startCmd = taskVM.GetType().GetMethod("OnAutoSweetAdventureStartTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            startCmd?.Invoke(taskVM, null);
                        }
                        else if (taskVM.AutoSweetAdventureStopButtonVisibility == Visibility.Visible)
                        {
                            var stopCmd = taskVM.GetType().GetMethod("OnAutoSweetAdventureStopTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            stopCmd?.Invoke(taskVM, null);
                        }
                    }
                    break;
                }
                default:
                    break;
            }
        }
    }

    public partial class HotkeyBinding : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _description;

        private ModifierKeys _modifiers;
        public ModifierKeys Modifiers
        {
            get => _modifiers;
            set
            {
                SetProperty(ref _modifiers, value);
                OnPropertyChanged(nameof(ModKeyTuple));
            }
        }

        private Key _key;
        public Key Key
        {
            get => _key;
            set
            {
                SetProperty(ref _key, value);
                OnPropertyChanged(nameof(ModKeyTuple));
            }
        }

        [ObservableProperty]
        private string _group;

        public Tuple<ModifierKeys, Key> ModKeyTuple => Tuple.Create(Modifiers, Key);
    }
}