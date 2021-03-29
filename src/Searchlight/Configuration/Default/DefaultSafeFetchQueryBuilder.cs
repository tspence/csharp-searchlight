
namespace Searchlight.Configuration.Default
{
    public class DefaultSafeFetchQueryBuilder : ISafeFetchQueryBuilder
    {
        public string BuildSelectQuery(string selectColumns, string table, string filter, string orderby)
        {
            string query = "SELECT " + selectColumns + " FROM " + table;

            if (!string.IsNullOrEmpty(filter))
            {
                query += " WHERE " + filter;
            }

            if (!string.IsNullOrEmpty(orderby))
            {
                query += " " + orderby;
            }

            return query;
        }
    }
}

