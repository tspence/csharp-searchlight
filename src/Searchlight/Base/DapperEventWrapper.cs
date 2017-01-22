using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Base
{
    /// <summary>
    /// Attach to this event to be notified when an SQL event occurs
    /// </summary>
    public class DapperEventWrapper : IDisposable
    {
        private DateTime _startTime;
        private string _sql;
        private Dap _dap;
        private IDbConnectionFactory _factory;

        /// <summary>
        /// Triggered when the event begins
        /// </summary>
        public DapperEventWrapper(Dap dap, IDbConnectionFactory factory, string sql)
        {
            _startTime = DateTime.UtcNow;
            _sql = sql;
            _dap = dap;
            _factory = factory;
        }

        /// <summary>
        /// Triggered when the object goes out of scope
        /// </summary>
        public void Dispose()
        {
            var e = GetEventArgs();

            // Factory needs to know a call occurred
            if (_factory != null) {
                _factory.LogDatabaseCall(e.Sql, e.Duration);
            }

            // Global hook needs to know a call occurred
            GlobalSqlHook?.Invoke(_dap, e);
        }

        /// <summary>
        /// Retrieve the event arguments for this wrapper
        /// </summary>
        /// <returns></returns>
        public DapperSqlEventArgs GetEventArgs()
        {
            return new DapperSqlEventArgs()
            {
                Duration = DateTime.UtcNow - _startTime,
                Sql = _sql
            };
        }

        /// <summary>
        /// Hook this event to add functionality to all SQL statements everywhere
        /// </summary>
        public static event EventHandler GlobalSqlHook;
    }
}
