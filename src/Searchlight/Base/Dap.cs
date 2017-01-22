using System;
using System.Collections.Generic;
using System.Data;
using Dapper;

namespace Searchlight.Base
{
    public sealed class Dap : IDisposable
    {
        private IDbConnection _connection;
        private IDbTransaction _transaction;
        private IDbConnectionFactory _factory;
        private bool _connection_owned_by_this_object;

        public Dap(IDbConnectionFactory connectionFactory)
        {
            _factory = connectionFactory;
            _connection = connectionFactory.Create();

            // We own this connection since it was created for us
            _connection_owned_by_this_object = true;
        }

        public Dap(Dap otherdap)
        {
            _connection = otherdap._connection;
            _transaction = otherdap._transaction;
            _factory = otherdap._factory;

            // We do not own this connection - the other dap does
            _connection_owned_by_this_object = false;
        }

        public IDbTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (_connection_owned_by_this_object) {
                OpenConnection();
                _transaction = _connection.BeginTransaction(isolationLevel);
                return _transaction;
            } else {
                throw new Exception("This dapper object does not own this connection and cannot control the transaction.");
            }
        }

        public void CommitTransaction()
        {
            if (_transaction != null) {
                if (_connection_owned_by_this_object) {
                    _transaction.Commit();
                    _transaction = null;
                } else {
                    throw new Exception("This dapper object does not own this connection and cannot control the transaction.");
                }
            } else {
                throw new Exception("No transaction was opened!");
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null) {
                if (_connection_owned_by_this_object) {
                    _transaction.Rollback();
                    _transaction = null;
                } else {
                    throw new Exception("This dapper object does not own this connection and cannot control the transaction.");
                }
            } else {
                throw new Exception("No transaction was opened!");
            }
        }

        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int Execute(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Execute(_connection, sql, param, transaction, commandTimeout, commandType);
                }
            } finally {
                if (transaction == null) {
                    CloseConnection();
                }
            }
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public IEnumerable<dynamic> Query(string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Query(_connection, sql, param, transaction, buffered, commandTimeout, commandType);
                }
            } finally {
                if (buffered && (transaction == null)) {
                    CloseConnection();
                }
            }
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
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Query<T>(_connection, sql, param, transaction, buffered, commandTimeout, commandType);
                }
            } finally {
                if (buffered && (transaction == null)) {
                    CloseConnection();
                }
            }
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Query<TFirst, TSecond, TReturn>(_connection, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
            } finally {
                if (buffered && (transaction == null)) {
                    CloseConnection();
                }
            }
        }

        /// <summary>
        /// Perform a multi mapping query with 5 input parameters
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(_connection, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
            } finally {
                if (buffered && (transaction == null)) {
                    CloseConnection();
                }
            }
        }

        /// <summary>
        /// Perform a multi mapping query with 4 input parameters
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Query<TFirst, TSecond, TThird, TFourth, TReturn>(_connection, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
            } finally {
                if (buffered && (transaction == null)) {
                    CloseConnection();
                }
            }
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            try {
                using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                    return SqlMapper.Query<TFirst, TSecond, TThird, TReturn>(_connection, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
            } finally {
                if (buffered && (transaction == null)) {
                    CloseConnection();
                }
            }
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = 0, CommandType? commandType = null)
        {
            if (transaction == null) transaction = _transaction;
            OpenConnection();
            using (var wrapper = new DapperEventWrapper(this, _factory, sql)) {
                return SqlMapper.QueryMultiple(_connection, sql, param, transaction, commandTimeout, commandType);
            }
        }

        private void OpenConnection()
        {
            if (_connection_owned_by_this_object) {
                if (_connection != null && _connection.State != ConnectionState.Open)
                    _connection.Open();
            }
        }

        private void CloseConnection()
        {
            if (_connection_owned_by_this_object) {
                if (_connection != null)
                    _connection.Close();
            }
        }

        public void Dispose()
        {
            if (_connection_owned_by_this_object) {
                if (_transaction != null) {
                    _transaction.Rollback();
                    _transaction.Dispose();
                }

                if (_connection != null) {
                    CloseConnection();
                    _connection.Dispose();
                }
            }

            _transaction = null;
            _connection = null;
        }
    }
}

