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
        /// <param name="line"></param>
        /// <returns></returns>
        public static Queue<string> GenerateTokens(string line)
        {
            var tokens = new Queue<string>();
            var sb = new StringBuilder();

            // Go through each character
            var i = 0;
            var inToken = false;
            while (i < line.Length)
            {
                var c = line[i];

                if (char.IsWhiteSpace(c))
                {
                    // Whitespace characters always end a token)
                    if (inToken)
                    {
                        tokens.Enqueue(sb.ToString());
                        sb.Length = 0;
                        inToken = false;
                    }
                }
                else if (IsSpecialChar(c))
                {
                    // If this is one of the special chars (>, =, etc) end the previous token and count char as its own token
                    if (inToken)
                    {
                        tokens.Enqueue(sb.ToString());
                        inToken = false;
                    }

                    // If the token is actually part of a >= or <= block, add the equal sign to it.
                    var c2 = (i + 1 < line.Length) ? (char?)line[i + 1] : null;
                    if ((c == '!' && c2 == '=') ||
                        (c == '<' && c2 == '>') ||
                        (c == '<' && c2 == '=') ||
                        (c == '>' && c2 == '='))
                    {
                        tokens.Enqueue(line.Substring(i, 2));
                        i++;
                    } else if (c == '<' || c == '>')
                    {
                        tokens.Enqueue(c.ToString());
                    }
                    else
                    {
                        // This probably means it's a syntax error, but let's let the parser figure that out
                        tokens.Enqueue(c.ToString());
                    }

                    sb.Length = 0;
                }
                else if (c == StringConstants.SINGLE_QUOTE)
                {
                    // Apostrophes trigger string mode
                    var inString = true;
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
                                tokens.Enqueue(sb.ToString());
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

                    // If the string failed to end properly, throw an error
                    if (inString)
                    {
                        throw new UnterminatedString() { Token = sb.ToString(), OriginalFilter = line };
                    }
                }
                else
                {
                    // Normal characters just get added to the token
                    if (char.IsWhiteSpace(c) == false && !IsSpecialChar(c))
                    {
                        sb.Append(c);
                        inToken = true;
                    }
                }

                // Move to the next char
                i++;
            }

            // Allow strings to end normally
            if (inToken)
            {
                tokens.Enqueue(sb.ToString());
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