using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ReactiveUI;

namespace NPS
{
    public class BrowseableUriEntry : UserControl
    {
        private readonly TextBox _textBox;
        private readonly TextBlock _label;

        public BrowseableUriEntry()
        {
            InitializeComponent();

            this.FindControl<Button>("BrowseButton").Command = ReactiveCommand.Create(async () =>
            {
                var dialog = new OpenFileDialog();
                var res = await dialog.ShowAsync((Window) this.GetVisualRoot());

                if (res != null)
                {
                    Text = res[0];
                }
            });

            _textBox = this.FindControl<TextBox>("TextBox");
            _label = this.FindControl<TextBlock>("Label");
        }

        public void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public string Label
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }
    }
}