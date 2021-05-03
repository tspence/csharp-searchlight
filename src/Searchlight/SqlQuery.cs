using System.Collections.Generic;
using System.Text;
using Searchlight.Query;

namespace Searchlight {
    public class SqlQuery
    {
        public SyntaxTree Syntax { get; set; }
        
        public SqlQuery()
        {
            Parameters = new Dictionary<string, object>();
            ResultSetClauses = new List<string>();
        }

        public string CommandText { get; set; }

        public Dictionary<string, object> Parameters { get; }

        public string WhereClause { get; set;  }
        public string OrderByClause { get; set;  }
        public List<string> ResultSetClauses { get; }

        internal string AddParameter(object p)
        {
            var num = Parameters.Count + 1;
            var name = $"@p{num}";
            Parameters.Add(name, p);
            return name;
        }
    }
}