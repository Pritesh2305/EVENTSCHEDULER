using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Reflection;

namespace EVENTSCHEDULER
{
    class clsMsSqlDbFunction
    {
        clsGeneral objclsgen = new clsGeneral();
        public static SqlConnection mssqlcon = new SqlConnection();
        public SqlDataAdapter msdtp = new SqlDataAdapter();
        public SqlCommand mscmd = new SqlCommand();
        public SqlDataReader msdr;
        //public SqlTransaction transaction;
        
        # region PRIVATE FUNCTION

        private bool OpenMsSqlDataConnection()
        {
            try
            {
                mssqlcon = new SqlConnection();
                
                if (mssqlcon.State == ConnectionState.Closed)
                {
                    mssqlcon.ConnectionString = "Data Source = " + clsPublicVariables.ServerName1.ToLower() + ";" +
                                    " Initial Catalog = " + clsPublicVariables.DatabaseName1.ToLower() + ";" +
                                    " User ID = " + clsPublicVariables.UserName1.ToLower() + ";" +
                                    " Password = " + clsPublicVariables.Password1.ToLower() + ";" +
                                    " Connect Timeout = 30 " + ";";
                    mssqlcon.Open();
                }
               

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        # region PUBLIC FUNCTION

        public bool OpenMsSqlConnection()
        {
            try
            {
                if (string.IsNullOrEmpty(mssqlcon.ConnectionString))
                {
                    OpenMsSqlDataConnection();
                }

                if (mssqlcon.State == ConnectionState.Closed)
                {
                    mssqlcon.Open();
                }

                return true;
            }
            catch (Exception)
            {
                
                return false;
            }
        }

        public bool CloseMsSqlConnection()
        {
            try
            {
                if (mssqlcon.State == ConnectionState.Open)
                {
                    mssqlcon.Close();
                }

                return true;
            }
            catch (Exception)
            {
               
                return false;
            }

        }

        public bool ExecuteMsSqlCommand(string cmdStr)
        {
            try
            {
                OpenMsSqlConnection();
                mscmd.Connection = mssqlcon;
                mscmd.CommandText = cmdStr;
                mscmd.ExecuteNonQuery();
                return true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in ExecuteMsSqlCommand())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            finally
            {
               // CloseMsSqlConnection();
            }
        }

        public object ExecuteMsSqlCommandScalar(string cmdStr)
        {
            object obj = null;

            try
            {
                OpenMsSqlConnection();
                mscmd.Connection = mssqlcon;
                mscmd.CommandText = cmdStr;
                obj = mscmd.ExecuteScalar();

                if (DBNull.Value.Equals(obj) == true)
                {
                    obj = "";
                }

                return obj;
            }
            catch (Exception )
            {
               
                return obj;
            }

        }

        public DataTable FillDataTable(string cmdstr, string tblnm1)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                OpenMsSqlConnection();

                msdtp = new SqlDataAdapter(cmdstr, mssqlcon);
                msdtp.Fill(ds, "test");
                dt = ds.Tables[0];
                return dt;
            }
            catch (Exception )
            {               
                return dt;
            }
            finally
            {
                //CloseMsSqlConnection();
            }
        }

        public SqlDataReader ExecuteMsSqlDataReader(string cmdStr)
        {
            SqlDataReader functionReturnValue = default(SqlDataReader);
            functionReturnValue = msdr;

            try
            {
                OpenMsSqlConnection();
                mscmd.CommandText = cmdStr;

                if ((msdr != null))
                {
                    msdr.Close();
                }
                msdr = mscmd.ExecuteReader();
                functionReturnValue = msdr;

            }
            catch (Exception )
            {
                
            }

            return functionReturnValue;
        }

        public bool FillMstCombo(System.Windows.Forms.ComboBox obj, string FieldName)
        {
            string item = "";
            string cmdStr = null;

            try
            {
                cmdStr = ExecuteMsSqlCommandScalar("Select FieldValue from MstCombo where FieldName='" + FieldName + "'").ToString();

                obj.Items.Clear();
                for (Int32 i = 0; i <= cmdStr.Length - 1; i++)
                {
                    if (cmdStr[i].Equals("!") || i == cmdStr.Length - 1)
                    {
                        item = item + cmdStr[i];

                        item = item.Trim().Replace("!", "");

                        if (!string.IsNullOrEmpty(item))
                        {
                            obj.Items.Add(item);
                            item = "";
                        }
                    }
                    else
                    {
                        item = item + cmdStr[i];
                    }
                }

                return true;
            }

            catch (Exception )
            {
               
                return false;
            }
            finally
            {
                //CloseMsSqlConnection();
            }
        }

