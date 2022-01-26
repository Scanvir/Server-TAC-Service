using MyLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server_TAC_Service
{
    class Server
    {
        TcpListener Listener;
        
        public delegate void WorkServer(bool state);
        public event WorkServer onChange;

        private string sqlServ = "192.168.0.101";
        private string sqlUser = "sa";
        private string sqlPass = "987312";

        private SQL sql;
        private LogFile log;

        private bool ServerWork;

        public Server()
        {
            sql = new SQL(String.Format(@"Provider=SQLOLEDB;Data Source={0};User ID={1};Password={2};", sqlServ, sqlUser, sqlPass));
            log = new LogFile(@"C:\ServerTAC\Server.log");
            log.TruncateLog(3);
            ServerWork = false;
        }
        public void StartServer(string Address, int Port)
        {
            IPAddress address = IPAddress.Parse(Address);
            Listener = new TcpListener(address, Port);
            Listener.Start(); // Запускаем его
            ServerWork = true;
            onChange?.Invoke(ServerWork);
            log.ToLog("Сервер запущен");
            while (ServerWork)
            {
                try
                {
                    TcpClient Client = Listener.AcceptTcpClient();
                    Thread Thread = new Thread(new ParameterizedThreadStart(ClientThread));
                    Thread.Start(Client);
                }
                catch (Exception ex)
                {
                    log.ToLog("Ошибка сервера: " + ex.Message);
                }
            }
        }
        public void StopServer()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
            ServerWork = false;
            onChange?.Invoke(ServerWork);
            log.ToLog("Сервер остановлен");
        }
        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
        }
        void ClientThread(Object StateInfo)
        {
            try
            {
                Client client = new Client(sql, log);
                client.Init((TcpClient)StateInfo);
            }
            catch (Exception ex)
            {
                log.ToLog("Ошибка клиента: " + ex.Message);
            }
        }
    }
}
