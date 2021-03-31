using System;

namespace oda
{
    internal static class ErrorProvider
    {
        internal static void ShowError(string message, string caption)
        {
            Messages.showException(new Exception(message), caption);
        }
    }
}