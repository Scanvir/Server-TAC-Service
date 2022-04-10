using MyLib;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Threading;

namespace Server_TAC_Service
{
    public partial class ServerTAC : ServiceBase
    {
        public bool ServerWork;

        private Thread ServerThread;
        private Server server;
        private string ip;
        private int port = 1221;
        private LogFile log;

        public ServerTAC()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            log = new LogFile(@"C:\ServerTAC\ServerTAC.log");

            Sheduler shed = new Sheduler();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(OnChangeNetworkState);
            AutoStartServer();
        }
        protected override void OnStop()
        {
            server.StopServer();
            Thread.Sleep(500);
            ServerThread.Abort();
        }
        public void OnDebug()
        {
            OnStart(null);
        }
        private bool GetIP()
        {
            ip = "";

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipAddress in hostEntry.AddressList)
            {
                if (ipAddress.AddressFamily.ToString() != "InterNetwork")
                    continue;

                if (ipAddress.ToString().Substring(0, 8) == "192.168.")
                {
                    ip = ipAddress.ToString();
                    return true;
                }
            }

            if (ServerWork)
                server.StopServer();

            log.ToLog("Локальная сеть не подключена");
            return false;
        }
        private bool AutoStartServer()
        {
            if (GetIP() && !ServerWork)
            {
                ServerThread = new Thread(new ParameterizedThreadStart(StartServer));
                ServerThread.Start();
                Thread.Sleep(500);
                return true;
            }
            return false;
        }
        private void StartServer(object Param)
        {
            if (ip == "")
                return;

            server = new Server();
            server.onChange += OnChangeServerState;
            server.StartServer(ip, port);
        }
        private void OnChangeNetworkState(object sender, EventArgs e)
        {
            AutoStartServer();
        }
        private void OnChangeServerState(bool state)
        {
            ServerWork = state;
            log.ToLog("Status server work: " + ServerWork);
        }
    }
}
