using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace EVENTSCHEDULER
{
    class clsPublicVariables
    {
        public static string AppPath = Path.GetDirectoryName(Application.ExecutablePath).ToString();
        public static string Project_Title = "KRUPA INFOTECH";
        public static string DateCriteriaconst = "'";
        //public static string DatabaseType1;
        public static string ServerName1;
        public static string DatabaseName1;
        public static string UserName1;
        public static string Password1;
        //public static string DataPort1;

        public static string BILLNOBASEDON;
        public static string PROPROVIDER;
        public static string PROUSERID;
        public static string PROPASSWORD;
        public static string TRAPROVIDER;
        public static string TRAUSERID;
        public static string TRAPASSWORD;
        public static string SMSSIGN;
        public static string SMSRESTNM;
        //public static string SENDSMS;
        public static string GENSMTPEMAILADDRESS;
        public static string GENSMTPEMAILPASSWORD;
        public static string GENSMTPADDRESS;
        public static string GENSMTPPORT;
        public static string ActualFilePath;
        public static string EventVer;
        public static string GENRESTNAME;
        public static string EMAILADDRECTYPE1 = "";
        public static string EMAILPASSRECTYPE1 = "";
        public static string EMAILPORTRECTYPE1 = "";
        public static string EMAILPOPADDTRECTYPE1 = "";
        public static string EMAILADDRECTYPE2 = "";
        public static string EMAILPASSRECTYPE2 = "";
        public static string EMAILPORTRECTYPE2 = "";
        public static string EMAILPOPADDTRECTYPE2 = "";
        public static string GENENABLEZOMATO = "";
        public static string GENZOMATOSERVICETYPE = "";
        public static string GENZOMATOEMAILADD = "";
        public static string ZOMATONEWORDERKEY = "New online order received";
        public static string GENSMSAUTHOKEY = "";
        public static string GENWHATSAPPPROVIDER = "";
        public static string GENWHATSAPPUSERID = "";
        public static string GENWHATSAPPPASSWORD = "";
        public static string GENWHATSAPPMOBILENO = "";
        public static string GENWHATSAPPAUTHKEY = "";



    }
}
