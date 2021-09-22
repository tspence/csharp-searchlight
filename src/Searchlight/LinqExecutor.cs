using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Searchlight.Query;
using System.Linq.Dynamic.Core;

namespace Searchlight
{
    public static class LinqExecutor
    {
        /// <summary>
        /// Run the query represented by this syntax tree against an in-memory collection using LINQ
        /// </summary>
        /// <param name="tree">The syntax tree of the query to execute</param>
        /// <param name="collection">The collection of data elements to query</param>
        /// <typeparam name="T">Generic type of the model</typeparam>
        /// <returns></returns>
        public static IEnumerable<T> QueryCollection<T>(this SyntaxTree tree, IEnumerable<T> collection)
        {
            // Goal of this function is to construct this LINQ expression:
            //   return (from obj in LIST where QUERY select obj)
            // Here's how we'll do it:
            var queryable = collection.AsQueryable<T>();

            // Construct a linq "select" expression (
            ParameterExpression select = Expression.Parameter(typeof(T), "obj");

            // If the user specified a filter 
            var filtered = queryable;
            Expression expression = BuildExpression(select, tree.Filter, tree.Source);
            if (expression != null)
            {
                // Convert that to a "where" method call
                var whereCallExpression = Expression.Call(
                    typeof(Queryable),
                    "Where",
                    new Type[] {queryable.ElementType},
                    queryable.Expression,
                    Expression.Lambda<Func<T, bool>>(expression, new ParameterExpression[] {select}));
                // Obtain a queryable interface
                filtered = queryable.Provider.CreateQuery<T>(whereCallExpression);
            }


            string sortExpression = "";
            
            foreach (SortInfo order in tree.OrderBy)
            {
                var column = order.Column.FieldName;
                var direction = order.Direction;
                if (sortExpression != "")
                {
                    sortExpression += ", ";
                }
                
                sortExpression += column + " " + ((direction == SortDirection.Ascending) ? "ASC" : "DESC");
            }
            Console.WriteLine(sortExpression);
            var ordered = filtered.AsQueryable().OrderBy(sortExpression);

            return ordered;
        }

        /// <summary>
        /// Build a complex expression from a list of clauses
        /// </summary>
        /// <param name="select"></param>
        /// <param name="query"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Expression BuildExpression(ParameterExpression select, List<BaseClause> query, DataSource src)
        {
            ConjunctionType ct = ConjunctionType.NONE;
            Expression result = null;
            foreach (var clause in query)
            {
                var clauseExpression = BuildOneExpression(select, clause, src);

                // First clause starts a run
                if (result == null)
                {
                    result = clauseExpression;

                    // If the previous clause specified 'and'
                }
                else if (ct == ConjunctionType.AND)
                {
                    result = Expression.And(result, clauseExpression);

                    // If the previous clause specified 'or'
                }
                else if (ct == ConjunctionType.OR)
                {
                    result = Expression.Or(result, clauseExpression);
                }

                ct = clause.Conjunction;
            }

            // Here's your expression
            return result;
        }

