using System.Windows;
using System.Windows.Controls;

namespace AutoHPMA.Views.Controls
{
    public partial class SettingRow : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingRow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingRow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SettingContentProperty =
            DependencyProperty.Register(nameof(SettingContent), typeof(object), typeof(SettingRow), new PropertyMetadata(null));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public object SettingContent
        {
            get => GetValue(SettingContentProperty);
            set => SetValue(SettingContentProperty, value);
        }

        public SettingRow()
        {
            InitializeComponent();
        }
    }
}
