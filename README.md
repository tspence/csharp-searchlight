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

You can construct searchlight models using the Searchlight data annotation, or you can construct a model manually.

```

```

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