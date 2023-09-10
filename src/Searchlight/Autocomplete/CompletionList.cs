using System.Collections.Generic;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Searchlight.Autocomplete
{
    /// <summary>
    /// Intended to be as similar as possible to Language Server Protocol 3.17
    /// </summary>
    public class CompletionList
    {
        public bool isIncomplete { get; set; }
        public List<CompletionItem> items { get; set; }
    }

    public class CompletionItem
    {
        public string label { get; set; }
        public CompletionItemKind? kind { get; set; }
        public string detail { get; set; }
        public bool deprecated { get; set; }
    }
    
    public enum CompletionItemKind {
        Text = 1,
        Method = 2,
        Function = 3,
        Constructor = 4,
        Field = 5,
        Variable = 6,
        Class = 7,
        Interface = 8,
        Module = 9,
        Property = 10,
        Unit = 11,
        Value = 12,
        Enum = 13,
        Keyword = 14,
        Snippet = 15,
        Color = 16,
        File = 17,
        Reference = 18,
        Folder = 19,
        EnumMember = 20,
        Constant = 21,
        Struct = 22,
        Event = 23,
        Operator = 24,
        TypeParameter = 25,
    }
}