using Dapper;

namespace Searchlight.Query
{
    public class WhereClause
    {
        public string ValidatedFilter { get; set; }
        public DynamicParameters SqlParameters { get; set; }
    }
}