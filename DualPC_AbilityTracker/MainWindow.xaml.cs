using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DualPC_AbilityTracker {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        HttpListener listener = new HttpListener();
        Display display;
        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

        }
        private void StartListener() {
            listener.Prefixes.Add("http://*:8086/");

            listener.Start();

            while (true) {
                HttpListenerContext ctx = listener.GetContext();
                using (HttpListenerResponse resp = ctx.Response) {
                    string endpoint = ctx.Request.Url.LocalPath;
                    switch (endpoint) {
                        case "/Start":
                            NameValueCollection qs = HttpUtility.ParseQueryString(ctx.Request.Url.Query, Encoding.UTF8);
                            string bar = qs["bar"] ?? "";

                            display = new Display(bar, false, true, true);
                            display.Show();
                            break;
                        case "Stop":
                            display.Close();
                            break;
                        case "SyncronizeKeybinds":

                            break;
                    }
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

        }
        private void StartDisplay(System.Collections.Specialized.NameValueCollection queryString) {

        }
        private void StartListener_Click(object sender, RoutedEventArgs e) {
            btnStartListener.IsEnabled = false;
            txtInfo.Text = "Listener Started";
            //StartListener();

            Display display = new Display("Mage", false, true, true);
            display.Show();
            //Task.Factory.StartNew(() => StartListener());

        }
    }
}
