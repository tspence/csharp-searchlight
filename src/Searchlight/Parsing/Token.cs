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