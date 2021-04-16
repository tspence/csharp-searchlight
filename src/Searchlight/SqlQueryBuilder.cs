using System.Collections.Generic;
using System.Text;

namespace Searchlight {
    public class SQLQueryBuilder
    {
        public DataSource Source;
        private readonly StringBuilder _sb = new StringBuilder();
        
        public string whereClause
        {
            get
            {
                if (_sb.Length > 0)
                {
                    return " WHERE " + _sb.ToString();
                }

                return "";
            }
        }

        public string orderByClause
        {
            get
            {
                return "";
            }
        }
        
        public readonly Dictionary<string, object> Parameters = new Dictionary<string, object>();

        public string AddParameter(object p)
        {
            int num = Parameters.Count + 1;
            var name = $"@p{num}";
            Parameters.Add(name, p);
            return name;
        }

        public void AppendString(string s)
        {
            _sb.Append(s);
        }

        public override string ToString()
        {
            return $"SELECT * FROM {Source.TableName} {whereClause} {orderByClause}";
        }
    }
}