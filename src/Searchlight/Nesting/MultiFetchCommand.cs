using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Searchlight.Query;
using System.Text;

namespace Searchlight.Nesting
{
    public class MultiFetchCommand<PARENT_PRIMARY_KEY, PARENT> : OptionalCommand
    {
        /// <summary>
        /// Override this function to define behavior for this optional fetch command
        /// </summary>
        /// <param name="parentList"></param>
        /// <param name="parentDict"></param>
        /// <param name="reader"></param>
        public virtual void ExecuteCommand(List<PARENT> parentList, Dictionary<PARENT_PRIMARY_KEY, PARENT> parentDict, SqlMapper.GridReader reader)
        {
        }
    }
}
