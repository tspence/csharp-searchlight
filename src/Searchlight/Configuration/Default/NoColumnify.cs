using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Configuration.Default
{
    /// <summary>
    /// Do no transformation in the columnification
    /// </summary>
    public class NoColumnify : IColumnify
    {
        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="clauseType"></param>
        /// <returns></returns>
        public string Columnify(string columnName, ClauseType clauseType)
        {
            return columnName;
        }
    }
}
