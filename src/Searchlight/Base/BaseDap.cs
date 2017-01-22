using System;
using System.Collections.Generic;
using System.Data;
using Dapper;

namespace Searchlight.Base
{
    public abstract partial class BaseDap : IDisposable
	{
        private Dap _dap;

        /// <summary>
        /// Construct a new dapper object that will create a connection of its own
        /// </summary>
        /// <param name="dbConnectionFactory"></param>
		protected BaseDap(IDbConnectionFactory dbConnectionFactory)
        {
            _dap = new Dap(dbConnectionFactory);
        }

        /// <summary>
        /// Construct a dapper object that will share another object's connection & transaction
        /// </summary>
        /// <param name="otherdap"></param>
        protected BaseDap(BaseDap otherdap)
        {
            _dap = new Dap(otherdap._dap);
        }

        /// <summary>
        /// Begin a transaction
        /// </summary>
        public void BeginTransaction()
        {
            _dap.BeginTransaction();
        }

        /// <summary>
        /// Commit an existing transaction
        /// </summary>
        public void CommitTransaction()
        {
            _dap.CommitTransaction();
        }

        /// <summary>
        /// Returns the database name of the table associated with this dapper object
        /// </summary>
        /// <returns></returns>
        public abstract string GetSqlTableName();

        /// <summary>
        /// Returns true if this table has a primary key
        /// </summary>
        /// <returns></returns>
        public abstract bool HasPrimaryKey();

        /// <summary>
        /// Returns the name of the primary key field for this table, if any
        /// </summary>
        /// <returns></returns>
        public abstract string GetPrimaryKeyFieldName();

        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int Execute(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Execute(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public IEnumerable<dynamic> Query(string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Query(sql, param, transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<T> Query<T>(string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Query<TFirst, TSecond, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Perform a multi mapping query with 5 input parameters
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Perform a multi mapping query with 4 input parameters
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Query<TFirst, TSecond, TThird, TFourth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.Query<TFirst, TSecond, TThird, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = 0, CommandType? commandType = null)
        {
            return _dap.QueryMultiple(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>
        /// Ensure this object and its database connection are properly disposed
        /// </summary>
        public void Dispose()
        {
            if (_dap != null) {
                _dap.Dispose();
                _dap = null;
            }
        }
    }
}
