using System;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Diagnostics;
using System.Threading;

namespace WinFormToast
{
    public partial class Form1 : Form
    {
        private NotifyIcon icon;
        private EventLog log;
        MqttClient client;
        System.Threading.Timer timer;

        public Form1()
        {
            InitializeComponent();
            this.components = new System.ComponentModel.Container();

            this.icon = new System.Windows.Forms.NotifyIcon(this.components);
            icon.Icon = Toasty.Properties.Resources.Icon1;
            icon.Visible = true;

            log = new EventLog();
            if (!EventLog.SourceExists("Toasty"))
            {
                EventLog.CreateEventSource("Toasty", "Application");
            }
            log.Source = "Toasty";
            log.Log = "Application";

            CreateMqtt();
            var statusChecker = new StatusChecker(client);
            timer = new System.Threading.Timer(statusChecker.CheckStatus, null, 0, 250);
        }

        protected override void Dispose(bool disposing)
        {
            client.Disconnect();
            // Clean up any components being used.
            if (disposing)
                if (components != null)
                    components.Dispose();
            base.Dispose(disposing);
        }

        private void CreateMqtt()
        {
            try
            {
                String broker = Toasty.Properties.Settings.Default.broker;
                int port = Toasty.Properties.Settings.Default.port;
                client = new MqttClient(broker, port, false, null, null, MqttSslProtocols.None);

                client.MqttMsgPublishReceived += ReceiveMsgMqtt;
            }
            catch (Exception ex)
            {
                log.WriteEntry("MQTT Create failed: " + ex.Message);
            }
        }

        private void ReceiveMsgMqtt(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string[] topics = e.Topic.Split('/');
                ShowToast("Alert Notification: " + topics[topics.Length - 1], Encoding.UTF8.GetString(e.Message));

            }
            catch (Exception ex)
            {
                log.WriteEntry("MQTT PublishReceived failed: " + ex.Message);
            }
        }

        private void ShowToast(string title, string message)
        {
            try
            {
                string toastXml = "<toast><visual><binding template='ToastGeneric'><text>%title%</text><text>%message%</text></binding></visual></toast>";
                toastXml = toastXml.Replace("%title%", title);
                toastXml = toastXml.Replace("%message%", message);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(toastXml);

                var toast = new ToastNotification(doc);

                ToastNotificationManager.CreateToastNotifier("Toasty").Show(toast);
            }
            catch (Exception ex)
            {
                log.WriteEntry("Show Toast Failed:" + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
        }
    }

    class StatusChecker
    {
        private MqttClient _client;

        public StatusChecker(MqttClient client)
        {
            _client = client;
        }

        // This method is called by the timer delegate.
        public void CheckStatus(object sender)
        {
            if (!_client.IsConnected)
            {
                try
                {
                    String[] topics = Toasty.Properties.Settings.Default.topics.Split('|');
                    byte code = _client.Connect("Toasty");
                    for (int i = 0; i < topics.Length; i++)
                        _client.Subscribe(new string[] { topics[i] }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                }
                catch
                {
                    //log.WriteEntry("MQTT ReConnect/subscribe failed: " + ex.Message);
                }
            }
        }
    }
}
