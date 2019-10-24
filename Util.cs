using System.Diagnostics;
using System.Data;

namespace CRMUtilites
{
    public class CRMUtil
    {
        public CRMUtil()
        {
            
        }

        public static string getParentMethodName(int level = 0)
        {
            StackTrace trace = new StackTrace(2, false);
            StackFrame sf = trace.GetFrame(level);
            return sf.GetMethod().Name.ToString();
        }

        public static object iif(bool Cond, object TrueValue, object FalseValue)
        {
            if (Cond)
                return TrueValue;
            else
                return FalseValue;
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            
            char[] chars = new char[bytes.Length  / sizeof(char)];

            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
        
            return new string(chars);
        }

        public static DataSet IDListToDataSet(string IDList)
        {
            int i = 0; 
            string ss = "";
            string c = "";
            DataSet ds = new DataSet("IDData");
            DataRow r;
            ds.Tables.Add("IDTable");

            ds.Tables[0].Columns.Add("ID", typeof(long));

            while (IDList.Length > 0)
            {
                c = IDList.Substring(0,1);
                if ((c == "0") || (c == "1") || (c == "2") || (c == "3") || (c == "4") || (c == "5") || (c == "6") || (c == "7") || (c == "8") || (c == "9") || (c == ","))
                    ss = ss + c;
                IDList = IDList.Substring(1,IDList.Length - 1);
            }

            IDList = ss;

            if (IDList != "")
              i = IDList.IndexOf(",");

            while (i>0)
            {
                ss = IDList.Substring(0,i);
                if (ss != "")
                {
                  r = ds.Tables[0].Rows.Add();
                  r["ID"] = long.Parse(ss);
                }

                IDList = IDList.Substring(i+1,IDList.Length - i - 1);
              
                i = IDList.IndexOf(",");
            }

            if (IDList != "")
            {
                  r = ds.Tables[0].Rows.Add();
                  r["ID"] = long.Parse(IDList);
            }
              
            return ds;
        }

        public static DataSet StringListToDataSet(string StringList)
        {
            int i = 0;
            string ss = "";
            DataSet ds = new DataSet("StringData");
            DataRow r;
            ds.Tables.Add("StringTable");

            ds.Tables[0].Columns.Add("Value", typeof(string));

            if (StringList != "")
                i = StringList.IndexOf(",");

            while (i > 0)
            {
                ss = StringList.Substring(0, i).Trim();
                if (ss != "")
                {
                    r = ds.Tables[0].Rows.Add();
                    r["Value"] = ss;
                }

                StringList = StringList.Substring(i + 1, StringList.Length - i - 1);

                i = StringList.IndexOf(",");
            }

            if (StringList != "")
            {
                r = ds.Tables[0].Rows.Add();
                r["Value"] = StringList.Trim();
            }

            return ds;
        }



    }
}