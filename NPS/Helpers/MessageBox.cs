using System;
using System.Threading.Tasks;

namespace NPS.Helpers
{
    public static class MessageBox
    {
        public static void Show(string text, string title = null, MessageBoxButtons buttons=default, MessageBoxIcon icon=default)
        {
            Console.WriteLine("msg box: {0}", text);
        }
    }

    public enum MessageBoxButtons
    {
        Default,
        OK
    }

    public enum MessageBoxIcon
    {
        Default,
        Error,
        Warning
    }
}