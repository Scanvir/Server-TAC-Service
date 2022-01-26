using MyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Server_TAC_Service
{
    class Client
    {
        private SQL sql;
        private LogFile log;
        private string TrueVersion = "0.8";
        private bool isTaraRest;

        public const string RootDir = @"C:\ServerTAC";

        public Client(SQL mainSql, LogFile mainlog)
        {
            sql = mainSql;
            log = mainlog;
            string settingsFileName = @"C:\ServerTAC\settings.ini";
            if (File.Exists(settingsFileName))
            {
                string text = File.ReadAllText(settingsFileName);
                log.ToLog("isTaraRests");
                log.ToLog(text);
                if (text == "1")
                    isTaraRest = false;
                else
                    isTaraRest = true;
            } else
            {
                isTaraRest = true;
            }
        }

        public void Init(TcpClient Client)
        {
            string Request = "";

            Client.ReceiveBufferSize = 8192;
            Client.SendBufferSize = 8192;

            byte[] Buffer = new byte[Client.ReceiveBufferSize];

            StringBuilder response = new StringBuilder();
            NetworkStream stream = Client.GetStream();
            var reader = new BinaryReader(stream, Encoding.UTF8, true);

            do
            {
                int bytes = reader.Read(Buffer, 0, Buffer.Length);
                System.Threading.Thread.Sleep(500);
                response.Append(Encoding.UTF8.GetString(Buffer, 0, bytes));
            }
            while (stream.DataAvailable); // пока данные есть в потоке

            Request = response.ToString();
            //log.ToLog("Request: " + Request);

            string json = "";

            if (Request.Substring(0, 4) == "POST")
            {
                int first = Request.IndexOf("{\"Auth\"");
                int last = Request.LastIndexOf('}');
                json = Request.Substring(first, last - first + 1);
            }

            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");
            if (ReqMatch == Match.Empty)
            {
                SendError(Client, 400);
                return;
            }

            string RequestUri = ReqMatch.Groups[0].Value;
            if (RequestUri == "")
            {
                SendError(Client, 400);
                return;
            }

            if (Request.Substring(0, 3) == "GET")
            {
                GetMethod(Client, RequestUri);
            }
            if (Request.Substring(0, 4) == "POST")
            {
                PostMethod(Client, RequestUri, json);
            }
        }
        public void PostMethod(TcpClient Client, string RequestUri, string json)
        {
            log.ToLog("Получен POST");
            string[] Param;

            Param = RequestUri.Split(' ');

            if (Param[1] == "/ExportDocs")
            {
                //log.ToLog("Экспорт документов: " + json);
                log.ToLog("Длина данных: " + json.Length);
                Update update = JsonConvert.DeserializeObject<Update>(json);
                string agent = update.Auth.Code;

                log.ToLog("Экспорт документов: " + update.Auth.Name + " version: " + update.Version);

                // PKO
                List<DocReturn> pkoReturn = new List<DocReturn>();

                PKO[] pkoList = update.PKO;
                foreach (PKO pko in pkoList)
                {
                    string query = "SELECT status FROM [Web-service].tac.PKO where GUID = '" + pko.GUID + "'";
                    //log.ToLog(query);
                    DataTable tbl = sql.SelectQuery(query, log, "Web-service");

                    if (tbl.Rows.Count == 0)
                    {
                        query = "INSERT INTO [Web-service].tac.PKO VALUES ('" + pko.GUID + "', '" + agent + "', '" + pko.DateDoc + "', '" + pko.NumDoc + "', " + pko.KlientCode + ", " + pko.DotCode + ", " + pko.Summ.ToString().Replace(",", ".") + ", '" + pko.DatePay + "', 1, null)";
                        //log.ToLog(query);
                        if (sql.Execute(query, log))
                        {
                            pkoReturn.Add(new DocReturn()
                            {
                                GUID = pko.GUID,
                                Status = 1
                            });
                        }
                    }
                    else
                    {
                        pkoReturn.Add(new DocReturn()
                        {
                            GUID = pko.GUID,
                            Status = int.Parse(tbl.Rows[0][0].ToString())
                        });
                    }
                }

                // Order
                List<DocReturn> orderReturn = new List<DocReturn>();

                Order[] orderList = update.Order;
                foreach (Order order in orderList)
                {
                    string query = "SELECT status FROM [Web-service].tac.OrderDoc where GUID = '" + order.GUID + "'";
                    DataTable tbl = sql.SelectQuery(query, log, "Web-service");

                    if (tbl.Rows.Count == 0)
                    {
                        query = "INSERT INTO [Web-service].tac.OrderDoc VALUES ('" + order.GUID + "', '" + agent + "', '" + order.DateDoc + "', " + order.KlientCode + ", " + order.DotCode + ", 1, " + order.FlagA + ", " + order.FlagF + ", '" + order.Comment + "', " + order.Form + ",null)";
                        if (sql.Execute(query, log))
                        {
                            foreach (OrderTab tab in order.OrderTab)
                            {
                                query = "INSERT INTO [Web-service].tac.OrderTab VALUES ('" + order.GUID + "', " + tab.GoodCode + ", " + tab.Quantity + ", " + tab.PriceUAH + ", " + tab.Summ + ")";
                                //log.ToLog(query);
                                sql.Execute(query, log);
                            }
                            orderReturn.Add(new DocReturn()
                            {
                                GUID = order.GUID,
                                Status = 1
                            });
                        }
                    }
                    else
                    {
                        orderReturn.Add(new DocReturn()
                        {
                            GUID = order.GUID,
                            Status = int.Parse(tbl.Rows[0][0].ToString())
                        });
                    }
                }

                // Tara Facing

                List<DocReturn> taraFacingReturn = new List<DocReturn>();

                List<TaraFacing> taraFacings = update.TaraFacing;

                foreach (TaraFacing taraFacing in taraFacings)
                {
                    string query = "SELECT status FROM [Web-service].tac.TaraFacing where GUID = '" + taraFacing.GUID + "'";
                    //log.ToLog(query);
                    DataTable tbl = sql.SelectQuery(query, log, "Web-service");

                    if (tbl.Rows.Count == 0)
                    {
                        query = "INSERT INTO [Web-service].tac.TaraFacing VALUES ('" + taraFacing.GUID + "', '" + agent + "', '" + taraFacing.DateDoc + "', " + taraFacing.KlientCode + ", " + taraFacing.DotCode + ", " + taraFacing.TaraCode + ", " + taraFacing.Quantity.ToString().Replace(",", ".") + ", 1, null)";
                        //log.ToLog(query);
                        if (sql.Execute(query, log))
                        {
                            taraFacingReturn.Add(new DocReturn()
                            {
                                GUID = taraFacing.GUID,
                                Status = 1
                            });
                        }
                    }
                    else
                    {
                        taraFacingReturn.Add(new DocReturn()
                        {
                            GUID = taraFacing.GUID,
                            Status = int.Parse(tbl.Rows[0][0].ToString())
                        });
                    }
                }

                // Oborud Facing

                List<DocReturn> oborudFacingReturn = new List<DocReturn>();

                List<OborudFacing> oborudFacings = update.OborudFacing;

                foreach (OborudFacing oborudFacing in oborudFacings)
                {
                    string query = "SELECT status FROM [Web-service].tac.OborudFacing where GUID = '" + oborudFacing.GUID + "'";
                    //log.ToLog(query);
                    DataTable tbl = sql.SelectQuery(query, log, "Web-service");

                    if (tbl.Rows.Count == 0)
                    {
                        query = "INSERT INTO [Web-service].tac.OborudFacing VALUES ('" + oborudFacing.GUID + "', '" + agent + "', '" + oborudFacing.DateDoc + "', " + oborudFacing.KlientCode + ", " + oborudFacing.DotCode + ", '" + oborudFacing.OborudCode + "', " + oborudFacing.Quantity.ToString().Replace(",", ".") + ", 1, null)";
                        //log.ToLog(query);
                        if (sql.Execute(query, log))
                        {
                            oborudFacingReturn.Add(new DocReturn()
                            {
                                GUID = oborudFacing.GUID,
                                Status = 1
                            });
                        }
                    }
                    else
                    {
                        oborudFacingReturn.Add(new DocReturn()
                        {
                            GUID = oborudFacing.GUID,
                            Status = int.Parse(tbl.Rows[0][0].ToString())
                        });
                    }
                }

                Update newUpdate = new Update();
                newUpdate.PKOReturn = pkoReturn.ToArray();
                newUpdate.OrderReturn = orderReturn.ToArray();
                newUpdate.TaraFacingReturn = taraFacingReturn;
                newUpdate.OborudFacingReturn = oborudFacingReturn;
                json = JsonConvert.SerializeObject(newUpdate);
                log.ToLog("Сформирован ответ:" + json);
                try
                {
                    SendResponse(Client, json);
                }
                catch (Exception ex)
                {
                    log.ToLog("Ошибка отправки данных клиенту: " + ex.Message);
                }
            }
        }
        private void GetMethod(TcpClient Client, string RequestUri)
        {
            log.ToLog("Получен GET");
            string[] Param;
            string fileLink = RequestUri;

            RequestUri = RequestUri.Remove(0, RequestUri.IndexOf('?') + 1);
            RequestUri = RequestUri.Remove(RequestUri.IndexOf(' '));

            if (RequestUri == "GET")
            {
                fileLink = fileLink.Remove(0, fileLink.IndexOf(' ') + 1);
                fileLink = fileLink.Remove(fileLink.IndexOf(' '));
                SendFile(Client, fileLink);
                return;
            }

            Param = RequestUri.Split('=');

            if (Param[0] == "GetAuth")
            {
                string version = "";
                try
                {
                    version = Param[2];
                }
                catch { }
                Auth auth = GetAuth(Param[1], version);
                string json = JsonConvert.SerializeObject(auth);
                SendResponse(Client, json);

                log.ToLog("Авторизация: " + auth.Name + " version: " + version);
            }
            else if (Param[0] == "GetUpdate")
            {
                string version = "";
                try
                {
                    version = Param[2];
                }
                catch { }
                Auth auth = GetAuth(Param[1], version);
                Update update = GetUpdate(Param[1], version);
                string json = JsonConvert.SerializeObject(update);
                SendResponse(Client, json);


                log.ToLog("Полное обновление: " + auth.Name + " version: " + version);
            }
            else if (Param[0] == "GetDocs")
            {
                string version = "";
                try
                {
                    version = Param[2];
                }
                catch { }
                Auth auth = GetAuth(Param[1], version);
                Update update = GetDocs(Param[1], version);
                string json = JsonConvert.SerializeObject(update);
                SendResponse(Client, json);

                log.ToLog("Импорт из архива: " + auth.Name + " version: " + version);
            }
            else if (Param[0] == "GetImg")
            {
                int code = int.Parse(Param[1]);
                string FileName = @"C:\ServerTAC\Photo\" + code + ".jpg";
                if (File.Exists(FileName))
                    SendFile(Client, @"C:\ServerTAC\Photo\" + code + ".jpg");
                else
                    SendFile(Client, @"C:\ServerTAC\Photo\no-photo.jpg");
            }
            else
            {
                SendError(Client, 400);
                return;
            }
        }
        private void SendResponse(TcpClient Client, string json)
        {
            string Str = "HTTP/1.1 200 OK\nContent-type: Application/json;charset=UTF-8\nContent-Length:" + Encoding.UTF8.GetByteCount(json).ToString() + "\n\n" + json;
            byte[] Buffer = Encoding.UTF8.GetBytes(Str);
            Client.GetStream().Write(Buffer, 0, Encoding.UTF8.GetByteCount(Str));
            Client.Close();
        }
        private void SendFile(TcpClient Client, string fileName)
        {
            try
            {
                byte[] Buffer = new byte[1024];
                FileStream FS = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                string Headers = "HTTP/1.1 200 OK\nContent-Type: image/jpeg\nContent-Length: " + FS.Length + "\n\n";
                byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
                Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
                while (FS.Position < FS.Length)
                {
                    int Count = FS.Read(Buffer, 0, Buffer.Length);
                    Client.GetStream().Write(Buffer, 0, Count);
                }

                IPAddress addr = ((IPEndPoint)Client.Client.RemoteEndPoint).Address;
                IPHostEntry entry = Dns.GetHostEntry(addr);

                FS.Close();
                Client.Close();
            }
            catch
            {
                SendError(Client, 400);
                Client.Close();
            }
        }
        private void SendError(TcpClient Client, int Code)
        {
            try
            {
                Console.WriteLine("Error code: " + Code.ToString());
                // Получаем строку вида "200 OK"
                // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
                string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
                // Код простой HTML-странички
                string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
                // Необходимые заголовки: ответ сервера, тип и длина содержимого. После двух пустых строк - само содержимое
                string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
                // Приведем строку к виду массива байт
                byte[] Buffer = Encoding.ASCII.GetBytes(Str);
                // Отправим его клиенту
                Client.GetStream().Write(Buffer, 0, Buffer.Length);
                // Закроем соединение
                Client.Close();
            }
            catch
            {

            }
        }

        private Update GetUpdate(string code, string version)
        {
            Update update = new Update();
            update.Auth = GetAuth(code, version);
            if (version == TrueVersion)
            {
                update.Klient = new List<Klient>();
                update.Dot = new List<Dot>();
                if (update.Auth.TAC != 0)
                {
                    if (update.Auth.Type == 1)
                    {
                        GetOborud(update, 1, code);
                        GetTara(update, 1);
                        return update;
                    }
                    else if (update.Auth.Type == 2)
                    {
                        GetOborudAgent(update, code);
                        GetTaraAgent(update, code);
                        GetUpdateAgent(update, code);
                        return update;
                    }
                }
            }
            return update;
        }
        private Update GetDocs(string code, string version)
        {
            Update update = new Update();
            update.Auth = GetAuth(code, version);
            if (version == TrueVersion)
            {
                if (update.Auth.TAC != 0)
                {
                    if (update.Auth.Type == 2)
                        GetUpdateAgentDocs(update, code);
                }
            }
            return update;
        }
        private Update GetUpdateAgentDocs(Update update, string code)
        {
            update.PKO = GetPKO(code).ToArray();
            update.Order = GetOrder(code).ToArray();
            return update;
        }
        private Update GetUpdateAgent(Update update, string code)
        {
            update.GoodView = GetGoodViews(code).ToArray();
            update.Klient = GetKlients(code);
            update.Dot = GetDots(code);
            update.Good = GetGoods(code).ToArray();
            update.GoodsDirectory = GetGoodsDirectory(code);
            update.GoodRests = GetGoodRests(code).ToArray();
            update.Debet = GetDebet(code).ToArray();
            return update;
        }
        private List<GoodsDirectory> GetGoodsDirectory(string code)
        {
            string query = "select DISTINCT p1.CODE, rtrim(p1.DESCR), p2.CODE, rtrim(p2.DESCR), p3.CODE, rtrim(p3.DESCR), p4.CODE, rtrim(p4.DESCR), p5.CODE, rtrim(p5.DESCR) from SC92 t " +
                "left join SC92 p5 on p5.id = t.parentid " +
                "left join SC92 p4 on p4.id = p5.parentid " +
                "left join SC92 p3 on p3.id = p4.parentid " +
                "left join SC92 p2 on p2.id = p3.parentid " +
                "left join SC92 p1 on p1.id = p2.parentid " +
                "where t.SP4032 in (select SP5213 from SC5204 where parentext in (select id from SC2286 where sp4767 = '" + code + "')) and t.isfolder <> 1 and t.ismark <> 1 " +
                "order by p1.Code,p2.Code,p3.Code,p4.Code,p5.Code";
            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Каталог товаров: " + tbl.Rows.Count);

            GoodsDirectoryCreator creator = new GoodsDirectoryCreator();

            return creator.Create(tbl);
        }
        private List<PKO> GetPKO(string code)
        {
            DateTime dd = DateTime.Now;
            string query = "select GUID, DateDoc, NumDoc, KlientCode, DotCode, Summ, DatePay, Status from tac.PKO where Upper(agentCode) = '" + code.ToUpper() + "'";
            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "Web-service");

            log.ToLog("ПКО: " + tbl.Rows.Count);

            List<PKO> pko = new List<PKO>();

            foreach (DataRow row in tbl.Rows)
            {
                pko.Add(new PKO()
                {
                    GUID = row[0].ToString(),
                    DateDoc = DateTime.Parse(row[1].ToString()).ToString("yyyy-MM-dd"),
                    NumDoc = row[2].ToString(),
                    KlientCode = int.Parse(row[3].ToString()),
                    DotCode = int.Parse(row[4].ToString()),
                    Summ = sql.DoubleFromSQL(row[5].ToString()),
                    DatePay = DateTime.Parse(row[6].ToString()).ToString("yyyy-MM-dd"),
                    Status = int.Parse(row[7].ToString())
                });
            }
            log.ToLog("Test-7");
            return pko;
        }
        private List<Order> GetOrder(string code)
        {
            DateTime dd = DateTime.Now;
            string query = "select GUID, DateDoc, KlientCode, DotCode, Status, FlagA, FlagF, Comment, Form from tac.OrderDoc where Upper(agentCode) = '" + code.ToUpper() + "'";
            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "Web-service");

            log.ToLog("Заказов: " + tbl.Rows.Count);

            List<Order> order = new List<Order>();

            foreach (DataRow row in tbl.Rows)
            {
                query = "select GoodCode, Quantity, PriceUAH, Summ from tac.OrderTab where GUID = '" + row[0].ToString() + "'";
                //log.ToLog(query);
                DataTable tbl1 = sql.SelectQuery(query, log, "Web-service");

                log.ToLog("Строк: " + tbl1.Rows.Count);

                List<OrderTab> tab = new List<OrderTab>();

                foreach (DataRow row1 in tbl1.Rows)
                {
                    tab.Add(new OrderTab()
                    {
                        GUID = row[0].ToString(),
                        GoodCode = int.Parse(row1[0].ToString()),
                        Quantity = int.Parse(row1[1].ToString()),
                        PriceUAH = sql.DoubleFromSQL(row1[2].ToString()),
                        Summ = sql.DoubleFromSQL(row1[3].ToString())
                    });
                }

                order.Add(new Order()
                {
                    GUID = row[0].ToString(),
                    DateDoc = DateTime.Parse(row[1].ToString()).ToString("yyyy-MM-dd"),
                    KlientCode = int.Parse(row[2].ToString()),
                    DotCode = int.Parse(row[3].ToString()),
                    Status = int.Parse(row[4].ToString()),
                    FlagA = int.Parse(row[5].ToString()),
                    FlagF = int.Parse(row[6].ToString()),
                    Comment = row[7].ToString(),
                    Form = int.Parse(row[8].ToString()),
                    OrderTab = tab
                });
            }
            return order;
        }
        private List<Debet> GetDebet(string code)
        {
            DateTime dd = DateTime.Now;
            string query = "SELECT rtrim(Ж.Docno), Ж.DATE_TIME_IDDOC,k.code,d.code,sum(sp1063),r.SP6340 " +
                "FROM rg1060(nolock) " +
                "left join _1SJOURN Ж(nolock)  on Ж.IDDOC = SUBSTRING(sp1062, 5, 9) " +
                "left join sc72 k(nolock) on k.id = sp1061 " +
                "left join SC2286 s(nolock) on s.id = sp5413 " +
                "left join SC4542 d(nolock) on d.id = sp4672 " +
                "left join DH1157 r (nolock) on r.iddoc = Ж.iddoc " +
                "WHERE rg1060.period = '" + dd.ToString("yyyyMM01") + "' and sp4767 = '" + code + "' and sp1063<> 0 " +
                "and Ж.IDDOCDEF = '1157' " +
                "group by Ж.Docno,Ж.DATE_TIME_IDDOC,k.code,d.code,r.SP6340";
            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Дебеторка: " + tbl.Rows.Count);

            List<Debet> debets = new List<Debet>();

            foreach (DataRow row in tbl.Rows)
            {
                debets.Add(new Debet()
                {
                    NumDoc = row[0].ToString(),
                    DateDoc = row[1].ToString().Substring(0, 8),
                    KlientCode = int.Parse(row[2].ToString()),
                    DotCode = int.Parse(row[3].ToString()),
                    Dolg = sql.DoubleFromSQL(row[4].ToString()),
                    DatePay = DateTime.Parse(row[5].ToString())
                });
            }
            return debets;
        }
        private List<Dot> GetDots(string code)
        {
            string query = "select p.code, d.code, rtrim(d.Descr), rtrim(sp4544) from SC4542 d " +
                "left join SC72 p on p.id = parentext " +
                "where p.id in (select k.id from DH5292 left join SC72 k on k.id = SP5295 left join SC2286 s on s.id = SP5297 where sp4767 = '" + code + "')";
            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Точки: " + tbl.Rows.Count);

            List<Dot> dots = new List<Dot>();

            foreach (DataRow row in tbl.Rows)
            {
                dots.Add(new Dot()
                {
                    KlientCode = int.Parse(row[0].ToString()),
                    DotCode = int.Parse(row[1].ToString()),
                    DotName = row[2].ToString(),
                    DotAddress = row[3].ToString()
                });
            }
            return dots;
        }
        private List<GoodRest> GetGoodRests(string code)
        {
            DateTime dd = DateTime.Now;
            string query = "select t.code, con.value, t2.SP1055, SP5394 from(select objid, max(date) date from sql2009.._1SCONST with(nolock) where id = 5230 group by objid) t1 " +
                "left join _1SCONST con with(nolock) on con.objid = t1.objid and con.date = t1.date  and con.id = 5230 left join SC5232 c with(nolock) on c.id = t1.objid " +
                "left join SC92 t with(nolock) on t.id = c.parentext " +
                "left join SC5234 tc with(nolock) on tc.id = c.SP5229 " +
                "left join(select SP1053, sum(SP1055) SP1055 from RG1051 where period = '" + dd.ToString("yyyyMM01") + "' and SP1052 = '     GMB ' and SP1055 > 0 group by SP1053) t2 on t2.SP1053 = t.id " +
                "where t.id in (select t.id from SC92 t where t.SP4032 in (select SP5213 from SC5204 where parentext in (select id from SC2286 where sp4767 = '" + code + "')) and t.isfolder <> 1 and t.ismark <> 1) " +
                "and tc.code = 1";
            log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Остатки: " + tbl.Rows.Count);

            List<GoodRest> goodRests = new List<GoodRest>();

            foreach (DataRow row in tbl.Rows)
            {
                try{
                    double price = sql.DoubleFromSQL(row[1].ToString());
                    if (int.Parse(row[3].ToString()) == 1)
                        price *= 1.525;

                    goodRests.Add(new GoodRest()
                    {
                        Code = int.Parse(row[0].ToString()),
                        Price = Math.Round(price, 2),
                        Quantity = sql.DoubleFromSQL(row[2].ToString())
                    });
                }catch(Exception ex)
                {
                    log.ToLog("Error: " + ex.Message + " = " + row[0].ToString());
                }
            }
            return goodRests;
        }
        private List<Good> GetGoods(string code)
        {
            string query = "select t.code, rtrim(t.descr), p.code, t.SP4473, kv.code from SC92 t " +
                "left join SC5207 kv on kv.id = SP4032 " +
                "left join SC92 p on p.id = t.parentid " +
                "where t.SP4032 in (select SP5213 from SC5204 where parentext in (select id from SC2286 where sp4767 = '" + code + "')) and t.isfolder <> 1 and t.ismark <> 1; ";
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Товары: " + tbl.Rows.Count);

            List<Good> goods = new List<Good>();

            foreach (DataRow row in tbl.Rows)
            {
                goods.Add(new Good()
                {
                    Code = int.Parse(row[0].ToString()),
                    Name = row[1].ToString(),
                    ParentID = int.Parse(row[2].ToString()),
                    Box = int.Parse(row[3].ToString()),
                    GoodView = row[4].ToString()
                });
            }
            return goods;
        }
        private List<GoodView> GetGoodViews(string code)
        {
            string query = "select kv.code, rtrim(kv.descr) from SC5204 v " +
                "left join SC5207 kv on kv.id = SP5213 " +
                "where parentext in (select id from SC2286 where sp4767 = '" + code + "')";

            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Виды товаров: " + tbl.Rows.Count);

            List<GoodView> goodViews = new List<GoodView>();

            foreach (DataRow row in tbl.Rows)
            {
                goodViews.Add(new GoodView()
                {
                    Сode = row[0].ToString(),
                    Name = row[1].ToString()
                });
            }
            return goodViews;
        }
        private List<Klient> GetKlients(string code)
        {
            string query = "select k.code, rtrim(k.DESCR) from DH5292 " +
                "left join SC72 k on k.id = SP5295 " +
                "left join SC2286 s on s.id = SP5297 " +
                "where sp4767 = '" + code + "'";

            //log.ToLog(query);
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            log.ToLog("Клиенты: " + tbl.Rows.Count);

            List<Klient> klients = new List<Klient>();

            foreach (DataRow row in tbl.Rows)
            {
                klients.Add(new Klient()
                {
                    KlientCode = int.Parse(row[0].ToString()),
                    KlientName = row[1].ToString()
                });
            }
            return klients;
        }
        private Update GetOborud(Update update, int withKlient, string code)
        {
            string query = "SELECT O.Code ObCode, rtrim(O.descr) ObName, K.Code KlientCode, rtrim(K.Descr) KlientName, t.Code DotCode, rtrim(T.Descr) DotName, rtrim(t.SP4544) DotAddress, sp5550 Qty, rtrim(SP5544) " +
                "FROM rg5545(nolock) " +
                "left join sc5541 O WITH(nolock) on O.ID = sp5546 " +
                "left join SC72 K(nolock) on k.id = sp5547 " +
                "left join SC4542 t(nolock) on t.id = sp5548 " +
                "WHERE rg5545.period = '" + DateTime.Now.ToString("yyyyMM01") + "' and sp5550 > 0 " +
                "and O.sp6421 in (select id from SC5210 where id in (select sp5212 from SC5207 where id in (select SP5213 from SC5204 where parentext in (select id from SC2286 where SP4767 = '" + code + "' and SP5667 = 1 and SP6320 = 1))))";
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            List<OborudRest> oborudRests = new List<OborudRest>();
            List<Oborud> oborud = new List<Oborud>();
            List<Klient> klient = update.Klient;
            List<Dot> dot = update.Dot;

            foreach (DataRow row in tbl.Rows)
            {
                oborudRests.Add(new OborudRest()
                {
                    OborudCode = row[0].ToString(),
                    KlientCode = int.Parse(row[2].ToString()),
                    DotCode = int.Parse(row[4].ToString()),
                    Qty = sql.DoubleFromSQL(row[7].ToString())
                });

                int i = oborud.FindIndex(x => x.OborudCode == row[0].ToString());
                if (i == -1)
                    oborud.Add(new Oborud()
                    {
                        OborudCode = row[0].ToString(),
                        OborudName = row[1].ToString(),
                        OborudInventCode = row[8].ToString()
                    });
                if (withKlient == 1)
                {
                    i = klient.FindIndex(x => x.KlientCode == int.Parse(row[2].ToString()));
                    if (i == -1)
                        klient.Add(new Klient()
                        {
                            KlientCode = int.Parse(row[2].ToString()),
                            KlientName = row[3].ToString()
                        });
                    i = dot.FindIndex(x => x.KlientCode == int.Parse(row[2].ToString()) & x.DotCode == int.Parse(row[4].ToString()));
                    if (i == -1)
                        dot.Add(new Dot()
                        {
                            KlientCode = int.Parse(row[2].ToString()),
                            DotCode = int.Parse(row[4].ToString()),
                            DotName = row[5].ToString(),
                            DotAddress = row[6].ToString()
                        });
                }
            }

            update.OborudRests = oborudRests.ToArray();
            update.Oborud = oborud.ToArray();
            update.Klient = klient;
            update.Dot = dot;

            return update;
        }
        private Update GetOborudAgent(Update update, string code)
        {
            string query = "SELECT O.Code ObCode, rtrim(O.descr) ObName, K.Code KlientCode, rtrim(K.Descr) KlientName, t.Code DotCode, rtrim(T.Descr) DotName, rtrim(t.SP4544) DotAddress, sp5550 Qty, rtrim(SP5544) " +
                "FROM rg5545(nolock) " +
                "left join sc5541 O WITH(nolock) on O.ID = sp5546 " +
                "left join SC72 K(nolock) on k.id = sp5547 " +
                "left join SC4542 t(nolock) on t.id = sp5548 " +
                "left join SC2286 S(nolock) on S.id = SP5549 " +
                "WHERE rg5545.period = '" + DateTime.Now.ToString("yyyyMM01") + "' and S.SP4767 = '" + code + "' and sp5550 > 0 " +
                "and O.sp6421 in (select id from SC5210 where id in (select sp5212 from SC5207 where id in (select SP5213 from SC5204 where parentext in " +
                "(select id from SC2286 where SP4767 = '" + code + "' and SP5667 = 2 and SP6320 = 1))))";
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            List<OborudRest> oborudRests = new List<OborudRest>();
            List<Oborud> oborud = new List<Oborud>();

            foreach (DataRow row in tbl.Rows)
            {
                oborudRests.Add(new OborudRest()
                {
                    OborudCode = row[0].ToString(),
                    KlientCode = int.Parse(row[2].ToString()),
                    DotCode = int.Parse(row[4].ToString()),
                    Qty = sql.DoubleFromSQL(row[7].ToString())
                });

                int i = oborud.FindIndex(x => x.OborudCode == row[0].ToString());
                if (i == -1)
                    oborud.Add(new Oborud()
                    {
                        OborudCode = row[0].ToString(),
                        OborudName = row[1].ToString(),
                        OborudInventCode = row[8].ToString()
                    });
            }

            update.OborudRests = oborudRests.ToArray();
            update.Oborud = oborud.ToArray();

            return update;
        }
        private Update GetTaraAgent(Update update, string code)
        {
            string query = "SELECT T.code TaraCode, rtrim(T.descr) TaraName, K.code KlientCode, rtrim(K.Descr) KlientName, A.code DotCode, rtrim(A.Descr) DotName, rtrim(A.SP4544) DotAddress, -sum(sp4451) qty  " +
                "FROM rg4447(nolock) " +
                "left join sc92 T(nolock) on T.ID = sp4450 " +
                "left join SC72 K(nolock) on K.ID = SP4449 " +
                "left join SC4542 A(nolock) on A.ID = sp6386 " +
                "left join SC2286 S(nolock) on S.ID = SP6387 " +
                "where T.sp4032 = '    4N   ' and period = '" + DateTime.Now.ToString("yyyyMM01") + "' and S.SP4767 = '" + code + "' and A.code is not null " +
                "group by T.code, T.descr, K.code, K.Descr, A.code, A.Descr, A.SP4544, S.SP4767, S.DESCR " +
                "having SUM(sp4451) <> 0";
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            List<TaraRest> taraRests = new List<TaraRest>();

            foreach (DataRow row in tbl.Rows)
            {
                Double Qty = sql.DoubleFromSQL(row[7].ToString());
                if (isTaraRest)
                    Qty = 0;

                taraRests.Add(new TaraRest()
                {
                    TaraCode = int.Parse(row[0].ToString()),
                    KlientCode = int.Parse(row[2].ToString()),
                    DotCode = int.Parse(row[4].ToString()),
                    Qty = Qty,
                    Delay = 0
                });
            }

            query = "select code, rtrim(descr) from SC92 where sp96 = '     X   ' and sp4032 = '    4N   '";
            tbl = sql.SelectQuery(query, log, "sql2009");
            List<Tara> tara = new List<Tara>();

            foreach (DataRow row in tbl.Rows)
            {
                tara.Add(new Tara()
                {
                    TaraCode = int.Parse(row[0].ToString()),
                    TaraName = row[1].ToString()
                });
            }

            update.TaraRests = taraRests.ToArray();
            update.Tara = tara.ToArray();

            return update;
        }
        private Update GetTara(Update update, int withKlient)
        {
            string query = "SELECT T.code TaraCode, rtrim(T.descr) TaraName, K.code KlientCode, rtrim(K.Descr) KlientName, A.code DotCode, rtrim(A.Descr) DotName, rtrim(A.SP4544) DotAddress, -sum(sp4451) qty FROM rg4447 (nolock) " +
                "left join sc92 T(nolock) on T.ID = sp4450 " +
                "left join SC72 K(nolock) on K.ID = SP4449 " +
                "left join SC4542 A(nolock) on A.ID = sp6386 " +
                "where T.sp4032 = '    4N   ' and period = '" + DateTime.Now.ToString("yyyyMM01") + "' and A.code is not null " +
                "group by T.code, T.descr, K.code, K.Descr, A.code, A.Descr, A.SP4544 " +
                "having SUM(sp4451) < 0";
            DataTable tbl = sql.SelectQuery(query, log, "sql2009");

            List<TaraRest> taraRests = new List<TaraRest>();
            List<Tara> tara = new List<Tara>();
            List<Klient> klient = update.Klient;
            List<Dot> dot = update.Dot;

            foreach (DataRow row in tbl.Rows)
            {
                Double Qty = sql.DoubleFromSQL(row[7].ToString());
                if (isTaraRest)
                    Qty = 0;

                taraRests.Add(new TaraRest()
                {
                    TaraCode = int.Parse(row[0].ToString()),
                    KlientCode = int.Parse(row[2].ToString()),
                    DotCode = int.Parse(row[4].ToString()),
                    Qty = Qty,
                    Delay = 0
                });

                int i = tara.FindIndex(x => x.TaraCode == int.Parse(row[0].ToString()));
                if (i == -1)
                    tara.Add(new Tara()
                    {
                        TaraCode = int.Parse(row[0].ToString()),
                        TaraName = row[1].ToString()
                    });
                if (withKlient == 1)
                {
                    i = klient.FindIndex(x => x.KlientCode == int.Parse(row[2].ToString()));
                    if (i == -1)
                        klient.Add(new Klient()
                        {
                            KlientCode = int.Parse(row[2].ToString()),
                            KlientName = row[3].ToString()
                        });
                    i = dot.FindIndex(x => x.KlientCode == int.Parse(row[2].ToString()) & x.DotCode == int.Parse(row[4].ToString()));
                    if (i == -1)
                        dot.Add(new Dot()
                        {
                            KlientCode = int.Parse(row[2].ToString()),
                            DotCode = int.Parse(row[4].ToString()),
                            DotName = row[5].ToString(),
                            DotAddress = row[6].ToString()
                        });
                }
            }

            update.TaraRests = taraRests.ToArray();
            update.Tara = tara.ToArray();
            update.Klient = klient;
            update.Dot = dot;

            return update;
        }
        private Auth GetAuth(string code, string version)
        {
            DataTable tbl = sql.SelectQuery("select rtrim(Descr), SP6320, SP5667 from SC2286 where sp4767 = '" + code + "'", log, "sql2009");
            Auth auth = new Auth();
            foreach (DataRow row in tbl.Rows)
            {
                auth = new Auth()
                {
                    Name = row[0].ToString(),
                    Code = code,
                    TAC = version == TrueVersion ? int.Parse(row[1].ToString()) : 0,
                    Type = int.Parse(row[2].ToString()),
                    Version = TrueVersion
                };
            }
            return auth;
        }
    }

    class Auth
    {
        public string Name;
        public string Code;
        public int TAC;
        public int Type;
        public string Version;
    }
    class Update
    {
        public Auth Auth;
        public string Version;
        public Good[] Good;
        public List<GoodsDirectory> GoodsDirectory;
        public Tara[] Tara;
        public Oborud[] Oborud;
        public List<Klient> Klient;
        public List<Dot> Dot;
        public GoodView[] GoodView;
        public GoodRest[] GoodRests;
        public TaraRest[] TaraRests;
        public OborudRest[] OborudRests;
        public Debet[] Debet;
        public PKO[] PKO;
        public DocReturn[] PKOReturn;
        public Order[] Order;
        public DocReturn[] OrderReturn;
        public List<TaraFacing> TaraFacing;
        public List<OborudFacing> OborudFacing;
        public List<DocReturn> TaraFacingReturn;
        public List<DocReturn> OborudFacingReturn;
    }
    class OborudRest
    {
        public string OborudCode;
        public int KlientCode;
        public int DotCode;
        public double Qty;
    }
    class Oborud
    {
        public string OborudCode;
        public string OborudName;
        public string OborudInventCode;
    }
    class TaraRest
    {
        public int TaraCode;
        public int KlientCode;
        public int DotCode;
        public double Qty;
        public double Delay;
    }
    class Tara
    {
        public int TaraCode;
        public string TaraName;
    }
    class Klient
    {
        public int KlientCode;
        public string KlientName;
    }
    class Dot
    {
        public int KlientCode;
        public int DotCode;
        public string DotName;
        public string DotAddress;
    }
    class GoodRest
    {
        public int Code;
        public double Quantity;
        public double Price;
    }
    class GoodView
    {
        public string Сode;
        public string Name;
    }

    class Good
    {
        public int Code;
        public string Name;
        public int ParentID;
        public int Box;
        public string GoodView;
    }
    public class GoodsDirectory
    {
        public int Code;
        public int ParentCode;
        public string Name;
        public int Level;
        public List<GoodsDirectory> Child;
    }
    class Debet
    {
        public string NumDoc;
        public string DateDoc;
        public int KlientCode;
        public int DotCode;
        public double Dolg;
        public DateTime DatePay;
    }
    public class PKO
    {
        public string GUID;
        public string NumDoc;
        public string DateDoc;
        public int KlientCode;
        public int DotCode;
        public double Summ;
        public string DatePay;
        public int Status;
    }
    public class Order
    {
        public string GUID;
        public string DateDoc;
        public int KlientCode;
        public int DotCode;
        public int Status;
        public int FlagA;
        public int FlagF;
        public string Comment;
        public int Form;
        public List<OrderTab> OrderTab;
    }
    public class OrderTab
    {
        public string GUID;
        public int GoodCode;
        public int Quantity;
        public double PriceUAH;
        public double Summ;
    }
    public class DocReturn
    {
        public string GUID;
        public int Status;
    }
    public class TaraFacing
    {
        public string DateDoc;
        public string GUID;
        public int KlientCode;
        public int DotCode;
        public int TaraCode;
        public double Quantity;
        public int Status;
    }
    public class OborudFacing
    {
        public string DateDoc;
        public string GUID;
        public int KlientCode;
        public int DotCode;
        public string OborudCode;
        public double Quantity;
        public int Status;
    }
}