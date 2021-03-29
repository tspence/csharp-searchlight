
namespace Searchlight.Configuration
{
    public interface ISafeFetchQueryBuilder
    {
        string BuildSelectQuery(string selectColumns, string table, string filter, string orderby);
    }
}

