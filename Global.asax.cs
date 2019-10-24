using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using NLog;
using System.Diagnostics;

namespace CRMAutoInfoLog
{
    public class Global : System.Web.HttpApplication
    {
        
        protected void Application_Start(object sender, EventArgs e)
        {
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Error("Command: {0}", Server.GetLastError().Message.ToString() + "<hr/>" + Server.GetLastError().ToString());
            LogManager.Flush();
            EventLog.WriteEntry("CRMAutoInfo", Server.GetLastError().Message.ToString() + "<hr/>" + Server.GetLastError().ToString(), EventLogEntryType.Error);
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}