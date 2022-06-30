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

        private LogFile log;
        private Config config;

        private bool ServerWork;

        public Server()
        {
            
            log = new LogFile(@"C:\ServerTAC\Server.log");
            log.TruncateLog(3);
            ServerWork = false;
        }
        public void StartServer(string Address, int Port, Config config)
        {
            IPAddress address = IPAddress.Parse(Address);
            this.config = config;
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
                catch (SocketException ex) when (ex.ErrorCode == 10004)
                {
                    return;
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
                Client client = new Client(log, config);
                client.Init((TcpClient)StateInfo);
            }
            catch (Exception ex)
            {
                log.ToLog("Ошибка клиента: " + ex.Message);
            }
        }
    }
}
