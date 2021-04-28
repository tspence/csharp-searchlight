[![NuGet](https://img.shields.io/nuget/v/Searchlight.svg?style=plastic)](https://www.nuget.org/packages/Searchlight/)
[![Travis](https://travis-ci.com/tspence/csharp-searchlight.svg?branch=master&style=plastic)](https://travis-ci.com/tspence/csharp-searchlight)

# csharp-searchlight

A lightweight, secure query language for searching through databases and in-memory collections using a fluent REST API with robust, secure searching features.

# What is Searchlight?

Searchlight is a simple and safe query language for API design.  [Designed with security in mind](https://tedspence.com/protecting-apis-with-layered-security-8c989fb5a19f), 
it works well with REST, provides complex features, and is easier to learn than GraphQL.  

The Searchlight query language is safe from SQL injection attacks.  Searchlight allows you to expose robust query functionality while maintaining full control
over the exact queries executed on your data store, whether those queries are executed via LINQ, SQL, or NoSQL.  You maintain the ability to enforce rules on
end-user query complexity, you can enable or disable querying on certain columns based on performance metrics, or adjust query criteria to enforce separation
of customer data.  All input fields provided from the customer are parameterized. 

An example API with Searchlight looks like this:

```
GET /customers/?query=CreatedDate gt '2019-01-01' and (IsApproved = false OR (approvalCode IS NULL AND daysWaiting between 5 and 10))
```

Searchlight uses type checking, validation, and parsing to convert this query text into an abstract syntax tree (AST) representing search clauses and parameters.  
You can then convert that AST into various forms and execute it on an SQL database, an in-memory object collection using LINQ, a MongoDB database, or so on.  
To ensure that no risky text is passed to your database, Searchlight reconstructs a completely new SQL query from string constants defined in your classes, and adds
parameters as appropriate.  All field names are converted from "customer-visible" field names to "actual database" names.  The above query would be transformed 
to the following:

```sql
SELECT * FROM customers WHERE created_date >= @p1 AND (approval_flag = @p2 OR (approval_code_str IS NULL AND days_waiting BETWEEN @p3 AND @p4))

Parameters:
    - @p1: '2019-01-01'
    - @p2: false
    - @p3: 5
    - @p4: 10
```

# How does Searchlight work?

To use searchlight, you construct a "model" that will be exposed via your API.  Tag your model with the `[SearchlightModel]` annotation, and tag each
queryable field with  `[SearchlightField]`.

```csharp
[SearchlightModel]
public class MyAccount
{
    // These fields are queryable
    [SearchlightField] public string AccountName { get; set; }
    [SearchlightField] public DateTime Created { get; set; }

    // This field will not be searchable
    public string SecretKey { get; set; }
}
```

When someone queries your API, Searchlight can transform their query into a SQL or LINQ statement:

```csharp
var list = new List<MyAccount>();
var syntax = src.Parse("AccountName startswith 'alice' and Created gt '2019-01-01'");

// To execute via SQL Server
var sql = syntax.ToSqlServerCommand();
var results = conn.Execute(sql.CommandText, sql.Parameters);

// To execute via an in-memory object collection using LINQ
var results = syntax.QueryCollection<EmployeeObj>(list);
```

# Database independence with Searchlight, Dapper, and AutoMapper

Searchlight is designed to mix with other powerful frameworks such as [Dapper](https://github.com/StackExchange/Dapper) and [AutoMapper](https://automapper.org/) to help 
provide high performance functionality on SQL Server. This example API demonstrates filtering, ordering, pagination, and the ability to return a full row count so the
application can display pagination UI elements.

This example demonstrates key techniques:
* Widgets are known inside the database by one class, "WidgetEntity", yet are expressed through the API as a different class, "WidgetModel".  This allows you to
rename fields, rename tables, enforce transformation logic, and make certain fields visible either internally or externally.
* Pagination uses the "Page Size" and "Page Number" pattern.  You could implement similar features using Skip and Take if preferred.
* The exact SQL Server query uses a temporary table and multiple result sets to ensure that only the exact rows specified are returned to the caller.  The SQL command
retrieves the minimum amount of data possible, plus it also tells you the total count of records so your user interface can show the exact number of pages.
* This pattern uses [Dapper Contrib](https://dapper-tutorial.net/dapper-contrib) to fetch widget entities using asynchronous queries.

```csharp
public async Task<FetchResult<WidgetModel>> QueryWidgets([FromQuery]string filter, [FromQuery]string order, [FromQuery]int? pageSize, [FromQuery]int? pageNumber)
{
    var request = new FetchRequest() {filter = filter, order = order, pageNumber = pageNumber, pageSize = pageSize};
    var source = DataSource.Create(typeof(WidgetModel), AttributeMode.Strict);
    var syntax = source.Parse(request);
    var sql = syntax.ToSqlServerCommand(true);
    using (var conn = new SqlConnection(_config.GetConnectionString("MyConnectionString")))
    {
        using (var multi = (await conn.QueryMultipleAsync(sql.CommandText, sql.Parameters, null, null, CommandType.Text)))
        {
            var totalCount = (await multi.ReadAsync<int>()).ToArray().FirstOrDefault();
            var entities = (await multi.ReadAsync<WidgetEntity>()).ToArray();
            var models = _mapper.Map<WidgetEntity[], WidgetModel[]>(entities);
            return new FetchResult<WidgetModel>(request, models.ToList(), totalCount);
        }
    }
}
```

# Fetching child collections with Searchlight

Searchlight allows you to specify optional child collections.  By default, child collections are not included in a query; but users can specify
other child collections to retrieve along with their primary query.  These additional collections are fetched through the multi-recordset mode
of Searchlight SQL, so you still have only one database query to retrieve all the information you need.

Using the `include` parameter, you can fetch `WaitList` and `Copies` objects with a single query:

```csharp
[SearchlightModel]
public class LibraryBook {
    [SearchlightField]
    public string ISBN { get; set; }

    [SearchlightCollection(KeyName = "ISBN")]
    public BookReservation[] WaitList { get; set; }

    [SearchlightCollection(KeyName = "ISBN")]
    public BookCopy[] Copies { get; set; }
}

[SearchlightModel]
public class BookReservation
{
    [SearchlightField] public string ISBN { get; set; }
    ... other fields ...
}

[SearchlightModel]
public class BookCopy
{
    [SearchlightField] public string ISBN { get; set; }
    ... other fields ...
}
```

# What if a developer makes a mistake when querying?

Searchlight provides detailed error messages that help you and your customers diagnose problems.

* `EmptyClause` - The user sent a query with an empty open/close parenthesis, like "()".
* `FieldNotFound` - The query specified a field whose name could not be found.
* `FieldTypeMismatch` - The user tried to compare a string field with an integer, for example.
* `OpenClause` - The query had an open parenthesis with no closing parenthesis.
* `InvalidToken` - The parser expected a token like "AND" or "OR", but something else was provided.
* `TooManyParameters` - The user has sent too many criteria or parameters (some data sources have limits, for example, parameterized TSQL).
* `TrailingConjunction` - The query ended with the word "AND" or "OR" but nothing after it.
* `UnterminatedString` - A string value parameter is missing its end quotation mark.

With these errors, your API can give direct and useful feedback to developers as they craft their interfaces.  In each case, Searchlight
provides useful help:

* When the user gets a `FieldNotFound` error, Searchlight provides the list of all valid field names in the error.
* If you see an `InvalidToken` error, Searchlight tells you exactly which token was invalid and what it thinks are the correct tokens.

# What if my data model changes over time?

Searchlight provides for aliases so that you can maintain backwards compatibility with prior versions.  If you decide
to rename a field, fix a typo, or migrate from one field to another, Searchlight allows you to tag the field for forwards and backwards
compatibility.

```csharp
[SearchlightModel]
public class MyAccount
{
    [SearchlightField(Aliases = new string[] { "OldName", "NewName", "TransitionalName" })]
    public string AccountName { get; set; }
}
```

# Constructing Searchlight models programmatically

Constructing a model manually works as follows:

```csharp
var source = new SearchlightDataSource()
    .WithColumn("a", typeof(String), null)
    .WithColumn("b", typeof(Int32), null)
    .WithColumn("colLong", typeof(Int64), null)
    .WithColumn("colNullableGuid", typeof(Nullable<Guid>), null)
    .WithColumn("colULong", typeof(UInt64), null)
    .WithColumn("colNullableULong", typeof(Nullable<UInt64>), null)
    .WithColumn("colGuid", typeof(Guid), null);
source.MaximumParameters = 200;
source.DefaultSortField = "a";
```