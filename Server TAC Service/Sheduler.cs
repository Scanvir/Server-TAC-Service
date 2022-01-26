using MyLib;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Server_TAC_Service
{
    class Sheduler
    {
        LogFile log;

        public Sheduler()
        {
            log = new LogFile(@"C:\ServerTAC\Sheduler.log");
            log.TruncateLog(3);
            _ = StartTimer();
            log.ToLog("Таймер запущен");

        }
        private async Task StartTimer()
        {
            var autoEvent = new AutoResetEvent(false);
            var stateTimer = new Timer(Tick, autoEvent, 1000, 1000 * 60);
            var getResult = Task.Run(() => autoEvent.WaitOne());
            await getResult;
        }
        private void Tick(Object stateInfo)
        {
            _ = AsyncTasks();
        }
        private async Task AsyncTasks()
        {
            var getResult = Task.Run(() => GetTasks());
            await getResult;
        }
        private void GetTasks()
        {
            DateTime current = DateTime.Now;
            int hh = current.Hour;
            int mm = current.Minute;
            int ss = current.Second;

            if (hh > 9 && hh < 19)
                if (mm % 10 == 0)
                {
                    log.ToLog("Обновление ТАС запущено");
                    ExchangeTAC();
                    log.ToLog("Обновление ТАС завершено");
                }
        }
        private void ExchangeTAC()
        {
            Process.Start(@"C:\ServerTAC\ExchangeTAC.vbs").WaitForExit(1000 * 60 * 10);
        }
    }
}
