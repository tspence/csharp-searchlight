using System;
using System.Collections.Generic;

namespace Searchlight.Query
{
    public class SelectClause
    {
        public List<string> SelectFieldList { get; set; }
        public List<string> SubtableList { get; set; }

        public string Expression { get { return string.Join(", ", SelectFieldList); } }

        protected bool Equals(SelectClause other)
        {
            return Utility.ListEquals(SelectFieldList, other.SelectFieldList, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SelectClause) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SelectFieldList != null ? SelectFieldList.GetHashCode() : 0)*397);
            }
        }
    }
}