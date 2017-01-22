namespace Searchlight.Query
{
    public class SortInfo
    {
        public SortInfo(string fieldName, SortDirection direction)
        {
            Fieldname = fieldName;
            Direction = direction;
        }

        public readonly string Fieldname;
        public readonly SortDirection Direction;

        protected bool Equals(SortInfo other)
        {
            return string.Equals(Fieldname, other.Fieldname) && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SortInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Fieldname != null ? Fieldname.GetHashCode() : 0)*397) ^ (int) Direction;
            }
        }
    }
}