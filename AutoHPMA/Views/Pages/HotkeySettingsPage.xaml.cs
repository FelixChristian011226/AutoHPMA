using AutoHPMA.ViewModels.Pages;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace AutoHPMA.Views.Pages
{
    public partial class HotkeySettingsPage : INavigableView<HotkeySettingsViewModel>
    {
        public HotkeySettingsViewModel ViewModel { get; }

        public HotkeySettingsPage(HotkeySettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            this.KeyDown += HotkeySettingsPage_KeyDown;
        }

        private void HotkeySettingsPage_KeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys modifiers = Keyboard.Modifiers;
            ViewModel.OnKeyDown(key, modifiers);
            e.Handled = true;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var binding = textBox.DataContext as HotkeyBinding;
            if (binding == null) return;

            if (!ViewModel.IsWaitingForKey)
                ViewModel.ChangeHotkey(binding);

            ModifierKeys modifiers = Keyboard.Modifiers;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            // 日志调试
            System.Diagnostics.Debug.WriteLine($"捕获到按键: key={key}, modifiers={modifiers}");

            // 过滤掉仅有修饰键的情况
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin)
            {
                e.Handled = true;
                return;
            }
            ViewModel.OnKeyDown(key, modifiers);
            e.Handled = true;
        }
    }
} 