        public void FillDataCombo(object Obj, string DisplayMember1, string ValueMember1, bool AddBlankValue1)
        {
            string strSQL = null;
            SqlDataAdapter DA = new SqlDataAdapter();
            DataSet DS = new DataSet();
            object[] vArrSp = null;

            try
            {
                vArrSp = DisplayMember1.Split('|');

                strSQL = "Select " + vArrSp[0].ToString() + ", " + vArrSp[1].ToString() + " from " + ValueMember1 + "";

                if (AddBlankValue1 == true)
                {
                    strSQL = strSQL + " UNION Select 0 as " + vArrSp[0].ToString() + ", '' as " + vArrSp[1].ToString() + " from " + ValueMember1 + "";
                }


                strSQL = strSQL + " Order by " + vArrSp[1].ToString();

                DA = new SqlDataAdapter(strSQL, mssqlcon);
                DA.Fill(DS, ValueMember1);
                ((System.Windows.Forms.ComboBox)Obj).DataSource = DS.Tables[0];
                ((System.Windows.Forms.ComboBox)Obj).DisplayMember = vArrSp[1].ToString();
                ((System.Windows.Forms.ComboBox)Obj).ValueMember = vArrSp[0].ToString();

            }
            catch (Exception )
            {
               
            }
        }

        public bool Fill_DistinctCombo_Values(System.Windows.Forms.ComboBox Cmb1, string DataField1, string DataMember1)
        {
            bool functionReturnValue = false;
            string SqlStr1 = null;
            DataTable dt = default(DataTable);
            //DataRow Row = default(DataRow);

            try
            {
                functionReturnValue = true;

                SqlStr1 = "Select Distinct " + DataField1 + " From " + DataMember1;
                OpenMsSqlConnection();
                dt = FillDataTable(SqlStr1, "");

                Cmb1.Items.Clear();
                foreach (DataRow Row in dt.Rows)
                {
                    Cmb1.Items.Add(Row[0].ToString());
                }
            }
            catch (Exception )
            {
               
                functionReturnValue = false;
            }
            finally
            {
                //CloseMsSqlConnection();
            }
            return functionReturnValue;
        }

        public object[] FillCombo(string cmdStr)
        {

            int Ctr1 = 0;

            try
            {
                OpenMsSqlConnection();
                DataTable dt = FillDataTable(cmdStr, "");
                //DataRow row = default(DataRow);

                object[] cmbItems = new object[dt.Rows.Count];

                Ctr1 = 0;

                foreach (DataRow row in dt.Rows)
                {
                    cmbItems[Ctr1] = row[0].ToString();
                    Ctr1 = Ctr1 + 1;
                }

                return cmbItems;

            }
            catch (Exception )
            {
               
                return null;
            }
            finally
            {
                //CloseMsSqlConnection();
            }
        }

        public long GetTotalNumberofRecord(string qry1)
        {
            object no1;
            Int64 cnt1;
            try
            {
                OpenMsSqlConnection();
                mscmd.Connection = mssqlcon;
                mscmd.CommandText = qry1;
                no1 = mscmd.ExecuteScalar();
                cnt1 = objclsgen.Null2lng(no1);
                return cnt1;
            }
            catch (Exception )
            {
                
                return 0;
            }
        }

        public long GetMaxRecordFromTable(string Tblnm1, string fldnm1)
        {
            long ret = 0;
            try
            {
                string str1 = "Select Max(" + fldnm1 + ") From " + Tblnm1;

                ret = GetTotalNumberofRecord(str1);
                return ret;
            }
            catch (Exception )
            {
               
                ret = 0;
            }

            return ret;
        }

        public string GetFieldType(string qry1)
        {
            object str;
            string str1;
            try
            {
                OpenMsSqlConnection();
                mscmd.Connection = mssqlcon;
                mscmd.CommandText = qry1;
                str = mscmd.ExecuteScalar();
                str1 = Convert.ToString(str);
                return str1;
            }
            catch (Exception )
            {                
                return "";
            }
        }

        #endregion

    }
}
