using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for ErrorListWindow.xaml
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        public ObservableCollection<ErrorMessage> ErrorMessages { get; set; } = [];

        public ErrorListWindow()
        {
            InitializeComponent();
            DataContext = this;
            Closing += ErrorListWindow_Closing;
            UpdateErrorCount();
            UpdateEmptyState();
        }

        public void ErrorListWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public void AddError(LogType logType, string message, string timestamp)
        {
            Dispatcher.Invoke(() =>
            {
                ErrorMessages.Add(new ErrorMessage
                {
                    LogType = logType.ToString(),
                    TypeIcon = Helpers.LogHelper.GetLogTypeIcon(logType),
                    Message = message,
                    Timestamp = timestamp
                });
                UpdateErrorCount();
                UpdateEmptyState();
            });
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessages.Clear();
            UpdateErrorCount();
            UpdateEmptyState();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateErrorCount();
            UpdateEmptyState();
        }

        private void UpdateErrorCount()
        {
            var errorCount = ErrorMessages.Count(m => m.LogType == "Error");
            var warningCount = ErrorMessages.Count(m => m.LogType == "Warning");
            
            if (errorCount > 0 && warningCount > 0)
            {
                ErrorCountText.Text = $"({errorCount} error{(errorCount != 1 ? "s" : "")}, {warningCount} warning{(warningCount != 1 ? "s" : "")})";
            }
            else if (errorCount > 0)
            {
                ErrorCountText.Text = $"({errorCount} error{(errorCount != 1 ? "s" : "")})";
            }
            else if (warningCount > 0)
            {
                ErrorCountText.Text = $"({warningCount} warning{(warningCount != 1 ? "s" : "")})";
            }
            else
            {
                ErrorCountText.Text = "";
            }

            // Update main window error badge
            MainWindow.Instance?.UpdateErrorCount(errorCount + warningCount);
        }

        private void UpdateEmptyState()
        {
            EmptyState.Visibility = ErrorMessages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ErrorListBox.Visibility = ErrorMessages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class ErrorMessage
    {
        public string Timestamp { get; set; }
        public string Message { get; set; }
        public string TypeIcon { get; set; }
        public string LogType { get; set; }
    }
}
