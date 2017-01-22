using Searchlight.Query;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Nesting
{
    public class OptionalCommand
    {
        /// <summary>
        /// This list of strings represents the names by which this command will be known
        /// </summary>
        public virtual string[] CommandNames { get; protected set; }

        /// <summary>
        /// If this value is set by the derived class, this will get added to the SQL statement
        /// </summary>
        public virtual string SqlStatement { get; protected set; }

        /// <summary>
        /// Attempt to match the supplied command name against ourselves
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public virtual bool IsNameMatch(string commandName)
        {
            // If this command matches our designators, this is a match and we are included
            if (CommandNames != null) {
                foreach (var cn in CommandNames) {
                    if (String.Equals(commandName, cn, StringComparison.OrdinalIgnoreCase)) {
                        IsIncluded = true;
                        return true;
                    }
                }
            }

            // Nope, no match
            return false;
        }

        /// <summary>
        /// If there is an SQL component to this command, apply it here
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dp"></param>
        public virtual void ApplySql(StringBuilder sql, DynamicParameters dp)
        {
            // Behavior can be overridden in the derived class
            if (!String.IsNullOrWhiteSpace(SqlStatement)) {
                sql.AppendLine(SqlStatement);
            }
        }

        /// <summary>
        /// Modify the results being output
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        public virtual void ApplyResults<T>(FetchResult<T> results)
        {
            // By default do nothing
        }

        /// <summary>
        /// If this command wants to modify the where clause
        /// </summary>
        public virtual void Preview(FetchRequest request)
        {
            // By default take no action
        }

        /// <summary>
        /// The select clause will set this value to true if this object is included
        /// </summary>
        public virtual bool IsIncluded { get; set; }
    }
}
