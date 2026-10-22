using System;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7
    }

    public enum AppMessageType { Info, Warning, Error }
    public enum AppMessageButtons { OK, YesNo, YesNoCancel }

    public class AppMessageBox
    {
        public static event Action<string, string, AppMessageType>? NotificationPublished;

        public static DialogResult Show(string title, string message, AppMessageType type, AppMessageButtons buttons)
        {
            if (type == AppMessageType.Error)
                Logger.LogError($"{title}: {message}");
            else if (type == AppMessageType.Warning)
                Logger.LogWarning($"{title}: {message}");
            else
                Logger.LogInformation($"{title}: {message}");

            NotificationPublished?.Invoke(title, message, type);
            return buttons == AppMessageButtons.OK ? DialogResult.OK : DialogResult.Yes;
        }
    }
}
