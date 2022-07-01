using MyLib;
using System;
using System.Diagnostics;
using System.Timers;

namespace Server_TAC_Service
{
    class Sheduler
    {
        private SQL sql;
        private LogFile log;
        private string strWorkPath;

        public Sheduler(Config config)
        {
            strWorkPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string sqlServ = config.sqlServ;
            string sqlUser = config.sqlUser;
            string sqlPass = config.sqlPass;

            sql = new SQL(String.Format(@"Provider=SQLOLEDB;Data Source={0};User ID={1};Password={2};", sqlServ, sqlUser, sqlPass));

            log = new LogFile(strWorkPath + "\\Sheduler.log");
            log.TruncateLog(3);
            Timer timer = new Timer()
            {
                Enabled = true,
                Interval = 1000 * 60
            };
            timer.Elapsed += GetTasks;
            log.ToLog("Таймер запущен");

        }
        private void GetTasks(object source, ElapsedEventArgs e)
        {
            DateTime current = DateTime.Now;
            int hh = current.Hour;
            int mm = current.Minute;
            int ss = current.Second;

            if (hh >= 9 && hh < 19)
                if (mm % 10 == 0)
                {
                    log.ToLog("Обновление ТАС запущено");
                    ExchangeTAC();
                    ClearDB();
                    log.ToLog("Обновление ТАС завершено");
                }
        }
        private void ExchangeTAC()
        {
            try
            {
                Process.Start(strWorkPath + "\\ExchangeTAC.vbs").WaitForExit(1000 * 60 * 10);
            }catch (Exception ex)
            {
                log.ToLog("Sheduler error: " + ex.Message);
            }
        }
        private void ClearDB()
        {
            try
            {
                sql.Execute("delete from [Web-Service].tac.OrderTab where Quantity = 0", log);
                sql.Execute("delete from [Web-Service].tac.OrderDoc where GUID not in(select GUID from [Web-Service].tac.OrderTab)", log);
                sql.Execute("delete from [Web-Service].tac.OrderDoc where DateDoc < DateAdd(Month, -3, GetDate())", log);
                sql.Execute("delete from [Web-Service].tac.OrderTab where GUID not in(select GUID from [Web-Service].Tac.OrderDoc)", log);
            } catch (Exception ex)
            {
                log.ToLog("Ошибка очистки DB: " + ex.Message);
            }
        }
    }
}
