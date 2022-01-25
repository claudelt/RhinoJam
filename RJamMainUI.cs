using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rhino;
using RJam.Data;

namespace RJam
{
    [System.Runtime.InteropServices.Guid("E7552E37-7313-45B9-9AC0-8D2AFC2C84E3")]
    public partial class RJamMainUI : UserControl
    {
        public RhinoDoc Document { get; set; }
        private DocumentDataHost host;
        public bool isStarted;
        public bool isConnected;
        public string connectedIP;

        public RJamMainUI()
        {
            InitializeComponent();

            this.Document = null;
            this.host = null;

            this.isStarted = false;
            this.isConnected = false;
            this.connectedIP = "0.0.0.0";

            this.partnerIPTextbox.ValidatingType = typeof(IPAddress);

            this.UpdateDoc();
            this.UpdateUI();
        }

        public void UpdateUI()
        {
            labelStatus.Text = (RJamPlugin.Instance.HasHost(this.Document)) ? "Started" : "Not Started";

            if (this.isStarted)
            {
                this.UpdateLocalIP();

                this.ButtonStartDoc.Text = "End Session";

                if(this.isConnected)
                {
                    this.ButtonStartDoc.Enabled = false;

                    this.partnerIPTextbox.Enabled = false;
                    this.buttonConnect.Text = "Disconnect";
                    this.buttonConnect.Enabled = true;

                    labelStatus.Text = "Connected";
                }
                else
                {
                    this.ButtonStartDoc.Enabled = true;

                    this.partnerIPTextbox.Text = "";
                    this.partnerIPTextbox.Enabled = true;
                    this.buttonConnect.Text = "Connect";
                    this.buttonConnect.Enabled = true;
                }
            }
            else
            {
                this.UpdateLocalIP("0.0.0.0");
                this.ButtonStartDoc.Text = "Start Session";
                this.ButtonStartDoc.Enabled = true;

                this.partnerIPTextbox.Text = "";
                this.partnerIPTextbox.Enabled = false;
                this.buttonConnect.Text = "Connect";
                this.buttonConnect.Enabled = false;
            }
        }

        private void UpdateDoc()
        {
            this.Document = RhinoDoc.ActiveDoc;
            labelDocumentName.Text = (this.Document == null)? "Unkown" : (this.Document.Name == "")? "Untitled" : this.Document.Name;
        }

        private void UpdateLocalIP()
        {
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = entry.AddressList;
            IPAddress localAddr = IPAddress.None;

            foreach (IPAddress address in addr)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    localAddr = address.MapToIPv4();

                    // Hack
                    if(localAddr.ToString().StartsWith("192."))
                    {
                        break;
                    }
                }
            }

            this.UpdateLocalIP(localAddr.ToString());
        }

        private void UpdateLocalIP(string ip)
        {
            string[] IPSegments = ip.Split('.');

            IPSegment1.Text = IPSegments[0];
            IPSegment2.Text = IPSegments[1];
            IPSegment3.Text = IPSegments[2];
            IPSegment4.Text = IPSegments[3];
        }

        private void ButtonStartDoc_Click(object sender, EventArgs e)
        {
            if(!ButtonStartDoc.Enabled)
            {
                return;
            }

            this.UpdateDoc();

            if (RJamPlugin.Instance.HasHost(this.Document))
            {
                RJamPlugin.Instance.StopHost(this.Document);
                this.isStarted = false;
                this.isConnected = false;
            }
            else
            {
                RJamPlugin.Instance.StartHost(RhinoDoc.ActiveDoc, 42069);
                this.host = RJamPlugin.Instance.GetHost(this.Document);
                this.host.mainUI = this;
                this.isStarted = true;
            }

            this.UpdateUI();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if(!buttonConnect.Enabled)
            {
                return;
            }

            this.UpdateDoc();

            if (this.isStarted)
            {
                if (RJamPlugin.Instance.HasHost(this.Document))
                {
                    if (!this.isConnected)
                    {
                        this.host = RJamPlugin.Instance.GetHost(this.Document);
                        this.host.ConnectToPartner(this.partnerIPTextbox.Text, 42069);
                        this.isConnected = true;
                        this.UpdateUI();
                    }
                    else
                    {
                        this.host = RJamPlugin.Instance.GetHost(this.Document);
                        this.host.DisconnectFromAll();
                        this.isConnected = false;
                        this.UpdateUI();
                    }
                }
            }
        }
    }
}
