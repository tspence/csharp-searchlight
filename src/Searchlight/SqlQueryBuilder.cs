using System.Collections.Generic;
using System.Text;

namespace Searchlight {
    public class SQLQueryBuilder
    {
        public SQLQueryBuilder(DataSource src)
        {
            _source = src;
        }
        
        private DataSource _source;
        private readonly StringBuilder _whereClause = new StringBuilder();
        private readonly StringBuilder _orderByClause = new StringBuilder();
        
        public string WhereClause => _whereClause.ToString();
        public string OrderByClause => _orderByClause.ToString();

        public readonly Dictionary<string, object> Parameters = new Dictionary<string, object>();

        public string AddParameter(object p)
        {
            int num = Parameters.Count + 1;
            var name = $"@p{num}";
            Parameters.Add(name, p);
            return name;
        }

        public void AppendWhereClause(string s)
        {
            _whereClause.Append(s);
        }

        public void AppendOrderByClause(string s)
        {
            _orderByClause.Append(s);
        }

        public override string ToString()
        {
            var where = _whereClause.Length > 0 ? $" WHERE {_whereClause}" : "";
            var order = _orderByClause.Length > 0 ? $" ORDER BY {_orderByClause}" : "";
            return $"SELECT * FROM {_source.TableName} {where} {order}";
        }
    }
}