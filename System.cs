using CRMUtilites;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Xml;
using NLog;
using System.Diagnostics;

public partial class CRMAutoInfo : System.Web.Services.WebService
{

    public bool isOp()
    {
        
        return ToInt(Session["IsOperator"]) == 1;

    }
    

    #region DataSet

    public static SqlParameter getParam(SqlParameter param, object value)
    {
        param.Value = value;
        return param;
    }

    private DataSet Error(string Message)
    {
        string parentMethod = CRMUtil.getParentMethodName(0);
        
        DataSet erds = new DataSet(parentMethod + "Data");
        erds.Tables.Add("ErrorTable");
        erds.Tables["ErrorTable"].Columns.Add("ErrorMessage", typeof(string));
        DataRow r = erds.Tables["ErrorTable"].NewRow();
        r["ErrorMessage"] = Message;
        erds.Tables["ErrorTable"].Rows.Add(r);

        return erds;
    }

    private DataSet GetDataSet(string storedProcName, string connectionStringName, List<SqlParameter> param = null, int level = 0)
    {
        if (!isOp())
           throw new Exception("Неверный идентификатор сессии");
        //    return null;
        if ((storedProcName.Trim() == "") || (connectionStringName.Trim() == "")) return null;

        if (Session["ClientID"] == null) Session["ClientID"] = "0";
        if (Session["ClientDiscount"] == null) Session["ClientDiscount"] = "0";

        string parentMethod = CRMUtil.getParentMethodName(level);

        SqlConnection c;
        try
        {
            c = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);
                }
        catch
            {
            string ErrMsg = "Ошибка соединения "+ connectionStringName;
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Error: {0}", ErrMsg);
            LogManager.Flush();
            EventLog.WriteEntry("CRMAutoInfo", ErrMsg, EventLogEntryType.Error);
            DataSet erds = new DataSet("ErrorData");
            erds.Tables.Add("ErrorTable");
            erds.Tables["ErrorTable"].Columns.Add("ErrorMessage", typeof(string));
            DataRow r = erds.Tables["ErrorTable"].NewRow();
            return erds;
        }
        using (SqlDataAdapter da = new SqlDataAdapter(storedProcName, c))
            using (DataSet ds = new DataSet(parentMethod + "Data"))
            {
                da.SelectCommand.CommandTimeout = 120;
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                if (param != null)
                    foreach (SqlParameter p in param)
                        da.SelectCommand.Parameters.Add(p);

                da.Fill(ds, parentMethod + "Table");
                da.SelectCommand.Parameters.Clear();

                return ds;
            }

    }

    private int ExecProc(string storedProcName, string connectionStringName, List<SqlParameter> param)
    {
        if (!isOp()) return 0;
        if ((storedProcName.Trim() == "") || (connectionStringName.Trim() == "")) return 0;
                       
        using (SqlConnection c = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString))
        using (SqlDataAdapter da = new SqlDataAdapter(storedProcName, c))
        {
            da.SelectCommand.CommandType = CommandType.StoredProcedure;
            if (param != null)
                foreach (SqlParameter p in param)
                    da.SelectCommand.Parameters.Add(p);

            c.Open();
            return da.SelectCommand.ExecuteNonQuery();
        }


    }

    private DataSet GetDataSetForThread(string storedProcName, string connectionStringName, List<SqlParameter> param, int level = 0)
    {
    // Метод используется для получения данных для потоков. Не проверяютя параметры сессии.
        if ((storedProcName.Trim() == "") || (connectionStringName.Trim() == "")) return null;

        string parentMethod = CRMUtil.getParentMethodName(level);

        using (SqlConnection c = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString))
        using (SqlDataAdapter da = new SqlDataAdapter(storedProcName, c))
        using (DataSet ds = new DataSet(parentMethod + "Data"))
        {
            da.SelectCommand.CommandTimeout = 120;
            da.SelectCommand.CommandType = CommandType.StoredProcedure;
            if (param != null)
                foreach (SqlParameter p in param)
                    da.SelectCommand.Parameters.Add(p);

            da.Fill(ds, parentMethod + "Table");
            da.SelectCommand.Parameters.Clear();
            return ds;
        }
    }

    private SqlDbType GetSQLDBType(string Type) 
    {
        switch (Type)
        {
            case "varchar": return SqlDbType.VarChar;
            case "int": return SqlDbType.Int;
            case "bigint": return SqlDbType.BigInt;
            case "datetime": return SqlDbType.DateTime;
            default: return SqlDbType.VarChar;
        }
    }
     
    public DataSet CommonDataSetGet(string xmltext)
    {

       List<SqlParameter> p = new List<SqlParameter>();
       XmlDocument xml = new XmlDocument();
       string Database;
       string StoredProc;
       string Name;
       string SQLDBType;
       int Size;
       string Value;
       xml.LoadXml(xmltext);
       string str = xml.GetElementsByTagName("StoredProc")[0].InnerText; // Это ХП
       Database = str.Substring(0, str.IndexOf(":") );
       StoredProc = str.Substring(str.IndexOf(":") + 1);
       // Параметры ХП
       XmlNode ParamsList = xml.GetElementsByTagName("Params")[0];
       foreach (XmlNode Params in ParamsList.ChildNodes)
       {
          Name = ""; SQLDBType = ""; Size = 0; Value = "";
          foreach (XmlNode Param in Params)
          {
              switch (Param.LocalName)
              {
                  case "Name": Name = Param.InnerText;
                               break;
                  case "SQLDBType": SQLDBType = Param.InnerText;
                                    break;
                  case "Size": Int32.TryParse(Param.InnerText, out Size);
                               break;
                  case "Value": Value = Param.InnerText;
                                break;
              }
         }
         p.Add(getParam(new SqlParameter(Name, GetSQLDBType(SQLDBType), Size), Value)); 
      }
     
      return GetDataSet(StoredProc, Database, p);
   }

    private bool isnull(DataSet ds)
    {
        return ((ds == null) || (ds.Tables.Count < 1) || (ds.Tables[0].Rows.Count < 1));
    }

    #endregion

    #region Action

    //[WebMethod(EnableSession = true)]
    public DataSet ActionListGet(string FormName, int ActionTypeID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@FormName", SqlDbType.VarChar, 500), FormName));
        p.Add(getParam(new SqlParameter("@ActionTypeID", SqlDbType.Int, 0), ActionTypeID));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("ActionFormLst", "CRM", p);
    }

    

    //[WebMethod(EnableSession = true)]
    public DataSet ActionFormLstByFormGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@FormID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionFormLstByForm", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionAllListGet()
    {
        return GetDataSet("ActionLst", "CRM");
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionGetUpdate(int objectid, int _actiontypeid, string _name, string _comment, int _formid,
                                    string _inputparams, int _isinsertaction, int _crmcategoryid, int _imageindex)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@ActionTypeID", SqlDbType.Int, 0), _actiontypeid));
        p.Add(getParam(new SqlParameter("@Name", SqlDbType.VarChar), _name));
        p.Add(getParam(new SqlParameter("@Comment", SqlDbType.VarChar), _comment));
        p.Add(getParam(new SqlParameter("@FormID", SqlDbType.Int, 0), _formid));
        p.Add(getParam(new SqlParameter("@InputParams", SqlDbType.VarChar), _inputparams));
        p.Add(getParam(new SqlParameter("@IsInsertAction", SqlDbType.Int, 0), _isinsertaction));
        p.Add(getParam(new SqlParameter("@CRMCategoryID", SqlDbType.Int, 0), _crmcategoryid));
        p.Add(getParam(new SqlParameter("@ImageIndex", SqlDbType.Int, 0), _imageindex));

        return GetDataSet("ActionSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionDel(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();

        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionDel", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionRightByActionListGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ActionID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionRightLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionFormGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionFormGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionFormGetUpdate(int objectid, int _formid, int _actionident, int _enabled,
                                       int _isdefault, int _sortorder)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@FormID", SqlDbType.Int, 0), _formid));
        p.Add(getParam(new SqlParameter("@ActionID", SqlDbType.Int, 0), _actionident));
        p.Add(getParam(new SqlParameter("@Enabled", SqlDbType.Int, 0), _enabled));
        p.Add(getParam(new SqlParameter("@IsDefault", SqlDbType.Int, 0), _isdefault));
        p.Add(getParam(new SqlParameter("@SortOrder", SqlDbType.Int, 0), _sortorder));

        return GetDataSet("ActionFormSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionFormDel(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();

        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionFormDel", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionTypeListGet()
    {
        return GetDataSet("ActionTypeLst", "CRM");
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionRightNewGetUpdate(int objectid, int _actionid, int _systemusergroupid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@ActionID", SqlDbType.Int, 0), _actionid));
        p.Add(getParam(new SqlParameter("@SystemUserGroupID", SqlDbType.Int, 0), _systemusergroupid));

        return GetDataSet("ActionRightSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionRightGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("ActionRightGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionRightListGet(string FormName, int ActionTypeID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@FormName", SqlDbType.VarChar, 500), FormName));
        p.Add(getParam(new SqlParameter("@ActionTypeID", SqlDbType.Int, 0), ActionTypeID));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("ActionFormLstForRight", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet ActionRightCrossListGet()
    {
        return GetDataSet("ActionRightCrossLst", "CRM");
    }

    #endregion
    #region UserSetting

    // Общие настройки
  
    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserSettingListGet(int SettingTypeID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        p.Add(getParam(new SqlParameter("@SettingTypeID", SqlDbType.Int, 0), SettingTypeID));
        return GetDataSet("SystemUserSettingLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserSettingListGetType1(int SettingTypeID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        p.Add(getParam(new SqlParameter("@SettingTypeID", SqlDbType.Int, 0), SettingTypeID));
        return GetDataSet("SystemUserSettingLst", "CRM", p);
    }
   
    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserSettingListGetType1Update(int objectid, int _Value1, int _Value2, string _Value3, string _Value4, int _Value5, int _Value6)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@Value1", SqlDbType.VarChar,8000), _Value1.ToString()));
        p.Add(getParam(new SqlParameter("@Value2", SqlDbType.VarChar, 8000), _Value2.ToString()));
        p.Add(getParam(new SqlParameter("@Value3", SqlDbType.VarChar, 8000), _Value3));
        p.Add(getParam(new SqlParameter("@Value4", SqlDbType.VarChar, 8000), _Value4));
        p.Add(getParam(new SqlParameter("@Value5", SqlDbType.VarChar, 8000), _Value5.ToString()));
        p.Add(getParam(new SqlParameter("@Value6", SqlDbType.VarChar, 8000), _Value6.ToString()));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        

        return GetDataSet("SystemUserSettingUpdForType1","CRM", p);

    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserSettingGet(int SettingID)
    {
        List<SqlParameter> p = new List<SqlParameter>();

        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        p.Add(getParam(new SqlParameter("@SettingID", SqlDbType.Int, 0), SettingID));

        return GetDataSet("SystemUserSettingGet", "CRM", p);
    }

  
    #endregion
    #region Sessions

    //[WebMethod(EnableSession = true)]
    public void SetValue(string name, string val)
    {
        if (name.ToUpper() != "CLIENTID" & name.ToUpper() != "ISOPERATOR" & name.ToUpper() != "CLIENTDISCOUNT")
        {
            Session[name] = val;
        }
    }

    //[WebMethod(EnableSession = true)]
    public string GetValue(string name)
    {
        if (name.ToUpper() != "CLIENTID" & name.ToUpper() != "CLIENTDISCOUNT")
        {
            string s = "";
            if (Session[name] != null) s = Session[name].ToString();
            return s;
        }
        else return "";
    }

    #endregion
    #region Util
    public int ToInt(object o)
    {
        int i;
        try { i = int.Parse(o.ToString()); }
        catch { i = 0; }
        return i;
    }
    public long ToLong(object o)
    {
        long i;
        try { i = long.Parse(o.ToString()); }
        catch { i = 0; }
        return i;
    }
    public float ToFloat(object o)
    {
        float i;
        try { i = float.Parse(o.ToString()); }
        catch { i = 0; }
        return i;
    }
    public DateTime ToDate(object o, int todatetype)
    {
        DateTime d;
        string s;
        s = o.ToString();
        if (todatetype == 0)
        {
            if (s.Length > 10 & s.Contains("T")) todatetype = 2;
        }
        if (todatetype == 2) s = s.Substring(8, 2) + "." + s.Substring(5, 2) + "." + s.Substring(0, 4);
        d = DateTime.Parse(s);
        return d;
    }


    #endregion    
    #region ObjectProperty

    //[WebMethod(EnableSession = true, Description = "Возвращает значение дополнительного свойства по системному имени")]
    public DataSet ObjectPropertyValueGetBySysName(string bd, long ObjectID, string SysName)
    {
        List<SqlParameter> p = new List<SqlParameter>();

        p.Add(getParam(new SqlParameter("@ObjectID", SqlDbType.BigInt), ObjectID));
        p.Add(getParam(new SqlParameter("@SysName", SqlDbType.VarChar, 8000), SysName));

        return GetDataSet("ObjectPropertyValueGetBySysName", bd, p);
    }

    //[WebMethod(EnableSession = true, Description = "Вставляет или обновляет значение дополнительного свойства по системному имени")]
    public DataSet ObjectPropertyValueSetBySysName(string bd, long ObjectID, string Value, string SysName)
    {
        List<SqlParameter> p = new List<SqlParameter>();

        p.Add(getParam(new SqlParameter("@ObjectID", SqlDbType.BigInt), ObjectID));
        p.Add(getParam(new SqlParameter("@Value", SqlDbType.VarChar, 8000), Value));
        p.Add(getParam(new SqlParameter("@SysName", SqlDbType.VarChar, 8000), SysName));
        if (bd == "CRM") p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int), Session["SystemUserID"]));

        return GetDataSet("ObjectPropertyValueSetBySysName", bd, p);
       
    }

    #endregion
}