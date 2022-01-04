using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVENTSCHEDULER
{
    class clssmshistoryBal
    {
        private Int64 _formmode;

        public Int64 Formmode
        {
            get { return _formmode; }
            set { _formmode = value; }
        }
        private Int64 _rid;

        public Int64 Rid
        {
            get { return _rid; }
            set { _rid = value; }
        }
        private String _smsevent;

        public String Smsevent
        {
            get { return _smsevent; }
            set { _smsevent = value; }
        }
        private Int64 _smseventid;

        public Int64 Smseventid
        {
            get { return _smseventid; }
            set { _smseventid = value; }
        }
        private String _mobno;

        public String Mobno
        {
            get { return _mobno; }
            set { _mobno = value; }
        }
        private String _smstext;

        public String Smstext
        {
            get { return _smstext; }
            set { _smstext = value; }
        }
        private Int64 _sendflg;

        public Int64 Sendflg
        {
            get { return _sendflg; }
            set { _sendflg = value; }
        }
        private String _smspername;

        public String Smspername
        {
            get { return _smspername; }
            set { _smspername = value; }
        }
        private String _smsaccuserid;

        public String Smsaccuserid
        {
            get { return _smsaccuserid; }
            set { _smsaccuserid = value; }
        }
        private String _smstype;

        public String Smstype
        {
            get { return _smstype; }
            set { _smstype = value; }
        }
        private String _resid;

        public String Resid
        {
            get { return _resid; }
            set { _resid = value; }
        }
        private DateTime _tobesenddatetime;

        public DateTime Tobesenddatetime
        {
            get { return _tobesenddatetime; }
            set { _tobesenddatetime = value; }
        }

        private String _rmsid;

        public String Rmsid
        {
            get { return _rmsid; }
            set { _rmsid = value; }
        }

        private Int64 _loginuserid = 0;

        public Int64 Loginuserid
        {
            get { return _loginuserid; }
            set { _loginuserid = value; }
        }
        private String _errstr = "";

        public String Errstr
        {
            get { return _errstr; }
            set { _errstr = value; }
        }
        private Int64 _retval = 0;

        public Int64 Retval
        {
            get { return _retval; }
            set { _retval = value; }
        }
        private Int64 _id = 0;

        public Int64 Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Int64 Db_Operation_SMSHISTORY(clssmshistoryBal smshistorybal)
        {
            try
            {
                clssmshistoryDal clssmshisdal = new clssmshistoryDal();
                bool ret1 = clssmshisdal.Db_Operation(smshistorybal);
                return smshistorybal.Id;
            }
            catch (Exception)
            {
                return 0;
            }
        }

    }
}
