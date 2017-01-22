using System;
using System.Collections.Generic;

namespace Searchlight.Query
{
    class Utility
    {
        public static bool ListEquals(List<string> list1, List<string> list2, StringComparison stringComparison)
        {
            if (ReferenceEquals(list1, null) && ReferenceEquals(list2, null)) return true;
            if (ReferenceEquals(null, list1)) return false;
            if (ReferenceEquals(null, list2)) return false;
            if (ReferenceEquals(list1, list2)) return true;
            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!string.Equals(list1[i], list2[i], stringComparison)) return false;
            }

            return true;
        }

        public static bool ListEquals(List<SortInfo> list1, List<SortInfo> list2, StringComparison stringComparison)
        {
            if (ReferenceEquals(list1, null) && ReferenceEquals(list2, null)) return true;
            if (ReferenceEquals(null, list1)) return false;
            if (ReferenceEquals(null, list2)) return false;
            if (ReferenceEquals(list1, list2)) return true;
            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!string.Equals(list1[i].Fieldname, list2[i].Fieldname, stringComparison) ||
                    list1[i].Direction != list2[i].Direction) return false;
            }

            return true;
        }
    }
}
