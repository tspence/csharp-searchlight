#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Searchlight.Parsing
{
    public class Token
    {
        public string Value { get; set; }
        public int StartPosition { get; set; }

        public Token(string value, int position)
        {
            Value = value;
            StartPosition = position;
        }
    }
}