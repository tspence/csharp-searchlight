﻿#pragma warning disable CS1591
namespace Searchlight
{
    /// <summary>
    /// The Searchlight model had two different included data sets with the same name.  Searchlight
    /// requires that all included data sets have unique names.
    /// </summary>
    public class DuplicateInclude : SearchlightException
    {
        public string Table { get; internal set; }
        public string ConflictingIncludeName { get; internal set; }
        public string ErrorMessage { 
            get => $"The Searchlight model {Table} had two different included data sets with the name '{ConflictingIncludeName}'.";
        }
    }
}