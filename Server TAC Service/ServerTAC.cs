using MyLib;
using Newtonsoft.Json;
using System;
using System.IO;
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
        private Config config;
        private LogFile log;
        
        private string ip;
        private int port = 1221;
        
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
            log = new LogFile(@"C:\ServerTAC\ServerTAC.log");
            ReadConfig();
            Client client = new Client(log, config);
            OnStart(null);
        }
        private bool ReadConfig()
        {
            try
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);

                string configFile = strWorkPath + "\\Server TAC Config.json";

                using (StreamReader r = new StreamReader(configFile))
                {
                    string json = r.ReadToEnd();
                    config = JsonConvert.DeserializeObject<Config>(json);
                }

                log.ToLog("SERVER SQL: " + config.sqlServ);
                log.ToLog("SERVER SQL User: " + config.sqlUser);
                log.ToLog("SERVER SQL Database: " + config.sqlDatabase);

                return true;
            }
            catch (Exception ex)
            {
                log.ToLog("ERROR Read json config file " + ex.Message);
                return false;
            }
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
            if (GetIP() && ReadConfig() && !ServerWork)
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
            server.StartServer(ip, port, config);
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
