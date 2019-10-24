using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Services;
using NLog;
using System.Diagnostics;

[WebService(Namespace = "http://ows.test.ru/CRMAutoInfo")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]

public partial class CRMAutoInfo : System.Web.Services.WebService
{
    public CRMAutoInfo()
    {
        
    }
    Logger logger = LogManager.GetCurrentClassLogger();
    [WebMethod(EnableSession = true)]
    public DataSet GetInfoForClient(string loginname, string password, string PhoneNumber)
    {
        string ErrMsg = "";
        if ((loginname == "") || (password == "") || (PhoneNumber == ""))
        {
            ErrMsg = "Пустые значения имя, пароль, телефон";
        }
        DataSet ds =  LogUser(loginname, password);
        if (ToInt(ds.Tables[0].Rows[0]["ErrCode"]) == 1)
        {
            ErrMsg = ds.Tables[0].Rows[0]["ErrMsg"].ToString();
        }
        if (ErrMsg == "")
        {
            List<SqlParameter> p = new List<SqlParameter>();
            p.Add(getParam(new SqlParameter("@SMSPhone", SqlDbType.VarChar, 0), PhoneNumber));
            ds = GetDataSet("ClientSearchInfoForCRM", "BO", p);
            return ds;
        }
        else
        {
            DataSet erds = new DataSet("GetInfoForClientData");
            erds.Tables.Add("ErrorTable");
            erds.Tables["ErrorTable"].Columns.Add("ErrorMessage", typeof(string));
            DataRow r = erds.Tables["ErrorTable"].NewRow();
            r["ErrorMessage"] = ErrMsg;
            erds.Tables["ErrorTable"].Rows.Add(r);
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Error: {0}", ErrMsg);
            LogManager.Flush();
            EventLog.WriteEntry("CRMAutoInfo", ErrMsg, EventLogEntryType.Error);
            return erds;
        }
    }
    [WebMethod(EnableSession = true)]
    public DataSet Sda_CRM_proc(string loginname, string password, string PhoneNumber)
    {
        string ErrMsg = "";
        if ((loginname == "") || (password == "") || (PhoneNumber == ""))
        {
            ErrMsg = "Пустые значения имя, пароль, телефон";
        }
        DataSet ds = LogUser(loginname, password);
        if (ToInt(ds.Tables[0].Rows[0]["ErrCode"]) == 1)
        {
            ErrMsg = ds.Tables[0].Rows[0]["ErrMsg"].ToString();
        }
        if (ErrMsg == "")
        {

            List<SqlParameter> p1 = new List<SqlParameter>();
            p1.Add(getParam(new SqlParameter("@SMSPhone", SqlDbType.VarChar, 100), PhoneNumber));
            DataSet ds1 = GetDataSet("ClientSearchForServicesOzEx", "BO", p1);
            if (ds1.Tables[0].Rows.Count > 0)
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(getParam(new SqlParameter("@userid", SqlDbType.BigInt, 0), ds1.Tables[0].Rows[0]["ID"]));
                ds = GetDataSet("sda_CRM_proc", "Reporting_Datamart", p);
                return ds;
            }
            else
                return null;
        }
        else
        {
            DataSet erds = new DataSet("sda_CRM_procData");
            erds.Tables.Add("ErrorTable");
            erds.Tables["ErrorTable"].Columns.Add("ErrorMessage", typeof(string));
            DataRow r = erds.Tables["ErrorTable"].NewRow();
            r["ErrorMessage"] = ErrMsg;
            erds.Tables["ErrorTable"].Rows.Add(r);
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Error: {0}", ErrMsg);
            LogManager.Flush();
            EventLog.WriteEntry("CRMAutoInfo", ErrMsg, EventLogEntryType.Error);
            return erds;
        }
    }
    #region Authorization

    //[WebMethod(EnableSession = true)]
    public DataSet LogUser(string lname, string pname)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@Login", SqlDbType.VarChar, 100), lname));
        p.Add(getParam(new SqlParameter("@Password", SqlDbType.VarChar, 100), pname));

        Session["IsOperator"] = 1;
        using (DataSet ds = GetDataSet("SystemUserLogin", "CRM", p))
        {
            Session["IsOperator"] = 0;
            if (isnull(ds)) return ds;
            Session["IsOperator"] = ds.Tables[0].Rows[0]["IsOperator"].ToString();
        
            if (isOp())
            {
                Session["SystemUserID"] = ds.Tables[0].Rows[0]["ID"].ToString();
                SystemUserDepartmentCurrentViewGetUpdate((int)ds.Tables[0].Rows[0]["DepartmentID"]);                
                SystemUserLastVisitUpd();
             
            }
            Session["Logged"] = 1;
            ds.DataSetName = "LogData";
            ds.Tables[0].TableName = "LogTable";
            return ds;
        }
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserLastVisitUpd()
    {
        if (Session["SystemUserID"] == null)
            return null;
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.BigInt, 0), Session["SystemUserID"]));

        return GetDataSet("SystemUserLastVisitUpd", "CRM", p);
    }
    #endregion
    #region Department

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserDepartmentCurrentViewGetUpdate(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        p.Add(getParam(new SqlParameter("@DepartmentID", SqlDbType.Int, 0), objectid));

        using (DataSet ds = GetDataSet("SystemUserDepartmentUpdCurrentView", "CRM", p))
        {
            if (isnull(ds)) return ds;
            Session["BranchID"] = ds.Tables[0].Rows[0]["BranchID"].ToString();
            return ds;
        }
    }

    //[WebMethod(EnableSession = true)]
    public DataSet DepartmentCurrentViewGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("DepartmentCurrentViewGet", "CRM", p);
    }

   

    //[WebMethod(EnableSession = true)]
    public DataSet DepartmentListGet()
    {
        return GetDataSet("DepartmentLst", "CRM");
    }

    //[WebMethod(EnableSession = true)]
    public DataSet DepartmentGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("DepartmentGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserDepartmentByDepartmentListGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@DepartmentID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemUserDepartmentLst", "CRM", p);
    }

        // Выводим список департаментов, в которых числится текущий пользователь
    //[WebMethod(EnableSession = true)]
    public DataSet DepartmentLookupLstGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), ToInt(Session["SystemUserID"])));

        return GetDataSet("DepartmentLstBySystemUser", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet DepartmentListBySystemUserGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), objectid));

        return GetDataSet("DepartmentLstBySystemUser", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserDepartmentGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemUserDepartmentGet", "CRM", p);
    }

   
    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserDepartmentBulkLoadGet(int objectid, string SystemUserListID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@DepartmentID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@SystemUserIDLst", SqlDbType.VarChar, 0), SystemUserListID));

        return GetDataSet("SystemUserDepartmentBulkLoad", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet DepartmentGetDefaultGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("DepartmentGetDefault", "CRM", p);
    }
        
    

    #endregion
    #region SystemMessage

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageCheckGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("SystemMessageInSystemUserCheck", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageListGet()
    {
        return GetDataSet("SystemMessageLst", "CRM");
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemMessageGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageGetUpdate(int objectid, int _enabled, string _name, string _description, int _systemusergroupid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@Enabled", SqlDbType.Int, 0), _enabled));
        p.Add(getParam(new SqlParameter("@Name", SqlDbType.VarChar, 1000), _name));
        p.Add(getParam(new SqlParameter("@Description", SqlDbType.VarChar, 1000), _description));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        p.Add(getParam(new SqlParameter("@SystemUserGroupID", SqlDbType.Int, 0), _systemusergroupid));

        return GetDataSet("SystemMessageSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageInSystemUserListGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("SystemMessageInSystemUserLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageCheckReadListGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemMessageID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemMessageCheckReadLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageInSystemUserGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemMessageInSystemUserGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemMessageInSystemUserDoneGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("SystemMessageInSystemUserUpd", "CRM", p);
    }

    #endregion
    #region SystemUser

    #region Group

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserGroupListTreeGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("SystemUserGroupLstTree", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserGroupGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemUserGroupGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet FeedBackUserGroupGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("FeedBackUserGroupGet", "CRM", p);
    }


    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserGroupGetUpdate(int objectid, string _name, int _enabled)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@Enabled", SqlDbType.Int, 0), _enabled));
        p.Add(getParam(new SqlParameter("@Name", SqlDbType.VarChar, 1000), _name));

        return GetDataSet("SystemUserGroupSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet FeedBackUserGroupGetUpdate(int objectid, string _name, string _color, int _DepartmentID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@Name", SqlDbType.VarChar, 1000), _name));
        p.Add(getParam(new SqlParameter("@Color", SqlDbType.VarChar, 50), _color));
        p.Add(getParam(new SqlParameter("@DepartmentID", SqlDbType.Int, 0), _DepartmentID));

        return GetDataSet("FeedBackUserGroupSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserInSystemUserGroupGet(string SystemUserGroupName)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserGroupName", SqlDbType.VarChar, 1000), SystemUserGroupName));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("SystemUserInSystemUserGroupGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserInSystemUserGroupGetUpdate(int _systemusergroupid, int _systemuserid, int _type)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserGroupID", SqlDbType.Int, 0), _systemusergroupid));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), _systemuserid));
                                         
        return GetDataSet(_type == 1 ? "SystemUserInSystemUserGroupIns" : "SystemUserInSystemUserGroupDel", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserGroupMembershipCrossListGet()
    {
        return GetDataSet("SystemUserGroupMembershipCrossLst", "CRM");
    }

    #endregion

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserLoginListGet()
    {
        return GetDataSet("SystemUserLoginLst", "CRM");
    }


    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserListGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), ToInt(Session["SystemUserID"])));

        return GetDataSet("SystemUserLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserLookupListGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        
        return GetDataSet("SystemUserLookupLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserAllListGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ShowDeleted", SqlDbType.Int, 0), 1));

        return GetDataSet("SystemUserLst", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemUserGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserGetUpdate(int objectid, string _firstname, string _lastname, string _middlename,
                                       string _login, string _email, int _isforbidden, int _departmentid, int _CallCenterID, int _WFMID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@IsForbidden", SqlDbType.Int, 0), _isforbidden));
        p.Add(getParam(new SqlParameter("@LastName", SqlDbType.VarChar, 1000), _lastname));
        p.Add(getParam(new SqlParameter("@FirstName", SqlDbType.VarChar, 1000), _firstname));
        p.Add(getParam(new SqlParameter("@MiddleName", SqlDbType.VarChar, 1000), _middlename));
        p.Add(getParam(new SqlParameter("@Login", SqlDbType.VarChar, 1000), _login));
        p.Add(getParam(new SqlParameter("@Email", SqlDbType.VarChar, 1000), _email));
        p.Add(getParam(new SqlParameter("@Password", SqlDbType.VarChar, 1000), ""));
        p.Add(getParam(new SqlParameter("@DepartmentID", SqlDbType.Int, 0), _departmentid));
        p.Add(getParam(new SqlParameter("@CallCenterID", SqlDbType.Int), _CallCenterID));
        p.Add(getParam(new SqlParameter("@WFMID", SqlDbType.Int), _WFMID));

        return GetDataSet("SystemUserSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserDelGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.BigInt, 0), objectid));

        return GetDataSet("SystemUserDel", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public void SystemUserUpdPassword(int objectid, string _password)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@Password", SqlDbType.VarChar, 1000), _password));

        ExecProc("SystemUserSetPassword", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserWebServiceListGet()
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));

        return GetDataSet("SystemUserWebServiceLst", "CRM", p);
    }
        

    #region SystemUserMapping
    /* связь пользователей CRM и доменных пользователей БД MetaZon */

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingListGet()
    {
        string MZLogin;
        List<SqlParameter> p = new List<SqlParameter>();
        DataRow[] drs;

        DataSet ds = GetDataSet("SystemUserMappingLst", "CRM");
        if ((ds == null) || (ds.Tables.Count==0) || (ds.Tables[0].Rows.Count==0))
            return null;

        p.Add(getParam(new SqlParameter("@Type", SqlDbType.Char, 1), 'S')); //S = SQL-Users
        using (DataSet dsp = GetDataSet("DatabasePrincipalsLstForCRM", "MZ", p))
        {
            if ((dsp != null) && (dsp.Tables.Count>0) && (dsp.Tables[0].Rows.Count>0))
            {
                foreach(DataRow dr in ds.Tables[0].Rows)
                {
                    MZLogin = dr["MZLogin"].ToString();
                    drs = dsp.Tables[0].Select("Name='" + MZLogin + "'");
                    if (drs.Length>0)
                        dr["MZAddDate"] = drs[0]["create_date"];
                }
            }
        }
        return ds;
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));

        return GetDataSet("SystemUserMappingGet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingGetUpdate(int objectid, int _SystemUserID, string _MZLogin, string _MZPassword, string _DateStart, string _DateEnd)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ID", SqlDbType.Int, 0), objectid));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), _SystemUserID));
        p.Add(getParam(new SqlParameter("@MZLogin", SqlDbType.VarChar, 1000), _MZLogin));
        p.Add(getParam(new SqlParameter("@MZPassword", SqlDbType.VarChar, 1000), _MZPassword));
        p.Add(getParam(new SqlParameter("@DateStart", SqlDbType.DateTime, 0), ToDate(_DateStart, 1)));
        p.Add(getParam(new SqlParameter("@DateEnd", SqlDbType.DateTime, 0), ToDate(_DateEnd, 1)));

        return GetDataSet("SystemUserMappingSet", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingDelGet(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("ID", SqlDbType.BigInt, 0), objectid));

        return GetDataSet("SystemUserMappingDel", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingActualGUIDGet(int SystemUserID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("SystemUserID", SqlDbType.Int, 0), SystemUserID));

        return GetDataSet("SystemUserMappingGetActualGUID", "CRM", p);
    }

    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingLogPassByGUIDGet(string GUID)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("GUID", SqlDbType.Text, 0), GUID));

        Session["IsOperator"] = 1;
        DataSet ds = GetDataSet("SystemUserMappingGetByGUID", "CRM", p);
        Session["IsOperator"] = 0;
        return ds;
    }

    /*******************************
     * Описание
     * 
     *   Добавляет пользователя в БД Метазона (системную запись и прикладную);
     * так же назначаются некоторые прикладные права
     *******************************/
    //[WebMethod(EnableSession = true)]
    public DataSet SystemUserMappingAppUserAdd(int objectid)
    {
        List<SqlParameter> p = new List<SqlParameter>();
        List<SqlParameter> p2 = new List<SqlParameter>();
        string UserFIO;
        string MZLogin;
        string MZPassword;

        p.Add(getParam(new SqlParameter("ID", SqlDbType.BigInt, 0), objectid));
        using (DataSet ds = SystemUserMappingGet(objectid))
        {
            if (ds == null)
                return null;
            if (ds.Tables.Count == 0)
                return null;
            if (ds.Tables[0].Rows.Count == 0)
                return null;

            UserFIO = ds.Tables[0].Rows[0]["SystemUser"].ToString();
            MZLogin = ds.Tables[0].Rows[0]["MZLogin"].ToString();
            MZPassword = ds.Tables[0].Rows[0]["MZPassword"].ToString();

            p2.Add(getParam(new SqlParameter("Name", SqlDbType.VarChar, 255), UserFIO));
            p2.Add(getParam(new SqlParameter("SysName", SqlDbType.VarChar, 255), MZLogin));
            p2.Add(getParam(new SqlParameter("Password", SqlDbType.VarChar, 255), MZPassword));

            ExecProc("AppUserAddByCRM", "MZ", p2);
        }
        return null;

    }


    #endregion //SystemUserMapping

    #endregion
    #region Util Functions

    public int ClientCommentAdd(int _ClientCommentTypeID, string _Comment, long _ObjectID = 0)
    {
        if (ToInt(Session["ClientID"]) == 0) return 0;

        List<SqlParameter> p = new List<SqlParameter>();
        p.Add(getParam(new SqlParameter("@ClientID", SqlDbType.BigInt, 0), Session["ClientID"]));
        p.Add(getParam(new SqlParameter("@ClientCommentTypeID", SqlDbType.Int, 0), _ClientCommentTypeID));
        p.Add(getParam(new SqlParameter("@SystemUserID", SqlDbType.Int, 0), Session["SystemUserID"]));
        p.Add(getParam(new SqlParameter("@Comment", SqlDbType.VarChar, 5000), _Comment));
        if (_ObjectID > 0)
            p.Add(getParam(new SqlParameter("@ObjectID", SqlDbType.BigInt), _ObjectID));

        return ExecProc("ClientCommentIns", "CRM", p);
    }

    #endregion

    //[WebMethod]
    public string ReverseDate()
    {
        string s = DateTime.Now.ToString();
        string ns = "";
        for (int i = s.Length-1; i >= 0; i--)
            ns += s[i];
        return ns;
    }

    //[WebMethod]
    public string Encrypt(string input, string AppPrefix)
    {
        //if (ToInt(Session["IsOperator"]) == 0) return "";
        byte[] salt = Encoding.Default.GetBytes(ConfigurationManager.AppSettings[AppPrefix +"Salt"].ToString());
        string key = ConfigurationManager.AppSettings[AppPrefix +"Key"].ToString();
        string iv = ConfigurationManager.AppSettings[AppPrefix +"Vector"].ToString();
        
        Rfc2898DeriveBytes dkey = new Rfc2898DeriveBytes(key, salt);
        Rfc2898DeriveBytes div = new Rfc2898DeriveBytes(iv, salt);
        TripleDESCryptoServiceProvider desProv = new TripleDESCryptoServiceProvider();
        byte[] cryptoStringByte = Encoding.Default.GetBytes(input);
        desProv.Key = dkey.GetBytes(16);
        desProv.IV = div.GetBytes(8);

        ICryptoTransform iDesEncrypt = desProv.CreateEncryptor(desProv.Key, desProv.IV);
        return Convert.ToBase64String(iDesEncrypt.TransformFinalBlock(cryptoStringByte, 0, cryptoStringByte.Length));
    }

    //[WebMethod]
    public string Decrypt(string input, string AppPrefix)
    {
        ////if (ToInt(Session["IsOperator"]) == 0) return "";
        byte[] source = Convert.FromBase64String(input);
        byte[] salt = Encoding.Default.GetBytes(ConfigurationManager.AppSettings[AppPrefix +"Salt"].ToString());
        string key = ConfigurationManager.AppSettings[AppPrefix +"Key"].ToString();
        string iv = ConfigurationManager.AppSettings[AppPrefix +"Vector"].ToString();

        Rfc2898DeriveBytes dkey = new Rfc2898DeriveBytes(key, salt);
        Rfc2898DeriveBytes div = new Rfc2898DeriveBytes(iv, salt);
        TripleDESCryptoServiceProvider desProv = new TripleDESCryptoServiceProvider();
        desProv.Key = dkey.GetBytes(16);
        desProv.IV = div.GetBytes(8);

        ICryptoTransform tripleDesDecrypt = desProv.CreateDecryptor(desProv.Key, desProv.IV);
        byte[] decryptedString = tripleDesDecrypt.TransformFinalBlock(source, 0, source.Length);
        return Encoding.Default.GetString(decryptedString);
    }
    
}
