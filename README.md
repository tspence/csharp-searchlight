# csharp-searchlight
A lightweight, secure framework for searching through databases and in-memory collections using a fluent REST API with robust, secure searching features.

# Status

Travis CI:

[![Travis](https://travis-ci.com/tspence/csharp-searchlight.svg?branch=master&style=plastic)](https://travis-ci.com/tspence/csharp-searchlight)

NuGet:

TODO

# What is Searchlight?

Searchlight is a simple and safe query language for API design.  It works well with REST, provides complex features, and is easier to learn than GraphQL.  
At the same time, Searchlight is safe from SQL injection attacks.  By using Searchlight, you gain the ability to offer your customers robust query functionality
while still being able to ensure that you are only executing query plans that have been rigorously validated and that all customer input data parameterized. 

An example API with Searchlight looks like this:

```
GET /customers/?query=CreatedDate gt '2019-01-01' and (IsApproved = false OR (approvalCode IS NULL AND daysWaiting between 5 and 10))
```

Searchlight uses type checking, validation, and parsing to convert this into an abstract syntax tree (AST) representing search clauses and parameters.  You can 
then convert that AST into various forms and execute it on an SQL database, an in-memory object collection using LINQ, a MongoDB database, or so on.  To ensure
that no risky text is passed to your database, Searchlight reconstructs a completely new SQL query from string constants defined in your classes, and adds
parameters as appropriate.  All field names are converted from "customer-visible" field names to "actual database" names.  The above query would be transformed 
to the following:

```
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

```
[SearchlightModel]
public class MyAccount
{
    [SearchlightField]
    public string AccountName { get; set; }
    [SearchlightField]
    public DateTime Created { get; set; }

    // This field will not be searchable
    public string SecretKey { get; set; }
}
```

When someone queries your API, Searchlight can transform their query into a SQL or LINQ statement:

```
var list = new List<MyAccount>();
var query = src.Parse("AccountName startswith 'alice' and Created gt '2019-01-01'");

// To execute via SQL, this function gives you a parameterized SQL statement
var sql = SqlExecutor.RenderSQL(_source, query);
... execute SQL via whatever method you prefer ...

// To execute via an in-memory object collection
var results = LinqExecutor.QueryCollection<EmployeeObj>(src, query.filter, list);
```

# What if a developer makes a mistake when querying?

Searchlight provides detailed error messages that explicitly indicate what was wrong about the customer's query string.

* EmptyClause - When the user sends a query for an empty open/close parenthesis, like "()".
* ExpectedConjunction - When the user connects two criteria with something other than AND / OR.
* FieldNotFound - The query specified a field whose name could not be found.
* FieldTypeMismatch - The user tried to compare a string field with an integer, for example.
* OpenClause - The query had an open parenthesis with no closing parenthesis.
* InvalidToken - The parser expected a specific token but something invalid was provided.
* TooManyParameters - If the user attempts to overwhelm the system by querying too many fields.
* TrailingConjunction - The query ended with the word "AND" or "OR" but nothing after it.
* UnterminatedString - If a value is missing its end quote.

With these errors, your API can give direct and useful feedback to developers as they craft their interfaces.

# What if my data model changes over time?

Searchlight provides for aliases so that you can maintain backwards c

# Constructing Searchlight models programmatically

Constructing a model manually works as follows:

```
var source = new SearchlightDataSource();
source.ColumnDefinitions = new CustomColumnDefinition()
    .WithColumn("a", typeof(String), null)
    .WithColumn("b", typeof(Int32), null)
    .WithColumn("colLong", typeof(Int64), null)
    .WithColumn("colNullableGuid", typeof(Nullable<Guid>), null)
    .WithColumn("colULong", typeof(UInt64), null)
    .WithColumn("colNullableULong", typeof(Nullable<UInt64>), null)
    .WithColumn("colGuid", typeof(Guid), null);
source.Columnifier = new NoColumnify();
source.MaximumParameters = 200;
source.DefaultSortField = "a";
```