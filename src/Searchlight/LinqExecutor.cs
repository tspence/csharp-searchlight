using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Searchlight.Query;

namespace Searchlight
{
    public class LinqExecutor
    {
        /// <summary>
        /// Query against a collection in memory
        /// </summary>
        /// <typeparam name="T">Underlying data type being queried</typeparam>
        /// <param name="src">Information about the data source</param>
        /// <param name="query">The query commands</param>
        /// <param name="list">The source enumerable</param>
        /// <returns></returns>
        public static IEnumerable<T> QueryCollection<T>(DataSource src, List<BaseClause> query, IEnumerable<T> list)
        {
            // Goal of this function is to construct this LINQ expression:
            //   return (from obj in LIST where QUERY select obj)
            // Here's how we'll do it:
            var queryable = list.AsQueryable<T>();

            // Construct a linq "select" expression (
            ParameterExpression select = Expression.Parameter(typeof(T), "obj");

            // Construct a linq "filter" expression 
            Expression expression = BuildExpression(select, query, src);

            // Convert that to a "where" method call
            var whereCallExpression = Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] { queryable.ElementType },
                queryable.Expression,
                Expression.Lambda<Func<T, bool>>(expression, new ParameterExpression[] { select }));

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
                        return Expression.Call(field, typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) }), value);
                    case OperationType.EndsWith:
                        return Expression.Call(field, typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) }), value);
                    case OperationType.Contains:
                        return Expression.Call(field, typeof(string).GetMethod("Contains", new Type[] { typeof(string) }), value);
                    case OperationType.In:
                    case OperationType.IsNull:
                    case OperationType.NotEqual:
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

            // We didn't understand the clause!
            throw new NotImplementedException();
        }
    }
}
