﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Exceptions
{
    /// <summary>
    /// A conjunction (AND or OR) was expected, but something else was found
    /// </summary>
    public class ExpectedConjunctionException : SearchlightException
    {
        public ExpectedConjunctionException(string found, string originalFilter)
            : base(originalFilter)
        {
            FoundToken = found;
        }

        public string FoundToken { get; set; }
    }
}
