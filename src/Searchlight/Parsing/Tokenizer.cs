using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Searchlight.Parsing
{
    /// <summary>
    /// Parse tokens out of a line
    /// </summary>
    public static class Tokenizer
    {
        /// <summary>
        /// Generate tokens out of a line
        /// </summary>
        /// <param name="line">Original text</param>
        /// <returns>The token stream to consume</returns>
        public static TokenStream GenerateTokens(string line)
        {
            var tokens = new TokenStream()
            {
                OriginalText = line,
                TokenQueue = new Queue<Token>(),
                HasUnterminatedLiteral = false,
            };
            var sb = new StringBuilder();

            // Go through each character
            var i = 0;
            var inToken = false;
            var inQuotes = false;
            while (i < line.Length)
            {
                var c = line[i];

                if (c.Equals('"'))
                {
                    inQuotes = !inQuotes;
                }
                else if (inQuotes)
                {
                    sb.Append(c);
                    inToken = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    // Whitespace characters always end a token)
                    if (inToken)
                    {
                        tokens.TokenQueue.Enqueue(new Token(sb.ToString(), i - sb.Length));
                        sb.Length = 0;
                        inToken = false;
                    }
                }
                else if (IsSpecialChar(c))
                {
                    // If this is one of the special chars (>, =, etc) end the previous token and count char as its own token
                    if (inToken)
                    {
                        tokens.TokenQueue.Enqueue(new Token(sb.ToString(), i - sb.Length));
                        inToken = false;
                    }

                    // If the token is actually part of a >= or <= block, add the equal sign to it.
                    var c2 = (i + 1 < line.Length) ? (char?)line[i + 1] : null;
                    if ((c == '!' && c2 == '=') ||
                        (c == '<' && c2 == '>') ||
                        (c == '<' && c2 == '=') ||
                        (c == '>' && c2 == '='))
                    {
                        tokens.TokenQueue.Enqueue(new Token(line.Substring(i, 2), i));
                        i++;
                    }
                    else if (c == '<' || c == '>')
                    {
                        tokens.TokenQueue.Enqueue(new Token(c.ToString(), i));
                    }
                    else
                    {
                        // This probably means it's a syntax error, but let's let the parser figure that out
                        tokens.TokenQueue.Enqueue(new Token(c.ToString(), i));
                    }

                    sb.Length = 0;
                }
                else if (c == StringConstants.SINGLE_QUOTE)
                {
                    // Apostrophes trigger string mode
                    var inString = true;
                    tokens.LastStringLiteralBegin = i;
                    while (++i < line.Length)
                    {
                        c = line[i];
                        if (c == StringConstants.SINGLE_QUOTE)
                        {
                            // If there's a double apostrophe, treat it as a single one
                            if ((i + 1 <= line.Length - 1) && (line[i + 1] == StringConstants.SINGLE_QUOTE))
                            {
                                sb.Append(StringConstants.SINGLE_QUOTE);
                                i++;
                            }
                            else
                            {
                                tokens.TokenQueue.Enqueue(new Token(sb.ToString(), i - sb.Length - 1));
                                sb.Length = 0;
                                inString = false;
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }

                    // If the string failed to end properly, trigger an error
                    if (inString)
                    {
                        tokens.HasUnterminatedLiteral = true;
                        break;
                    }
                }
                else
                {
                    // Normal characters just get added to the token
                    sb.Append(c);
                    inToken = true;
                }

                // Move to the next char
                i++;
            }

            // Allow strings to end normally
            if (inToken)
            {
                tokens.TokenQueue.Enqueue(new Token(sb.ToString(), i - sb.Length));
            }

            if (inQuotes)
            {
                tokens.HasUnterminatedLiteral = true;
            }

            // Here's your tokenized list
            return tokens;
        }

        /// <summary>
        /// Checks to see if char is in list
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsSpecialChar(char c)
        {
            return StringConstants.SINGLE_CHARACTER_OPERATORS.Contains(c);
        }
    }
}