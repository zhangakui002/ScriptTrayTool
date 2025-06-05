using ScriptTrayTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ScriptTrayTool.Views
{
    public partial class ScriptOutputWindow : Window
    {
        private ScriptOutputViewModel? _viewModel;

        public ScriptOutputWindow()
        {
            InitializeComponent();
        }

        public ScriptOutputWindow(ScriptOutputViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;

            // Subscribe to property changes for auto-scroll
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptOutputViewModel.Output) && _viewModel?.AutoScroll == true)
            {
                // Auto-scroll to bottom when new output is added
                Dispatcher.BeginInvoke(() =>
                {
                    OutputScrollViewer.ScrollToEnd();
                });
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.Dispose();
            }
            base.OnClosed(e);
        }
    }

    // Converter for counting lines in output
    public class LineCountConverter : System.Windows.Data.IValueConverter
    {
        public static readonly LineCountConverter Instance = new();

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string text)
            {
                return string.IsNullOrEmpty(text) ? 0 : text.Split('\n').Length;
            }
            return 0;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }


}
