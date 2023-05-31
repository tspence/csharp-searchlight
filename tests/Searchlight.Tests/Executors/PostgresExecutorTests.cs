using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Searchlight.Query;
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
            // Create basic table
            using (var command = new NpgsqlCommand("CREATE TABLE employeeobj (name text null, id int not null, hired date, paycheck numeric, onduty bool)", connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            
            // Insert rows
            foreach (var record in EmployeeObj.GetTestList())
            {
                using (var command = new NpgsqlCommand("INSERT INTO employeeobj (name, id, hired, paycheck, onduty) VALUES (@name, @id, @hired, @paycheck, @onduty)", connection))
                {
                    command.Parameters.AddWithValue("name", record.name);
                    command.Parameters.AddWithValue("id", record.id);
                    command.Parameters.AddWithValue("hired", record.hired);
                    command.Parameters.AddWithValue("paycheck", record.paycheck);
                    command.Parameters.AddWithValue("onduty", record.onduty);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // var client = new MongoClient(_runner.ConnectionString);
        // var database = client.GetDatabase("IntegrationTest");
        // _collection = database.GetCollection<EmployeeObj>("TestCollection");
        _list = EmployeeObj.GetTestList();
        // await _collection.InsertManyAsync(_list);
        // _mongo = syntax => syntax.QueryMongo(_collection);
        _postgres = syntax =>
        {
            // TODO: Implement postgres and Dapper here
            return null;
        };
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
        await Tests.EmployeeTestSuite.BasicTestSuite(_src, _list, _postgres);
    }
}