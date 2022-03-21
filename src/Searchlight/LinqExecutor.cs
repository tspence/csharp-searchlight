using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Searchlight.Parsing;
using Searchlight.Query;

namespace Searchlight
{
    /// <summary>
    /// Class for executing Searchlight queries against in-memory collections using LINQ
    /// </summary>
    public static class LinqExecutor
    {
        /// <summary>
        /// Run the query represented by this syntax tree against an in-memory collection using LINQ
        /// </summary>
        /// <param name="tree">The syntax tree of the query to execute</param>
        /// <param name="collection">The collection of data elements to query</param>
        /// <typeparam name="T">Generic type of the model</typeparam>
        /// <returns></returns>
        public static FetchResult<T> QueryCollection<T>(this SyntaxTree tree, IEnumerable<T> collection)
        {
            // Goal of this function is to construct this LINQ expression:
            //   return (from obj in LIST where QUERY select obj)
            // Here's how we'll do it:
            var queryable = collection.AsQueryable();
            
            // If the user specified a filter 
            var select = Expression.Parameter(typeof(T), "obj");
            var expression = BuildExpression<T>(select, tree.Filter, tree.Source);
            if (expression != null)
            {
                var whereCallExpression = Expression.Call(
                    typeof(Queryable),
                    "Where",
                    new [] { queryable.ElementType },
                    queryable.Expression,
                    Expression.Lambda<Func<T, bool>>(expression, new ParameterExpression[] { select }));
                queryable = queryable.Provider.CreateQuery<T>(whereCallExpression);
            }

            // If the user specified a sorting clause
            if (tree.OrderBy.Any())
            {
                queryable = InternalOrderBy<T>(queryable, tree.Source, tree.OrderBy);
            }

            // Compute the list once and keep track of full length
            var filteredAndSorted = queryable.ToList();
            var totalCount = filteredAndSorted.Count;
            
            // If the user requested pagination
            IEnumerable<T> paginated = filteredAndSorted;
            if (tree.PageNumber > 0 && tree.PageSize > 0)
            {
                // case 1: user specified page number and page size
                paginated = filteredAndSorted.Skip((int)(tree.PageSize * tree.PageNumber)).Take((int)tree.PageSize);
            }
            else if (tree.PageNumber == 0 && tree.PageSize > 0)
            {
                // case 2: user specified a page size but no page number
                paginated = filteredAndSorted.Take((int)tree.PageSize);
            }

            // construct the return fetch result
            var result = new FetchResult<T>
            {
                pageSize = tree.PageSize,
                pageNumber = tree.PageNumber,
                totalCount = totalCount,
                records = paginated.ToArray()
            };

            return result;
        }
        
        private static IQueryable<T> InternalOrderBy<T>(IQueryable source, DataSource src, List<SortInfo> orderBy)
        {
            var queryExpr = source.Expression;
            var count = 0;
            ParameterExpression[] parameterExpressions =
            {
                Expression.Parameter(source.ElementType, "Param_0")
            };
            foreach (var sort in orderBy)
            {
                AssertClassHasProperty(typeof(T), src, sort.Column);
                var methodName = count == 0 ? "OrderBy" : "ThenBy";
                if (sort.Direction == SortDirection.Descending)
                {
                    methodName += "Descending";
                }
                var field = Expression.Property(parameterExpressions[0], sort.Column.FieldName);
                var quote = Expression.Quote(Expression.Lambda(field, parameterExpressions));
                queryExpr = Expression.Call(
                    typeof(Queryable), 
                    methodName,
                    new[] { source.ElementType, sort.Column.FieldType },
                    queryExpr, 
                    quote);
                count++;
            }

            return source.Provider.CreateQuery<T>(queryExpr);
        }

        /// <summary>
        /// Build a complex expression from a list of clauses
        /// </summary>
        /// <param name="select"></param>
        /// <param name="query"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Expression BuildExpression<T>(ParameterExpression select, List<BaseClause> query, DataSource src)
        {
            var ct = ConjunctionType.NONE;
            Expression result = null;
            foreach (var clause in query)
            {
                var clauseExpression = BuildOneExpression<T>(select, clause, src);
                if (result == null)
                {
                    result = clauseExpression;
                }
                else if (ct == ConjunctionType.AND)
                {
                    result = Expression.And(result, clauseExpression);
                }
                else if (ct == ConjunctionType.OR)
                {
                    result = Expression.Or(result, clauseExpression);
                }

                ct = clause.Conjunction;
            }
            return result;
        }

