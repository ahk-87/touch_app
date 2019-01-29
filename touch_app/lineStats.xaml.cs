using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.IO;
using System.Net;
using System.Net.Http;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace touch_app
{
    /// <summary>
    /// Interaction logic for lineStats.xaml
    /// </summary>
    public partial class lineStats : UserControl
    {
        const string loginURL = "https://app.touch.com.lb/touchserver/login.php";
        const string balanceURL = "https://app.touch.com.lb/v2/self/balances.php";
        const string serviceActivationURL = "https://app.touch.com.lb/touchserver/activateservice.php";
        const string servicesURL = "https://app.touch.com.lb/v2/self/services.php";

        const string token = "APA91bH8keD6_LCneuGnW5g5xyFSl0FvHfXwhtdf_hly9mVsfX5aRcNhb55v4RvDYIwaV08ymsCZRT8cJDkiGrqPTi3QIX2xt2p6mKPqzoKA2gjumYVRsPo";
        const string twoDayVoiceServiceID = "VOICE2D";
        const string webAndTalk = "d";


        LineInfo info = new LineInfo();
        DispatcherTimer timer = new DispatcherTimer();
        string elapsedString = "";


        public lineStats()
        {

            InitializeComponent();
            timer.Interval = new TimeSpan(0, 0, 30);
            timer.Tick += Timer_Tick;
            timer.Start();
            this.DataContext = info;
            info.ServiceValidity = DateTime.Now.AddDays(1);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (info.ServiceValidity < DateTime.Now)
            {
                info.RemainingMinutes = 0;
                buttonRenew.Content = "Renew";
                buttonRenew.IsEnabled = true;
            }

            TimeSpan elapsed = DateTime.Now - info.LastRefresh;
            if (elapsed < new TimeSpan(0, 1, 0))
            {
                textElapsed.Text = ",  Just now";
            }
            else if (elapsed.Days < 1)
            {
                elapsedString = ",  " + (elapsed.Hours > 0 ? elapsed.Hours + " Hour" + (elapsed.Hours > 1 ? "s" : "") + (elapsed.Minutes == 0 ? "" : " & ") : "") +
                    (elapsed.Minutes > 0 ? elapsed.Minutes + " Minute" + (elapsed.Minutes > 1 ? "s" : "") : "") + " ago";
                textElapsed.Text = elapsedString;
            }
        }

        async private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            showNoInternetError(false);
            timer.IsEnabled = false;
            try
            {
                buttonRefresh.IsEnabled = false;
                if (string.IsNullOrWhiteSpace(info.Session))
                {
                    await login();
                    await getBalance();
                    goto SetRefresh;
                }

                //checkLogin();
                await getBalance();

            SetRefresh:
                info.LastRefresh = DateTime.Now;
                textElapsed.Text = ",  Just now";

                FocusManager.SetFocusedElement(this, textBalance);
            }
            catch
            {
                showNoInternetError(true);
            }
            finally
            {
                timer.IsEnabled = true;
                buttonRefresh.IsEnabled = true;
            }
        }

        async private Task login()
        {
            WebClient client = new WebClient();
            client.QueryString.Add("token", token);
            client.QueryString.Add("username", info.Username);
            client.QueryString.Add("password", info.Password);
            client.QueryString.Add("format", "xml");
            client.QueryString.Add("sandbox", "false");
            client.QueryString.Add("client", "android");
            //client.QueryString.Add("appversion", "2.17.3");
            //client.QueryString.Add("slhaumsah", "1503323873844");
            //client.QueryString.Add("ip", "192.168.1.66");
            //client.QueryString.Add("osversion", "5.1.1");

            string result = await client.DownloadStringTaskAsync(loginURL);

            info.PhoneNumber = Regex.Match(result, "\\[(\\d+)\\]\\]></phone>").Groups[1].Value;
            info.Session = Regex.Match(result, "\\[(\\w+)\\]\\]></session>").Groups[1].Value;
        }

        async private Task getBalance()
        {
            WebClient client = new WebClient();
            client.QueryString.Add("client", "android");
            client.QueryString.Add("sandbox", "false");
            client.QueryString.Add("session", info.Session);
            client.QueryString.Add("userisdn", info.Username);
            client.QueryString.Add("phoneisdn", info.PhoneNumber);
            client.QueryString.Add("format", "json");
            //client.QueryString.Add("appversion", "2.17.3");
            //client.QueryString.Add("slhaumsah", "1503323873844");
            //client.QueryString.Add("ip", "192.168.1.66");
            //client.QueryString.Add("osversion", "5.1.1");

            string result = await client.DownloadStringTaskAsync(balanceURL);

            jsonClasses.Balances data = Json.Desirialize<jsonClasses.Balances>(result);

            info.Balance = double.Parse(data.balances[0].subs[0].value.text.Trim(new char[] { '$' }));

            if (info.OldBalance == 0)
            {
                info.OldBalance = info.Balance;
            }
            else
            {
                double balanceDifference = info.Balance - info.OldBalance;
                if (balanceDifference < 0)
                {
                    labelErrorBalance.Background = Brushes.Red;
                    labelErrorBalance.Content = string.Format("Lost {0:0.0} $\r\non {1:HH:mm}", Math.Abs(balanceDifference), DateTime.Now);
                }
                else if (balanceDifference > 0)
                {
                    labelErrorBalance.Background = Brushes.Green;
                    labelErrorBalance.Content = string.Format("   Added\r\n{0:0.0} $", balanceDifference);
                }
            }

            info.LineValidity = DateTime.Parse(data.balances[0].valid.boldify);

            buttonRenew.Content = "Renew";
            buttonRenew.IsEnabled = true;
            textRemainingMinutesPreferred.Visibility = Visibility.Collapsed;

            if (data.balances.Count > 2)
            {
                textRemainingMinutesPreferred.Visibility = Visibility.Visible;
                if (data.balances[2].title.text == "2 Days Voice")
                {
                    bool preferred = false;
                    int usedMinutes = int.Parse(data.balances[2].subs[0].value.text.Substring(0, 2).TrimEnd(new char[] { ' ' }));
                    int usedMinutesWeb = int.Parse(data.balances[1].subs[2].value.text.Substring(0, 2).TrimEnd(new char[] { ' ' }));
                    int usedMinutesPreferredNumber = int.Parse(data.balances[1].subs[1].value.text.Substring(0, 3).TrimEnd(new char[] { ' ', '/' }));
                    info.RemainingMinutes = 60 - usedMinutes + 60 - usedMinutesWeb;
                    if (info.LastCall == 0)
                    {
                        info.RemainingMinutesPreferred = 300 - usedMinutesPreferredNumber;
                        preferred = true;
                    }
                    if (info.LastCall > 0)
                    {
                        textLastCallRefresh.Visibility = Visibility.Visible;
                        if (!preferred)
                            textLastCallRefresh.Text = "Last refresh was on " + info.LastRefresh.ToShortTimeString();
                        else
                            textLastCallRefresh.Text = "Preferred";
                    }
                    else
                    {
                        textLastCallRefresh.Visibility = Visibility.Collapsed;
                    }

                    info.ServiceValidity = DateTime.Parse(data.balances[1].valid.boldify);
                    buttonRenew.Content = string.Format("service valid till{0}     {1:hh:mm tt}{0}   {1:dd-MM-yyyy}",
                        Environment.NewLine, info.ServiceValidity);
                    buttonRenew.IsEnabled = false;
                }
                else if (data.balances[1].title.text == "2 Days Voice")
                {
                    bool preferred = false;
                    int usedMinutes = int.Parse(data.balances[1].subs[0].value.text.Substring(0, 2).TrimEnd(new char[] { ' ' }));
                    int usedMinutesWeb = int.Parse(data.balances[2].subs[2].value.text.Substring(0, 2).TrimEnd(new char[] { ' ' }));
                    int usedMinutesPreferredNumber = int.Parse(data.balances[2].subs[1].value.text.Substring(0, 3).TrimEnd(new char[] { ' ', '/' }));
                    info.RemainingMinutes = 60 - usedMinutes + 60 - usedMinutesWeb;
                    if (info.LastCall == 0)
                    {
                        info.RemainingMinutesPreferred = 300 - usedMinutesPreferredNumber;
                        preferred = true;
                    }
                    if (info.LastCall > 0)
                    {
                        textLastCallRefresh.Visibility = Visibility.Visible;
                        if (!preferred)
                            textLastCallRefresh.Text = "Last refresh was on " + info.LastRefresh.ToShortTimeString();
                        else
                            textLastCallRefresh.Text = "Preferred";
                    }
                    else
                    {
                        textLastCallRefresh.Visibility = Visibility.Collapsed;
                    }

                    info.ServiceValidity = DateTime.Parse(data.balances[1].valid.boldify);
                    buttonRenew.Content = string.Format("service valid till{0}     {1:hh:mm tt}{0}   {1:dd-MM-yyyy}",
                        Environment.NewLine, info.ServiceValidity);
                    buttonRenew.IsEnabled = false;
                }
            }
            else if (data.balances.Count > 1)
            {
                if (data.balances[1].title.text == "2 Days Voice")
                {
                    int usedMinutes = int.Parse(data.balances[1].subs[0].value.text.Substring(0, 2).TrimEnd(new char[] { ' ' }));
                    info.RemainingMinutes = 60 - usedMinutes;
                    if (info.LastCall > 0)
                    {
                        textLastCallRefresh.Visibility = Visibility.Visible;
                        textLastCallRefresh.Text = "Last refresh was on " + info.LastRefresh.ToShortTimeString();
                    }
                    else
                    {
                        textLastCallRefresh.Visibility = Visibility.Collapsed;
                    }
                    info.ServiceValidity = DateTime.Parse(data.balances[1].valid.boldify);
                    buttonRenew.Content = string.Format("service valid till{0}     {1:hh:mm tt}{0}   {1:dd-MM-yyyy}",
                        Environment.NewLine, info.ServiceValidity);
                    buttonRenew.IsEnabled = false;
                }
                else if (data.balances[1].title.text == "Web & Talk")
                {
                    textRemainingMinutesPreferred.Visibility = Visibility.Visible;
                    bool preferred = false;
                    int usedMinutes = int.Parse(data.balances[1].subs[2].value.text.Substring(0, 2).TrimEnd(new char[] { ' ' }));
                    int usedMinutesPreferredNumber = int.Parse(data.balances[1].subs[1].value.text.Substring(0, 3).TrimEnd(new char[] { ' ', '/' }));
                    info.RemainingMinutes = 60 - usedMinutes;
                    if (info.LastCall == 0)
                    {
                        info.RemainingMinutesPreferred = 300 - usedMinutesPreferredNumber;
                        preferred = true;
                    }
                    if (info.LastCall > 0)
                    {
                        textLastCallRefresh.Visibility = Visibility.Visible;
                        if (!preferred)
                            textLastCallRefresh.Text = "Last refresh was on " + info.LastRefresh.ToShortTimeString();
                        else
                            textLastCallRefresh.Text = "Preferred";
                    }
                    else
                    {
                        textLastCallRefresh.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        async private void buttonRenew_Click(object sender, RoutedEventArgs e)
        {
            showNoInternetError(false);
            try
            {
                buttonRenew.IsEnabled = false;
                if (MessageBox.Show("Are you sure to activate the 2 Day Voice service on this line number?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    if (await activateTwoDayVoiceService())
                    {
                        MessageBox.Show("Service Activated Successfully", "Success", MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("Service Not Activated", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        buttonRenew.IsEnabled = true;
                        return;
                    }

                    buttonRenew.Content = string.Format("service valid till{0}     {1:hh:mm tt}{0}   {1:dd-MM-yyyy}",
                        Environment.NewLine, info.ServiceValidity);
                }
                else
                {
                    buttonRenew.IsEnabled = true;
                }
            }
            catch
            {
                showNoInternetError(true);
                buttonRenew.IsEnabled = true;
            }
        }

        async private Task<bool> activateTwoDayVoiceService()
        {
            WebClient client1 = new WebClient();
            client1.QueryString.Add("client", "android");
            client1.QueryString.Add("sandbox", "false");
            client1.QueryString.Add("format", "json");
            //client.QueryString.Add("slhaumsah", "1503323873844");
            //client.QueryString.Add("ip", "192.168.1.66");
            //client.QueryString.Add("appversion", "2.17.3");
            //client.QueryString.Add("osversion", "5.1.1");
            client1.QueryString.Add("session", info.Session);
            client1.QueryString.Add("userisdn", info.Username);
            client1.QueryString.Add("phoneisdn", info.PhoneNumber);

            string result1 = await client1.DownloadStringTaskAsync(servicesURL);


            WebClient client = new WebClient();
            client.QueryString.Add("serviceid", twoDayVoiceServiceID);
            client.QueryString.Add("client", "android");
            client.QueryString.Add("sandbox", "false");
            client.QueryString.Add("format", "xml");
            client.QueryString.Add("slhaumsah", "1503323873844");
            client.QueryString.Add("ip", "192.168.1.66");
            client.QueryString.Add("appversion", "2.17.3");
            client.QueryString.Add("osversion", "5.1.1");
            client.QueryString.Add("session", info.Session);
            client.QueryString.Add("userisdn", info.Username);
            client.QueryString.Add("phoneisdn", info.PhoneNumber);

            string result = await client.DownloadStringTaskAsync(serviceActivationURL);

            info.Balance = Math.Round(info.Balance - 2.0, 2);
            info.OldBalance = info.Balance;
            File.WriteAllText(info.Username + " - Log.txt", result + "\r\n\r\n" + result1);
            if (result.Contains("success"))
            {
                string date, time;
                date = Regex.Match(result, "\\d{4}-\\d{1,2}-\\d\\d").Value;
                time = Regex.Match(result, "\\d\\d:\\d\\d:\\d\\d").Value;

                info.RemainingMinutes = 60;
                info.LastRefresh = DateTime.Now;
                info.ServiceValidity = DateTime.Parse(time + " " + date);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void setUserPass(string username, string password)
        {
            info.Username = username;
            info.Password = password;
        }

        void showNoInternetError(bool show)
        {
            if (show)
                textError.Visibility = Visibility.Visible;
            else
                textError.Visibility = Visibility.Hidden;
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            buttonRefresh.IsEnabled = !buttonRefresh.IsEnabled;
            if (buttonRefresh.IsEnabled)
                textEnableRefresh.Text = "Disable";
            else
                textEnableRefresh.Text = "Enable";
        }

        private void Error_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            labelErrorBalance.Content = "";
            info.OldBalance = info.Balance;
        }

        private void textCollapseExpand_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.ActualHeight > 200)
            {
                collapse();
            }
            else
            {
                this.Height = 220;
                textCollapseExpand.Text = "Collapse";
                textCollapseExpand.Background = Brushes.Red;
            }
        }

        public void collapse()
        {
            this.Height = 15;
            textCollapseExpand.Text = "Expand";
            textCollapseExpand.Background = Brushes.Green;

        }
    }



    public class LineInfo : INotifyPropertyChanged
    {

        string phoneNumber;
        int remainingMinutes, remainingMinutesPreferred;
        int lastCall;
        double balance;
        DateTime lineValidity;
        DateTime lastRefresh;

        public string Username { get; set; }
        public string Password { get; set; }
        public string Session { get; set; }
        public DateTime ServiceValidity { get; set; }
        public double OldBalance { get; set; }

        public int RemainingMinutesPreferred
        {
            get
            {
                return remainingMinutesPreferred;
            }

            set
            {
                int last = remainingMinutesPreferred - value;
                LastCall = last < 0 ? 0 : last;
                if (remainingMinutesPreferred != value)
                {
                    remainingMinutesPreferred = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int RemainingMinutes
        {
            get
            {
                return remainingMinutes;
            }

            set
            {
                int last = remainingMinutes - value;
                LastCall = last < 0 ? 0 : last;
                if (remainingMinutes != value)
                {
                    remainingMinutes = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int LastCall
        {
            get
            {
                return lastCall;
            }

            set
            {
                if (lastCall != value)
                {
                    lastCall = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string PhoneNumber
        {
            get
            {
                return phoneNumber;
            }

            set
            {
                if (phoneNumber != value)
                {
                    phoneNumber = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Balance
        {
            get
            {
                return balance;
            }

            set
            {
                if (balance != value)
                {
                    balance = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime LineValidity
        {
            get
            {
                return lineValidity;
            }

            set
            {
                if (lineValidity != value)
                {
                    lineValidity = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public DateTime LastRefresh
        {
            get
            {
                return lastRefresh;
            }
            set
            {
                if (lastRefresh != value)
                {
                    lastRefresh = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
