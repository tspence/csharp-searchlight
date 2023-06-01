using System;
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
    private Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _postgres;
    private PostgreSqlContainer _container;
    private List<EmployeeObj> _list;

    [TestInitialize]
    public async Task SetupClient()
    {
        _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Loose);
        _container = new PostgreSqlBuilder()
            .Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        
        // Construct the database schema and insert some test data
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Create basic table
            using (var command = new NpgsqlCommand("CREATE TABLE employeeobj (name text null, id int not null, hired timestamp with time zone, paycheck numeric, onduty bool)", connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            
            // Insert rows
            foreach (var record in EmployeeObj.GetTestList())
            {
                using (var command = new NpgsqlCommand("INSERT INTO employeeobj (name, id, hired, paycheck, onduty) VALUES (@name, @id, @hired, @paycheck, @onduty)", connection))
                {
                    command.Parameters.AddWithValue("@name", NpgsqlDbType.Text, (object)record.name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, record.id);
                    command.Parameters.AddWithValue("@hired", NpgsqlDbType.TimestampTz, record.hired);
                    command.Parameters.AddWithValue("@paycheck", NpgsqlDbType.Numeric, record.paycheck);
                    command.Parameters.AddWithValue("@onduty", NpgsqlDbType.Boolean, record.onduty);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Keep track of the correct result expectations and execution process
        _list = EmployeeObj.GetTestList();
        _postgres = async syntax =>
        {
            var sql = syntax.ToPostgresCommand();
            var result = new List<EmployeeObj>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(sql.CommandText, connection))
                {
                    foreach (var p in sql.Parameters)
                    {
                        command.Parameters.AddWithValue(p.Key, ConvertNpgsqlType(sql.ParameterTypes[p.Key]), p.Value);
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
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
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
        else if (parameterType == typeof(string))
        {
            return NpgsqlDbType.Text;
        }
        else if (parameterType == typeof(Int32))
        {
            return NpgsqlDbType.Integer;
        }
        else if (parameterType == typeof(decimal))
        {
            return NpgsqlDbType.Numeric;
        }
        else if (parameterType == typeof(DateTime))
        {
            return NpgsqlDbType.TimestampTz;
        }

        throw new Exception("Not recognized type");
    }

    [TestCleanup]
    public async Task CleanupMongo()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task EmployeeTestSuite()
    {
        await Executors.EmployeeTestSuite.BasicTestSuite(_src, _list, _postgres);
    }
}