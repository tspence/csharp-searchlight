[![NuGet](https://img.shields.io/nuget/v/Searchlight.svg?style=plastic)](https://www.nuget.org/packages/Searchlight/)
[![Travis](https://travis-ci.com/tspence/csharp-searchlight.svg?branch=master&style=plastic)](https://travis-ci.com/tspence/csharp-searchlight)

# csharp-searchlight

A lightweight, secure query language for searching through databases and in-memory collections using a fluent REST API with robust, secure searching features.

# What is Searchlight?

Searchlight is a simple and safe query language for API design.  [Designed with security in mind](https://tedspence.com/protecting-apis-with-layered-security-8c989fb5a19f), 
it works well with REST, provides complex features, and is easier to learn than GraphQL.  

* **Safe from SQL injection** 
As a compiled language, the Searchlight query language is safe from SQL injection attacks.  Malformed queries generate clear
error messages within Searchlight, and if you choose to use Searchlight on top of an SQL database, all queries executed on 
your database will use parameterized values.
* **Database independent**
You can use Searchlight against SQL databases, NoSQL databases, or in-memory collections.  If you change your mind later
and decide to switch to a different database technology, Searchlight still works.
* **Search in memory**
With Searchlight, you can search in-memory collections or use REDIS to cache data.  You can still search the data just like
it was in a SQL-based database.
* **Powerful queries**
Searchlight lets you execute complex search statements such as `in`, `startsWith`, `contains`, and others.  You can create
complex queries using parenthesis and conjunctions (AND/OR).
* **Reduce database usage**
You can use Searchlight to make multiple-result-set database calls with an SQL database to avoid executing multiple
fetch statements.
* **Self-documenting**
If you mistype the name of a field, you get an error that indicates exactly which field name was misspelled, and a list of all
known fields you can use.
* **Standardized queries**
The Searchlight API pattern allows for filtering, fetching extra data, sorting, and pagination.  
* **Programmatic control**
You can examine the Searchlight abstract syntax tree for performance problems, inappropriate filters, or query statements
too complex for your database and reject those queries before they waste unnecessary query cycles on your data store.
* **Human readable**
Unlike JSON-based query systems, Searchlight is easily readable and should be familiar to most people who are comfortable
using SQL and LINQ languages.  Searchlight uses words instead of symbols to avoid unnecessary escaping rules for HTML and HTTP
requests.

# Using Searchlight
The typical API pattern for Searchlight works as follows:

```
GET /api/v1/elements?filter=active eq true&include=comments&order=name&pageNumber=2&pageSize=100
```

This example query does the following things:
* Fetch data from the `elements` collection
* Only fetch `elements` whose `active` flags are set to true
* Include the extra data element known as `comments`
* Paginate the results into pages of size 100, and fetch page number two

A more complex Searchlight query might include multiple filter criteria, with more complex conjunctions:

```
GET /customers/?query=CreatedDate gt '2019-01-01' and (IsApproved = false OR (approvalCode IS NULL AND daysWaiting between 5 and 10))
```

Searchlight uses type checking, validation, and parsing to convert this query text into an abstract syntax tree (AST) representing 
search clauses and parameters.  You can then convert that AST into various forms and execute it on an SQL database, an in-memory 
object collection using LINQ, a MongoDB database, or so on.  To ensure that no risky text is passed to your database, Searchlight 
reconstructs a completely new SQL query from string constants defined in your classes, and adds parameters as appropriate.  All 
field names are converted from "customer-visible" field names to "actual database" names.  The above query would be transformed 
to the following:

```sql
SELECT * 
  FROM customers 
 WHERE created_date >= @p1 
   AND (approval_flag = @p2 OR (approval_code_str IS NULL AND days_waiting BETWEEN @p3 AND @p4))

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
var engine = new SearchlightEngine().AddAssembly(this.GetType().Assembly);
var list = new List<MyAccount>();
var syntax = engine.Parse(new FetchRequest() { 
    Table = "MyAccount", 
    Filter = "AccountName startswith 'alice' and Created gt '2019-01-01'"
});

// To execute via SQL Server
var sql = syntax.ToSqlServerCommand();
var results = conn.Execute(sql.CommandText, sql.Parameters);

// To execute via an in-memory object collection using LINQ
var results = syntax.QueryCollection<EmployeeObj>(list);
```

# Constructing a Query

Searchlight supports most operators common to SQL, such as:
* Equals (=, EQ)
* Greater Than (>, GT)
* Greater Than Or Equal (>=, GE)
* Less Than (<, LT)
* Less Than Or Equal (<=, LE)
* Not Equal (!=, NE, <>)
* In
* Contains
* StartsWith
* EndsWith
* IsNull
* AND
* OR

As well as sort directions specified by `ASC` and `DESC`, and encapsulated quotes denoted by `''` or `""` for filters like `Category eq 'Metallica''s Covers'`.

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
* `UnterminatedString` - A string value parameter is missing its end quotation mark, encapsulated quotes are supported using `''` or `""`.

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
