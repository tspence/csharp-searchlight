using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Query;
using Searchlight.Tests.Models;
using Testcontainers.MsSql;

namespace Searchlight.Tests.Executors;

[TestClass]
public class SqlServerExecutorTests
{
    private DataSource _src;
    private string _connectionString;
    private Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _postgres;
    private List<EmployeeObj> _list;
    private MsSqlContainer _container;

    [TestInitialize]
    public async Task SetupClient()
    {
        _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Strict);
        _container = new MsSqlBuilder()
            .Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        
        // Construct the database schema and insert some test data
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Create basic table
            using (var command =
                   new SqlCommand(
                       "CREATE TABLE employeeobj (name nvarchar(255) null, id int not null, hired datetime not null, paycheck decimal not null, onduty bit not null, employeetype tinyint null DEFAULT 0)",
                       connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            
            // Insert rows
            foreach (var record in EmployeeObj.GetTestList())
            {
                using (var command = new SqlCommand("INSERT INTO employeeobj (name, id, hired, paycheck, onduty, employeetype) VALUES (@name, @id, @hired, @paycheck, @onduty, @employeetype)", connection))
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
        _postgres = async syntax =>
        {
            var sql = syntax.ToSqlServerCommand();
            var result = new List<EmployeeObj>();
            int numResults = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql.CommandText, connection))
                {
                    foreach (var p in sql.Parameters)
                    {
                        var type = sql.ParameterTypes[p.Key];
                        if (type == typeof(DateTime))
                        {
                            command.Parameters.AddWithValue(p.Key, ((DateTime)p.Value).ToUniversalTime());
                        }
                        else
                        {
                            command.Parameters.AddWithValue(p.Key, p.Value);
                        }
                    }

                    try
                    {
                        var reader = await command.ExecuteReaderAsync();
                        await reader.ReadAsync();
                        numResults = reader.GetInt32(0);
                        
                        // Skip ahead to next result set
                        await reader.NextResultAsync();
                        while (await reader.ReadAsync())
                        {
                            result.Add(new EmployeeObj()
                            {
                                name = reader.IsDBNull(0) ? null : reader.GetString("name"),
                                id = reader.GetInt32("id"),
                                hired = reader.GetDateTime("hired"),
                                paycheck = reader.GetDecimal("paycheck"),
                                onduty = reader.GetBoolean("onduty"),
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(ex.ToString());
                    }
                }
            }

            // TODO: Would this be better if we used dapper?
            return new FetchResult<EmployeeObj>()
            {
                totalCount = numResults,
                records = result.ToArray(),
            };
        };
    }

    private SqlDbType ConvertTsqlType(Type parameterType)
    {
        if (parameterType == typeof(bool))
        {
            return SqlDbType.Bit;
        }
        else if (parameterType == typeof(string))
        {
            return SqlDbType.NVarChar;
        }
        else if (parameterType == typeof(Int32))
        {
            return SqlDbType.Int;
        }
        else if (parameterType == typeof(decimal))
        {
            return SqlDbType.Decimal;
        }
        else if (parameterType == typeof(DateTime))
        {
            return SqlDbType.DateTime;
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