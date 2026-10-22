using System;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    public class MessageBoxHelper
    {
        public static DialogResult ShowInformation(string message, bool logging)
        {
            if (logging) Logger.LogInformation(message);
            return AppMessageBox.Show("Information", message, AppMessageType.Info, AppMessageButtons.OK);
        }

        public static DialogResult ShowError(string message, bool logging)
        {
            if (logging) Logger.LogError(message);
            return AppMessageBox.Show("Error", message, AppMessageType.Error, AppMessageButtons.OK);
        }

        public static DialogResult ShowWarning(string message, bool logging)
        {
            if (logging) Logger.LogWarning(message);
            return AppMessageBox.Show("Warning", message, AppMessageType.Warning, AppMessageButtons.OK);
        }

        public static DialogResult DialogResultYesNo(string message)
        {
            return AppMessageBox.Show("Confirm", message, AppMessageType.Info, AppMessageButtons.YesNo);
        }

        public static DialogResult DialogResultYesNoCancel(string message)
        {
            return AppMessageBox.Show("Confirm", message, AppMessageType.Info, AppMessageButtons.YesNoCancel);
        }
    }
}
