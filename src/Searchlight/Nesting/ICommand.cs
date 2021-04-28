namespace Searchlight.Nesting
{
    public interface ICommand
    {
        /// <summary>
        /// Returns true if this command matches a specific name
        /// </summary>
        /// <param name="commandName">The name to test</param>
        /// <returns>True if the command matches</returns>
        public bool MatchesName(string commandName);

        /// <summary>
        /// If there is an SQL component to this command, apply it here
        /// </summary>
        /// <param name="sql"></param>
        public void Apply(SqlQuery sql);

        /// <summary>
        /// If this command wants to modify the fetch request, apply changes here
        /// </summary>
        public void Preview(FetchRequest request);
    }
}