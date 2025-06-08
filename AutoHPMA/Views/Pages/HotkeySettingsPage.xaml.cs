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
            ViewModel.OnKeyDown(e.Key);
            e.Handled = true;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var binding = textBox.DataContext as HotkeyBinding;
            if (binding == null) return;

            ViewModel.ChangeHotkey(binding);
            ViewModel.OnKeyDown(e.Key);
            e.Handled = true;
        }
    }
} 