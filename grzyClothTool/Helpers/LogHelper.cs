using grzyClothTool.Views;
using System;

namespace grzyClothTool.Helpers;

public class LogMessageEventArgs : EventArgs
{
    public string TypeIcon { get; set; }
    public string Message { get; set; }
}

public static class LogHelper
{
    private static LogWindow _logWindow;
    private static ErrorListWindow _errorListWindow;
    public static event EventHandler<LogMessageEventArgs> LogMessageCreated;

    public static void Init()
    {
        _logWindow = new LogWindow();
        _errorListWindow = new ErrorListWindow();
    }

    public static void Log(string message, LogType logtype = LogType.Info)
    {
        if (_logWindow == null)
            return;

        _logWindow.Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var type = GetLogTypeIcon(logtype);

            _logWindow.LogMessages.Add(new LogMessage { TypeIcon = type, Message = message, Timestamp = timestamp });
            LogMessageCreated?.Invoke(_logWindow, new LogMessageEventArgs { TypeIcon = type, Message = message });
            
            // Add errors and warnings to error list
            if (logtype == LogType.Warning || logtype == LogType.Error)
            {
                _errorListWindow?.AddError(logtype, message, timestamp);
            }
        });
    }

    public static string GetLogTypeIcon(LogType type)
    {
        return type switch
        {
            LogType.Info => "Check",
            LogType.Warning => "WarningOutline",
            LogType.Error => "Close",
            _ => "Info"
        };
    }

    public static void OpenLogWindow()
    {
        _logWindow.Show();
    }

    public static void OpenErrorListWindow()
    {
        _errorListWindow?.Show();
        _errorListWindow?.Activate();
    }

    public static void Close()
    {
        if (_logWindow != null)
        {
            _logWindow.Closing -= _logWindow.LogWindow_Closing;
            _logWindow.Close();
            _logWindow = null;
        }

        if (_errorListWindow != null)
        {
            _errorListWindow.Closing -= _errorListWindow.ErrorListWindow_Closing;
            _errorListWindow.Close();
            _errorListWindow = null;
        }
    }
}
