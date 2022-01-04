using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Forms;

namespace EVENTSCHEDULER
{
    class clssmshistoryDal
    {
        clsMsSqlDbFunction clssql = new clsMsSqlDbFunction();

        public bool Db_Operation(clssmshistoryBal smshistorybal)
        {
            bool funRetval;
           // Int64 setlerid = 0;
            //string str1 = "";
            DataTable dtbanqbilling = new DataTable();
                       
            try
            {
                funRetval = true;

                clssql.OpenMsSqlConnection();

                SqlCommand mscmd = new SqlCommand("sp_SMSHISTORY", clsMsSqlDbFunction.mssqlcon);

                if (clsMsSqlDbFunction.mssqlcon.State == ConnectionState.Closed)
                {
                    clsMsSqlDbFunction.mssqlcon.Open();
                }

                mscmd.Parameters.Add("@p_mode", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_rid", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_smsevent", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_smseventid", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_mobno", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_smstext", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_sendflg", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_smspername", SqlDbType.NVarChar);

                mscmd.Parameters.Add("@p_smsaccuserid", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_smstype", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_resid", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_tobesenddatetime", SqlDbType.DateTime);
                mscmd.Parameters.Add("@p_rmsid", SqlDbType.NVarChar);
                
                mscmd.Parameters.Add("@p_userid", SqlDbType.BigInt);

                SqlParameter param_errstr = new SqlParameter("@p_errstr", SqlDbType.NVarChar, 500);
                param_errstr.Direction = ParameterDirection.Output;
                mscmd.Parameters.Add(param_errstr);

                SqlParameter param_retval = new SqlParameter("@p_retval", SqlDbType.BigInt);
                param_retval.Direction = ParameterDirection.Output;
                mscmd.Parameters.Add(param_retval);

                SqlParameter param_id = new SqlParameter("@p_id", SqlDbType.BigInt);
                param_id.Direction = ParameterDirection.Output;
                mscmd.Parameters.Add(param_id);

                mscmd.CommandType = CommandType.StoredProcedure;

                mscmd.Parameters["@p_mode"].Value = smshistorybal.Formmode;
                mscmd.Parameters["@p_rid"].Value = smshistorybal.Rid;
                mscmd.Parameters["@p_smsevent"].Value = smshistorybal.Smsevent;
                mscmd.Parameters["@p_smseventid"].Value = smshistorybal.Smseventid;
                mscmd.Parameters["@p_mobno"].Value = smshistorybal.Mobno;
                mscmd.Parameters["@p_smstext"].Value = smshistorybal.Smstext;
                mscmd.Parameters["@p_sendflg"].Value = smshistorybal.Sendflg;
                mscmd.Parameters["@p_smspername"].Value = smshistorybal.Smspername;
                mscmd.Parameters["@p_smsaccuserid"].Value = smshistorybal.Smsaccuserid;
                mscmd.Parameters["@p_smstype"].Value = smshistorybal.Smstype;
                mscmd.Parameters["@p_resid"].Value = smshistorybal.Resid;
                mscmd.Parameters["@p_tobesenddatetime"].Value = smshistorybal.Tobesenddatetime;
                mscmd.Parameters["@p_rmsid"].Value = smshistorybal.Rmsid;
                
                mscmd.Parameters["@p_userid"].Value = smshistorybal.Loginuserid;
                int ret = mscmd.ExecuteNonQuery();

                smshistorybal.Errstr = mscmd.Parameters["@p_Errstr"].Value.ToString();
                smshistorybal.Retval = Convert.ToInt32(mscmd.Parameters["@p_RetVal"].Value);
                smshistorybal.Id = Convert.ToInt32(mscmd.Parameters["@p_id"].Value);

                funRetval = true;

                if (smshistorybal.Retval > 0)
                {
                    MessageBox.Show(smshistorybal.Errstr, clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    funRetval = false;
                }

                return funRetval;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in Db_Operation())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                smshistorybal.Errstr = ex.Message.ToString() + " Error occures in Db_Operation()";
                smshistorybal.Retval = 1;
                smshistorybal.Id = 0;
                return false;
            }
        }

    }
}
