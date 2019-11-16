using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace NPS.Helpers
{
    public class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
        }

        public static void Show(string text, string title = "Alert!", MessageBoxButtons buttons = default,
            MessageBoxIcon icon = default)
        {
            Show(NpsBrowser.MainWindow, text, title, buttons, icon);
        }

        public static Task ShowAsync(Window parent, string text, string title = "Alert!", MessageBoxButtons buttons = default,
            MessageBoxIcon icon = default)
        {
            var box = CreateMessageBox(text, title);
            return box.ShowDialog(parent);
        }

        public static void Show(Window parent, string text, string title = "Alert!", MessageBoxButtons buttons = default,
            MessageBoxIcon icon = default)
        {
            var box = CreateMessageBox(text, title);
            box.ShowDialog(parent);
        }

        private static MessageBox CreateMessageBox(string text, string title)
        {
            return new MessageBox
            {
                Title = title,
                _text = {Text = text}
            };
        }

        private Button _button;
        private TextBlock _text;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _button = this.FindControl<Button>("Button");
            _text = this.FindControl<TextBlock>("Text");

            _button.Command = ReactiveCommand.Create(Close);
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