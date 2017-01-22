using Searchlight.Parsing;

namespace Searchlight.Configuration
{
    /// <summary>
    /// Performs any necessary conversion of a column name before it is added to a SQL command
    /// </summary>
    public interface IColumnify
    {
        string Columnify(string columnName, ClauseType clauseType);
    }
}