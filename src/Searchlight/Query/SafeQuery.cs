using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Searchlight.DataSource;
using Searchlight.Parsing;

namespace Searchlight.Query
{
    public class SafeQuery
    {
        /// <summary>
        /// Query against a collection in memory
        /// </summary>
        /// <typeparam name="T">Underlying data type being queried</typeparam>
        /// <param name="src">Information about the data source</param>
        /// <param name="query">The query commands</param>
        /// <param name="list">The source enumerable</param>
        /// <returns></returns>
        public static IEnumerable<T> QueryCollection<T>(SearchlightDataSource src, List<BaseClause> query, IEnumerable<T> list)
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
        private static Expression BuildExpression(ParameterExpression select, List<BaseClause> query, SearchlightDataSource src)
        {
            ConjunctionType ct = ConjunctionType.NONE;
            Expression result = null;
            foreach (var clause in query) {
                var clauseExpression = BuildOneExpression(select, clause, src);

                // First clause starts a run
                if (result == null) {
                    result = clauseExpression;

                // If the previous clause specified 'and'
                } else if (ct == ConjunctionType.AND) {
                    result = Expression.And(result, clauseExpression);

                // If the previous clause specified 'or'
                } else if (ct == ConjunctionType.OR) {
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
        private static Expression BuildOneExpression(ParameterExpression select, BaseClause clause, SearchlightDataSource src)
        {
            if (clause is CriteriaClause) {
                var c = clause as CriteriaClause;

                // Obtain a parameter from this object
                Expression field = Expression.Property(select, c.Column.FieldName);
                Expression value = Expression.Constant(c.Value, c.Column.FieldType);
                switch (c.Operation) {
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
                    case OperationType.Between:
                    case OperationType.Contains:
                    case OperationType.EndsWith:
                    case OperationType.In:
                    case OperationType.IsNull:
                    case OperationType.Like:
                    case OperationType.NotEqual:
                    case OperationType.StartsWith:
                        throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }
    }
}
