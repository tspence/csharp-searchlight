using System.Collections.Generic;

namespace Searchlight.Nesting
{
    /// <summary>
    /// An extension to the searchlight "include" system that allows you to fetch optional elements
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// If there is an SQL component to this command, apply it here
        /// </summary>
        /// <param name="sql"></param>
        void Apply(SqlQuery sql);

        /// <summary>
        /// The official name by which this command is known
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// List all names by which this command is known
        /// </summary>
        /// <returns></returns>
        string[] GetAliases();
    }
}