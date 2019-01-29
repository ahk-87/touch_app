using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

namespace touch_app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string user1, user2, user3;
        string pass1, pass2, pass3;

        public MainWindow()
        {
            InitializeComponent();

            extractData();
            line1.setUserPass(user1, pass1);
            line2.setUserPass(user2, pass2);
            line3.setUserPass(user3, pass3);
            line3.collapse();
        }

        string wifiUrl, wifiUsername, wifiPassword;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.PrimaryScreenWidth - this.ActualWidth;
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
                user1 = "abbas2222"; pass1 = "fdbwer147";
                user2 = "samir1111"; pass2 = "afh1236";
                user3 = "shosho1122"; pass3 = "sal14725";
                ButtonWifi.Visibility = Visibility.Collapsed;
            }
        }
    }
}
