// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using AutoHPMA.Config;

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

            AppVersion = $"AutoHPMA - {GetAssemblyVersion()}";

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
            Application.Current.Shutdown();
        }
    }
}
