using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Exceptions
{
    /// <summary>
    /// This table contained an invalid default sort value.  Searchlight requires that all models contain
    /// a default sorting value so that pagination can work reliably even when the user does not specify
    /// a sorting order preference.
    /// </summary>
    public class InvalidDefaultSort : SearchlightException
    {
        public string Table { get; internal set; }
        public string DefaultSort { get; internal set; }
        public string ErrorMessage
        {
            get =>
                $"The table {Table} contained an invalid default sort value [{DefaultSort}]. Searchlight requires that all models contain a default sorting" +
                "value so that pagination can work reliably even when the user does not specify a sorting order preference.";
        }
    }
}