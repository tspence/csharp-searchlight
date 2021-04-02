using System.Collections.Generic;
using System.Text;

namespace Searchlight {
    public class SQLQueryBuilder
    {
        private StringBuilder _sb = new StringBuilder();
        public string whereClause
        {
            get
            {
                return _sb.ToString();
            }
        }
        public Dictionary<string, object> parameters = new Dictionary<string, object>();

        public string AddParameter(object p)
        {
            int num = parameters.Count + 1;
            var name = $"@p{num}";
            parameters.Add(name, p);
            return name;
        }

        public void AppendString(string s)
        {
            _sb.Append(s);
        }
    }
}