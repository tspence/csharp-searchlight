using System;
using System.Collections.Generic;

namespace Searchlight.Query
{
    public class OrderByClause
    {
        public List<SortInfo> SortInfoList { get; set; }
        public string Expression { get; set; }

        protected bool Equals(OrderByClause other)
        {
            return string.Equals(Expression, other.Expression) &&
                Utility.ListEquals(SortInfoList, other.SortInfoList, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderByClause) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SortInfoList != null ? SortInfoList.GetHashCode() : 0)*397) ^ (Expression != null ? Expression.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return string.Join(", ", SortInfoList) + " EXP: " + Expression;
        }
    }
}