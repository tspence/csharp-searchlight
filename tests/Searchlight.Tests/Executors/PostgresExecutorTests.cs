﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NpgsqlTypes;
using Searchlight.Query;
using Searchlight.Tests.Models;
using Testcontainers.PostgreSql;

namespace Searchlight.Tests.Executors;

[TestClass]
public class PostgresExecutorTests
{
    private DataSource _src;
    private string _connectionString;
    private Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _executor;
    private PostgreSqlContainer _container;
    private List<EmployeeObj> _list;

    [TestInitialize]
    public async Task SetupClient()
    {
        _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Strict);
        _container = new PostgreSqlBuilder()
            .Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        
        // Construct the database schema and insert some test data
        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Create basic table
            await using (var command =
                         new NpgsqlCommand(
                             "CREATE TABLE employeeobj (name text null, id int not null, hired timestamp with time zone, paycheck numeric, onduty bool, employeetype int not null)",
                             connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            
            // Insert rows
            foreach (var record in EmployeeObj.GetTestList())
            {
                await using (var command = new NpgsqlCommand("INSERT INTO employeeobj (name, id, hired, paycheck, onduty, employeetype) VALUES (@name, @id, @hired, @paycheck, @onduty, @employeetype)", connection))
                {
                    command.Parameters.AddWithValue("@name", NpgsqlDbType.Text, (object)record.name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, record.id);
                    command.Parameters.AddWithValue("@hired", NpgsqlDbType.TimestampTz, record.hired);
                    command.Parameters.AddWithValue("@paycheck", NpgsqlDbType.Numeric, record.paycheck);
                    command.Parameters.AddWithValue("@onduty", NpgsqlDbType.Boolean, record.onduty);
                    command.Parameters.AddWithValue("@employeetype", NpgsqlDbType.Integer, (int)record.employeeType);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Keep track of the correct result expectations and execution process
        _list = EmployeeObj.GetTestList();
        _executor = async syntax =>
        {
            var sql = syntax.ToPostgresCommand();
            var result = new List<EmployeeObj>();
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(sql.CommandText, connection))
                {
                    foreach (var p in sql.Parameters)
                    {
                        var type = sql.ParameterTypes[p.Key];
                        command.Parameters.AddWithValue(p.Key, ConvertNpgsqlType(sql.ParameterTypes[p.Key]),
                            type == typeof(DateTime) ? ((DateTime)p.Value).ToUniversalTime() : p.Value);
                    }

                    try
                    {
                        var reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            result.Add(new EmployeeObj()
                            {
                                name = reader.IsDBNull(0) ? null : reader.GetString(0),
                                id = reader.GetInt32(1),
                                hired = reader.GetDateTime(2),
                                paycheck = reader.GetDecimal(3),
                                onduty = reader.GetBoolean(4),
                                employeeType = (EmployeeObj.EmployeeType)reader.GetInt32(5),
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"Postgres executor generated invalid SQL: {ex}");
                    }
                }
            }

            // TODO: Would this be better if we used dapper?
            return new FetchResult<EmployeeObj>()
            {
                records = result.ToArray(),
            };
        };
    }

    private NpgsqlDbType ConvertNpgsqlType(Type parameterType)
    {
        if (parameterType == typeof(bool))
        {
            return NpgsqlDbType.Boolean;
        }
        if (parameterType == typeof(string))
        {
            return NpgsqlDbType.Text;
        }
        if (parameterType == typeof(Int32))
        {
            return NpgsqlDbType.Integer;
        }
        if (parameterType == typeof(decimal))
        {
            return NpgsqlDbType.Numeric;
        }
        if (parameterType == typeof(DateTime))
        {
            return NpgsqlDbType.TimestampTz;
        }

        throw new Exception("Not recognized type");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task EmployeeTestSuite()
    {
        await Executors.EmployeeTestSuite.BasicTestSuite(_src, _list, _executor);
    }
}