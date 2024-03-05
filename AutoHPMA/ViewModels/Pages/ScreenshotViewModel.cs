using AutoHPMA.Models;
using AutoHPMA.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace AutoHPMA.ViewModels.Pages
{
    public class ScreenshotViewModel : ObservableObject, INavigationAware
    {

        private bool _isInitialized = false;
        private readonly LogWindow _logWindow;

        private string _inputText;

        public string InputText
        {
            get { return _inputText; }
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddToLogCommand { get; }

        public ScreenshotViewModel(LogWindow logWindow)
        {
            _logWindow = logWindow;

            AddToLogCommand = new RelayCommand(AddToLog);
        }

        private void AddToLog()
        {
            if (!string.IsNullOrWhiteSpace(InputText) && _logWindow != null)
            {
                _logWindow.AddLogMessage(InputText);
                InputText = ""; // 清空输入框
            }
        }
        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }
    }
}
