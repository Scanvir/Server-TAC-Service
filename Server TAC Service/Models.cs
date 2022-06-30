using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_TAC_Service
{
    public class Auth
    {
        public string Name;
        public string Code;
        public int TAC;
        public int Type;
        public string Version;
    }
    public class Update
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
    public class OborudRest
    {
        public string OborudCode;
        public int KlientCode;
        public int DotCode;
        public double Qty;
    }
    public class Oborud
    {
        public string OborudCode;
        public string OborudName;
        public string OborudInventCode;
    }
    public class TaraRest
    {
        public int TaraCode;
        public int KlientCode;
        public int DotCode;
        public double Qty;
        public double Delay;
    }
    public class Tara
    {
        public int TaraCode;
        public string TaraName;
    }
    public class Klient
    {
        public int KlientCode;
        public string KlientName;
    }
    public class Dot
    {
        public int KlientCode;
        public int DotCode;
        public string DotName;
        public string DotAddress;
        public string DotFillial;
    }
    public class GoodRest
    {
        public int Code;
        public double Quantity;
        public string Fillial;
    }
    public class GoodView
    {
        public string Сode;
        public string Name;
    }
    public class Good
    {
        public int Code;
        public string Name;
        public int ParentID;
        public int Box;
        public string GoodView;
        public double Price;
    }
    public class GoodsDirectory
    {
        public int Code;
        public int ParentCode;
        public string Name;
        public int Level;
        public List<GoodsDirectory> Child;
    }
    public class Debet
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

    public class Config
    {
        public string TrueVersion;
        public string sqlDatabase;
        public string sqlServ;
        public string sqlUser;
        public string sqlPass;
        public bool isTaraRest;
    }
}
