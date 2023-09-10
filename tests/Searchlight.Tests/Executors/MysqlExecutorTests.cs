using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using Searchlight.Query;
using Searchlight.Tests.Models;
using Testcontainers.MySql;

namespace Searchlight.Tests.Executors;

[TestClass]
public class MysqlExecutorTests
{
    private DataSource _src;
    private string _connectionString;
    private Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _executor;
    private MySqlContainer _container;
    private List<EmployeeObj> _list;

    [TestInitialize]
    public async Task SetupClient()
    {
        _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Strict);
        _container = new MySqlBuilder()
            .Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        
        // Construct the database schema and insert some test data
        await using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Create basic table
            await using (var command =
                         new MySqlCommand(
                             "CREATE TABLE EmployeeObj (name text null, id int not null, hired timestamp, paycheck decimal, onduty bit, employeetype int not null)",
                             connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            
            // Insert rows
            foreach (var record in EmployeeObj.GetTestList())
            {
                await using (var command = new MySqlCommand("INSERT INTO EmployeeObj (name, id, hired, paycheck, onduty, employeetype) VALUES (@name, @id, @hired, @paycheck, @onduty, @employeetype)", connection))
                {
                    command.Parameters.AddWithValue("@name", (object)record.name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", record.id);
                    command.Parameters.AddWithValue("@hired", record.hired);
                    command.Parameters.AddWithValue("@paycheck", record.paycheck);
                    command.Parameters.AddWithValue("@onduty", record.onduty);
                    command.Parameters.AddWithValue("@employeetype", record.employeeType);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Keep track of the correct result expectations and execution process
        _list = EmployeeObj.GetTestList();
        _executor = async syntax =>
        {
            var sql = syntax.ToMySqlCommand();
            var result = new List<EmployeeObj>();
            await using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new MySqlCommand(sql.CommandText, connection))
                {
                    foreach (var p in sql.Parameters)
                    {
                        var type = sql.ParameterTypes[p.Key];
                        command.Parameters.AddWithValue(p.Key, type == typeof(DateTime) ? ((DateTime)p.Value).ToUniversalTime() : p.Value);
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
                        Assert.Fail($"MySQL executor generated invalid SQL: {ex}");
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