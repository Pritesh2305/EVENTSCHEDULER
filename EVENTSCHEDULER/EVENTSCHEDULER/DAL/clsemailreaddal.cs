using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Forms;

namespace EVENTSCHEDULER
{
    class clsemailreaddal
    {
        clsMsSqlDbFunction clssql = new clsMsSqlDbFunction();

        public bool Db_Operation(clsemailreadbal tblemailread)
        {
            bool funRetval;

            try
            {
                funRetval = true;

                clssql.OpenMsSqlConnection();

                SqlCommand mscmd = new SqlCommand("sp_EMAILREAD", clsMsSqlDbFunction.mssqlcon);

                if (clsMsSqlDbFunction.mssqlcon.State == ConnectionState.Closed)
                {
                    clsMsSqlDbFunction.mssqlcon.Open();
                }

                mscmd.Parameters.Add("@p_mode", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_rid", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_emailtype", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_fromadd", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_emailsub", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_emailbody", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_emailremark", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_isemailused", SqlDbType.Bit);
                mscmd.Parameters.Add("@p_refrid1", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_refrid2", SqlDbType.BigInt);
                mscmd.Parameters.Add("@p_orderdate", SqlDbType.DateTime);
                mscmd.Parameters.Add("@p_orderid", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_custnm", SqlDbType.NVarChar);
                mscmd.Parameters.Add("@p_custno", SqlDbType.NVarChar);

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

                mscmd.Parameters["@p_mode"].Value = tblemailread.FormMode;
                mscmd.Parameters["@p_rid"].Value = tblemailread.Rid;
                mscmd.Parameters["@p_emailtype"].Value = tblemailread.Emailtype;
                mscmd.Parameters["@p_fromadd"].Value = tblemailread.Fromadd;
                mscmd.Parameters["@p_emailsub"].Value = tblemailread.Emailsub;
                mscmd.Parameters["@p_emailbody"].Value = tblemailread.Emailbody;
                mscmd.Parameters["@p_emailremark"].Value = tblemailread.Emailremark;
                mscmd.Parameters["@p_isemailused"].Value = tblemailread.Isemailused;
                mscmd.Parameters["@p_refrid1"].Value = tblemailread.Refrid1;
                mscmd.Parameters["@p_refrid2"].Value = tblemailread.Refrid2;
                mscmd.Parameters["@p_orderdate"].Value = tblemailread.Orderdate;
                mscmd.Parameters["@p_orderid"].Value = tblemailread.Orderid;
                mscmd.Parameters["@p_custnm"].Value = tblemailread.Custnm;
                mscmd.Parameters["@p_custno"].Value = tblemailread.Custno;

                mscmd.Parameters["@p_userid"].Value = tblemailread.LoginUserId;

                if (clsMsSqlDbFunction.mssqlcon.State == ConnectionState.Closed)
                {
                    clsMsSqlDbFunction.mssqlcon.Open();
                }

                int ret = mscmd.ExecuteNonQuery();

                tblemailread.Errstr = mscmd.Parameters["@p_Errstr"].Value.ToString();
                tblemailread.Retval = Convert.ToInt32(mscmd.Parameters["@p_RetVal"].Value);
                tblemailread.Id = Convert.ToInt32(mscmd.Parameters["@p_id"].Value);

                funRetval = true;

                if (tblemailread.Retval > 0)
                {
                    MessageBox.Show(tblemailread.Errstr, clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    funRetval = false;
                }

                return funRetval;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in Db_Operation())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }

        public bool DeleteRecord(Int64 Rid, long UserId)
        {
            bool funRetval;

            DataTable dt1 = new DataTable();
            try
            {
                funRetval = false;

                clssql.OpenMsSqlConnection();

                funRetval = clssql.ExecuteMsSqlCommand("UPDATE EMAILREAD set DelFlg = 1,DUSERID = " + UserId + " , DDATETIME=getdate() WHERE Rid = " + Rid);

                return funRetval;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in DeleteRecord())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }
    }
}
