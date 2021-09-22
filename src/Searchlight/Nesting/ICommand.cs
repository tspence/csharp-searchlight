namespace Searchlight.Nesting
{
    public interface ICommand
    {
        /// <summary>
        /// If there is an SQL component to this command, apply it here
        /// </summary>
        /// <param name="sql"></param>
        public void Apply(SqlQuery sql);

        /// <summary>
        /// The official name by which this command is known
        /// </summary>
        /// <returns></returns>
        public string GetName();

        /// <summary>
        /// List all names by which this command is known
        /// </summary>
        /// <returns></returns>
        public string[] GetAliases();
    }
}