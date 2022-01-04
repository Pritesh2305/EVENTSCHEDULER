using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVENTSCHEDULER.ReadEmail
{
    internal enum ConnectionState
    {
        /// <summary>
        /// This is when the Pop3Client is not even connected to the server
        /// </summary>
        Disconnected,

        /// <summary>
        /// This is when the server is awaiting user credentials
        /// </summary>
        Authorization,

        /// <summary>
        /// This is when the server has been given the user credentials, and we are allowed
        /// to use commands specific to this users mail drop
        /// </summary>
        Transaction
    }
}
