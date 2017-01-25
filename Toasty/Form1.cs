using System;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Configuration;

namespace WinFormToast
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.NotifyIcon notifyIcon1;

        public Form1()
        {
            InitializeComponent();
            this.components = new System.ComponentModel.Container();

            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            notifyIcon1.Icon = Toasty.Properties.Resources.Icon1;
            notifyIcon1.Visible = true;

            ConnectMqtt();
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up any components being used.
            if (disposing)
                if (components != null)
                    components.Dispose();

            base.Dispose(disposing);
        }

        private void ConnectMqtt()
        {
            MqttClient client = new MqttClient(ConfigurationSettings.AppSettings["broker"]);
            string clientId = ConfigurationSettings.AppSettings["topic"];
            byte code = client.Connect(clientId);
            client.Subscribe(new string[] { "toasty/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string[] topics = e.Topic.Split('/');
            ShowToast("Alert Notification: " + topics[1], Encoding.UTF8.GetString(e.Message));
        }

        private void ShowToast(string title, string message)
        {
            string toastXml = "<toast><visual><binding template='ToastGeneric'><text>%title%</text><text>%message%</text></binding></visual></toast>";
            toastXml = toastXml.Replace("%title%", title);
            toastXml = toastXml.Replace("%message%", message);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc);

            ToastNotificationManager.CreateToastNotifier("Toasty").Show(toast);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
        }
    }
}
