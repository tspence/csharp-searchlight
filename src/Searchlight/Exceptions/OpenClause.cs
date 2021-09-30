
namespace Searchlight
{
    /// <summary>
    /// The query had a different number of open parenthesis and closing parenthesis.
    /// 
    /// Example: `(name eq Alice`
    /// </summary>
    public class OpenClause : SearchlightException
    {
        public string OriginalFilter { get; internal set; }

        public string ErrorMessage { get; internal set;  } =
            "The query had a different number of open parenthesis and closing parenthesis. Example: `(name eq Alice`";
    }
}
