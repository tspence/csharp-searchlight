using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Base
{
    public class DapperSqlEventArgs : EventArgs
    {
        /// <summary>
        /// The SQL statement that was executed
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Number of rows returned
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// The length of time it took to execute this SQL statement
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
