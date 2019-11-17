using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Newtonsoft.Json;
using ReactiveUI;
using Image = Avalonia.Controls.Image;

namespace NPS
{
    public class Desc : Window
    {
        private readonly DataGrid _lst;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private string _currentContentId = "";
        private CancellationTokenSource _cancellation;

        private IDisposable _disposeSubscription;

        private LoadingState State
        {
            set
            {
                pb_status_error.IsVisible = value == LoadingState.Error;
                pb_status_loading.IsVisible = value == LoadingState.Loading;
            }
        }

        public Desc(DataGrid lst)
        {
            InitializeComponent();
            _lst = lst;
        }

        public Desc() : this(null)
        {
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            _disposeSubscription = this.WhenAnyValue(x => x._lst.SelectedItem)
                .Subscribe(a => OnSelectedItemChanged(((TitleEntry) a).Item));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _disposeSubscription?.Dispose();
            _disposeSubscription = null;
        }

        private async void OnSelectedItemChanged(Item item)
        {
            if (item?.ContentId == _currentContentId)
            {
                return;
            }

            _cancellation?.Cancel();
            await _semaphore.WaitAsync();

            _cancellation = new CancellationTokenSource();
            var cts = _cancellation.Token;

            try
            {
                var contentId = _currentContentId = item.ContentId;
                var region = item.Region;

                pictureBox1.Source = null;
                pictureBox2.Source = null;
                pictureBox3.Source = null;

                label1.Text = "";
                richTextBox1.Text = "";

                if (contentId == null || contentId.ToLower().Equals("missing"))
                {
                    State = LoadingState.Error;
                    return;
                }

                State = LoadingState.Loading;

                region = region switch
                {
                    "EU" => "GB/en",
                    "US" => "CA/en",
                    "JP" => "JP/ja",
                    "ASIA" => "JP/ja",
                    _ => region
                };

                var wc = new HttpClient(new HttpClientHandler
                {
                    Proxy = Settings.Instance.Proxy
                });

                var uri = new Uri(
                    "https://store.playstation.com/chihiro-api/viewfinder/" + region + "/19/" + contentId);
                var resp = await wc.GetAsync(uri, cts);
                resp.EnsureSuccessStatusCode();
                var content = await resp.Content.ReadAsStringAsync();

                var contentJson = JsonConvert.DeserializeObject<PSNJson>(content);

                LoadImages(wc, contentJson, cts);

                State = LoadingState.Loaded;
                richTextBox1.Text = contentJson.desc;
                label1.Text = contentJson.title_name + " (rating: " + contentJson.Stars + "/5.00)";
            }
            catch (TaskCanceledException)
            {
                // Do nothing.
            }
            catch (Exception e)
            {
                State = LoadingState.Error;
                Console.WriteLine("Failed to download description:\n{0}", e);
            }
            finally
            {
                _cancellation = null;
                _semaphore.Release();
            }
        }

        private async void LoadImages(HttpClient client, PSNJson json, CancellationToken cts)
        {
            async Task FetchImage(string addr, Image target)
            {
                if (addr == null)
                {
                    return;
                }

                // ReSharper disable once AccessToDisposedClosure
                var resp = await client.GetAsync(addr, cts);

                resp.EnsureSuccessStatusCode();

                var bitmap = await Task.Run(async () => new Bitmap(await resp.Content.ReadAsStreamAsync()), cts);

                target.Source = bitmap;
            }

            try
            {
                await Task.WhenAll(
                    FetchImage(json.cover, pictureBox1),
                    FetchImage(json.picture1, pictureBox2),
                    FetchImage(json.picture2, pictureBox3)
                );
            }
            catch (TaskCanceledException)
            {
                // Nada.
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while loading images:\n{0}", e);
            }
            finally
            {
                client.Dispose();
            }
        }

/*        private void pictureClicked(object sender, EventArgs e)
        {
            var a = (sender as PictureBox);
            if (a.Tag == null)
            {
                a.Tag = a.Location;
                a.Location = new Point(0, 0);
                a.Size = this.Size;

                foreach (Control c in this.Controls)
                {
                    if (a != c) c.Visible = false;
                }
            }
            else
            {
                a.Location = (a.Tag as Point?).Value;
                a.Tag = null;
                a.Size = new Size(280, 129);

                foreach (Control c in this.Controls)
                {
                    if (a != c && c != pb_status) c.Visible = true;
                }
            }
        }*/

        // ReSharper disable InconsistentNaming
        private Image pictureBox1;
        private Image pictureBox2;
        private Image pictureBox3;
        private TextBlock label1;
        private TextBlock richTextBox1;
        private Image pb_status_loading;

        private Image pb_status_error;
        // ReSharper restore InconsistentNaming

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            pictureBox1 = this.FindControl<Image>("pictureBox1");
            pictureBox2 = this.FindControl<Image>("pictureBox2");
            pictureBox3 = this.FindControl<Image>("pictureBox3");
            label1 = this.FindControl<TextBlock>("label1");
            richTextBox1 = this.FindControl<TextBlock>("richTextBox1");
            pb_status_loading = this.FindControl<Image>("pb_status_loading");
            pb_status_error = this.FindControl<Image>("pb_status_error");
        }

        private enum LoadingState
        {
            Loading,
            Error,
            Loaded
        }
    }
}