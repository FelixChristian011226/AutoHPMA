using AutoHPMA.Config;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class HotkeySettingsViewModel : ObservableObject
    {
        private readonly AppSettings _settings;

        [ObservableProperty]
        private ObservableCollection<HotkeyBinding> _hotkeyBindings;

        [ObservableProperty]
        private HotkeyBinding _selectedBinding;

        [ObservableProperty]
        private bool _isWaitingForKey;

        public HotkeySettingsViewModel(AppSettings settings)
        {
            _settings = settings;
            _hotkeyBindings = new ObservableCollection<HotkeyBinding>();
            LoadHotkeyBindings();
        }

        private void LoadHotkeyBindings()
        {
            // 添加默认的热键绑定
            HotkeyBindings.Add(new HotkeyBinding { Name = "开始/停止", Description = "开始或停止自动操作", Key = Key.F5 });
            HotkeyBindings.Add(new HotkeyBinding { Name = "暂停/继续", Description = "暂停或继续自动操作", Key = Key.F6 });
            HotkeyBindings.Add(new HotkeyBinding { Name = "截图", Description = "截取当前屏幕", Key = Key.F7 });
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
        }

        public void OnKeyDown(Key key)
        {
            if (!IsWaitingForKey || SelectedBinding == null)
                return;

            // 检查是否与其他热键冲突
            if (HotkeyBindings.Any(b => b != SelectedBinding && b.Key == key))
            {
                MessageBox.Show("该热键已被其他功能使用，请选择其他按键。", "热键冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedBinding.Key = key;
            IsWaitingForKey = false;
            _settings.Save();
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
    }
} 