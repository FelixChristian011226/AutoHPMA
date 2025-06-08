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

        public HotkeySettingsViewModel(AppSettings settings, ILogger<HotkeySettingsViewModel> logger)
        {
            _settings = settings;
            _logger = logger;
            _hotkeyBindings = new ObservableCollection<HotkeyBinding>();
            LoadHotkeyBindings();
        }

        private void LoadHotkeyBindings()
        {
            // 基础功能
            HotkeyBindings.Add(new HotkeyBinding { Name = "截图", Description = "截图当前游戏页面", Key = Key.None, Group = "基础功能" });

            // 任务启动
            HotkeyBindings.Add(new HotkeyBinding { Name = "AutoHPMA", Description = "开始/停止 AutoHPMA", Key = Key.None, Group = "任务启动" });
            HotkeyBindings.Add(new HotkeyBinding { Name = "社团答题", Description = "开始/停止 社团答题", Key = Key.None, Group = "任务启动" });
            HotkeyBindings.Add(new HotkeyBinding { Name = "禁林探索", Description = "开始/停止 禁林探索", Key = Key.None, Group = "任务启动" });
            HotkeyBindings.Add(new HotkeyBinding { Name = "甜蜜冒险", Description = "开始/停止 甜蜜冒险", Key = Key.None, Group = "任务启动" });

            // 从设置中加载保存的热键
            foreach (var binding in HotkeyBindings)
            {
                if (_settings.HotkeyBindings.TryGetValue(binding.Name, out int keyValue))
                {
                    binding.Key = (Key)keyValue;
                    _logger.LogDebug($"Loaded hotkey binding: {binding.Name} -> {binding.Key}");
                }
            }
        }

        public void ChangeHotkey(HotkeyBinding binding)
        {
            if (IsWaitingForKey)
            {
                IsWaitingForKey = false;
                return;
            }

            SelectedBinding = binding;
            IsWaitingForKey = true;
            //_logger.LogDebug($"Waiting for key for binding: {binding.Name}");
        }

        public void OnKeyDown(Key key)
        {
            if (!IsWaitingForKey || SelectedBinding == null)
                return;

            // 如果按下ESC键，取消绑定
            if (key == Key.Escape)
            {
                SelectedBinding.Key = Key.None;
                IsWaitingForKey = false;
                _settings.HotkeyBindings[SelectedBinding.Name] = (int)Key.None;
                _settings.Save();
                _logger.LogDebug($"Cancelled hotkey binding for: {SelectedBinding.Name}");
                return;
            }

            // 检查是否与其他热键冲突
            if (HotkeyBindings.Any(b => b != SelectedBinding && b.Key == key))
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
            IsWaitingForKey = false;
            _settings.HotkeyBindings[SelectedBinding.Name] = (int)key;
            _settings.Save();
            _logger.LogInformation($"Set hotkey binding: {SelectedBinding.Name} -> {key}");
        }

        public void ExecuteHotkeyAction(string actionName)
        {
            //_logger.LogInformation($"Executing hotkey action: {actionName}");
            switch (actionName)
            {
                case "截图":
                    ScreenshotViewModel.TakeScreenshot();
                    break;
                // 其他热键动作可以在这里添加
            }
        }
    }

    public partial class HotkeyBinding : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private Key _key;

        [ObservableProperty]
        private string _group;
    }
} 