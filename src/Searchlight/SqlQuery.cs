using System.Collections.Generic;
using System.Text;

namespace Searchlight {
    public class SqlQuery
    {
        public SqlQuery()
        {
            Parameters = new Dictionary<string, object>();
            WhereClause = new StringBuilder();
            OrderByClause = new StringBuilder();
        }

        public string CommandText { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        public StringBuilder WhereClause { get; set; }
        public StringBuilder OrderByClause { get; set; }

        internal string AddParameter(object p)
        {
            int num = Parameters.Count + 1;
            var name = $"@p{num}";
            Parameters.Add(name, p);
            return name;
        }
    }
}