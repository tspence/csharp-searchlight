#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Searchlight.Query
{
    public class MalformedClause : BaseClause
    {
        public override string ToString()
        {
            return "(malformed)";
        }
    }
}