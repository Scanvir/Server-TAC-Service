using System.ServiceProcess;

namespace Server_TAC_Service
{
    static class Program
    {
        static void Main()
        {
            /*
            ServerTAC MyService = new ServerTAC();
            MyService.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            */

            ServiceBase[] ServicesToRun = new ServiceBase[] { new ServerTAC() };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
