using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIM.Connect.Simconnect;
using System.ServiceModel;
using SIM.Connect;
using SIM.Connect.Common;
using System.IO;
using SIM.Connect.Aircraft;
using SIM.Connect.Aircraft.Fuel;
using SIM.Connect.Aircraft.ElectricalSystems;
using SIM.Connect.Aircraft.PositionSpeed;
using SIM.Connect.Aircraft.FlightInstrumentation;

namespace SimConnect.ServiceHostApp
{
    public partial class Form1 : Form
    {
        const int WM_USER_SIMCONNECT = 0x0402;

        ServiceHost mHost;
        FileSystemWatcher mFileWatcher;
        FileInfo mFileInfo;
        Timer mFileWatchTimer;

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                SimconnectProvider.Instance.ReceiveMessage();
            }
            base.DefWndProc(ref m);
        }

        public Form1()
        {
            SimLogger.Log(LogMode.Info, "ServiceHost", "Initializing");

            InitializeComponent();

            this.startFileWatcher();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SimconnectProvider.Instance.UiHandle = this;

            this.loadLogFile();

            this.simconnectProviderBindingSource.DataSource = SimconnectProvider.Instance;
            this.aircraftProviderBindingSource.DataSource = AircraftProvider.Instance;
            this.fuelProviderBindingSource.DataSource = FuelProvider.Instance;
            this.electricalSystemsProviderBindingSource.DataSource = ElectricalSystemsProvider.Instance;
            this.positionSpeedProviderBindingSource.DataSource = PositionSpeedProvider.Instance;
            this.flightInstrumentationProviderBindingSource.DataSource = FlightInstrumentationProvider.Instance;
        }

        private void startFileWatcher()
        {
            this.mFileInfo = new FileInfo(this.getLogFileName());

            this.mFileWatchTimer = new Timer();
            this.mFileWatchTimer.Interval = 100;
            this.mFileWatchTimer.Tick += new EventHandler(delegate(object sender, EventArgs e)
            {
                this.mFileInfo.Refresh();
            });
            this.mFileWatchTimer.Enabled = true;
            this.mFileWatchTimer.Start();

            this.mFileWatcher = new FileSystemWatcher();
            this.mFileWatcher.Path = SimLogger.LogDirectory;
            this.mFileWatcher.Filter = "*.txt";
            this.mFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.LastAccess;
            this.mFileWatcher.InternalBufferSize = 32768; // 32Kb

            this.mFileWatcher.Changed += new FileSystemEventHandler(delegate(object sender, FileSystemEventArgs e)
            {
                try
                {
                    this.loadLogFile();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format("{0} {1} {2}", exc.InnerException.ToString(), exc.Message, exc.StackTrace));
                }
            });

            this.mFileWatcher.EnableRaisingEvents = true;
        }

        private void loadLogFile()
        {
            string wFilePath = this.getLogFileName();
            using (FileStream wFileStream = new FileStream(wFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                StreamReader wStreamReader = new StreamReader(wFileStream);
                if (this.richTextBox1.InvokeRequired)
                {
                    this.richTextBox1.Invoke(new MethodInvoker(delegate()
                    {
                        this.richTextBox1.Text = wStreamReader.ReadToEnd();
                        this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
                        this.richTextBox1.ScrollToCaret();
                        this.richTextBox1.Refresh();
                    }));
                }
                else
                {
                    this.richTextBox1.Text = wStreamReader.ReadToEnd();
                    this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
                    this.richTextBox1.ScrollToCaret();
                    this.richTextBox1.Refresh();

                }
                wStreamReader.Close();
                wFileStream.Close();
            }
        }

        private string getLogFileName()
        {
            var wDirectory = new DirectoryInfo(SimLogger.LogDirectory);
            var wLogFileName = (from f in wDirectory.GetFiles()
                                orderby f.LastWriteTime descending
                                select f).First();

            return wLogFileName.FullName;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            this.mHost = new ServiceHost(typeof(SimconnectService));
            try
            {
                SimconnectProvider.Instance.InitializeSimconnect(this.Text, this.Handle);
                this.mHost.Open();

                this.lblServiceName.Text = this.mHost.Description.Name;
                this.lblServiceAddress.Text = this.mHost.BaseAddresses[0].AbsoluteUri;

                this.btnStart.Enabled = false;
                this.btnStop.Enabled = true;

                SimLogger.Log(LogMode.Info, "ServiceHost", "Service has started");
            }
            catch (Exception ex)
            {
                SimLogger.Log(LogMode.Error, "ServiceHost", "Error", ex.Message, ex.StackTrace);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                SimconnectProvider.Instance.ResetSimconnect();

                this.mHost.Close();
                this.mHost = null;

                SimLogger.Log(LogMode.Info, "ServiceHost", "Service has stopped");
            }

            this.lblServiceName.Text = string.Empty;
            this.lblServiceAddress.Text = string.Empty;

            this.btnStart.Enabled = true;
            this.btnStop.Enabled = false;
        }

        #region Console

        bool mHideConsole = false;

        private void lnklblToggle_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.mHideConsole)
            {
                this.mHideConsole = false;
                this.richTextBox1.Visible = true;
                this.lnklblToggle.Text = "Hide";
                
                // TODO: logic to start processing log
            }
            else
            {
                this.mHideConsole = true;
                this.richTextBox1.Visible = false;
                this.lnklblToggle.Text = "Show";
                
                // TODO: logic to stop processing log
            }
        }

        #endregion
    }
}
