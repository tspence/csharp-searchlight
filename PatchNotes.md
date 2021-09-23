# 0.9.10
September 23, 2021

* Fixed bug with foreign table SQL requesting data for multiple tables
* Added tests to verify that multiple searchlight flags work as expected
* Fixed issue where a searchlight flag without aliases caused a crash

# 0.9.8
September 23, 2021

Fixed compile issue where LINQ dynamic core was not referenced correctly

# 0.9.7
September 22, 2021

Searchlight queries now support date shorthand: TODAY, TOMORROW, YESTERDAY can be used as values for date criteria.
This means you can now write queries such as `date between yesterday and tomorrow`.  We may add more support for
date constants in the future.

* LINQ executor sorts results according to the same logic as the SQL executor
* LINQ executor works correctly if you send in a null or filter statement
* Recognizes "ascending" and "descending" in addition to "asc" and "desc" for sort orders
* Removed references to Dapper, which is no longer required to compile

# 0.9.6
September 19, 2021

* LINQ executor now uses case insensitive string comparison correctly

# 0.9.5
September 19, 2021

* Added support for SearchlightFlags, a system you can use to detect options selected via the "$include" parameter
* Added tests for flags and refactored the CommandCollection system for faster processing

# 0.9.4
August 16, 2021

* Fix issue where `FieldName in ()` could result in a null pointer exception

# 0.9.3
July 20, 2021

* LINQ executor can now handle comparatives for strings such as >, >=, GTE, and so on

# 0.9.2 
June 23, 2021

* LINQ executor can handle `in` cluases for intrinsic data types with boxing
* Added tests for decimals and integer `in` clause queries
* Modified string operations to be case insensitive

# 0.9.1 
June 19, 2021

* Improvements to LINQ executor to handle null values properly
* Added support for In and NotEqual to LINQ executor
* More tests

# 0.9.0
June 16, 2021

* Restrict the Contains, StartsWith, and EndsWith operators to work on strings

# 0.8.8
April 28, 2021

* Bug fix for FetchRequest.append

# 0.8.7 
April 28, 2021

* Update the method signature for LINQ querying to be similar to SQL.
* Add foreign key functionality via SearchlightEngine