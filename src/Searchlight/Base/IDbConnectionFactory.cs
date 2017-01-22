using System;
using System.Data;

namespace Searchlight.Base
{
    /// <summary>
    /// Root class for a factory that can create database connections
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Called when a connection is required
        /// </summary>
        /// <returns></returns>
        IDbConnection Create();

        /// <summary>
        /// Called when a database call occurs
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="duration"></param>
        void LogDatabaseCall(string sql, TimeSpan duration);
    }
}
