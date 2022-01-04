using System;
using System.IO;
using System.Net;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace EVENTSCHEDULER
{
    class clsGeneral
    {

        public void GetConnectionDetails()
        {
            try
            {
                clsPublicVariables.ServerName1 = System.Configuration.ConfigurationManager.AppSettings["SERVER"];
                clsPublicVariables.DatabaseName1 = System.Configuration.ConfigurationManager.AppSettings["DATABASE"];
                clsPublicVariables.UserName1 = System.Configuration.ConfigurationManager.AppSettings["DBUSERID"];
                clsPublicVariables.Password1 = System.Configuration.ConfigurationManager.AppSettings["DBPASSWORD"];
                clsPublicVariables.ActualFilePath = clsPublicVariables.AppPath + "\\EVENTSCHEDULER.exe";
                clsPublicVariables.EventVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(clsPublicVariables.ActualFilePath).FileVersion.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in GetConnectionDetails())");
            }
        }

        public double Null2Dbl(object Numbr1)
        {
            Double Amt1;
            try
            {

                if (DBNull.Value == Numbr1)
                {
                    Amt1 = 0;
                }

                else if ((string.IsNullOrEmpty(Numbr1.ToString()) || (Numbr1.ToString().Trim() == "")))
                {
                    Amt1 = 0;
                }
                else
                {
                    Amt1 = (double)Numbr1;
                }

                return (double)Amt1;
            }
            catch (Exception)
            {

                return 0;
            }
        }

        public long Null2lng(object Numbr1)
        {
            long Amt1;
            try
            {

                if (DBNull.Value == Numbr1)
                {
                    Amt1 = 0;
                }

                else if ((string.IsNullOrEmpty(Numbr1.ToString()) || (Numbr1.ToString().Trim() == "")))
                {
                    Amt1 = 0;
                }
                else
                {
                    Amt1 = Convert.ToInt64(Numbr1);
                }

                return Convert.ToInt64(Amt1);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public DateTime Null2Date(object Dt1)
        {
            DateTime tDt1;
            //DateTime eDt1;
            bool IsEmpDateYn;
            try
            {
                if (DBNull.Value == Dt1)
                {
                    IsEmpDateYn = true;
                }
                else if (Dt1.ToString() == "1/1/1753")
                {
                    IsEmpDateYn = true;
                }
                else if ((Dt1.ToString().Trim() == ""))
                {
                    IsEmpDateYn = true;
                }
                else
                {
                    IsEmpDateYn = false;
                }
                if ((IsEmpDateYn == true))
                {
                    tDt1 = DateTime.MinValue;
                }
                else
                {
                    tDt1 = ((DateTime)(Dt1));
                }

                return tDt1;
            }
            catch (Exception)
            {

                return (DateTime)Dt1;
            }
        }

        public string Null2Str(object Str1)
        {
            string tStr1;
            try
            {
                if (DBNull.Value == (Str1))
                {
                    tStr1 = " ";
                }
                else if (string.IsNullOrEmpty(Str1.ToString()))
                {
                    tStr1 = " ";
                }
                else
                {
                    tStr1 = Str1.ToString();
                }
                return tStr1;
            }
            catch (Exception)
            {
                return Str1.ToString();
            }
        }

        public string SEND_WEB_REQUEST(string url, string smsmethod1 = "0")
        {
            string result = "";
            string xml = "";
            var responseString = "";

            try
            {
                if (smsmethod1 == "1")
                {
                    responseString = this.SEND_WEB_REQUET_2(url);
                }
                else
                {
                    var request1 = (HttpWebRequest)WebRequest.Create(url);

                    var bytes = Encoding.ASCII.GetBytes(xml);

                    request1.Method = "POST";

                    request1.ContentType = "application/x-www-form-urlencoded";
                    request1.ContentLength = bytes.Length;

                    using (var stream = request1.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    var response1 = (HttpWebResponse)request1.GetResponse();

                    responseString = new StreamReader(response1.GetResponseStream()).ReadToEnd();
                }
                // MessageBox.Show("RESPONSE : " + responseString);

                if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "FECUND")
                {
                    string str1;
                    //str1 = reader.ReadToEnd();
                    str1 = "1 sms send";
                    //if (str1.Length > 11)
                    //{
                    //    result = str1.Substring(0, 10);
                    //}
                    result = str1;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSGUPSHUP")
                {
                    //result = reader.ReadToEnd();
                    result = responseString;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSIDEA")
                {
                    string strresult1;
                    strresult1 = responseString;
                    result = strresult1.Substring(0, 150);
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "HAKIMI")
                {
                    result = responseString;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "HAKIMI2")
                {
                    result = responseString;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "ASK4SMS")
                {
                    string strresult1;
                    strresult1 = responseString;
                    result = strresult1.Substring(0, 50);
                }
                else
                {
                    string strresult1;
                    //strresult1 = reader.ReadToEnd();
                    strresult1 = responseString;
                    result = strresult1.Substring(0, 50);
                }

                //reader1.Close();
                //stream.Close();

                return result;
            }
            catch (Exception ex)
            {
                result = "ERROR,ERROR" + ex.Message.ToString().Substring(0, 100);
                //Console.WriteLine(ex.ToString());
                //MessageBox.Show(ex.Message);
                //frmentser.Write_In_Error_Log(ex.Message.ToString() + " Error occures in SEND_WEB_REQUEST()) " + DateTime.Now.ToString());
                //MessageBox.Show(ex.Message.ToString() + " Error in Send_web_request()");
                return result;
            }
            finally
            {
                //if (response != null)
                //    response.Close();
            }
        }

        public string SEND_WEB_REQUET_2(string url)
        {
            string result = "";
            string xml = "";

            try
            {

                // Create a request for the URL.
                WebRequest request = WebRequest.Create(url);
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);

                // Get the stream containing content returned by the server.
                // The using block ensures the stream is automatically closed.
                using (Stream dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();
                    // Display the content.
                    result = responseFromServer+"";
                }

                // Close the response.
                response.Close();

                return result;
            }
            catch (Exception ex)
            {
                result = "ERROR,ERROR" + ex.Message.ToString().Substring(0, 100);
                return result;
            }
            finally
            { }
        }



        public bool FillSMSDetails()
        {
            DataTable dtsetting = new DataTable();
            clsMsSqlDbFunction mssql = new clsMsSqlDbFunction();
            try
            {
                string str1 = "SELECT BILLNOBASEDON,PROPROVIDER,PROUSERID,PROPASSWORD,TRAPROVIDER,TRAUSERID,TRAPASSWORD,SMSSIGN,SMTPEMAILADDRESS, " +
                                " SMTPEMAILPASSWORD,SMTPADDRESS,SMTPPORT,SMSRESTNAME,ENABLEZOMATO,ZOMATOSERVICETYPE, " +
                                " EMAILADDRECTYPE1,EMAILADDRECTYPE2,EMAILPASSRECTYPE1,EMAILPASSRECTYPE2,EMAILPORTRECTYPE1,EMAILPORTRECTYPE2,EMAILPOPADDRECTYPE1,EMAILPOPADDRECTYPE2,ZOMATOEMAILADD, " +
                                " WHATSAPPPROVIDER,WHATSAPPUSERID,WHATSAPPPASSWORD,WHATSAPPMOBILENO,WHATSAPPAUTHKEY " +
                                " FROM RMSSETTING";

                dtsetting = mssql.FillDataTable(str1, "RMSSETTING");

                if (dtsetting.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtsetting.Rows)
                    {
                        clsPublicVariables.BILLNOBASEDON = row1["BILLNOBASEDON"] + "".Trim();
                        clsPublicVariables.PROPROVIDER = row1["PROPROVIDER"] + "".Trim();
                        clsPublicVariables.PROUSERID = row1["PROUSERID"] + "".Trim();
                        clsPublicVariables.PROPASSWORD = row1["PROPASSWORD"] + "".Trim();
                        clsPublicVariables.TRAPROVIDER = row1["TRAPROVIDER"] + "".Trim();
                        clsPublicVariables.TRAUSERID = row1["TRAUSERID"] + "".Trim();
                        clsPublicVariables.TRAPASSWORD = row1["TRAPASSWORD"] + "".Trim();
                        clsPublicVariables.SMSSIGN = row1["SMSSIGN"] + "".Trim();
                        clsPublicVariables.SMSRESTNM = row1["SMSRESTNAME"] + "".Trim();
                        clsPublicVariables.GENSMTPEMAILADDRESS = (row1["SMTPEMAILADDRESS"]) + "".Trim();
                        clsPublicVariables.GENSMTPEMAILPASSWORD = (row1["SMTPEMAILPASSWORD"]) + "".Trim();
                        clsPublicVariables.GENSMTPADDRESS = (row1["SMTPADDRESS"]) + "".Trim();
                        clsPublicVariables.GENSMTPPORT = (row1["SMTPPORT"]) + "".Trim();
                        clsPublicVariables.GENRESTNAME = (row1["SMSRESTNAME"]) + "".Trim();
                        clsPublicVariables.GENENABLEZOMATO = (row1["ENABLEZOMATO"]) + "".Trim();
                        clsPublicVariables.GENZOMATOSERVICETYPE = (row1["ZOMATOSERVICETYPE"]) + "".Trim();
                        clsPublicVariables.EMAILADDRECTYPE1 = (row1["EMAILADDRECTYPE1"]) + "".Trim();
                        clsPublicVariables.EMAILPASSRECTYPE1 = (row1["EMAILPASSRECTYPE1"]) + "".Trim();
                        clsPublicVariables.EMAILPORTRECTYPE1 = (row1["EMAILPORTRECTYPE1"]) + "".Trim();
                        clsPublicVariables.EMAILPOPADDTRECTYPE1 = (row1["EMAILPOPADDRECTYPE1"]) + "".Trim();
                        clsPublicVariables.EMAILADDRECTYPE2 = (row1["EMAILADDRECTYPE2"]) + "".Trim();
                        clsPublicVariables.EMAILPASSRECTYPE2 = (row1["EMAILPASSRECTYPE2"]) + "".Trim();
                        clsPublicVariables.EMAILPORTRECTYPE2 = (row1["EMAILPORTRECTYPE2"]) + "".Trim();
                        clsPublicVariables.EMAILPOPADDTRECTYPE2 = (row1["EMAILPOPADDRECTYPE2"]) + "".Trim();
                        clsPublicVariables.GENZOMATOEMAILADD = (row1["ZOMATOEMAILADD"]) + "".Trim();
                        clsPublicVariables.GENSMSAUTHOKEY = (row1["SMSAUTHOKEY"]) + "".Trim();

                        clsPublicVariables.GENWHATSAPPPROVIDER = (row1["WHATSAPPPROVIDER"]) + "".Trim();
                        clsPublicVariables.GENWHATSAPPUSERID = (row1["WHATSAPPUSERID"]) + "".Trim();
                        clsPublicVariables.GENWHATSAPPPASSWORD = (row1["WHATSAPPPASSWORD"]) + "".Trim();
                        clsPublicVariables.GENWHATSAPPMOBILENO = (row1["WHATSAPPMOBILENO"]) + "".Trim();
                        clsPublicVariables.GENWHATSAPPAUTHKEY = (row1["WHATSAPPAUTHKEY"]) + "".Trim();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string RightString(string s, int length)
        {
            try
            {
                length = Math.Max(length, 0);

                if (s.Length > length)
                {
                    return s.Substring(s.Length - length, length);
                }
                else
                {
                    return s;
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

    }
}
