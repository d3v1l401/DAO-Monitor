using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DAOMonGUI
{
    public partial class Main : Form
    {
        string[] _list = null;
        List<string> _deliveredNotify = new List<string>();
        bool keepScanning = true;
        Task _task = null;

        private void LoadListAndScan() {
            string _tmplist = File.ReadAllText("tracks.txt");
            _list = _tmplist.Split("\r\n".ToCharArray());

            if (_list.Length > 0)
                this._task = Task.Factory.StartNew(() => this.ThreadEPAsync());
        }

        private async void ThreadEPAsync() {

            while (this.keepScanning) {

                foreach (var _track in this._list) {
                    if (string.IsNullOrEmpty(_track))
                        continue;

                    using (WebClient _client = new WebClient()) {
                        _client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.157 Safari/537.36");
                        _client.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");
                        _client.Headers.Add("Accept-Language", "it-IT,it;q=0.9,en-US;q=0.8,en;q=0.7");
                        _client.Headers.Add(HttpRequestHeader.Cookie, "DNT=1");

                        var _uri = string.Format("http://www.dao.as/?stregkode={0}#trackandtrace", _track);

                        string _html = _client.DownloadString(_uri);
                        if (_html.Contains("Pakke afleveret")) 
                            this._deliveredNotify.Add(_track);

                    }

                }

                // Removes delivered items, to avoid duplicate results.
                foreach (var _delieveredTrack in this._deliveredNotify) {
                    for (var i = 0; i < this._list.Length; i++)
                        if (this._list[i].Equals(_delieveredTrack))
                            this._list[i] = string.Empty;

                    this.ntfyIcon.ShowBalloonTip(3 * 1000, "DAO Package Delivery", string.Format("{0} delivered.", _delieveredTrack).Replace('\r', ' '), ToolTipIcon.Info);
                    await Task.Delay((3) * 1000);
                }

                await Task.Delay((60 * 5) * 1000);
            }

        }
        void MenuTest1_Click(object sender, EventArgs e) 
            => Application.Exit();
        

        void MenuTest2_Click(object sender, EventArgs e) {
            if (this._task != null)
                this.keepScanning = false;

            this._task.Wait();

            this.keepScanning = true;
            this.LoadListAndScan();
            this.ntfyIcon.ShowBalloonTip(3 * 1000, "Status update", "Track list reloaded", ToolTipIcon.Info);
        }

        public Main() {
            InitializeComponent();

            this.ntfyIcon.ContextMenuStrip = new ContextMenuStrip();
            this.ntfyIcon.ContextMenuStrip.Items.Add("Reload", null, this.MenuTest2_Click);
            this.ntfyIcon.ContextMenuStrip.Items.Add("Exit", null, this.MenuTest1_Click);
        }

        private void CloseBtn_Click(object sender, EventArgs e) {

            this.Hide();
            this.ntfyIcon.Visible = true;
        }

        private void Main_Resize(object sender, EventArgs e) {

            if (this.WindowState == FormWindowState.Minimized) {
                Hide();
                this.ntfyIcon.Visible = true;
            }

        }

        private void Main_Load(object sender, EventArgs e) {

            this.ntfyIcon.Visible = true;
            this.Hide();

            this.LoadListAndScan();
            if (this._task == null)
                Application.Exit();
        }

        private void NtfyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {

            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ntfyIcon.Visible = false;
        }
    }
}
