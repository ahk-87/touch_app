using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace touch_app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string mainURL = "http://192.168.8.1/html/index.html";
        const string statisticsURL = "http://192.168.8.1/api/monitoring/traffic-statistics";
        const string statusURL = "http://192.168.8.1/api/monitoring/status";
        string user1, user2, user3;
        string pass1, pass2, pass3;

        HttpClient client;
        HttpClientHandler handler;

        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            ServicePointManager.ServerCertificateValidationCallback = delegate (
            Object obj, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors errors)
            {
                return (true);
            };

            extractData();
            line1.setUserPass(user1, pass1);
            line2.setUserPass(user2, pass2);
            line3.setUserPass(user3, pass3);
            line3.collapse();

            handler = new HttpClientHandler();
            client = new HttpClient(handler);
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 15);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        async private void Timer_Tick(object sender, EventArgs e)
        {
            string response;
            try
            {
                response = await client.GetStringAsync(statisticsURL);
                if (response.Contains("error"))
                {
                    await client.GetAsync(mainURL);
                    response = await client.GetStringAsync(statisticsURL);
                }
                var match = Regex.Match(response, "<CurrentUpload>(\\d*)</CurrentUpload>");
                long upload = int.Parse(match.Groups[1].Value);
                match = Regex.Match(response, "<CurrentDownload>(\\d*)</CurrentDownload>");
                long download = int.Parse(match.Groups[1].Value);
                response = await client.GetStringAsync(statusURL);
                txtWifiUsers.Text = Regex.Match(response, "<CurrentWifiUser>(\\d{0,1})</CurrentWifiUser>").Groups[1].Value;
                txtWifiUsers.Foreground = Brushes.Black;
                txtDownload.Text = reformatBytes(download);
                txtUpload.Text = reformatBytes(upload);
                if (download > (700 * 1024 * 1024))
                    txtDownload.Background = Brushes.Red;
                else if (download > (500 * 1024 * 1024))
                    txtDownload.Background = Brushes.Orange;
                else if (download > (300 * 1024 * 1024))
                    txtDownload.Background = Brushes.Yellow;
                else
                    txtDownload.Background = Brushes.White;
            }
            catch (Exception ex)
            {
                txtDownload.Text = "0";
                txtUpload.Text = "0";
                txtWifiUsers.Text = "Error";
                txtWifiUsers.Foreground = Brushes.Red;
            }
        }

        private string reformatBytes(long bytes)
        {
            if (bytes < 1024)
                return bytes.ToString("0.00 bytes");
            else if (bytes < 1024 * 1024)
            {
                double b = bytes / 1024.0;
                return b.ToString("0.00 KB");
            }
            else if (bytes < 1024 * 1024 * 1024)
            {
                double b = bytes / 1024.0 / 1024;
                return b.ToString("0.00 MB");
            }
            else if (bytes < 1024 * 1024 * 1024 *1024L)
            {
                double b = bytes / 1024.0 / 1024/ 1024;
                return b.ToString("0.00 GB (Are u crazy?!!)");
            }
            return "0";
        }

        string wifiUrl, wifiUsername, wifiPassword;

        async private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.PrimaryScreenWidth - this.ActualWidth;

            Timer_Tick(null, null);
        }

        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            ButtonWifi.IsEnabled = false;

            string sessionID = "";
            string requestData = "submit_button=login&change_action=&gui_action=Apply&wait_time=19&submit_type=&http_username={0}&http_passwd={1}";
            string result = "";
            string loginUrl = "http://{0}/login.cgi";
            string applyUrl = "http://{0}/apply.cgi;session_id={1}";

            WebClient client = new WebClient();
            result = await client.UploadStringTaskAsync(string.Format(loginUrl, wifiUrl), string.Format(requestData, wifiUsername, wifiPassword));
            sessionID = Regex.Match(result, "session_id=(.*)\";").Groups[1].Value;
            result = await client.UploadStringTaskAsync(string.Format(applyUrl, wifiUrl, sessionID), "submit_button=Wireless_Basic&gui_action=Apply&submit_type=&change_action=&next_page=&commit=1&wl0_nctrlsb=none&wl1_nctrlsb=none&channel_5g=0&channel_24g=0&nbw_5g=20&nbw_24g=20&wait_time=3&guest_ssid=sk-guest&wsc_security_mode=&wsc_smode=1&net_mode_5g=disabled&net_mode_24g=mixed&ssid_24g=sk&_wl0_nbw=20&_wl0_channel=0&closed_24g=1");

            ButtonWifi.IsEnabled = true;

        }

        private void extractData()
        {
            try
            {
                string[] data = File.ReadAllLines("dataTouch.txt");
                user1 = data[0].Split(new char[] { ',' })[0];
                pass1 = data[0].Split(new char[] { ',' })[1];
                user2 = data[1].Split(new char[] { ',' })[0];
                pass2 = data[1].Split(new char[] { ',' })[1];
                user3 = data[2].Split(new char[] { ',' })[0];
                pass3 = data[2].Split(new char[] { ',' })[1];
                wifiUrl = data[2];
                wifiUsername = data[4].Split(new char[] { ',' })[0];
                wifiPassword = data[4].Split(new char[] { ',' })[1];
                ButtonWifi.Visibility = Visibility.Collapsed;
            }
            catch
            {
                ButtonWifi.Visibility = Visibility.Collapsed;
            }
        }
    }
}
