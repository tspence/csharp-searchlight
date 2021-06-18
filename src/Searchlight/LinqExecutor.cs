using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Searchlight.Query;

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

            // Construct a linq "filter" expression 
            Expression expression = BuildExpression(select, tree.Filter, tree.Source);

            // Convert that to a "where" method call
            var whereCallExpression = Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] {queryable.ElementType},
                queryable.Expression,
                Expression.Lambda<Func<T, bool>>(expression, new ParameterExpression[] {select}));

            // Obtain a queryable interface
            return queryable.Provider.CreateQuery<T>(whereCallExpression);
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
            // Check if this is a basic criteria clause
            var criteria = clause as CriteriaClause;
            if (criteria != null)
            {
                // Obtain a parameter from this object
                Expression field = Expression.Property(select, criteria.Column.FieldName);
                Expression value = Expression.Constant(criteria.Value, criteria.Column.FieldType);
                switch (criteria.Operation)
                {
                    case OperationType.Equals:
                        return Expression.Equal(field, value);
                    case OperationType.GreaterThan:
                        return Expression.GreaterThan(field, value);
                    case OperationType.GreaterThanOrEqual:
                        return Expression.GreaterThanOrEqual(field, value);
                    case OperationType.LessThan:
                        return Expression.LessThan(field, value);
                    case OperationType.LessThanOrEqual:
                        return Expression.LessThanOrEqual(field, value);
                    case OperationType.StartsWith:
                        return Expression.TryCatch(
                            Expression.Call(field,
                                typeof(string).GetMethod("StartsWith", new Type[] {typeof(string)}), value),
                            Expression.MakeCatchBlock(typeof(Exception), null,
                                Expression.Constant(false, typeof(Boolean)), null)
                        );

                    case OperationType.EndsWith:
                        return Expression.TryCatch(
                            Expression.Call(field,
                                typeof(string).GetMethod("EndsWith", new Type[] {typeof(string)}), value),
                            Expression.MakeCatchBlock(typeof(Exception), null,
                                Expression.Constant(false, typeof(Boolean)), null)
                        );
                    case OperationType.Contains:
                        return Expression.TryCatch(
                            Expression.Call(field, typeof(string).GetMethod("Contains", new Type[] {typeof(string)}),
                                value),
                            Expression.MakeCatchBlock(typeof(Exception), null,
                                Expression.Constant(false, typeof(Boolean)), null)
                        );
                    case OperationType.NotEqual:
                        return Expression.NotEqual(field, value);
                    case OperationType.In:
                        //implemented below, not a CriteriaClause
                    case OperationType.IsNull:
                    case OperationType.Between:
                        throw new NotImplementedException();
                }
            }

            // Is this a between clause?
            var between = clause as BetweenClause;
            if (between != null)
            {
                Expression field = Expression.Property(select, between.Column.FieldName);
                Expression lowerValue = Expression.Constant(between.LowerValue, between.Column.FieldType);
                Expression upperValue = Expression.Constant(between.UpperValue, between.Column.FieldType);
                Expression lower = Expression.GreaterThanOrEqual(field, lowerValue);
                Expression upper = Expression.LessThanOrEqual(field, upperValue);
                return Expression.And(lower, upper);
            }

            // Check if this is a compound clause and build it nested
            var compound = clause as CompoundClause;
            if (compound != null)
            {
                return BuildExpression(select, compound.Children, src);
            }

            var inClause = clause as InClause;
            if (inClause != null)
            {
                Expression field = Expression.Property(select, inClause.Column.FieldName);
                Expression value = Expression.Constant(inClause.Values, typeof(List<object>));
                return Expression.Call(value, typeof(List<object>).GetMethod("Contains", new Type[] {typeof(object)}),
                    field);
            }
            
            // We didn't understand the clause!
            throw new NotImplementedException();
        }
    }
}