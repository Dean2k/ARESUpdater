using Octokit;
using Octokit.Internal;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ARES_UPDATER
{
    public partial class Updater : Form
    {
        public IReadOnlyList<Release> Releases;
        private static string fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string guiLocation = fileLocation + @"\GUI\ARES.exe";

        public Updater()
        {
            InitializeComponent();
        }

        private void Updater_Load(object sender, EventArgs e)
        {
            var connection = new Connection(new ProductHeaderValue("ARES"),
            new HttpClientAdapter(() => HttpMessageHandlerFactory.CreateDefault()));

            // and pass this connection to your client
            var client = new GitHubClient(connection);
            var releases = client.Repository.Release.GetAll("Dean2k", "A.R.E.S");
            Releases = releases.Result;
            foreach (var item in Releases)
            {
                cbVersions.Items.Add(item.Name);
            }
            cbVersions.SelectedIndex = 0;
            string application = SHA256CheckSum(guiLocation);
            string latestString = GetHashLatestAsync("https://raw.githubusercontent.com/Dean2k/A.R.E.S/main/VersionHashes/ARESGUI.txt");
            if (application.ToLower() != latestString.ToLower())
            {
                MessageBox.Show("Your ARES version is currently not upto date, please update to the latest version");
            }
        }

        private static void extractGUI()
        {
            try
            {
                using (Stream stream = File.OpenRead(fileLocation + @"\GUI.rar"))
                {
                    var reader = ReaderFactory.Open(stream);
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            Console.WriteLine(reader.Entry.Key);
                            reader.WriteEntryToDirectory(fileLocation + @"\GUI", new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Extraction Error: " + ex.Message);
            }

        }

        private static void startARES()
        {
            try
            {
                string commands = string.Format("/C ARES.exe");

                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = commands,
                    WorkingDirectory = fileLocation + @"\GUI\",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                p.StartInfo = psi;
                p.Start();
            }
            catch { }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            label2.Text = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
            downloadProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string releaseVersion = Releases.FirstOrDefault(x => x.Name == cbVersions.Text).Assets.FirstOrDefault(x => x.Name == "GUI.rar").BrowserDownloadUrl;
            if (releaseVersion != default)
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                client.DownloadFileAsync(new Uri(releaseVersion), fileLocation + @"\GUI.rar");
            }

        }

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            label2.Text = "Completed";
            extractGUI();
            startARES();
        }


        private static string SHA256CheckSum(string filePath)
        {
            try
            {
                using (SHA256 SHA256 = SHA256Managed.Create())
                {
                    using (FileStream fileStream = File.OpenRead(filePath))
                        return BitConverter.ToString(SHA256.ComputeHash(fileStream)).Replace("-", "");
                }
            }
            catch { return "0"; }
        }

        private static string GetHashLatestAsync(string url)
        {
            string result;
            using (HttpClient client = new HttpClient())
            {
                result = client.GetStringAsync(url).Result;
            }
            return result;
        }

        private void btnPatchNotes_Click(object sender, EventArgs e)
        {
            string patchNotes = Releases.FirstOrDefault(x => x.Name == cbVersions.Text).Body;
            MessageBox.Show(patchNotes);
        }
    }
}