        /// <summary>
        /// Build one expression from a clause
        /// </summary>
        /// <param name="select"></param>
        /// <param name="clause"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Expression BuildOneExpression<T>(ParameterExpression select, BaseClause clause, DataSource src)
        {
            Expression field;
            Expression value;
            Expression result;

            var t = typeof(T);

            switch (clause)
            {
                case CriteriaClause criteria:
                    AssertClassHasProperty(t, src, criteria.Column);
                    
                    // Set up LINQ expressions for this object
                    var valueType = criteria.Column.FieldType;
                    field = Expression.Property(select, criteria.Column.FieldName);
                    value = Expression.Constant(criteria.Value, valueType);
                    switch (criteria.Operation)
                    {
                        case OperationType.Equals:
                            if (field.Type == typeof(string))
                            {
                                result = Expression.Call(null,
                                    typeof(string).GetMethod("Equals",
                                        new [] { typeof(string), typeof(string), typeof(StringComparison) }),
                                    field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                            }
                            else
                            {
                                result = Expression.Equal(field, value);
                            }
                            break;
                        case OperationType.GreaterThan:
                            if (field.Type == typeof(string))
                            {
                                result = Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.GreaterThan(Expression.Call(null,
                                            typeof(string).GetMethod("Compare",
                                                new []
                                                {
                                                    typeof(string), typeof(string), typeof(StringComparison)
                                                }),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                result = Expression.GreaterThan(field, value);
                            }
                            break;
                        case OperationType.GreaterThanOrEqual:
                            if (field.Type == typeof(string))
                            {
                                result = Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.GreaterThanOrEqual(Expression.Call(null,
                                            typeof(string).GetMethod("Compare",
                                                new []
                                                {
                                                    typeof(string), typeof(string), typeof(StringComparison)
                                                }),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                result = Expression.GreaterThanOrEqual(field, value);
                            }
                            break;
                        case OperationType.LessThan:
                            if (field.Type == typeof(string))
                            {
                                result = Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.LessThan(Expression.Call(null,
                                            typeof(string).GetMethod("Compare",
                                                new []
                                                {
                                                    typeof(string), typeof(string), typeof(StringComparison)
                                                }),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                result = Expression.LessThan(field, value);
                            }
                            break;
                        case OperationType.LessThanOrEqual:
                            if (field.Type == typeof(string))
                            {
                                result = Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.LessThanOrEqual(Expression.Call(null,
                                            typeof(string).GetMethod("Compare",
                                                new []
                                                {
                                                    typeof(string), typeof(string), typeof(StringComparison)
                                                }),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                result = Expression.LessThanOrEqual(field, value);
                            }
                            break;
                        case OperationType.StartsWith:
                            result = Expression.TryCatch(
                                Expression.Call(field,
                                    typeof(string).GetMethod("StartsWith",
                                        new [] { typeof(string), typeof(StringComparison) }),
                                    value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                Expression.MakeCatchBlock(typeof(Exception), null,
                                    Expression.Constant(false, typeof(Boolean)), null)
                            );
                            break;

                        case OperationType.EndsWith:
                            result = Expression.TryCatch(
                                Expression.Call(field,
                                    typeof(string).GetMethod("EndsWith",
                                        new [] { typeof(string), typeof(StringComparison) }),
                                    value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                Expression.MakeCatchBlock(typeof(Exception), null,
                                    Expression.Constant(false, typeof(Boolean)), null)
                            );
                            break;
                        case OperationType.Contains:
                            result = Expression.TryCatch(
                                Expression.Call(field,
                                    typeof(string).GetMethod("Contains",
                                        new [] { typeof(string), typeof(StringComparison) }),
                                    value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                Expression.MakeCatchBlock(typeof(Exception), null,
                                    Expression.Constant(false, typeof(Boolean)), null)
                            );
                            break;
                        case OperationType.NotEqual:
                            result = Expression.NotEqual(field, value);
                            break;
                        case OperationType.In:
                        case OperationType.IsNull:
                        case OperationType.Between:
                        case OperationType.Unknown:
                            // All three are implemented below and are not CriteriaClause objects
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case BetweenClause betweenClause:
                    field = Expression.Property(@select, betweenClause.Column.FieldName);
                    Expression lowerValue =
                        Expression.Constant(betweenClause.LowerValue, betweenClause.Column.FieldType);
                    Expression upperValue =
                        Expression.Constant(betweenClause.UpperValue, betweenClause.Column.FieldType);
                    Expression lower = Expression.GreaterThanOrEqual(field, lowerValue);
                    Expression upper = Expression.LessThanOrEqual(field, upperValue);
                    result = Expression.And(lower, upper);
                    break;

                case CompoundClause compoundClause:
                    result = BuildExpression<T>(select, compoundClause.Children, src);
                    break;

                case InClause inClause:
                    field = Expression.Convert(Expression.Property(@select, inClause.Column.FieldName), typeof(object));
                    value = Expression.Constant(inClause.Values, typeof(List<object>));
                    result = Expression.Call(value,
                        typeof(List<object>).GetMethod("Contains", new [] { typeof(object) }),
                        field);
                    break;

                case IsNullClause isNullClause:
                    field = Expression.Property(@select, isNullClause.Column.FieldName);
                    result = Expression.Equal(field, Expression.Constant(null));
                    break;

                default:
                    throw new NotImplementedException();
            }

            // Negate the final expression if specified
            return clause.Negated ? Expression.Not(result) : result;
        }

        /// <summary>
        /// Test this class to make sure it has the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="source"></param>
        /// <param name="column"></param>
        /// <exception cref="FieldTypeMismatch"></exception>
        /// <exception cref="FieldNotFound"></exception>
        private static void AssertClassHasProperty(Type type, DataSource source, ColumnInfo column)
        {
            // If the types match, we've already verified it during engine definition
            if (source.ModelType == type) return;
            
            // If the types don't match, we need to verify at runtime or throw a clear exception
            var propInfo = type.GetProperty(column.FieldName);
            if (propInfo != null)
            {
                if (propInfo.PropertyType != column.FieldType)
                {
                    throw new FieldTypeMismatch()
                    {
                        FieldName = $"{column.FieldName} on type {type.Name}",
                        FieldType = propInfo.PropertyType.ToString(),
                    };
                }
            }
            else
            {
                throw new FieldNotFound()
                {
                    FieldName = column.FieldName,
                    KnownFields = (from p in type.GetProperties() select p.Name).ToArray()
                };
            }
        }
    }
}