        /// <summary>
        /// Build one expression from a clause
        /// </summary>
        /// <param name="select"></param>
        /// <param name="clause"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Expression BuildOneExpression(ParameterExpression select, BaseClause clause, DataSource src)
        {
            Expression field;
            Expression value;

            switch (clause)
            {
                case CriteriaClause criteria:
                    // Obtain a parameter from this object
                    field = Expression.Property(@select, criteria.Column.FieldName);
                    value = Expression.Constant(criteria.Value, criteria.Column.FieldType);
                    switch (criteria.Operation)
                    {
                        case OperationType.Equals:
                            if (field.Type == typeof(string))
                            {
                                return Expression.Call(null,
                                    typeof(string).GetMethod("Equals", new Type[] { typeof(string), typeof(string), typeof(StringComparison) }),
                                    field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                            } else
                            {
                                return Expression.Equal(field, value);
                            }
                        case OperationType.GreaterThan:
                            if (field.Type == typeof(string))
                            {
                                return Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.GreaterThan(Expression.Call(null,
                                            typeof(string).GetMethod("Compare", 
                                                new Type[] {typeof(string), typeof(string), typeof(StringComparison)}),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                return Expression.GreaterThan(field, value);
                            }
                        case OperationType.GreaterThanOrEqual:
                            if (field.Type == typeof(string))
                            {
                                return Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.GreaterThanOrEqual(Expression.Call(null,
                                            typeof(string).GetMethod("Compare", 
                                                new Type[] {typeof(string), typeof(string), typeof(StringComparison)}),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                return Expression.GreaterThanOrEqual(field, value);
                            }
                        case OperationType.LessThan:
                            if (field.Type == typeof(string))
                            {
                                return Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.LessThan(Expression.Call(null,
                                            typeof(string).GetMethod("Compare", 
                                                new Type[] {typeof(string), typeof(string), typeof(StringComparison)}),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                return Expression.LessThan(field, value);
                            }
                        case OperationType.LessThanOrEqual:
                            if (field.Type == typeof(string))
                            {
                                return Expression.And(Expression.NotEqual(field, Expression.Constant(null)),
                                    Expression.LessThanOrEqual(Expression.Call(null,
                                            typeof(string).GetMethod("Compare", 
                                                new Type[] {typeof(string), typeof(string), typeof(StringComparison)}),
                                            field, value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                        Expression.Constant(0)));
                            }
                            else
                            {
                                return Expression.LessThanOrEqual(field, value);
                            }
                        case OperationType.StartsWith:
                            return Expression.TryCatch(
                                Expression.Call(field,
                                    typeof(string).GetMethod("StartsWith", new Type[] {typeof(string), typeof(StringComparison)}), 
                                    value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                Expression.MakeCatchBlock(typeof(Exception), null,
                                    Expression.Constant(false, typeof(Boolean)), null)
                            );

                        case OperationType.EndsWith:
                            return Expression.TryCatch(
                                Expression.Call(field,
                                    typeof(string).GetMethod("EndsWith", new Type[] {typeof(string), typeof(StringComparison)}),
                                    value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                Expression.MakeCatchBlock(typeof(Exception), null,
                                    Expression.Constant(false, typeof(Boolean)), null)
                            );
                        case OperationType.Contains:
                            return Expression.TryCatch(
                                Expression.Call(field,
                                    typeof(string).GetMethod("Contains", new Type[] {typeof(string), typeof(StringComparison)}),
                                    value, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                                Expression.MakeCatchBlock(typeof(Exception), null,
                                    Expression.Constant(false, typeof(Boolean)), null)
                            );
                        case OperationType.NotEqual:
                            return Expression.NotEqual(field, value);
                        case OperationType.In:
                        case OperationType.IsNull:
                        case OperationType.Between:
                        case OperationType.Unknown:
                            // All three are implemented below and are not CriteriaClause objects
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();
                    }

                case BetweenClause betweenClause:
                    field = Expression.Property(@select, betweenClause.Column.FieldName);
                    Expression lowerValue =
                        Expression.Constant(betweenClause.LowerValue, betweenClause.Column.FieldType);
                    Expression upperValue =
                        Expression.Constant(betweenClause.UpperValue, betweenClause.Column.FieldType);
                    Expression lower = Expression.GreaterThanOrEqual(field, lowerValue);
                    Expression upper = Expression.LessThanOrEqual(field, upperValue);
                    return Expression.And(lower, upper);

                case CompoundClause compoundClause:
                    return BuildExpression(select, compoundClause.Children, src);

                case InClause inClause:
                    field = Expression.Convert(Expression.Property(@select, inClause.Column.FieldName), typeof(object));
                    value = Expression.Constant(inClause.Values, typeof(List<object>));
                    return Expression.Call(value,
                        typeof(List<object>).GetMethod("Contains", new Type[] {typeof(object)}),
                        field);

                case IsNullClause isNullClause:
                    field = Expression.Property(@select, isNullClause.Column.FieldName);
                    return Expression.Equal(field, Expression.Constant(null));

                default:
                    throw new NotImplementedException();
            }
        }
    }
}