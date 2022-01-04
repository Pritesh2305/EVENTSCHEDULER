using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using OpenPop.Pop3;
using OpenPop.Mime;
using OpenPop.Mime.Header;
using OpenPop.Pop3.Exceptions;
using OpenPop.Common.Logging;
using Message = OpenPop.Mime.Message;
using System.Data.SqlClient;

namespace EVENTSCHEDULER
{
    public partial class frmeventserver : Form
    {
        clsGeneral objclsgen = new clsGeneral();
        clsMsSqlDbFunction mssql = new clsMsSqlDbFunction();
        int PMAXCHAR = 44;

        private readonly Pop3Client pop3Client;
        private readonly Dictionary<int, Message> messages = new Dictionary<int, Message>();

        public frmeventserver()
        {
            InitializeComponent();

            pop3Client = new Pop3Client();

            objclsgen.GetConnectionDetails();
            objclsgen.FillSMSDetails();
        }

        private bool START_Timer()
        {
            try
            {
                this.tmrevent.Interval = 60000;
                this.tmrsms.Interval = 25000;// 1 minutes
                this.tmrread.Interval = 125000; // 5 minutes

                this.tmrevent.Enabled = true;
                this.tmrsms.Enabled = true;

                if (clsPublicVariables.GENENABLEZOMATO == "True")
                {
                    this.tmrread.Enabled = true;
                }

                this.Write_In_Error_Log("EVENT SCHEDULER START [" + DateTime.Now.ToString() + " ]");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool STOP_Timer()
        {
            try
            {
                this.tmrevent.Enabled = false;
                this.tmrsms.Enabled = false;
                this.tmrread.Enabled = false;

                this.Write_In_Error_Log("EVENT SCHEDULER  STOP [" + DateTime.Now.ToString() + " ]");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void btnrun_Click(object sender, EventArgs e)
        {
            this.START_Timer();
        }

        private void btnstop_Click(object sender, EventArgs e)
        {
            this.STOP_Timer();
        }

        private void tmrevent_Tick(object sender, EventArgs e)
        {
            this.RunEvent();
        }

        private bool RunEvent()
        {
            DataTable dtevent = new DataTable();
            Int64 rid = 0;
            String eventname, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail = "";
            String sendsms, sendemail, sendwhatsapp1, eventstop = "";
            string templateid1, otherid1, para1, para2, para3 = "";

            try
            {
                //Write_In_Error_Log("GENERATING NEW SMS [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log("ServerName1 = " + clsPublicVariables.ServerName1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log("DatabaseName1 = " +clsPublicVariables.DatabaseName1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log(" UserName1 = "+ clsPublicVariables.UserName1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log(" Password1 = " + clsPublicVariables.Password1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log("1 . IN RUNEVENT[ " + DateTime.Now.ToString() + " ]");

                string str1 = " SELECT *,DATEDIFF(SECOND, CONVERT(varchar(20),EVENTLASTRUN,120), CONVERT(varchar(20),getdate(),120)) as TIMEDIFF  " +
                                " FROM EVENTSCHEDULER  " +
                                " WHERE (ISNULL(EVENTSTOP,0)=0 AND (ISNULL(SENDSMS,0)=1) OR ISNULL(SENDEMAIL,0)=1)   " +
                                " AND CONVERT(varchar(20),EVENTLASTRUN,120) <= CONVERT(varchar(20),getdate(),120)   " +
                                " ORDER BY TIMEDIFF DESC ";

                dtevent = mssql.FillDataTable(str1, "EVENTSCHEDULER");

                //Write_In_Error_Log( dtevent.Rows.Count.ToString() +  " [ " + DateTime.Now.ToString() + " ]");

                if (dtevent.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtevent.Rows)
                    {
                        Int64.TryParse(row1["rid"] + "".ToString(), out rid);
                        eventname = row1["EVENTNAME"] + "".ToString();
                        eventruntype = row1["EVENTRUNTYPE"] + "".ToString();
                        eventinterval = row1["EVENTINTERVAL"] + "".ToString();
                        eventstartdate = row1["EVENTSTARTDATE"] + "".ToString();
                        eventlastrun = row1["EVENTLASTRUN"] + "".ToString();
                        eventtext = row1["EVENTTEXT"] + "".ToString();
                        eventmobno = row1["EVENTMOBNO"] + "".ToString();
                        eventemail = row1["EVENTEMAIL"] + "".ToString();
                        sendsms = row1["SENDSMS"] + "".ToString();
                        sendemail = row1["SENDEMAIL"] + "".ToString();
                        sendwhatsapp1 = row1["SENDWHATSAPP"] + "".ToString();
                        eventstop = row1["EVENTSTOP"] + "".ToString();

                        templateid1 = row1["TEMPLATEID"] + "".Trim();
                        otherid1 = row1["OTHERID"] + "".Trim();
                        para1 = row1["PARA1"] + "".Trim();
                        para2 = row1["PARA2"] + "".Trim();
                        para3 = row1["PARA3"] + "".Trim();

                        if (eventruntype == "EVENT_ONCEADAY")
                        {
                            if (Convert.ToInt64(eventinterval) > DateTime.Now.Hour)
                            {
                                return true;
                            }
                        }
                        else if (eventruntype == "EVENT_ONCEAMONTH")
                        {
                            if (Convert.ToInt16(eventinterval) != DateTime.Now.Day)
                            {
                                return true;
                            }
                        }
                        else if (eventruntype == "EVENT_ONCEAWEEK")
                        {
                            if (Convert.ToInt16(eventinterval) != (int)DateTime.Now.DayOfWeek)
                            {
                                return true;
                            }
                        }

                        if ((sendsms.ToLower().Trim() == "true"))
                        {
                            //Write_In_Error_Log("1. GENERATING NEW SMS [ " + DateTime.Now.ToString() + " ]");
                            bool retvalsms = this.SEND_SMS_EVENT(rid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail, templateid1, otherid1, para1, para2, para3);
                        }

                        if ((sendemail.ToLower().Trim() == "true"))
                        {
                            //Write_In_Error_Log("2. GENERATING NEW EMAIL [ " + DateTime.Now.ToString() + " ]");
                            bool retvalemail = this.SEND_EMAIL_EVENT(rid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        }

                        if ((sendwhatsapp1.ToLower().Trim() == "true"))
                        {
                            //Write_In_Error_Log("3. GENERATING NEW WHATSAPP [ " + DateTime.Now.ToString() + " ]");
                            bool retvalemail = this.SEND_WHATSAPP_EVENT(rid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        }

                        // Update time
                        DateTime nextrun1;
                        nextrun1 = GetEventLastRunTime(rid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                        str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + rid;
                        mssql.ExecuteMsSqlCommand(str1);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in RunEvent()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SEND_SMS_EVENT(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                   string eventtext, string eventmobno, string eventemail,
                                   string templateid1 = "", string otherid1 = "", string para1 = "", string para2 = "", string para3 = "")
        {
            try
            {
                // Write_In_Error_Log("In SEND_SMS_EVENT ( " + eventid + " )");

                switch (eventid)
                {
                    case 1:
                        this.SMS_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 2:
                        this.SMS_Feedback(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 3:
                        this.SMS_BirthdayWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 4:
                        this.SMS_AnniversaryWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 5:
                        this.SMS_BillWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 6:
                        this.SMS_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 7:
                        this.SMS_YesterdaySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 8:
                        // soul restaurant
                        this.SMS_Feedback_2(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 9:
                        // 7 wonders restaurant
                        this.SMS_Bithday_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 10:
                        // 7 wonders restaurant
                        this.SMS_Anniversary_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 11:
                        // Soup
                        this.SMS_Bithday3_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 12:
                        // Soup
                        this.SMS_Anniversary3_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 13:
                        this.SMS_BillWishes_MOMOMEN(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 14:
                        // Basic Amount in Total
                        this.SMS_DailySalesSummary_BasicAmount(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 15:
                        // Frequntly Business SMS
                        this.SMS_DailySalesSummary_Frequenty(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 16:
                        this.SMS_BillWishes_No_Amt(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 17:
                        //Write_In_Error_Log(" In SEND_SMS_EVENT (SMS_Bill_Parcle)");
                        this.SMS_Bill_Parcle(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 18:
                        //Write_In_Error_Log(" In SEND_SMS_EVENT (SMS_Settlement_Parcle)");
                        this.SMS_Settlement_Parcle(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 19:
                        this.SMS_Bill_EDIT_Info(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 20:
                        this.SMS_Bill_DELETE_Info(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 21:
                        // For BangBang
                        this.SMS_BillWishes_With_Amount(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 22: //PURCAHSE STOCK REGISTER - PAPA ROTI
                        break;
                    case 23: //BILL WISHES SMS BANG BANG
                        // For BangBang
                        this.SMS_BillWishes_With_Amount_Type2(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 24: //BILL WISHES SMS PIZZ ZONE
                        // FOR PIZZAZONE
                        this.SMS_BillWishes_PizzZone(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 25: //cash summary
                        break;
                    case 26:
                        this.SMS_EveryBill(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 27: //REWARD POINT ENTRY SMS
                        this.SMS_RewardPointEntry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 28: //BILL REWARD SMS
                        this.SMS_BillRewardSMS(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 29: //EVERY BILL WITH TOTAL BILLING
                        this.SMS_EveryBillWithTotalBilling(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 30:
                        this.SMS_BillWishes_No_Amt_MagicIceCream(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 31: //ITEM WISE SALES EMAIL
                        break;
                    case 32: // RAJ THAL BIRTH DAY SMS
                        this.SMS_Birthday_RAJTHAAL(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail, templateid1, otherid1, para1, para2, para3);
                        break;
                    case 33: // RAJ THAL MARRIAGE ANNIVERSARY SMS 
                        this.SMS_AnniversaryWishes_RAJTHAAL(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail, templateid1, otherid1, para1, para2, para3);
                        break;
                    default:
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SEND_SMS_EVENT()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SEND_EMAIL_EVENT(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                 string eventtext, string eventmobno, string eventemail)
        {
            try
            {
                // Write_In_Error_Log("GENERATING NEW E-MAIL [ " + DateTime.Now.ToString() + " ]");

                switch (eventid)

                {
                    case 1:
                        this.EMAIL_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 2:
                        this.EMAIL_Feedback(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 3:
                        this.EMAIL_BirthdayWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 4:
                        this.EMAIL_AnniversaryWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 5:
                        this.EMAIL_BillWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 6:
                        this.EMAIL_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 7:
                        this.EMAIL_YesterdaySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 15:
                        //Write_In_Error_Log("3. Daily Sales [ " + DateTime.Now.ToString() + " ]");
                        this.EMAIL_DailySalesSummary_Frequentry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 19:
                        //BILL EDIT AND DELETE INFORMATION
                        this.EMAIL_BillEditDelete_Frequentry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 20:
                    case 22: //PURCAHSE STOCK REGISTER - PAPA ROTI
                        this.EMAIL_DailyPurchaseStockRegister_Frequentry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 23:
                        break;
                    case 24:
                        break;
                    case 25:
                        this.EMAIL_CashBusinessSummary_Frequentry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 26:// Every Bill
                        //this.EMAIL_EveryBill(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 27: //REWARD POINT ENTRY SMS
                        break;
                    case 28: //BILL REWARD SMS
                        break;
                    case 29: // EveryBillWithTotalBilling
                        break;
                    case 30: // Bill wishes SMS magic ice cream
                        break;
                    case 31: //ITEM WISE SALES EMAIL
                        this.EMAIL_ItemWiseSales_Frequentry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    default:
                        break;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SEND_WHATSAPP_EVENT(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                   string eventtext, string eventmobno, string eventemail)
        {
            try
            {
                // Write_In_Error_Log("In SEND_SMS_EVENT ( " + eventid + " )");

                switch (eventid)
                {
                    case 1:
                        this.WHATSAPP_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 2:
                        this.SMS_Feedback(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 3:
                        this.SMS_BirthdayWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 4:
                        this.SMS_AnniversaryWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 5:
                        this.SMS_BillWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 6:
                        this.SMS_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 7:
                        this.SMS_YesterdaySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 8:
                        // soul restaurant
                        this.SMS_Feedback_2(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 9:
                        // 7 wonders restaurant
                        this.SMS_Bithday_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 10:
                        // 7 wonders restaurant
                        this.SMS_Anniversary_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 11:
                        // Soup
                        this.SMS_Bithday3_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 12:
                        // Soup
                        this.SMS_Anniversary3_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 13:
                        this.SMS_BillWishes_MOMOMEN(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 14:
                        // Basic Amount in Total
                        this.SMS_DailySalesSummary_BasicAmount(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 15:
                        // Frequntly Business SMS
                        this.SMS_DailySalesSummary_Frequenty(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 16:
                        this.SMS_BillWishes_No_Amt(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 17:
                        //Write_In_Error_Log(" In SEND_SMS_EVENT (SMS_Bill_Parcle)");
                        this.SMS_Bill_Parcle(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 18:
                        //Write_In_Error_Log(" In SEND_SMS_EVENT (SMS_Settlement_Parcle)");
                        this.SMS_Settlement_Parcle(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 19:
                        this.SMS_Bill_EDIT_Info(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 20:
                        this.SMS_Bill_DELETE_Info(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 21:
                        // For BangBang
                        this.SMS_BillWishes_With_Amount(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 22: //PURCAHSE STOCK REGISTER - PAPA ROTI
                        break;
                    case 23: //BILL WISHES SMS BANG BANG
                        // For BangBang
                        this.SMS_BillWishes_With_Amount_Type2(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 24: //BILL WISHES SMS PIZZ ZONE
                        // FOR PIZZAZONE
                        this.SMS_BillWishes_PizzZone(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 25: //cash summary
                        break;
                    case 26:
                        this.SMS_EveryBill(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 27: //REWARD POINT ENTRY SMS
                        this.SMS_RewardPointEntry(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 28: //BILL REWARD SMS
                        this.SMS_BillRewardSMS(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 29: //EVERY BILL WITH TOTAL BILLING
                        this.SMS_EveryBillWithTotalBilling(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 30:
                        this.SMS_BillWishes_No_Amt_MagicIceCream(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 31: //ITEM WISE SALES EMAIL
                        break;

                    default:
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SEND_SMS_EVENT()) " + DateTime.Now.ToString());
                return false;
            }
        }

        public bool Write_In_Error_Log(string errstr)
        {
            try
            {
                this.txtinfo.Text = errstr + System.Environment.NewLine + this.txtinfo.Text;
                this.Refresh();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DateTime GetEventLastRunTime(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun)
        {
            DateTime lastrun1, newlastrun1;
            Int64 eventint;

            lastrun1 = Convert.ToDateTime(eventlastrun);
            newlastrun1 = lastrun1;

            Int64.TryParse(eventinterval, out eventint);

            try
            {
                switch (eventruntype)
                {
                    case "EVENT_FREQUENTLY":
                        newlastrun1 = DateTime.Now.AddMinutes(eventint);
                        break;

                    case "EVENT_ONCEADAY":
                        int eventint1 = (int)eventint;
                        newlastrun1 = lastrun1.AddDays(1);

                        if (newlastrun1.Date < DateTime.Now.Date)
                        {
                            newlastrun1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, eventint1, 0, 0);
                        }
                        else
                        {
                            newlastrun1 = new DateTime(newlastrun1.Year, newlastrun1.Month, newlastrun1.Day, eventint1, 0, 0);
                        }
                        break;

                    case "EVENT_ONCEAWEEK":
                        int eventint2 = (int)eventint;
                        newlastrun1 = lastrun1.AddDays(7);
                        newlastrun1 = new DateTime(newlastrun1.Year, newlastrun1.Month, newlastrun1.Day, eventint2, 0, 0);
                        break;

                    case "EVENT_ONCEAMONTH":
                        int eventint3 = (int)eventint;
                        newlastrun1 = lastrun1.AddMonths(1);
                        newlastrun1 = new DateTime(newlastrun1.Year, newlastrun1.Month, newlastrun1.Day, 10, 0, 0);
                        break;

                    default:
                        break;
                }

                return newlastrun1;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in GetEventLastRunTime())");
                return lastrun1;
            }
        }

        private bool Check_SMS_Exit(string rmsid, Int64 eventid)
        {
            string str1;
            DataTable dtsms1 = new DataTable();
            try
            {
                str1 = "Select rid from SMSHISTORY Where Rmsid='" + rmsid + "' and isnull(SENDFLG,0)=1 and SMSEVENTID = " + eventid;
                dtsms1 = mssql.FillDataTable(str1, "SMSHISTORY");

                if (dtsms1.Rows.Count > 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region SMS EVENT CODE

        private bool SMS_YesterdaySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_rev_bill, wstr_date1;
            // DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, totbill = "0";

            try
            {

                Write_In_Error_Log("GENERATING YESTERDAY SALES SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),(getdate()-1),112)";
                str1 = "select ISNULL(COUNT(RID),0) AS TOTBILL,ISNULL(sum(Netamount),0) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                totbill = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
                    totbill = dtsalesinfo.Rows[0]["TOTBILL"] + "";
                }

                ///// Settlements
                //str2 = "";
                //wstr_date1 = "";
                //wstr_delflg = "isnull(delflg,0)=0";
                //wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate()-1,112)";

                //str2 = " select " +
                //      " sum(setleamount) as NetAmt from settlement " +
                //      " where " + wstr_delflg + " And " + wstr_date1;
                //mssql.OpenMsSqlConnection();
                //dtsettinfo = mssql.FillDataTable(str2, "settlement");

                //settamt = "0";
                //if (dtsettinfo.Rows.Count > 0)
                //{
                //    settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
                //}

                //if (settamt.Trim() == "")
                //{
                //    settamt = "0";
                //}

                //smstext1 = "Date : " + DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy") + ", Bill : " + totbill + " Billing Amount : " + billamt + "@" + clsPublicVariables.SMSRESTNM;
                smstext1 = "Date : " + DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy") + System.Environment.NewLine + "Total Bill : " + totbill + System.Environment.NewLine + "Total Amount : " + billamt + System.Environment.NewLine + "@ " + clsPublicVariables.SMSRESTNM;
                //Date : 123, Total Bill : 123, Billing Amount : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "YesterdaySalesSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "YSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_YesterdaySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_DailySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                            string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_rev_bill, wstr_date1;
            // DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, settamt, totbill = "0";

            try
            {

                //Write_In_Error_Log("GENERATING DAILY SALES SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = "select ISNULL(COUNT(RID),0) AS TOTBILL,isnull(sum(Netamount),0) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                totbill = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
                    totbill = dtsalesinfo.Rows[0]["TOTBILL"] + "";
                }

                ///// Settlements
                //str2 = "";
                //wstr_date1 = "";
                //wstr_delflg = "isnull(delflg,0)=0";
                //wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate(),112)";

                //str2 = "Select " +
                //       " Sum(setleamount) AS NetAmt FROM Settlement " +
                //       " Where " + wstr_delflg + " And " + wstr_date1;

                //mssql.OpenMsSqlConnection();
                //dtsettinfo = mssql.FillDataTable(str2, "settlement");

                //settamt = "0";
                //if (dtsettinfo.Rows.Count > 0)
                //{
                //    settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
                //}

                //if (settamt.Trim() == "")
                //{
                //    settamt = "0";
                //}

                //smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.GENRESTNAME;
                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + System.Environment.NewLine + "Total Bill : " + totbill + System.Environment.NewLine + "Total Amount : " + billamt + System.Environment.NewLine + "@" + clsPublicVariables.GENRESTNAME;
                //Date : 123, Billing : 123, Settlement : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "DailySalesSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "DSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_Feedback(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_mobno = "";
            string wstr_delflg = "";
            wstr_delflg = "isnull(MSTFEEDBACK.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),MSTFEEDBACK.FEEDDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(MSTFEEDBACK.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";

            DataTable dtfeedback = new DataTable();

            string custname;
            string custmobno;
            string feedrid;
            string smstext1 = "";
            // DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING FEEDBACK SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "Select MSTFEEDBACK.RID,MSTFEEDBACK.CUSTCONTNO,MSTFEEDBACK.FEEDDATE,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO From MSTFEEDBACK " +
                        " LEFT JOIN MSTCUST ON (MSTCUST.RID = MSTFEEDBACK.CUSTRID)" +
                        " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtfeedback = mssql.FillDataTable(str1, "MSTFEEDBACK");

                if (dtfeedback.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtfeedback.Rows)
                    {
                        feedrid = row1["RID"] + "".ToString();

                        custname = row1["CUSTNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTMOBNO"] + "".ToString();
                        }

                        smstext1 = "Dear " + custname + " Thank you for feedback " + clsPublicVariables.SMSRESTNM;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "FeedbackSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "FED" + feedrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_BirthdayWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            // DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Birthday From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy return of the day if you visit " + clsPublicVariables.SMSRESTNM + " will get pestry complementary";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BirthdaySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIR" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);



                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_AnniversaryWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy anniversary if you visit " + clsPublicVariables.SMSRESTNM + " will get pestry complementary";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "AnniversarySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "ANN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_BillWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();
            string custname;
            string custmobno;
            string billrid;
            string smstext1 = "";
            // DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".ToString();

                        custname = row1["CUTOMERNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTMOBNO"] + "".ToString();
                        }

                        Netamt1 = row1["NETAMOUNT"] + "".ToString();

                        //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
                        smstext1 = "Thanks for Visiting " + clsPublicVariables.SMSRESTNM + " your bill amount is " + Netamt1 + " looking for next visit";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishes";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIL" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Feedback_2(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                             string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_mobno = "";
            string wstr_delflg = "";
            wstr_delflg = "isnull(MSTFEEDBACK.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),MSTFEEDBACK.FEEDDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(MSTFEEDBACK.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";

            DataTable dtfeedback = new DataTable();
            string custname;
            string custmobno;
            string feedrid;
            string smstext1 = "";

            try
            {

                Write_In_Error_Log("GENERATING FEEDBACK SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "Select MSTFEEDBACK.RID,MSTFEEDBACK.CUSTCONTNO,MSTFEEDBACK.FEEDDATE,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO From MSTFEEDBACK " +
                        " LEFT JOIN MSTCUST ON (MSTCUST.RID = MSTFEEDBACK.CUSTRID)" +
                        " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtfeedback = mssql.FillDataTable(str1, "MSTFEEDBACK");

                if (dtfeedback.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtfeedback.Rows)
                    {
                        feedrid = row1["RID"] + "".ToString();

                        custname = row1["CUSTNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTMOBNO"] + "".ToString();
                        }

                        smstext1 = "Thank you for dinning at " + clsPublicVariables.SMSRESTNM + " and sharing your valuable feedback. We hope to serve you soon.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "FeedbackSMS2";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "FED2" + feedrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Bithday_Wishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //   DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        smstext1 = "Wishing you a many many happy returns of the day. Enjoy your birthday with " + clsPublicVariables.GENRESTNAME + ".";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BirthdaySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIR" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Anniversary_Wishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                            string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //  DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Wishing you a many many happy returns of the day. Enjoy your Anniversary with " + clsPublicVariables.GENRESTNAME + ".";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "AnniversarySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "ANN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Bithday3_Wishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";

            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        smstext1 = clsPublicVariables.GENRESTNAME + " wishes you a very Happy Birthday,filled with excitement,joy and laughter. - Team " + clsPublicVariables.GENRESTNAME;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BirthdaySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIR" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Anniversary3_Wishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";

            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "May your Anniversary lead to many more Glorious years of Happiness and Joy.Happy Anniversary. - Team " + clsPublicVariables.GENRESTNAME;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "AnniversarySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "ANN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_BillWishes_MOMOMEN(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname;
            string custmobno;
            string billrid;
            string smstext1 = "";
            //  DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING MOMOMEN Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".ToString();

                        custname = row1["CUTOMERNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTMOBNO"] + "".ToString();
                        }

                        Netamt1 = row1["NETAMOUNT"] + "".ToString();

                        //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
                        smstext1 = "Welcome to Momoman. Your Feedback is important to us. Type Momoman space your feedback and send to 56070.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishesMOMO";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BILMOMO" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_DailySalesSummary_BasicAmount(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING DAILY SALES SUMMARY BASIC AMOUNT SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = "select sum(TOTAMOUNT) as TotAmt from BILL where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["TotAmt"] + "";
                }

                if (billamt.Trim() == "")
                {
                    billamt = "0";
                }

                /// Settlements
                str2 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate(),112)";

                str2 = " select " +
                      " sum(setleamount) as NetAmt from settlement " +
                      " where " + wstr_delflg + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsettinfo = mssql.FillDataTable(str2, "settlement");

                settamt = "0";
                if (dtsettinfo.Rows.Count > 0)
                {
                    settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
                }

                if (settamt.Trim() == "")
                {
                    settamt = "0";
                }

                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.SMSSIGN;
                //Date : 123, Billing : 123, Settlement : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "DailyBasicSalesSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "DSBS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary_BasicAmount()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_DailySalesSummary_Frequenty(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_rev_bill, wstr_date1;
            //DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, totbill = "0";

            try
            {

                Write_In_Error_Log("GENERATING DAILY SALES SUMMARY Frequenty SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = "select ISNULL(count(bill.RID),0) AS TOTBILL,isnull(sum(Netamount),0) AS NetAmt  from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                totbill = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
                    totbill = dtsalesinfo.Rows[0]["TOTBILL"] + "";
                }

                //if (billamt.Trim() == "")
                //{
                //    billamt = "0";
                //}

                ///// Settlements
                //str2 = "";
                //wstr_date1 = "";
                //wstr_delflg = "isnull(delflg,0)=0";
                //wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate(),112)";

                //str2 = " select " +
                //      " sum(setleamount) as NetAmt from settlement " +
                //      " where " + wstr_delflg + " And " + wstr_date1;
                //mssql.OpenMsSqlConnection();
                //dtsettinfo = mssql.FillDataTable(str2, "settlement");

                //settamt = "0";
                //if (dtsettinfo.Rows.Count > 0)
                //{
                //    settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
                //}

                //if (settamt.Trim() == "")
                //{
                //    settamt = "0";
                //}

                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + System.Environment.NewLine + "Total Bill : " + totbill + System.Environment.NewLine + "Total Amount : " + billamt + System.Environment.NewLine + "@ " + clsPublicVariables.SMSRESTNM;
                //Date : 123, Billing : 123, Settlement : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "DailySalesSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "DSSF" + DateTime.Now.ToString("yyyyMMddHHmm") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary_Frequenty()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_BillWishes_No_Amt(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname;
            string custmobno;
            string billrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".ToString();

                        custname = row1["CUTOMERNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTMOBNO"] + "".ToString();
                        }

                        Netamt1 = row1["NETAMOUNT"] + "".ToString();

                        //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
                        smstext1 = "Thanks for Visiting " + clsPublicVariables.SMSRESTNM + " Looking For Next Visit.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishesNoAmt";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIL" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Bill_Parcle(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_rev_bill, wstr_date1;
            //DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string wstr_mobno = "";
            try
            {

                //Write_In_Error_Log("GENERATING PARCLE SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing

                wstr_delflg = "isnull(BILL.delflg,0)=0";
                wstr_rev_bill = "isnull(BILL.ISREVISEDBILL,0)=0";
                wstr_date1 = " CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";

                str1 = " Select BILL.RID,mstcust.CUSTNAME,mstcust.CUSTMOBNO " +
                        " From Bill " +
                        " INNER join mstcust on (mstcust.rid = bill.CUSTRID) " +
                        " where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1 + " AND ISNULL(bill.ISPARCELBILL,0)=1 AND " + wstr_mobno;

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                string custnm1 = "";
                string billrid = "";
                string custmobno = "";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtsalesinfo.Rows)
                    {
                        billrid = row1["RID"] + "";
                        custnm1 = row1["CUSTNAME"] + "";
                        custmobno = row1["CUSTMOBNO"] + "";

                        smstext1 = "Dear " + custnm1 + " You Will Get Your Order Food Item With In 30 min. From : " + clsPublicVariables.SMSSIGN;

                        //// INSERT INTO SMSHISTORY

                        //string[] strArr = custmobno.Split(',');
                        //Int64 cnt = 0;

                        //for (cnt = 1; cnt <= strArr.Length; cnt++)
                        //{
                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "ParcleBill";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = "";
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "PB" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                        //}
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_Bill_Parcle()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_Settlement_Parcle(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            //DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string wstr_mobno = "";
            try
            {

                //Write_In_Error_Log("GENERATING PARCLE THNX SMS [ " + DateTime.Now.ToString() + " ]");

                wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";

                str1 = "SELECT SETTLEMENT.RID,SETTLEMENT.BILLRID,mstcust.CUSTMOBNO FROM SETTLEMENT " +
                        " INNER JOIN BILL ON (SETTLEMENT.BILLRID=BILL.RID) " +
                        " INNER join mstcust on (mstcust.rid = bill.CUSTRID) " +
                        " WHERE ISNULL(BILL.ISPARCELBILL,0)=1 AND ISNULL(BILL.DELFLG,0)=0 AND " + wstr_mobno;

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                string billrid = "";
                string custmobno = "";

                if (dtsalesinfo.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtsalesinfo.Rows)
                    {

                        billrid = row1["BILLRID"] + "";
                        custmobno = row1["CUSTMOBNO"] + "";

                        smstext1 = "Thnx For Using Our Service. Looking Forward to Serve You Better. From : " + clsPublicVariables.SMSSIGN;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "SETTLEMENTBill";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = "";
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "PS" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }



                //// INSERT INTO SMSHISTORY

                //string[] strArr = eventmobno.Split(',');
                //Int64 cnt = 0;

                //for (cnt = 1; cnt <= strArr.Length; cnt++)
                //{

                //}


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_Settlent_Parcle()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_Bill_EDIT_Info(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_date1;
            //DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";

            try
            {

                Write_In_Error_Log("GENERATING Bill Edit Information SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing                

                wstr_date1 = " CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = " SELECT BILL.RID,BILL.BILLNO,BILL.REFBILLNO,MSTUSERS.USERNAME,BILL.EDATETIME " +
                        " FROM BILL " +
                        " LEFT JOIN MSTUSERS ON (MSTUSERS.RID=BILL.EUSERID) " +
                        " WHERE ISNULL(BILL.DELFLG,0)=0 AND ISNULL(BILL.EUSERID,0)>0 " +
                        " AND " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                string billno1, refno1, usernm1;
                string stredatetime1, billrid1;
                DateTime edatetime1;

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    if (dtsalesinfo.Rows.Count > 0)
                    {
                        foreach (DataRow row1 in dtsalesinfo.Rows)
                        {
                            billrid1 = row1["RID"] + "";
                            billno1 = row1["BILLNO"] + "";
                            refno1 = row1["REFBILLNO"] + "";
                            usernm1 = row1["USERNAME"] + "";
                            edatetime1 = Convert.ToDateTime(row1["EDATETIME"]);
                            stredatetime1 = edatetime1.ToString("dd/MM/yyyy hh:mm");

                            smstext1 = "BILL NO : " + billno1 + "\r\n" +
                                       "REF.NO : " + refno1 + "\r\n" +
                                       "EDITED BY USER : " + usernm1 + " : " + stredatetime1 + "\r\n" +
                                       "FROM : " + clsPublicVariables.SMSSIGN;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "BEI";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = "";
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "BEI" + billrid1 + strArr[cnt - 1].ToString();
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_Bill_EDIT_Info()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_Bill_DELETE_Info(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_date1;
            //DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";

            try
            {

                Write_In_Error_Log("GENERATING Bill Delete Information SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing                

                wstr_date1 = " CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = " SELECT BILL.RID,BILL.BILLNO,BILL.REFBILLNO,MSTUSERS.USERNAME,BILL.DDATETIME " +
                        " FROM BILL " +
                        " LEFT JOIN MSTUSERS ON (MSTUSERS.RID=BILL.DUSERID) " +
                        " WHERE ISNULL(BILL.DELFLG,0)=1 AND ISNULL(BILL.DUSERID,0)>0 " +
                        " AND " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                string billno1, refno1, usernm1;
                string stredatetime1, billrid1;
                DateTime edatetime1;

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    if (dtsalesinfo.Rows.Count > 0)
                    {
                        foreach (DataRow row1 in dtsalesinfo.Rows)
                        {
                            billrid1 = row1["RID"] + "";
                            billno1 = row1["BILLNO"] + "";
                            refno1 = row1["REFBILLNO"] + "";
                            usernm1 = row1["USERNAME"] + "";
                            edatetime1 = Convert.ToDateTime(row1["DDATETIME"]);
                            stredatetime1 = edatetime1.ToString("dd/MM/yyyy hh:mm");

                            smstext1 = "BILL NO : " + billno1 + "\r\n" +
                                       "REF.NO : " + refno1 + "\r\n" +
                                       "DELETED BY USER : " + usernm1 + " : " + stredatetime1 + "\r\n" +
                                       "FROM : " + clsPublicVariables.SMSSIGN;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "BDI";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = "";
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "BDI" + billrid1 + strArr[cnt - 1].ToString();
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_Bill_DELETE_Info()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_BillWishes_With_Amount(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname = "";
            string custmobno = "";
            string billrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes With Amount SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".Trim();

                        custname = row1["CUTOMERNAME"] + "".Trim();

                        if (custname == "")
                        {
                            custname = "Guest";
                        }

                        //custmobno = row1["CUSTCONTNO"] + "".ToString();

                        //if (custmobno.Trim() == "")
                        //{
                        custmobno = row1["CUSTMOBNO"] + "".Trim();
                        //}

                        Netamt1 = row1["NETAMOUNT"] + "".Trim();

                        smstext1 = "Hi " + custname + " Thank you for visiting " + clsPublicVariables.GENRESTNAME + "." + System.Environment.NewLine +
                                    "Your total bill is " + Netamt1 + ". Visit us again.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishesWithAmt";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BILWA" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_BillWishes_With_Amount()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_BillWishes_With_Amount_Type2(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname = "";
            string custmobno = "";
            string billrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes With Amount SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".Trim();

                        custname = row1["CUTOMERNAME"] + "".Trim();

                        if (custname == "")
                        {
                            custname = "Guest";
                        }
                        custmobno = row1["CUSTMOBNO"] + "".Trim();

                        Netamt1 = row1["NETAMOUNT"] + "".Trim();

                        smstext1 = "and the ADVENTURE begins at " + clsPublicVariables.GENRESTNAME + System.Environment.NewLine +
                                    "Your bill is " + Netamt1 + " Rs." + System.Environment.NewLine +
                                    "Keep Exploring";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishesWithAmtType2";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BILWA2" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_BillWishes_With_Amount_Type2()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_BillWishes_PizzZone(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname = "";
            string custmobno = "";
            string billrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".Trim();

                        custname = row1["CUTOMERNAME"] + "".Trim();

                        if (custname == "")
                        {
                            custname = "Guest";
                        }
                        custmobno = row1["CUSTMOBNO"] + "".Trim();

                        Netamt1 = row1["NETAMOUNT"] + "".Trim();

                        smstext1 = "Thank you For Visiting " + clsPublicVariables.GENRESTNAME + ". We Commited To Serve You Better And Better Food Quality " +
                                    "Please Visit Again And Kindly Share Your Experience With Us or Kindly Visit Our website For More Info " +
                                    "WWW.PIZZAZONE.CO.IN or Order Online Through (079-27559666,27494440)";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishesPizzZone";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BILWPIZZZONE" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_BillWishes()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_EveryBill(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            String str1;
            String wstr_date1 = "";
            String wstr_delflg = "";

            wstr_delflg = "Isnull(BILL.Delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            DataTable dtbill = new DataTable();

            String billno = "";
            String billrid;
            String smstext1 = "";
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING BILL SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.REFBILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT FROM BILL " +
                             " WHERE " + wstr_delflg + " AND " + wstr_date1;

                dtbill = mssql.FillDataTable(str1, "BILL");

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                Netamt1 = "0";

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    if (dtbill.Rows.Count > 0)
                    {
                        foreach (DataRow row1 in dtbill.Rows)
                        {

                            billrid = row1["RID"] + "".Trim();

                            billno = row1["BILLNO"] + "".Trim();
                            if (clsPublicVariables.BILLNOBASEDON == "REFBILLNO")
                            {
                                billno = row1["REFBILLNO"] + "".Trim();
                            }

                            Netamt1 = row1["NETAMOUNT"] + "".Trim();

                            smstext1 = "BILL NO : " + billno + " AMOUNT : " + Netamt1 + System.Environment.NewLine + clsPublicVariables.GENRESTNAME;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "EBILL";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = "";
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "EBILL" + billrid + strArr[cnt - 1].ToString();
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_EveryBill()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_RewardPointEntry(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                       string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "ISNULL(REWPTS.DELFLG,0)=0";
            wstr_date1 = "CONVERT(varchar(10),REWPTS.REWDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname = "";
            string custmobno = "";
            string rewrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String rewpts1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Reward Point SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT REWPTS.RID,REWPTS.CUSTRID,MSTCUST.CUSTNAME,CUSTMOBNO,REWPTS.CARDNO,REWPTS.REWPTS " +
                            " FROM REWPTS " +
                            " INNER JOIN MSTCUST ON (MSTCUST.RID=REWPTS.CUSTRID)" +
                            " WHERE " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno + " AND MSTCUST.CARDSTATUS='Activated'";

                dtbill = mssql.FillDataTable(str1, "REWPTS");

                rewpts1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        rewrid = row1["RID"] + "".Trim();

                        custname = row1["CUSTNAME"] + "".Trim();

                        if (custname == "")
                        {
                            custname = "Guest";
                        }
                        custmobno = row1["CUSTMOBNO"] + "".Trim();

                        rewpts1 = row1["REWPTS"] + "".Trim();

                        smstext1 = "Welcome to South diaries Restaurant's rewards program. You earned " + rewpts1 + " pts." +
                                    "Now earn points on every bill and like and share in our Facebook page thank u visit again.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "REWPTS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "REWPTS" + rewrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_RewardPointEntry()) " + DateTime.Now.ToString());
                return false;
            }
        }


        private bool SMS_BillRewardSMS(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                      string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_mobno = "";
            string wstr_delflg = "";
            wstr_delflg = "ISNULL(BILL.DELFLG,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname = "";
            string custmobno = "";
            string rewrid;
            string smstext1 = "";
            //DateTime nextrun1;

            Decimal rewpts1 = 0;
            try
            {

                Write_In_Error_Log("GENERATING Bill Reward Point SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.NETAMOUNT,(ISNULL(BILL.TOTBILLREWPOINT,0) + ISNULL(BILL.TOTITEMREWPOINT,0)) AS TOTREWARD,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO,MSTCUST.CARDSTATUS,MSTCUST.CARDACTDATE " +
                        " FROM BILL  " +
                        " INNER JOIN MSTCUST ON (MSTCUST.CARDNO=BILL.CARDNO) " +
                        " WHERE " + wstr_delflg + " AND " + wstr_mobno +
                        " AND ISNULL(MSTCUST.CARDNO,'') <>''" + " AND MSTCUST.CARDSTATUS='Activated' " +
                        " AND ISNULL((ISNULL(BILL.TOTBILLREWPOINT,0) + ISNULL(BILL.TOTITEMREWPOINT,0)),0)<>0 AND BILL.BILLDATE >= MSTCUST.CARDACTDATE " +
                        " AND " + wstr_date1 + " ORDER BY BILL.RID ";

                dtbill = mssql.FillDataTable(str1, "REWPTS");

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        rewrid = row1["RID"] + "".Trim();

                        custname = row1["CUSTNAME"] + "".Trim();

                        if (custname == "")
                        {
                            custname = "Guest";
                        }
                        custmobno = row1["CUSTMOBNO"] + "".Trim();

                        Decimal.TryParse(row1["TOTREWARD"] + "", out rewpts1);

                        if (rewpts1 < 0)
                        {
                            smstext1 = "Hi, thanks for visiting South diaries Authentic South Indian Food. You used " + rewpts1 + " pts." +
                                        "Share and like our Facebook page thank u visit again.";
                        }
                        else
                        {
                            smstext1 = "Hi, thanks for visiting South diaries Authentic South Indian Food. You earned " + rewpts1 + " pts." +
                                        "Share and like our Facebook page thank u visit again.";
                        }

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BILLREWPTS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BILLREWPTS" + rewrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_BillRewardSMS()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_EveryBillWithTotalBilling(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                                    string eventtext, string eventmobno, string eventemail)
        {
            String str1;
            String str2;
            String wstr_date1 = "";
            String wstr_delflg = "";

            wstr_delflg = "Isnull(BILL.Delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";

            DataTable dtbill = new DataTable();
            DataTable dttotbill = new DataTable();

            String billno = "";
            String billrid;
            String smstext1 = "";
            String Netamt1 = "0";
            String totnetamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING BILL WITH TOTAL SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.REFBILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT FROM BILL " +
                             " WHERE " + wstr_delflg + " AND " + wstr_date1;
                dtbill = mssql.FillDataTable(str1, "BILL");

                str2 = "SELECT ISNULL(SUM(BILL.NETAMOUNT),0) AS TOTNETAMOUNT FROM BILL " +
                            " WHERE " + wstr_delflg + " AND " + wstr_date1;
                dttotbill = mssql.FillDataTable(str2, "BILL");

                if (dttotbill.Rows.Count > 0)
                {
                    totnetamt1 = dttotbill.Rows[0]["TOTNETAMOUNT"] + "".Trim();
                }

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                Netamt1 = "0";

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    if (dtbill.Rows.Count > 0)
                    {
                        foreach (DataRow row1 in dtbill.Rows)
                        {

                            billrid = row1["RID"] + "".Trim();

                            billno = row1["BILLNO"] + "".Trim();
                            if (clsPublicVariables.BILLNOBASEDON == "REFBILLNO")
                            {
                                billno = row1["REFBILLNO"] + "".Trim();
                            }

                            Netamt1 = row1["NETAMOUNT"] + "".Trim();

                            smstext1 = "BILL NO : " + billno + " BILL AMOUNT : " + Netamt1 + System.Environment.NewLine +
                                        "TOTAL BILLING : " + totnetamt1 + System.Environment.NewLine + clsPublicVariables.GENRESTNAME;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "EBILL";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = "";
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "ETBILL" + billrid + strArr[cnt - 1].ToString();
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_EveryBillWithTotalBilling()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_BillWishes_No_Amt_MagicIceCream(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname;
            string custmobno;
            string billrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".ToString();

                        custname = row1["CUTOMERNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTMOBNO"] + "".ToString();
                        }

                        Netamt1 = row1["NETAMOUNT"] + "".ToString();

                        //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
                        smstext1 = "you can order on our contactno - 9537990525 and take advantage of free home delivery.looking for your next visit.thanks for visiting the magic fruit.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BillWishesNoAmt";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIL" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Birthday_RAJTHAAL(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail,
                                              string templateid1, string otherid1, string para1, string para2, string para3)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";

            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT * FROM BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        smstext1 = para1;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BirthdaySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIRRAJTHAAL" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);



                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_AnniversaryWishes_RAJTHAAL(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail,
                                              string templateid1, string otherid1, string para1, string para2, string para3)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        smstext1 = para1;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "AnniversarySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "ANNRAJTHAL" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }





        #endregion

        #region EMAIL EVENT CODE

        private bool EMAIL_YesterdaySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            // string billamt, settamt = "0";

            try
            {

                //Write_In_Error_Log("GENERATING YESTERDAY SALES SUMMARY E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_BusinessSummary(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date.AddDays(-1));

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {

                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Yesterday Sales Summary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "Email - Yesterday Sales Summary " + DateTime.Now.Date.AddDays(-1);
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILYSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smstext1, false);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_YesterdaySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_DailySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                            string eventtext, string eventmobno, string eventemail)
        {
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();
            string smstext1 = "";

            try
            {
                smstext1 = this.Generate_BusinessSummary(DateTime.Now.Date, DateTime.Now.Date);

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Daily Sales Summary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "EMAIL - Daily Sales Summary" + DateTime.Now.Date;
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILDSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smstext1, false);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_DailyPurchaseStockRegister_Frequentry(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                        string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                // Write_In_Error_Log("GENERATING PURCHASE STOCK REGISTER E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_PurchaseStockRegister(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + PadCenter("PURCAHSE STOCK REGISTER : " + clsPublicVariables.SMSRESTNM, PMAXCHAR);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Purchase Stock Register Frequentry";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "Email - Purchase Stock Register Frequentry : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILPURSTKREG" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smstext1, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_DailyPurchaseStockRegister_Frequentry()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_DailySalesSummary_Frequentry(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING DAILY SALES SUMMARY E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_BusinessSummary(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + PadCenter("BUSINESS SUMMARY REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Daily Sales Summary Frequentry";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "EMAIL - Daily Sales Summary Frequentry : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILDSSFRE" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL((strArr[cnt - 1] + ""), smsbal.Smsevent, smstext1, false);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_DailySalesSummary_Frequentry()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_CashBusinessSummary_Frequentry(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING DAILY CASH SUMMARY E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_CashSummary(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + PadCenter("CASH SUMMARY REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Daily Cash Summary Frequentry";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "EMAIL - Daily Cash Summary Frequentry : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILCASHSUM" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL((strArr[cnt - 1] + ""), smsbal.Smsevent, smstext1, false);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_CashBusinessSummary_Frequentry()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_EveryBill(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING BILL E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_CashSummary(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + PadCenter("CASH SUMMARY REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Daily Cash Summary Frequentry";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "EMAIL - Daily Cash Summary Frequentry : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILCASHSUM" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL((strArr[cnt - 1] + ""), smsbal.Smsevent, smstext1, false);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_CashBusinessSummary_Frequentry()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_Feedback(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_mobno = "";
            string wstr_delflg = "";
            wstr_delflg = "isnull(MSTFEEDBACK.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),MSTFEEDBACK.FEEDDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(MSTCUST.CUSTEMAIL,'') <> '')";

            DataTable dtfeedback = new DataTable();

            string custname;
            string custmobno;
            string feedrid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING FEEDBACK E-MAIL [ " + DateTime.Now.ToString() + " ]");

                str1 = "Select MSTFEEDBACK.RID,MSTFEEDBACK.CUSTCONTNO,MSTFEEDBACK.FEEDDATE,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO,MSTCUST.CUSTEMAIL From MSTFEEDBACK " +
                        " LEFT JOIN MSTCUST ON (MSTCUST.RID = MSTFEEDBACK.CUSTRID)" +
                        " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtfeedback = mssql.FillDataTable(str1, "MSTFEEDBACK");

                if (dtfeedback.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtfeedback.Rows)
                    {
                        feedrid = row1["RID"] + "".ToString();

                        custname = row1["CUSTNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTEMAIL"] + "".ToString();
                        }

                        //custname = row1["CUSTNAME"] + "".ToString();
                        //custmobno = row1["CUSTCONTNO"] + "".ToString();

                        smstext1 = "Dear " + custname + " Thank you for feedback " + clsPublicVariables.SMSSIGN;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "Email Feedback SMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = "";
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = "";
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILFED" + feedrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_BirthdayWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY E-MAIL [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["EMAIL"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Birthday From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy return of the day if you visit " + clsPublicVariables.SMSSIGN + " will get pestry complementary";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "EMAIL Birthday";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = "";
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILBIR" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_AnniversaryWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY E-MAIL [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["EMAIL"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy anniversary if you visit " + clsPublicVariables.SMSSIGN + " will get pestry complementary";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "EMAIL Anniversary";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILANN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_BillWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "( ISNULL(MSTCUST.CUSTEMAIL,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname;
            string custmobno;
            string billrid;
            string smstext1 = "";
            //  DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO, ISNULL(MSTCUST.CUSTEMAIL,'') AS  CUSTEMAIL From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".ToString();

                        custname = row1["CUTOMERNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }
                        custmobno = row1["CUSTEMAIL"] + "".ToString();
                        Netamt1 = row1["NETAMOUNT"] + "".ToString();

                        //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
                        smstext1 = "Thanks For Visiting " + clsPublicVariables.SMSSIGN + " Your bill amount is " + Netamt1 + " Looking for Next visit";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "EMAIL Bill Wishes";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILBIL" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_BillEditDelete_Frequentry(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                        string eventtext, string eventmobno, string eventemail)
        {

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING BILL EDIT/DELETE E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_BillEditDeleteSummaryEmail(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + PadCenter("BILL EDIT/DELETE REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Bill Edit Delete";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "Email - Bill Edit Delete : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILBED" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL((strArr[cnt - 1] + ""), smsbal.Smsevent, smstext1, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_BillEditDelete_Frequentry()) " + DateTime.Now.ToString());
                return false;
            }
        }


        private bool EMAIL_ItemWiseSales_Frequentry(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                        string eventtext, string eventmobno, string eventemail)
        {

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING ITEM WISE SALES E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_ItemWiseSalesRegister(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + PadCenter("Item Sales REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Item Wise Sales";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "Email - Item Wise Sales : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILIWS" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL((strArr[cnt - 1] + ""), smsbal.Smsevent, smstext1, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_ItemWiseSales_Frequentry()) " + DateTime.Now.ToString());
                return false;
            }
        }

        #endregion

        #region WHATSAPP EVENT CODE

        private bool WHATSAPP_DailySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_rev_bill, wstr_date1;
            // DateTime nextrun1;
            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, totbill = "0";

            try
            {

                //Write_In_Error_Log("GENERATING DAILY SALES SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = "select ISNULL(COUNT(RID),0) AS TOTBILL,isnull(sum(Netamount),0) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                totbill = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
                    totbill = dtsalesinfo.Rows[0]["TOTBILL"] + "";
                }

                //smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.GENRESTNAME;
                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + System.Environment.NewLine + "Total Bill : " + totbill + System.Environment.NewLine + "Total Amount : " + billamt + System.Environment.NewLine + "@" + clsPublicVariables.GENRESTNAME;
                //Date : 123, Billing : 123, Settlement : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "DailySalesSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "DSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in WHATSAPP_DailySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        #endregion  

        private void tmrsms_Tick(object sender, EventArgs e)
        {
            this.Send_SMS_Scheduler();
        }

        private bool Send_SMS_Scheduler()
        {
            String str1, str2, wstr_date1 = "";

            DataTable dtsms = new DataTable();
            DataTable dtinfo1 = new DataTable();

            Int64 rid1 = 0;
            String sms1 = "";
            String mob1 = "";
            String response = "";

            string smseventid1 = "";
            string templateid1 = "";
            string otherid1 = "";
            string para1 = "";
            string para2 = "";
            string para3 = "";


            try
            {
                //Write_In_Error_Log("CHECKING FOR NEW SMS [ " + DateTime.Now.ToString() + " ]");

                wstr_date1 = "CONVERT(varchar(10),TOBESENDDATETIME,112) <= CONVERT(varchar(10),getdate(),112)";

                str1 = "SELECT * FROM SMSHISTORY WHERE ISNULL(SENDFLG,0)=0 AND " + wstr_date1 + " Order by TOBESENDDATETIME,SMSEVENTID,RID";

                dtsms = mssql.FillDataTable(str1, "SMSHISTORY");

                if (dtsms.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtsms.Rows)
                    {
                        Int64.TryParse(row1["RID"] + "".ToString(), out rid1);
                        sms1 = row1["SMSTEXT"] + "".ToString();
                        mob1 = row1["MOBNO"] + "".ToString();
                        smseventid1 = row1["SMSEVENTID"] + "".Trim();

                        string strqry1 = "SELECT * FROM EVENTSCHEDULER WHERE RID = " + smseventid1;
                        dtinfo1 = mssql.FillDataTable(strqry1, "EVENTSCHEDULER");

                        if (dtinfo1.Rows.Count>0)
                        {
                            templateid1 = dtinfo1.Rows[0]["TEMPLATEID"] + "".Trim();
                            otherid1 = dtinfo1.Rows[0]["OTHERID"] + "".Trim();
                            para1 = dtinfo1.Rows[0]["PARA1"] + "".Trim();
                            para2 = dtinfo1.Rows[0]["PARA2"] + "".Trim();
                            para3 = dtinfo1.Rows[0]["PARA3"] + "".Trim();
                        }

                        response = this.SENDSMS_TRANSACTIONAL(sms1, mob1, templateid1, otherid1, para1, para2, para3);

                        str2 = "UPDATE SMSHISTORY SET SENDFLG = 1,RESID = '" + response + "' , SENDDATETIME = '" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + rid1;
                        //Write_In_Error_Log("SMS ( " + sms1 + " ) SEND. @ [" + DateTime.Now.ToString() + " ]");
                        mssql.ExecuteMsSqlCommand(str2);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in Send_SMS()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private string SENDSMS_TRANSACTIONAL(string smstext, string mobno1, string templateid1 = "", string otherid1 = "", string para1 = "", string para2 = "", string para3 = "")
        {
            string smsurl = "";
            string smsresponse = "ERROR";
            string smsmethod = "0";

            try
            {
                if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "ASK4SMS")
                {
                    mobno1 = "91" + objclsgen.RightString(mobno1, 10);
                    smstext = smstext.Replace("&", "%26");
                    smstext = smstext.Replace("$", "%24");
                    smstext = smstext.Replace("@", "%40");
                    smstext = smstext.Replace("!", "%21");
                    smstext = smstext.Replace("?", "%3F");
                    smstext = smstext.Replace("/", "%2F");
                    smstext = smstext.Replace("-", "%2D");
                    smstext = smstext.Replace("*", "%2A");

                    //smsurl = "http://59.144.126.28/smsserver/SMS10N.aspx?Userid=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&to=" + mobno1 + "&text=" + smstext + "&from=" + clsPublicVariables.SMSSIGN;
                    smsurl = "http://api.ask4sms.com/sms/1/text/query?username=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&to=" + mobno1 + "&text=" + smstext + "&from=" + clsPublicVariables.SMSSIGN;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "FECUND")
                {
                    smsurl = "http://sms.fecundtechno.com/sendsms.aspx?mobile=" + clsPublicVariables.TRAUSERID + "&pass=" + clsPublicVariables.TRAPASSWORD + "&senderid=" + clsPublicVariables.SMSSIGN + "&to=" + mobno1 + "&msg=" + smstext;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "MOBISOFT")
                {
                    smsurl = "http://mobi1.blogdns.com/WebSMSS/SMSSenders.aspx?UserID=" + clsPublicVariables.TRAUSERID + "&UserPass=" + clsPublicVariables.TRAPASSWORD + "&Message=" + smstext + "&MobileNo=" + mobno1 + "&GSMID=" + clsPublicVariables.SMSSIGN;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSGUPSHUP")
                {
                    smstext = smstext.Replace("&", "%26");
                    smsurl = "http://enterprise.smsgupshup.com/GatewayAPI/rest?msg=" + smstext + "&Message&v=1.1&userid=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&send_to=" + mobno1 + "&msg_type=text&method=sendMessage";
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSIDEA")
                {
                    //smstext = smstext.Replace("&", "%26");                    
                    smsurl = "http://www.smsidea.co.in/sendsms.aspx?mobile=" + clsPublicVariables.TRAUSERID + "&pass=" + clsPublicVariables.TRAPASSWORD + "&senderid=" + clsPublicVariables.SMSSIGN + "&to=" + mobno1 + "&msg=" + smstext;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "HAKIMI")
                {
                    smsurl = "http://psms.hakimisolution.com/submitsms.jsp?user=" + clsPublicVariables.TRAUSERID + "&key=" + clsPublicVariables.TRAPASSWORD + "&mobile=" + mobno1 + "&message=" + smstext + "&senderid=" + clsPublicVariables.SMSSIGN + "&accusage=1";
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "HAKIMI2")
                {
                    smsurl = "http://psms.hakimisolution.com/submitsms.jsp?user=" + clsPublicVariables.TRAUSERID + "&key=" + clsPublicVariables.TRAPASSWORD + "&mobile=" + mobno1 + "&message=" + smstext + "&senderid=" + clsPublicVariables.SMSSIGN + "&accusage=2";
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SHREESMS")
                {
                    smsurl = "http://59.144.126.28/smsserver/SMS10N.aspx?Userid=" + clsPublicVariables.TRAUSERID + "&UserPassword=" + clsPublicVariables.TRAPASSWORD + "&PhoneNumber=" + mobno1 + "&Text=" + smstext + "&GSM=" + clsPublicVariables.SMSSIGN;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSSCHOOL")
                {
                    smstext = smstext.Replace("&", "%26");
                    smsurl = "http://smsschool.in/api/sendhttp.php?authkey=" + clsPublicVariables.TRAUSERID + "&mobiles=" + mobno1 + "&message=" + smstext + "&sender=" + clsPublicVariables.SMSSIGN + "&route=4&country=0";
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "BNRATHI")
                {
                    smstext = smstext.Replace("&", "%26");
                    smsurl = "http://bulkpush.mytoday.com/BulkSms/SingleMsgApi?feedid=" + clsPublicVariables.SMSSIGN + "&username=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&To=" + mobno1 + "&Text=" + smstext;
                }

                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "ARSONS")
                {
                    smstext = smstext.Replace("&", "%26");
                    smsurl = "http://vtermination.com/api/sendhttp.php?country=91&sender=" + clsPublicVariables.SMSSIGN + "&route=4&mobiles=" + mobno1 + "&authkey=" + clsPublicVariables.GENSMSAUTHOKEY + "&message=" + smstext;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "MART2GLOBAL")
                {
                    mobno1 = "91" + objclsgen.RightString(mobno1, 10);
                    smstext = smstext.Replace("&", "%26");
                    //smstext = smstext.Replace("$", "%24");
                    //smstext = smstext.Replace("@", "%40");
                    //smstext = smstext.Replace("!", "%21");
                    //smstext = smstext.Replace("?", "%3F");
                    //smstext = smstext.Replace("/", "%2F");
                    //smstext = smstext.Replace("-", "%2D");
                    //smstext = smstext.Replace("*", "%2A");
                    smsurl = "http://sendsmsbox.com/api/mt/SendSMS?user=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&senderid=" + clsPublicVariables.SMSSIGN + "&channel=Trans&DCS=0&flashsms=0&number=" + mobno1 + "&text=" + smstext + "&route=3&PEId=" + otherid1 + "&DLTTemplateId=" + templateid1;
                    smsmethod = "1";
                }
                else
                {
                    // SMSINDIAHUB 
                    smsurl = "http://cloud.smsindiahub.in/vendorsms/pushsms.aspx?user=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&msisdn=" + mobno1 + "&sid=" + clsPublicVariables.SMSSIGN + "&msg=" + smstext + "&fl=0";
                    //Write_In_Error_Log(smsurl);
                }

                //MessageBox.Show(smsurl);
                smsresponse = objclsgen.SEND_WEB_REQUEST(smsurl,smsmethod);

                return smsresponse;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SENDSMS_TRANSACTIONAL()) " + DateTime.Now.ToString());
                return smsresponse;
            }
        }

        public string SENDWHATSAPP_TRANSACTION(string smstext, string mobno1)
        {
            string url1 = "";
            string response1 = "ERROR";
            string whatsapppro1 = "";

            try
            {
                whatsapppro1 = clsPublicVariables.GENWHATSAPPPROVIDER + "".ToUpper();

                switch (whatsapppro1)
                {
                    case "DOUBLETICK":
                        mobno1 = "91" + objclsgen.RightString(mobno1, 10);
                        smstext = smstext.Replace("&", "%26");
                        smstext = smstext.Replace("$", "%24");
                        smstext = smstext.Replace("@", "%40");
                        smstext = smstext.Replace("!", "%21");
                        smstext = smstext.Replace("?", "%3F");
                        smstext = smstext.Replace("/", "%2F");
                        smstext = smstext.Replace("-", "%2D");
                        smstext = smstext.Replace("*", "%2A");
                        url1 = "http://wa.doubletick.co.in/api/v2/sendWAMessage?token=" + clsPublicVariables.GENWHATSAPPAUTHKEY + "& to =" + mobno1 + "& type = text & text =" + smstext;
                        break;
                    case "BIGTOS":
                        mobno1 = "91" + objclsgen.RightString(mobno1, 10);
                        smstext = smstext.Replace("&", "%26");
                        smstext = smstext.Replace("$", "%24");
                        smstext = smstext.Replace("@", "%40");
                        smstext = smstext.Replace("!", "%21");
                        smstext = smstext.Replace("?", "%3F");
                        smstext = smstext.Replace("/", "%2F");
                        smstext = smstext.Replace("-", "%2D");
                        smstext = smstext.Replace("*", "%2A");
                        url1 = "http://www.cp.bigtos.com/api/v1/sendmessage?key=" + clsPublicVariables.GENWHATSAPPAUTHKEY + "&type=Text&mobileno=" + mobno1 + "&msg=" + smstext;
                        break;

                    default:
                        break;

                }

                response1 = objclsgen.SEND_WEB_REQUEST(url1);

                return response1;
            }
            catch (Exception)
            {
                return response1;
            }

        }

        private bool SEND_EMAIL(string emailto1, string subject1, string smstext1, bool isbodyhtml)
        {
            MailMessage message;
            SmtpClient smtp;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                message = new MailMessage();

                message.To.Add(emailto1);
                //message.CC.Add(this.txtcc.Text.Trim());
                message.Subject = subject1;
                message.From = new MailAddress(clsPublicVariables.GENSMTPEMAILADDRESS);
                message.Body = smstext1;
                if (isbodyhtml)
                {
                    message.IsBodyHtml = true;
                }
                // set smtp details
                smtp = new SmtpClient(clsPublicVariables.GENSMTPADDRESS);

                Int32 gensmtpport;
                Int32.TryParse(clsPublicVariables.GENSMTPPORT, out gensmtpport);
                smtp.Port = gensmtpport;

                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(clsPublicVariables.GENSMTPEMAILADDRESS, clsPublicVariables.GENSMTPEMAILPASSWORD);
                smtp.SendAsync(message, message.Subject);
                smtp.SendCompleted += new SendCompletedEventHandler(smtp_SendCompleted);

                return true;
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                //MessageBox.Show("Error occured in SEND_EMAIL() : " + ex.Message.ToString(), clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Write_In_Error_Log("Error occured in SEND_EMAIL() : " + ex.Message.ToString() + DateTime.Now.ToString());
                return false;
            }
        }

        void smtp_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                //Cursor.Current = Cursors.WaitCursor;

                if (e.Cancelled == true)
                {
                    //Cursor.Current = Cursors.Default;
                    //MessageBox.Show("Email sending cancelled.", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Write_In_Error_Log("Email sending cancelled.");
                }
                else if (e.Error != null)
                {
                    //Cursor.Current = Cursors.Default;
                    //MessageBox.Show("Error occured in Sending E-Mail : " + e.Error.Message, clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Write_In_Error_Log("Error occured in Sending E-Mail : " + e.Error.Message);
                }
                else
                {
                    //Cursor.Current = Cursors.Default;
                    //MessageBox.Show("E-Mail Sent Sucessfully.", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Write_In_Error_Log("E-Mail Sent Sucessfully.");
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                //MessageBox.Show("Error occured in smtp_SendCompleted()." + ex.Message.ToString(), clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Write_In_Error_Log("Error occured in smtp_SendCompleted()." + ex.Message.ToString());

            }
        }

        private void frmeventserver_Load(object sender, EventArgs e)
        {
            try
            {
                clsPublicVariables.EventVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(clsPublicVariables.ActualFilePath).FileVersion.ToString();
                this.Text = "EVENT SCHEDULER SERVER [ Version : " + clsPublicVariables.EventVer + " ] Date : " + DateTime.Today.ToShortDateString();


                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg == "AUTO")
                    {
                        this.btnrun_Click(sender, e);
                    }
                }
            }
            catch (Exception)
            { }
        }

        private String Generate_BusinessSummary(DateTime Fromdate, DateTime Todate)
        {
            string PrintStr1 = "";
            string str1 = "";
            string tstr1 = "";
            string wstr_date1 = "";
            string wstr_Billdate = "";
            string wstr_delflg = "";
            string wstr_rev_bill = "";
            DataTable dtbill = new DataTable();
            DataTable dtbill1 = new DataTable();

            try
            {

                //Write_In_Error_Log("In Generate_BusinessSummary [ " + DateTime.Now.ToString() + " ]");
                PrintStr1 = "";

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("BUSINESS SUMMARY REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR + 100);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("FROM:" + Fromdate.ToShortDateString() + " " + "TO:" + Todate.ToShortDateString(), PMAXCHAR + 100);

                ////////////////////////////////////////////////////////////////////////////////////////////
                // BILL BREAKUP INFORMATION

                str1 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "billdate >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and billdate <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "SELECT " +
                         " count(rid)  As Totbill " +
                         " From Bill" +
                         " where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "BILL");

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("BILL BREAKUP INFORMATION", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Total Bill Count").PadLeft(15) + " : " + (dtbill.Rows[0]["Totbill"] + "".PadLeft(15));
                }

                //Write_In_Error_Log("AFTER BILL BREAKUP INFORMATION [ " + DateTime.Now.ToString() + " ]");

                ////////////////////////
                // Billing Details
                str1 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "billdate >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and billdate <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "Select " +
                          " sum(Totamount)  As Totamount, " +
                          " sum(TOTADDVATAMOUNT) As TOTADDVATAMOUNT, " +
                          " sum(TOTDISCAMOUNT) As TOTDISCAMOUNT," +
                          " sum(TOTADDDISCAMT) As TOTADDDISCAMT," +
                          " sum(TOTSERCHRAMT) As TOTSERCHRAMT, " +
                          " sum(TOTVATAMOUNT) As TOTVATAMOUNT," +
                          " sum(TOTBEVVATAMT) As TOTBEVVATAMT," +
                          " sum(TOTLIQVATAMT) As TOTLIQVATAMT," +
                          " sum(TOTGSTAMT) As TOTGSTAMT," +
                          " sum(CGSTAMT) As TOTCGSTAMT," +
                          " sum(SGSTAMT) As TOTSGSTAMT," +
                          " sum(IGSTAMT) As TOTIGSTAMT," +
                          " sum(TOTROFF)  As TOTROFF, " +
                          " sum(NETAMOUNT)  As NETAMOUNT " +
                          " From Bill" +
                          " where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "bill");

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("BILLING DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Total Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["Totamount"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Discount").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTDISCAMOUNT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Ser.Chr").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTSERCHRAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Food VAT").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTVATAMOUNT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Bev.VAT").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTBEVVATAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Liq.VAT").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTLIQVATAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("CGST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTCGSTAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("SGST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTSGSTAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("IGST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTIGSTAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Total GST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTGSTAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Round Off").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTROFF"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Add.Disc").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTADDDISCAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Net Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["NETAMOUNT"] + "".PadLeft(15));
                }

                //Write_In_Error_Log("2. AFTER BILL  INFORMATION [ " + DateTime.Now.ToString() + " ]");
                ////////////////////////////////////////////////////////////////////////////////////////////
                //COUPON DISCOUNT

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("COUPON DISCOUNT", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                str1 = "";
                wstr_date1 = "";

                wstr_delflg = "isnull(BILL.Delflg,0)=0";
                wstr_rev_bill = "isnull(BILL.ISREVISEDBILL,0)=0";
                wstr_date1 = "Bill.billdate >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and Bill.billdate <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "Select " +
                          " ((SUM(ISNULL(BILLDTL.IAMT,0))) * -1) AS COUPONDISC " +
                          " From Bill" +
                          " LEFT JOIN BILLDTL ON (BILLDTL.BILLRID = Bill.RID) " +
                          " where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1 + " And " + " ISNULL(BILLDTL.DELFLG,0)=0 " + " AND ISNULL(BILLDTL.IQTY,0)< 0 ";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "bill");

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Coupon Discount").PadLeft(15) + " : " + (dtbill.Rows[0]["COUPONDISC"] + "".PadLeft(15));
                }

                //Write_In_Error_Log("3. coupon discount INFORMATION [ " + DateTime.Now.ToString() + " ]");
                /////////////////////////////////////////////////////////////////////////////////////

                str1 = "";
                tstr1 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(SETTLEMENT.delflg,0)=0";
                wstr_date1 = "SETTLEMENT.SETLEDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and SETTLEMENT.SETLEDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;
                wstr_Billdate = "BILL.BILLDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and BILL.BILLDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = " SELECT  " +
                            " sum(case when SETLETYPE='CASH' then SETLEAMOUNT else 0 end)  As CashAmt, " +
                            " sum(case when SETLETYPE='CHEQUE' then SETLEAMOUNT else 0 end)  As ChequeAmt, " +
                            " sum(case when SETLETYPE='CREDIT CARD' then SETLEAMOUNT else 0 end)  As CreditCardAmt, " +
                            " sum(case when SETLETYPE='OTHER' then SETLEAMOUNT else 0 end)  As OtherAmt, " +
                            " sum(case when SETLETYPE='COMPLEMENTARY' then SETLEAMOUNT else 0 end)  As ComplementryAmt, " +
                            " sum(case when SETLETYPE='CUSTOMER CREDIT' then SETLEAMOUNT else 0 end)  As CustCreditAmt," +
                            " sum(case when SETLETYPE='ROOM CREDIT' then SETLEAMOUNT else 0 end)  As RoomCreditAmt," +
                            " sum(isnull(adjamt,0)) as AdjAmt " +
                            " FROM SETTLEMENT " +
                            " where " + wstr_delflg + " And " + wstr_date1;

                tstr1 = " SELECT SUM(CUSTCREDIT.PENDINGAMT) as CUSTCREDIT " +
                           " FROM (" +
                           " SELECT BILL.NETAMOUNT,SETTLEINFO.SETLEAMT,(ISNULL(BILL.NETAMOUNT,0) - ISNULL(SETTLEINFO.SETLEAMT,0)) AS PENDINGAMT" +
                           " FROM BILL" +
                           " LEFT JOIN (" +
                                       " SELECT SUM(ISNULL(SETLEAMOUNT,0) + ISNULL(ADJAMT,0)) AS SETLEAMT,SETTLEMENT.BILLRID" +
                                           " FROM SETTLEMENT " +
                                           " WHERE ISNULL(SETTLEMENT.DELFLG, 0) = 0 AND isnull(SETTLEMENT.BILLRID,0)>0 " +
                                           // " AND " + wstr_date1 +
                                           " GROUP BY SETTLEMENT.BILLRID" +
                                     " ) AS SETTLEINFO ON (SETTLEINFO.BILLRID = BILL.RID)  " +
                           " WHERE ISNULL(BILL.DELFLG,0)=0 AND " + wstr_Billdate +
                           " AND BILL.NETAMOUNT > SETTLEINFO.SETLEAMT" +
                           " )  AS CUSTCREDIT";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "SETTLEMENT");
                dtbill1 = mssql.FillDataTable(tstr1, "SETTLEMENT");

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("SETTLEMENT DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cash").PadLeft(15) + " : " + (dtbill.Rows[0]["CashAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cheque").PadLeft(15) + " : " + (dtbill.Rows[0]["ChequeAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Credit Card").PadLeft(15) + " : " + (dtbill.Rows[0]["CreditCardAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Other").PadLeft(15) + " : " + (dtbill.Rows[0]["OtherAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Complementy").PadLeft(15) + " : " + (dtbill.Rows[0]["ComplementryAmt"] + "".PadLeft(15));
                    if (dtbill1.Rows.Count > 0)
                    {
                        PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cust. Credit").PadLeft(15) + " : " + (dtbill1.Rows[0]["CUSTCREDIT"] + "".PadLeft(15));
                    }
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Room Credit").PadLeft(15) + " : " + (dtbill.Rows[0]["RoomCreditAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Adjust Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["AdjAmt"] + "".PadLeft(15));
                }

                //Write_In_Error_Log("4. settlement INFORMATION [ " + DateTime.Now.ToString() + " ]");

                ///// OTHER PAYMENT INFORMATION
                ///
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("OTHER PAYMENT DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                str1 = " SELECT SETTLEMENT.SETLETYPE,SETTLEMENT.OTHERPAYMENTBY, SUM(SETTLEMENT.SETLEAMOUNT) AS SETLEAMT " +
                        " FROM SETTLEMENT " +
                        " LEFT JOIN BILL ON (BILL.RID=SETTLEMENT.BILLRID) " +
                        " WHERE ISNULL(SETTLEMENT.DELFLG,0)=0 AND SETTLEMENT.SETLETYPE='OTHER'  " +
                        " AND  " + wstr_date1 +
                        " GROUP BY SETTLEMENT.SETLETYPE,SETTLEMENT.OTHERPAYMENTBY " +
                        " ORDER BY SETTLEMENT.OTHERPAYMENTBY ";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "SETTLEMENT");

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row in dtbill.Rows)
                    {
                        string otherpay1 = "";
                        otherpay1 = (row["OTHERPAYMENTBY"] + "");
                        if ((row["OTHERPAYMENTBY"] + "").Length > 15)
                        {
                            otherpay1 = (row["OTHERPAYMENTBY"] + "").Substring(0, 14);
                        }
                        PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + (otherpay1.PadRight(15, ' ')) + " : " + (row["SETLEAMT"] + "".PadLeft(15));
                    }
                }

                //Write_In_Error_Log("5. other INFORMATION [ " + DateTime.Now.ToString() + " ]");
                ////////
                // INCOME INFORMATION

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("INCOME ENTRY DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                str1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date1 = "INDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and INDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "Select " +
                        "  SUM(INAMOUNT) AS INAMOUNT " +
                        " FROM INCOME " +
                        " where " + wstr_delflg + " AND " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "INCOME");

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Income").PadLeft(15) + " : " + (dtbill.Rows[0]["INAMOUNT"] + "".PadLeft(15));
                }

                /////////////////////////////////////////////////////////////////////////////////////////////////////
                // EXPENCE INFORMATION

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("EXPENCE ENTRY DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                str1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date1 = "EXDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and EXDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "Select " +
                        " SUM(EXAMOUNT) AS EXAMOUNT " +
                        " FROM EXPENCE " +
                        " WHERE " + wstr_delflg + " AND " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "EXPENCE");

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Expence").PadLeft(15) + " : " + (dtbill.Rows[0]["EXAMOUNT"] + "".PadLeft(15));
                }

                //Write_In_Error_Log("7. expence INFORMATION [ " + DateTime.Now.ToString() + " ]");
                ////////
                // CASH ON HAND ENTERY INFORMATION

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("CASH ON HAND ENTRY DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                str1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date1 = "CASHDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and CASHDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "Select " +
                        "  SUM(CASE WHEN CASHSTATUS = 'PLUS' THEN CASHAMT ELSE 0 END) AS 'PLUSAMT' ," +
                        " SUM(CASE WHEN CASHSTATUS = 'MINUS' THEN CASHAMT ELSE 0 END) AS 'MINUSAMT'," +
                        " SUM(CASHAMT) AS NETAMT from CASHONHAND " +
                        " where " + wstr_delflg + " And " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "CASHONHAND");

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Plus Cash").PadLeft(15) + " : " + (dtbill.Rows[0]["PLUSAMT"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Minus Cash").PadLeft(15) + " : " + (dtbill.Rows[0]["MINUSAMT"] + "".PadLeft(15));
                }
                //Write_In_Error_Log("7. cash on hand  INFORMATION [ " + DateTime.Now.ToString() + " ]");

                ///////////////////////////////////////////////////////////
                // TIEUP COMPANY INFORMATION

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("TIEUP COMPANY WISE DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                str1 = "";
                wstr_delflg = "isnull(BILL.delflg,0)=0 ";
                wstr_rev_bill = "isnull(BILL.ISREVISEDBILL,0)=0";
                wstr_date1 = "BILL.BILLDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and BILL.BILLDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = " SELECT MSTTIEUPCOMPANY.RID,MSTTIEUPCOMPANY.COMPNAME,SUM(BILL.NETAMOUNT) AS NETAMOUNT " +
                        " FROM BILL" +
                        " LEFT JOIN MSTTIEUPCOMPANY ON (MSTTIEUPCOMPANY.RID=BILL.MSTTIEUPCOMPRID) " +
                        " WHERE isnull(BILL.MSTTIEUPCOMPRID,0)>0 AND " + wstr_delflg + " AND " + wstr_rev_bill + " AND " + wstr_date1 +
                        " GROUP BY MSTTIEUPCOMPANY.RID,MSTTIEUPCOMPANY.COMPNAME ";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "BILL");

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row in dtbill.Rows)
                    {
                        string compnm1 = "";
                        compnm1 = (row["COMPNAME"] + "");
                        if ((row["COMPNAME"] + "").Length > 15)
                        {
                            compnm1 = (row["COMPNAME"] + "").Substring(0, 14);
                        }

                        PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + (compnm1.PadRight(15, ' ')) + " : " + (row["NETAMOUNT"] + "".PadLeft(15));
                    }
                }
                //Write_In_Error_Log("8. tieup  INFORMATION [ " + DateTime.Now.ToString() + " ]");
                ////////////////////////////////////////////
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + " E-Mail Generate At : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, ' ');

                return PrintStr1;

            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message.ToString() + " Error occures in Generate_BusinessSummary())", clspublicvariable.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";
            }
        }

        private String Generate_CashSummary(DateTime Fromdate, DateTime Todate)
        {
            string PrintStr1 = "";
            string str1 = "";
            string tstr1 = "";
            string wstr_date1 = "";
            string wstr_Billdate = "";
            string wstr_delflg = "";
            string wstr_rev_bill = "";
            string wstr_date_exp1 = "";
            DataTable dtbill = new DataTable();
            DataTable dtbill1 = new DataTable();
            DataTable dtbillEXP = new DataTable();

            try
            {

                //Write_In_Error_Log("In Generate_BusinessSummary [ " + DateTime.Now.ToString() + " ]");
                PrintStr1 = "";

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("CASH BUSINESS SUMMARY REPORT : " + clsPublicVariables.SMSRESTNM, PMAXCHAR + 50);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("FROM:" + Fromdate.ToShortDateString() + " " + "TO:" + Todate.ToShortDateString(), PMAXCHAR + 50);

                ////////////////////////////////////////////////////////////////////////////////////////////

                str1 = "";
                tstr1 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(SETTLEMENT.delflg,0)=0";
                wstr_date1 = "SETTLEMENT.SETLEDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and SETTLEMENT.SETLEDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;
                wstr_Billdate = "BILL.BILLDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and BILL.BILLDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = " SELECT  " +
                            " sum(case when SETLETYPE='CASH' then SETLEAMOUNT else 0 end)  As CashAmt, " +
                            " sum(case when SETLETYPE='CHEQUE' then SETLEAMOUNT else 0 end)  As ChequeAmt, " +
                            " sum(case when SETLETYPE='CREDIT CARD' then SETLEAMOUNT else 0 end)  As CreditCardAmt, " +
                            " sum(case when SETLETYPE='OTHER' then SETLEAMOUNT else 0 end)  As OtherAmt, " +
                            " sum(case when SETLETYPE='COMPLEMENTARY' then SETLEAMOUNT else 0 end)  As ComplementryAmt, " +
                            " sum(case when SETLETYPE='CUSTOMER CREDIT' then SETLEAMOUNT else 0 end)  As CustCreditAmt," +
                            " sum(case when SETLETYPE='ROOM CREDIT' then SETLEAMOUNT else 0 end)  As RoomCreditAmt," +
                            " sum(isnull(adjamt,0)) as AdjAmt " +
                            " FROM SETTLEMENT " +
                            " where " + wstr_delflg + " And " + wstr_date1;

                tstr1 = " SELECT SUM(CUSTCREDIT.PENDINGAMT) as CUSTCREDIT " +
                           " FROM (" +
                           " SELECT BILL.NETAMOUNT,SETTLEINFO.SETLEAMT,(ISNULL(BILL.NETAMOUNT,0) - ISNULL(SETTLEINFO.SETLEAMT,0)) AS PENDINGAMT" +
                           " FROM BILL" +
                           " LEFT JOIN (" +
                                       " SELECT SUM(ISNULL(SETLEAMOUNT,0) + ISNULL(ADJAMT,0)) AS SETLEAMT,SETTLEMENT.BILLRID" +
                                           " FROM SETTLEMENT " +
                                           " WHERE ISNULL(SETTLEMENT.DELFLG, 0) = 0 AND isnull(SETTLEMENT.BILLRID,0)>0 " +
                                           " GROUP BY SETTLEMENT.BILLRID" +
                                     " ) AS SETTLEINFO ON (SETTLEINFO.BILLRID = BILL.RID)  " +
                           " WHERE ISNULL(BILL.DELFLG,0)=0 AND " + wstr_Billdate +
                           " AND BILL.NETAMOUNT > SETTLEINFO.SETLEAMT" +
                           " )  AS CUSTCREDIT";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "SETTLEMENT");
                dtbill1 = mssql.FillDataTable(tstr1, "SETTLEMENT");

                /////////////////////////////////////////////////////

                str1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date_exp1 = "EXDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and EXDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = "Select " +
                        " SUM(EXAMOUNT) AS EXAMOUNT " +
                        " FROM EXPENCE " +
                        " WHERE " + wstr_delflg + " AND " + wstr_date_exp1;

                mssql.OpenMsSqlConnection();
                dtbillEXP = mssql.FillDataTable(str1, "EXPENCE");

                Decimal cashamt1 = 0;
                Decimal expamt1 = 0;
                Decimal netcash1 = 0;

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                if (dtbill.Rows.Count > 0)
                {
                    Decimal.TryParse(dtbill.Rows[0]["CashAmt"] + "", out cashamt1);
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("CASH").PadLeft(15) + " : " + (dtbill.Rows[0]["CashAmt"] + "".PadLeft(15));
                }

                if (dtbillEXP.Rows.Count > 0)
                {
                    Decimal.TryParse(dtbillEXP.Rows[0]["EXAMOUNT"] + "", out expamt1);
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("EXPENCE").PadLeft(15) + " : " + (dtbillEXP.Rows[0]["EXAMOUNT"] + "".PadLeft(15));
                }

                netcash1 = cashamt1 - expamt1;
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("NET CASH").PadLeft(15) + " : " + netcash1 + "".PadLeft(15);

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("SETTLEMENT DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

                if (dtbill.Rows.Count > 0)
                {
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cash").PadLeft(15) + " : " + (dtbill.Rows[0]["CashAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cheque").PadLeft(15) + " : " + (dtbill.Rows[0]["ChequeAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Credit Card").PadLeft(15) + " : " + (dtbill.Rows[0]["CreditCardAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Other").PadLeft(15) + " : " + (dtbill.Rows[0]["OtherAmt"] + "".PadLeft(15));
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Complementy").PadLeft(15) + " : " + (dtbill.Rows[0]["ComplementryAmt"] + "".PadLeft(15));
                    if (dtbill1.Rows.Count > 0)
                    {
                        PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cust. Credit").PadLeft(15) + " : " + (dtbill1.Rows[0]["CUSTCREDIT"] + "".PadLeft(15));
                    }
                    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Adjust Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["AdjAmt"] + "".PadLeft(15));
                }

                ///// OTHER PAYMENT INFORMATION
                ///
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("OTHER PAYMENT DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR + 100, '-');

                str1 = " SELECT SETTLEMENT.SETLETYPE,SETTLEMENT.OTHERPAYMENTBY, SUM(SETTLEMENT.SETLEAMOUNT) AS SETLEAMT " +
                        " FROM SETTLEMENT " +
                        " LEFT JOIN BILL ON (BILL.RID=SETTLEMENT.BILLRID) " +
                        " WHERE ISNULL(SETTLEMENT.DELFLG,0)=0 AND SETTLEMENT.SETLETYPE='OTHER'  " +
                        " AND  " + wstr_date1 +
                        " GROUP BY SETTLEMENT.SETLETYPE,SETTLEMENT.OTHERPAYMENTBY " +
                        " ORDER BY SETTLEMENT.OTHERPAYMENTBY ";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "SETTLEMENT");

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row in dtbill.Rows)
                    {
                        string otherpay1 = "";
                        otherpay1 = (row["OTHERPAYMENTBY"] + "");
                        if ((row["OTHERPAYMENTBY"] + "").Length > 15)
                        {
                            otherpay1 = (row["OTHERPAYMENTBY"] + "").Substring(0, 14);
                        }
                        PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + (otherpay1.PadRight(15, ' ')) + " : " + (row["SETLEAMT"] + "".PadLeft(15));
                    }
                }

                ///////////////////////////////////////////////////////////
                // TIEUP COMPANY INFORMATION

                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR + 100, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("TIEUP COMPANY WISE DETAILS", PMAXCHAR);
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR + 100, '-');

                str1 = "";
                wstr_delflg = "isnull(BILL.delflg,0)=0 ";
                wstr_rev_bill = "isnull(BILL.ISREVISEDBILL,0)=0";
                wstr_date1 = "BILL.BILLDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and BILL.BILLDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                str1 = " SELECT MSTTIEUPCOMPANY.RID,MSTTIEUPCOMPANY.COMPNAME,SUM(BILL.NETAMOUNT) AS NETAMOUNT " +
                        " FROM BILL" +
                        " LEFT JOIN MSTTIEUPCOMPANY ON (MSTTIEUPCOMPANY.RID=BILL.MSTTIEUPCOMPRID) " +
                        " WHERE isnull(BILL.MSTTIEUPCOMPRID,0)>0 AND " + wstr_delflg + " AND " + wstr_rev_bill + " AND " + wstr_date1 +
                        " GROUP BY MSTTIEUPCOMPANY.RID,MSTTIEUPCOMPANY.COMPNAME ";

                mssql.OpenMsSqlConnection();
                dtbill = mssql.FillDataTable(str1, "BILL");

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row in dtbill.Rows)
                    {
                        string compnm1 = "";
                        compnm1 = (row["COMPNAME"] + "");
                        if ((row["COMPNAME"] + "").Length > 15)
                        {
                            compnm1 = (row["COMPNAME"] + "").Substring(0, 14);
                        }

                        PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + (compnm1.PadRight(15, ' ')) + " : " + (row["NETAMOUNT"] + "".PadLeft(15));
                    }
                }

                ////////////////////////////////////////////
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + " E-Mail Generate At : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, ' ');

                return PrintStr1;

            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message.ToString() + " Error occures in Generate_BusinessSummary())", clspublicvariable.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";
            }
        }

        //private String Generate_BusinessSummary_Html(DateTime Fromdate, DateTime Todate)
        //{
        //    string PrintStr1 = "";
        //    string str1 = "";
        //    string tstr1 = "";
        //    string wstr_date1 = "";
        //    string wstr_Billdate = "";
        //    string wstr_delflg = "";
        //    string wstr_rev_bill = "";
        //    DataTable dtbill = new DataTable();
        //    DataTable dtbill1 = new DataTable();

        //    try
        //    {
        //        PrintStr1 = "";

        //        PrintStr1 = "<h3>" + "BUSINESS SUMMARY REPORT : " + clsPublicVariables.SMSRESTNM + "</h3>" +
        //                    " <h4>" + "FROM:" + Fromdate.ToShortDateString() + " " + "TO:" + Todate.ToShortDateString() + "</h4>";

        //        ////////////////////////////////////////////////////////////////////////////////////////////
        //        // BILL BREAKUP INFORMATION

        //        str1 = "";
        //        wstr_date1 = "";
        //        wstr_delflg = "isnull(delflg,0)=0";
        //        wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
        //        wstr_date1 = "billdate >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and billdate <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

        //        str1 = "SELECT " +
        //                 " count(rid)  As Totbill " +
        //                 " From Bill" +
        //                 " where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;

        //        mssql.OpenMsSqlConnection();
        //        dtbill = mssql.FillDataTable(str1, "BILL");

        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("BILL BREAKUP INFORMATION", PMAXCHAR);
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

        //        PrintStr1 = PrintStr1 + "<h4>" + "BILL BREAKUP INFORMATION : " + clsPublicVariables.SMSRESTNM + "</h4>";

        //        if (dtbill.Rows.Count > 0)
        //        {
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Total Bill Count").PadLeft(15) + " : " + (dtbill.Rows[0]["Totbill"] + "".PadLeft(15));
        //            PrintStr1 = PrintStr1 + "<h5>" + "TOTAL BILL GENERATE : " + (dtbill.Rows[0]["Totbill"] + "") + "</h5>";
        //        }

        //        ////////////////////////
        //        // Billing Details
        //        str1 = "";
        //        wstr_date1 = "";
        //        wstr_delflg = "isnull(delflg,0)=0";
        //        wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
        //        wstr_date1 = "billdate >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and billdate <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

        //        str1 = "Select " +
        //                  " sum(Totamount)  As Totamount, " +
        //                  " sum(TOTADDVATAMOUNT) As TOTADDVATAMOUNT, " +
        //                  " sum(TOTDISCAMOUNT) As TOTDISCAMOUNT," +
        //                  " sum(TOTADDDISCAMT) As TOTADDDISCAMT," +
        //                  " sum(TOTSERCHRAMT) As TOTSERCHRAMT, " +
        //                  " sum(TOTVATAMOUNT) As TOTVATAMOUNT," +
        //                  " sum(TOTBEVVATAMT) As TOTBEVVATAMT," +
        //                  " sum(TOTLIQVATAMT) As TOTLIQVATAMT," +
        //                  " sum(TOTGSTAMT) As TOTGSTAMT," +
        //                  " sum(CGSTAMT) As TOTCGSTAMT," +
        //                  " sum(SGSTAMT) As TOTSGSTAMT," +
        //                  " sum(IGSTAMT) As TOTIGSTAMT," +
        //                  " sum(TOTROFF)  As TOTROFF, " +
        //                  " sum(NETAMOUNT)  As NETAMOUNT " +
        //                  " From Bill" +
        //                  " where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;

        //        mssql.OpenMsSqlConnection();
        //        dtbill = mssql.FillDataTable(str1, "bill");

        //        PrintStr1 = PrintStr1 + "<h4>" + "BILLING DETAILS INFORMATION : " + clsPublicVariables.SMSRESTNM + "</h4>";

        //        if (dtbill.Rows.Count > 0)
        //        {

        //            PrintStr1 = PrintStr1 + "<h5>" + "TOTAL AMOUNT : " + (dtbill.Rows[0]["Totamount"] + "") + "</h5>" +
        //                        "<h5>" + "DISCOUNT : " + (dtbill.Rows[0]["TOTDISCAMOUNT"] + "") + "</h5>" +
        //                        "<h5>" + "SER.CHR : " + (dtbill.Rows[0]["TOTSERCHRAMT"] + "") + "</h5>" +
        //                        "<h5>" + "SER.CHR : " + (dtbill.Rows[0]["TOTSERCHRAMT"] + "") + "</h5>" +
        //                        "<h5>" + "CGST : " + (dtbill.Rows[0]["TOTCGSTAMT"] + "") + "</h5>" +
        //                        "<h5>" + "SGST : " + (dtbill.Rows[0]["TOTSGSTAMT"] + "") + "</h5>" +
        //                        "<h5>" + "IGST : " + (dtbill.Rows[0]["TOTIGSTAMT"] + "") + "</h5>" +
        //                        "<h5>" + "TOTAL GST : " + (dtbill.Rows[0]["TOTGSTAMT"] + "") + "</h5>" +
        //                        "<h5>" + "R.OFF : " + (dtbill.Rows[0]["TOTROFF"] + "") + "</h5>" +
        //                        "<h5>" + "ADD.DISC : " + (dtbill.Rows[0]["TOTADDDISCAMT"] + "") + "</h5>" +
        //                        "<h4>" + "NET AMOUNT : " + (dtbill.Rows[0]["NETAMOUNT"] + "") + "</h4>";

        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Total Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["Totamount"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Discount").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTDISCAMOUNT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Ser.Chr").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTSERCHRAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Food VAT").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTVATAMOUNT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Bev.VAT").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTBEVVATAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Liq.VAT").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTLIQVATAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("CGST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTCGSTAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("SGST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTSGSTAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("IGST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTIGSTAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Total GST").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTGSTAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Round Off").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTROFF"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Add.Disc").PadLeft(15) + " : " + (dtbill.Rows[0]["TOTADDDISCAMT"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Net Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["NETAMOUNT"] + "".PadLeft(15));
        //        }

        //        /////////////////////////////////////////////////////////////////////////////////////

        //        str1 = "";
        //        tstr1 = "";
        //        wstr_date1 = "";
        //        wstr_delflg = "isnull(SETTLEMENT.delflg,0)=0";
        //        wstr_date1 = "SETTLEMENT.SETLEDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and SETTLEMENT.SETLEDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;
        //        wstr_Billdate = "BILL.BILLDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and BILL.BILLDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

        //        str1 = " SELECT  " +
        //                    " sum(case when SETLETYPE='CASH' then SETLEAMOUNT else 0 end)  As CashAmt, " +
        //                    " sum(case when SETLETYPE='CHEQUE' then SETLEAMOUNT else 0 end)  As ChequeAmt, " +
        //                    " sum(case when SETLETYPE='CREDIT CARD' then SETLEAMOUNT else 0 end)  As CreditCardAmt, " +
        //                    " sum(case when SETLETYPE='OTHER' then SETLEAMOUNT else 0 end)  As OtherAmt, " +
        //                    " sum(case when SETLETYPE='COMPLEMENTARY' then SETLEAMOUNT else 0 end)  As ComplementryAmt, " +
        //                    " sum(case when SETLETYPE='CUSTOMER CREDIT' then SETLEAMOUNT else 0 end)  As CustCreditAmt," +
        //                    " sum(case when SETLETYPE='ROOM CREDIT' then SETLEAMOUNT else 0 end)  As RoomCreditAmt," +
        //                    " sum(isnull(adjamt,0)) as AdjAmt " +
        //                    " FROM SETTLEMENT " +
        //                    " where " + wstr_delflg + " And " + wstr_date1;

        //        tstr1 = " SELECT SUM(CUSTCREDIT.PENDINGAMT) as CUSTCREDIT " +
        //                   " FROM (" +
        //                   " SELECT BILL.NETAMOUNT,SETTLEINFO.SETLEAMT,(ISNULL(BILL.NETAMOUNT,0) - ISNULL(SETTLEINFO.SETLEAMT,0)) AS PENDINGAMT" +
        //                   " FROM BILL" +
        //                   " LEFT JOIN (" +
        //                               " SELECT SUM(ISNULL(SETLEAMOUNT,0) + ISNULL(ADJAMT,0)) AS SETLEAMT,SETTLEMENT.BILLRID" +
        //                                   " FROM SETTLEMENT " +
        //                                   " WHERE ISNULL(SETTLEMENT.DELFLG, 0) = 0 AND isnull(SETTLEMENT.BILLRID,0)>0 " +
        //                                   " GROUP BY SETTLEMENT.BILLRID" +
        //                             " ) AS SETTLEINFO ON (SETTLEINFO.BILLRID = BILL.RID)  " +
        //                   " WHERE ISNULL(BILL.DELFLG,0)=0 AND " + wstr_Billdate +
        //                   " AND BILL.NETAMOUNT > SETTLEINFO.SETLEAMT" +
        //                   " )  AS CUSTCREDIT";

        //        mssql.OpenMsSqlConnection();
        //        dtbill = mssql.FillDataTable(str1, "SETTLEMENT");
        //        dtbill1 = mssql.FillDataTable(tstr1, "SETTLEMENT");

        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine);
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + PadCenter("SETTLEMENT DETAILS", PMAXCHAR);
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, '-');

        //        if (dtbill.Rows.Count > 0)
        //        {
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cash").PadLeft(15) + " : " + (dtbill.Rows[0]["CashAmt"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cheque").PadLeft(15) + " : " + (dtbill.Rows[0]["ChequeAmt"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Credit Card").PadLeft(15) + " : " + (dtbill.Rows[0]["CreditCardAmt"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Other").PadLeft(15) + " : " + (dtbill.Rows[0]["OtherAmt"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Complementy").PadLeft(15) + " : " + (dtbill.Rows[0]["ComplementryAmt"] + "".PadLeft(15));
        //            //if (dtbill1.Rows.Count > 0)
        //            //{
        //            //    PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Cust. Credit").PadLeft(15) + " : " + (dtbill1.Rows[0]["CUSTCREDIT"] + "".PadLeft(15));
        //            //}
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Room Credit").PadLeft(15) + " : " + (dtbill.Rows[0]["RoomCreditAmt"] + "".PadLeft(15));
        //            //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + ("Adjust Amount").PadLeft(15) + " : " + (dtbill.Rows[0]["AdjAmt"] + "".PadLeft(15));

        //            PrintStr1 = PrintStr1 + "<h5>" + "CASH : " + (dtbill.Rows[0]["CashAmt"] + "") + "</h5>" +
        //                                    "<h5>" + "CHEQUE : " + (dtbill.Rows[0]["ChequeAmt"] + "") + "</h5>" +
        //                                    "<h5>" + "CREDIT CARD : " + (dtbill.Rows[0]["CreditCardAmt"] + "") + "</h5>" +
        //                                    "<h5>" + "OTHER : " + (dtbill.Rows[0]["OtherAmt"] + "") + "</h5>" +
        //                                    "<h5>" + "COMPLEMENTRY : " + (dtbill.Rows[0]["ComplementryAmt"] + "") + "</h5>";
        //            if (dtbill1.Rows.Count > 0)
        //            {
        //                PrintStr1 = PrintStr1 + "<h5>" + "CUST.CREDIT : " + (dtbill1.Rows[0]["CUSTCREDIT"] + "") + "</h5>";
        //            }

        //            PrintStr1 = PrintStr1 + "<h5>" + "ROOM CREDIT : " + (dtbill.Rows[0]["RoomCreditAmt"] + "") + "</h5>" +
        //                                    "<h5>" + "ADJ.AMOUNT : " + (dtbill.Rows[0]["AdjAmt"] + "") + "</h5>";

        //        }
        //        ////////////////////////////////////////////
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, ' ');
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + " E-Mail Generate At : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        //        //PrintStr1 = (PrintStr1 == "" ? "" : PrintStr1 + Environment.NewLine) + "".PadRight(PMAXCHAR, ' ');

        //        PrintStr1 = PrintStr1 + "<h2>" + "" + "</h2>";
        //        PrintStr1 = PrintStr1 + "<h2>" + "" + "</h2>";
        //        PrintStr1 = PrintStr1 + "<h6>" + " E-Mail Generate At :" + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</h6>";
        //        return PrintStr1;

        //    }
        //    catch (Exception)
        //    {
        //        //MessageBox.Show(ex.Message.ToString() + " Error occures in Generate_BusinessSummary())", clspublicvariable.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        return "";
        //    }
        //}

        private String Generate_PurchaseStockRegister(DateTime Fromdate, DateTime Todate)
        {
            string textBody = "";
            DataTable dtrpt = new DataTable();
            try
            {
                textBody = "";

                textBody = " <h3>" + "PURCHASE STOCK REGISTER : " + clsPublicVariables.SMSRESTNM + "</h3>" +
                         " <h4>" + "FROM:" + Fromdate.ToShortDateString() + " " + "TO:" + Todate.ToShortDateString() + "</h4>";

                ////////////////////////////////////////////////////////////////////////////////////////////

                mssql.OpenMsSqlConnection();

                SqlCommand mscmd = new SqlCommand("SP_PURCHASESTOCKREG", clsMsSqlDbFunction.mssqlcon);
                mscmd.Parameters.Add("@p_fromdate", SqlDbType.DateTime);
                mscmd.Parameters.Add("@p_todate", SqlDbType.DateTime);

                mscmd.Parameters["@p_fromdate"].Value = Fromdate;
                mscmd.Parameters["@p_todate"].Value = Todate;

                mscmd.CommandType = CommandType.StoredProcedure;

                dtrpt.Load(mscmd.ExecuteReader());

                if (dtrpt.Rows.Count <= 0)
                {
                    return textBody;
                }



                textBody = textBody + " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 400 + "><tr bgcolor='#4da6ff'> " +
                    " <td><b>ITEM NAME</b></td>" +
                                   " <td><b>UNIT</b></td>" +
                                   " <td><b>OP.ISSUE</b></td>" +
                                   " <td><b>OP.STOCK</b></td>" +
                                   " <td><b>PURCHASE</b></td>" +
                                   " <td><b>ISSUE</b></td>" +
                                   " <td><b>WASTAGE</b></td>" +
                                   " <td><b>USAGE</b></td>" +
                                   " <td><b>STOCK</b></td>" +
                                   " <td><b>CL.STOCK</b></td>" +
                                   " <td><b>CL.ISSUE</b></td>" +
                                   "</tr>";
                for (int loopCount = 0; loopCount < dtrpt.Rows.Count; loopCount++)
                {
                    textBody += "<tr><td>" + (dtrpt.Rows[loopCount]["PURINAME"] + "") + "</td> " +
                                    "<td> " + (dtrpt.Rows[loopCount]["PURIUNIT"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["OPISSQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["OPSTOCKQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["PURQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["ISSQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["WASTAGEQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["USAGEQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["STOCKQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["CLSTOCKQTY"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["CLISSUEQTY"] + "") + "</td>" +
                                    "</tr>";
                }
                textBody += "</table>";

                return textBody;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string Generate_BillEditDeleteSummaryEmail(DateTime Fromdate, DateTime Todate)
        {
            string qry = "";
            string wstr_Billdate = "";
            DataTable dtrpt = new DataTable();
            string textBody = "";
            try
            {
                wstr_Billdate = "BILL.BILLDATE >=" + clsPublicVariables.DateCriteriaconst + Fromdate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst + " and BILL.BILLDATE <= " + clsPublicVariables.DateCriteriaconst + Todate.ToString("dd-MMM-yyyy") + clsPublicVariables.DateCriteriaconst;

                qry = "SELECT BILL.RID,BILL.BILLNO,BILL.REFBILLNO,BILL.BILLDATE,BILLTIME,BILL.CUSTNAME, " +
                            " MSTTABLE.TABLENAME,BILL.NETAMOUNT,BILL.BILLREMARK," +
                            " (CASE WHEN ISNULL(BILL.DELFLG,0)= 1 THEN 'DELETED' ELSE 'MODIFIED' END) AS STATUS, " +
                            " BILL.BILLINFO," +
                            " BILL.BILLPREPBY,ADDUSER.USERNAME" +
                            " FROM BILL" +
                            " LEFT JOIN MSTTABLE ON (MSTTABLE.RID=BILL.TABLERID)" +
                            " LEFT JOIN MSTUSERS ADDUSER ON (ADDUSER.RID=BILL.AUSERID)" +
                            " WHERE " +
                            " (RTRIM(LTRIM(BILL.BILLINFO))!='' OR ISNULL(BILL.DELFLG,0)=1) " +
                            " AND " + wstr_Billdate;

                mssql.OpenMsSqlConnection();
                dtrpt = mssql.FillDataTable(qry, "BILL");

                if (dtrpt.Rows.Count <= 0)
                {
                    return textBody;
                }

                textBody = textBody + " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 400 + "><tr bgcolor='#4da6ff'> " +
                                   " <td><b>BILLNO</b></td>" +
                                   " <td><b>BILL DATE</b></td>" +
                                   " <td><b>CUSTOMER</b></td>" +
                                   " <td><b>STATUS</b></td>" +
                                   " <td><b>INFORMATION</b></td>" +
                                   " <td><b>NETAMOUNT</b></td>" +
                                   "</tr>";

                for (int loopCount = 0; loopCount < dtrpt.Rows.Count; loopCount++)
                {
                    textBody += "<tr><td>" + (dtrpt.Rows[loopCount]["BILLNO"] + "") + "</td> " +
                                    "<td> " + (dtrpt.Rows[loopCount]["BILLTIME"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["CUSTNAME"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["STATUS"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["BILLINFO"] + "") + "</td>" +
                                    "<td> " + (dtrpt.Rows[loopCount]["NETAMOUNT"] + "") + "</td>" +
                                    "</tr>";
                }
                textBody += "</table>";

                return textBody;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string Generate_ItemWiseSalesRegister(DateTime Fromdate, DateTime Todate)
        {
            DataTable dtitemsales = new DataTable();
            SqlDataAdapter da2 = new SqlDataAdapter();

            string textBody = "";
            try
            {
                mssql.OpenMsSqlConnection();

                SqlCommand mscmd = new SqlCommand("SP_ITEMSALES", clsMsSqlDbFunction.mssqlcon);
                mscmd.Parameters.Add("@p_fromdate", SqlDbType.DateTime);
                mscmd.Parameters.Add("@p_todate", SqlDbType.DateTime);

                mscmd.Parameters["@p_fromdate"].Value = Fromdate;
                mscmd.Parameters["@p_todate"].Value = Todate;

                mscmd.CommandType = CommandType.StoredProcedure;

                dtitemsales.Load(mscmd.ExecuteReader());

                if (dtitemsales.Rows.Count <= 0)
                {
                    return "";
                }

                textBody = "";

                textBody = textBody + " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 400 + "><tr bgcolor='#4da6ff'> " +
                                   " <td><b>ITEM NAME</b></td>" +
                                   " <td><b>QTY</b></td>" +
                                   " <td><b>AMOUNT</b></td>" +
                                   "</tr>";

                for (int loopCount = 0; loopCount < dtitemsales.Rows.Count; loopCount++)
                {
                    textBody += "<tr><td>" + (dtitemsales.Rows[loopCount]["Iname"] + "") + "</td> " +
                                    "<td> " + (dtitemsales.Rows[loopCount]["IQTY"] + "") + "</td>" +
                                    "<td> " + (dtitemsales.Rows[loopCount]["IAMT"] + "") + "</td>" +
                                    "</tr>";
                }
                textBody += "</table>";

                return textBody;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in Generate_ItemWiseSalesRegister())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";
            }
        }

        static string PadCenter(string text, int newWidth)
        {
            const char filler = ' ';
            int length = text.Length;
            int charactersToPad = newWidth - length;
            if (charactersToPad < 0) throw new ArgumentException("New width must be greater than string length.", "newWidth");
            int padLeft = charactersToPad / 2 + charactersToPad % 2;
            //add a space to the left if the string is an odd number
            int padRight = charactersToPad / 2;

            StringBuilder resultBuilder = new StringBuilder(newWidth);
            for (int i = 0; i < padLeft; i++) resultBuilder.Insert(i, filler);
            for (int i = 0; i < length; i++) resultBuilder.Insert(i + padLeft, text[i]);
            for (int i = newWidth - padRight; i < newWidth; i++) resultBuilder.Insert(i, filler);
            return resultBuilder.ToString();
        }

        private void tmrread_Tick(object sender, EventArgs e)
        {
            this.Reading_EMAIL();
        }

        private bool Reading_EMAIL()
        {
            string emailadd = "";
            string pass = "";
            string popadd = "";
            string port = "";

            try
            {
                if (clsPublicVariables.GENENABLEZOMATO == "True")
                {
                    if (clsPublicVariables.GENZOMATOSERVICETYPE != "")
                    {
                        string zomatosertyp1 = "";
                        zomatosertyp1 = clsPublicVariables.GENZOMATOSERVICETYPE;

                        if (zomatosertyp1 == "SERVICETYPE-1")
                        {
                            emailadd = clsPublicVariables.EMAILADDRECTYPE1;
                            pass = clsPublicVariables.EMAILPASSRECTYPE1;
                            popadd = clsPublicVariables.EMAILPOPADDTRECTYPE1;
                            port = clsPublicVariables.EMAILPORTRECTYPE1;

                            this.Read_Zomato_Email(emailadd, pass, popadd, port);

                        }
                        else if (zomatosertyp1 == "SERVICETYPE-2")
                        {
                            emailadd = clsPublicVariables.EMAILADDRECTYPE2;
                            pass = clsPublicVariables.EMAILPASSRECTYPE2;
                            popadd = clsPublicVariables.EMAILPOPADDTRECTYPE2;
                            port = clsPublicVariables.EMAILPORTRECTYPE2;

                            this.Read_Zomato_Email(emailadd, pass, popadd, port);

                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool Read_Zomato_Email(string emailadd1, string emailpass1, string popadd1, string portno1)
        {
            int totemailcount = 0;

            try
            {
                if (pop3Client.Connected)
                {
                    pop3Client.Disconnect();
                }

                pop3Client.Connect(popadd1, int.Parse(portno1), true);
                pop3Client.Authenticate(emailadd1, emailpass1, AuthenticationMethod.UsernameAndPassword);

                totemailcount = pop3Client.GetMessageCount();

                for (int i = totemailcount; i >= 1; i -= 1)
                {
                    // Check if the form is closed while we are working. If so, abort
                    if (IsDisposed)
                    {
                        return false;
                    }
                    // Refresh the form while fetching emails
                    // This will fix the "Application is not responding" problem
                    //Application.DoEvents();

                    try
                    {
                        string strfromadd, stremailbody = "";
                        //string emailbodyhtml = "";

                        Message message = pop3Client.GetMessage(i);
                        // Add the message to the dictionary from the messageNumber to the Message
                        // messages.Add(i, message);

                        strfromadd = message.Headers.From.Address.ToString();
                        MessagePart plainTextPart = message.FindFirstPlainTextVersion();

                        if (plainTextPart != null)
                        {
                            // The message had a text/plain version - show that one
                            stremailbody = plainTextPart.GetBodyAsText();
                        }
                        else
                        {
                            // Try to find a body to show in some of the other text versions
                            List<MessagePart> textVersions = message.FindAllTextVersions();
                            if (textVersions.Count >= 1)
                                stremailbody = textVersions[0].GetBodyAsText();
                            else
                                stremailbody = "<<OpenPop>> Cannot find a text version body in this message to show <<OpenPop>>";
                        }

                        if (strfromadd == clsPublicVariables.GENZOMATOEMAILADD)
                        {
                            string t_from_add1 = message.Headers.From.Address.ToString();
                            string t_sub = message.Headers.Subject.ToString();

                            if (t_sub.Contains(clsPublicVariables.ZOMATONEWORDERKEY))
                            {
                                this.Insert_Read_Email("ZOMATO", t_from_add1, t_sub, stremailbody);
                            }
                        }

                        // success++;
                    }
                    catch (Exception ex)
                    {
                        this.Write_In_Error_Log("Error Occured in Read E-Mail : " + ex.Message.ToString() + " @ " + System.DateTime.Now.ToString());
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                this.Write_In_Error_Log("Error Occured in Read E-Mail : " + ex.Message.ToString() + " @ " + System.DateTime.Now.ToString());
                return false;
            }
        }

        private bool Insert_Read_Email(string emailtype1, string fromadd1, string emailsub1, string emailbody1)
        {
            Int64 Rid1 = 0;
            string stroutput1 = "";
            try
            {
                clsemailreadbal Bal1 = new clsemailreadbal();

                // string[] lines = emailbody1.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                stroutput1 = StripHTML(emailbody1);
                string[] lines = StripHTML(emailbody1).Split("\r".ToCharArray());

                Bal1.Rid = 0;
                Bal1.Emailtype = emailtype1;
                Bal1.Fromadd = fromadd1;
                Bal1.Emailsub = emailsub1;
                Bal1.Emailbody = stroutput1;
                Bal1.Emailremark = "";
                Bal1.Isemailused = 0;
                Bal1.Refrid1 = 0;
                Bal1.Refrid2 = 0;
                Bal1.FormMode = 0;
                Bal1.LoginUserId = 1;
                Bal1.Orderdate = System.DateTime.Today.Date;
                Bal1.Orderid = "";
                Bal1.Custnm = "";
                Bal1.Custno = "";

                Rid1 = Bal1.Db_Operation_EMAILREAD(Bal1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string StripHTML(string source)
        {
            try
            {
                string result;

                // Remove HTML Development formatting
                // Replace line breaks with space
                // because browsers inserts space
                result = source.Replace("\r", " ");
                // Replace line breaks with space
                // because browsers inserts space
                result = result.Replace("\n", " ");
                // Remove step-formatting
                result = result.Replace("\t", string.Empty);
                // Remove repeating spaces because browsers ignore them
                result = System.Text.RegularExpressions.Regex.Replace(result,
                                                                      @"( )+", " ");

                // Remove the header (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*head([^>])*>", "<head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*head( )*>)", "</head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<head>).*(</head>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all scripts (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*script([^>])*>", "<script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*script( )*>)", "</script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                //result = System.Text.RegularExpressions.Regex.Replace(result,
                //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
                //         string.Empty,
                //         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<script>).*(</script>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all styles (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*style([^>])*>", "<style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*style( )*>)", "</style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<style>).*(</style>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert tabs in spaces of <td> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*td([^>])*>", "\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line breaks in places of <BR> and <LI> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*br( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*li( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line paragraphs (double line breaks) in place
                // if <P>, <DIV> and <TR> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*div([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*tr([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*p([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Remove remaining tags like <a>, links, images,
                // comments etc - anything that's enclosed inside < >
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<[^>]*>", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // replace special characters:
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @" ", " ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&bull;", " * ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lsaquo;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&rsaquo;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&trade;", "(tm)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&frasl;", "/",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lt;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&gt;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&copy;", "(c)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&reg;", "(r)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove all others. More can be added, see
                // http://hotwired.lycos.com/webmonkey/reference/special_characters/
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&(.{2,6});", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // for testing
                //System.Text.RegularExpressions.Regex.Replace(result,
                //       this.txtRegex.Text,string.Empty,
                //       System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // make line breaking consistent
                result = result.Replace("\n", "\r");

                // Remove extra line breaks and tabs:
                // replace over 2 breaks with 2 and over 4 tabs with 4.
                // Prepare first to remove any whitespaces in between
                // the escaped characters and remove redundant tabs in between line breaks
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\t)", "\t\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\r)", "\t\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\t)", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove redundant tabs
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove multiple tabs following a line break with just one tab
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Initial replacement target string for line breaks
                string breaks = "\r\r\r";
                // Initial replacement target string for tabs
                string tabs = "\t\t\t\t\t";
                for (int index = 0; index < result.Length; index++)
                {
                    result = result.Replace(breaks, "\r\r");
                    result = result.Replace(tabs, "\t\t\t\t");
                    breaks = breaks + "\r";
                    tabs = tabs + "\t";
                }

                // That's it.
                return result;
            }
            catch (Exception ex)
            {
                this.Write_In_Error_Log("Error Occured in StripHTML : " + ex.Message.ToString() + " @ " + System.DateTime.Now.ToString());
                //MessageBox.Show("Error");
                return source;
            }
        }
    }
}
