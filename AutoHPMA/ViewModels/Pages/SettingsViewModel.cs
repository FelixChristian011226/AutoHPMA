﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Config;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private readonly AppSettings _settings;

        [ObservableProperty]
        private string _appVersion = String.Empty;
        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;
        [ObservableProperty]
        private ThemeOption _selectedThemeOption;

        [ObservableProperty]
        private int _logFileLimit = 10;

        public class ThemeOption
        {
            public ApplicationTheme Theme { get; set; }
            public string Name { get; set; }
        }

        private readonly ThemeOption[] _themeOptions = new[]
        {
            new ThemeOption { Theme = ApplicationTheme.Light, Name = "浅色" },
            new ThemeOption { Theme = ApplicationTheme.Dark, Name = "深色" }
        };

        public IEnumerable<ThemeOption> ThemeOptions => _themeOptions;

        public SettingsViewModel(AppSettings settings)
        {
            _settings = settings;
            LogFileLimit = _settings.LogFileLimit;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            var systemTheme = GetSystemTheme();
            
            SelectedThemeOption = _themeOptions.FirstOrDefault(t => t.Theme == systemTheme);
            
            CurrentTheme = systemTheme;
            
            ApplicationThemeManager.Apply(systemTheme);

            //AppVersion = $"v{GetAssemblyVersion()}";
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = $"v{version.Major}.{version.Minor}.{version.Build}";

            _isInitialized = true;
        }

        private ApplicationTheme GetSystemTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null)
                        {
                            return (int)value == 1 ? ApplicationTheme.Light : ApplicationTheme.Dark;
                        }
                    }
                }
            }
            catch
            {
                // 如果无法获取系统主题，默认使用浅色主题
            }
            return ApplicationTheme.Light;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        partial void OnCurrentThemeChanged(ApplicationTheme value)
        {
            SelectedThemeOption = _themeOptions.FirstOrDefault(t => t.Theme == value);
        }

        partial void OnSelectedThemeOptionChanged(ThemeOption value)
        {
            if (value != null)
            {
                OnChangeTheme(value.Theme);
            }
        }

        public void OnChangeTheme(ApplicationTheme theme)
        {
            if (CurrentTheme == theme)
                return;

            try
            {
                ApplicationThemeManager.Apply(theme);
                CurrentTheme = theme;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"切换主题时发生错误: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ResetSettings()
        {
            //_settings.Reset();
            _settings.Clear();
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "⚠️ 提示",
                Content = "偏好设置已重置，程序即将退出。",
            };
            var result = uiMessageBox.ShowDialogAsync();
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            var logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = logFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        partial void OnLogFileLimitChanged(int value)
        {
            _settings.LogFileLimit = value;
            _settings.Save();
        }
    }
}
