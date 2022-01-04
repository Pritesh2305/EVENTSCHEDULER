using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVENTSCHEDULER
{
    class clsemailreadbal
    {
        private int _formmode;
        private Int64 _rid;

        private string _emailtype;

        public string Emailtype
        {
            get { return _emailtype; }
            set { _emailtype = value; }
        }

        private string _fromadd;

        public string Fromadd
        {
            get { return _fromadd; }
            set { _fromadd = value; }
        }

        private string _emailsub;

        public string Emailsub
        {
            get { return _emailsub; }
            set { _emailsub = value; }
        }
        private string _emailbody;

        public string Emailbody
        {
            get { return _emailbody; }
            set { _emailbody = value; }
        }

        private string _emailremark;

        public string Emailremark
        {
            get { return _emailremark; }
            set { _emailremark = value; }
        }

        private byte _isemailused;

        public byte Isemailused
        {
            get { return _isemailused; }
            set { _isemailused = value; }
        }

        private Int64 _refrid1;

        public Int64 Refrid1
        {
            get { return _refrid1; }
            set { _refrid1 = value; }
        }
        private Int64 _refrid2;

        public Int64 Refrid2
        {
            get { return _refrid2; }
            set { _refrid2 = value; }
        }
        private DateTime _orderdate;

        public DateTime Orderdate
        {
            get { return _orderdate; }
            set { _orderdate = value; }
        }
        private string _orderid;

        public string Orderid
        {
            get { return _orderid; }
            set { _orderid = value; }
        }
        private string _custnm;

        public string Custnm
        {
            get { return _custnm; }
            set { _custnm = value; }
        }
        private string _custno;

        public string Custno
        {
            get { return _custno; }
            set { _custno = value; }
        }

        private long _loginuserid = 0;
        private string _errstr = "";
        private long _retval = 0;
        private long _id = 0;

        #region Property

        public int FormMode
        {
            get { return this._formmode; }
            set { this._formmode = value; }
        }

        public Int64 Rid
        {
            get { return _rid; }
            set { _rid = value; }
        }

        public long LoginUserId
        {
            get { return this._loginuserid; }
            set { this._loginuserid = value; }
        }

        public string Errstr
        {
            get { return this._errstr; }
            set { this._errstr = value; }
        }

        public long Retval
        {
            get { return this._retval; }
            set { this._retval = value; }
        }

        public long Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        #endregion


        public bool Delete_EMAILREAD()
        {
            try
            {
                clsemailreaddal objexp1 = new clsemailreaddal();
                bool ret1 = objexp1.DeleteRecord(_rid, _loginuserid);
                return ret1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Int64 Db_Operation_EMAILREAD(clsemailreadbal tblreademail)
        {
            try
            {
                clsemailreaddal objexp1 = new clsemailreaddal();
                bool ret1 = objexp1.Db_Operation(tblreademail);

                if (FormMode == 1)
                {
                    tblreademail.Id = tblreademail.Rid;
                }

                return tblreademail.Id;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
