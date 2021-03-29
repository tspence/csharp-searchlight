using Searchlight.Exceptions;
using Searchlight.Query;
using System;
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
            Queue<string> tokens = new Queue<string>();
            StringBuilder sb = new StringBuilder();

            // Go through each character
            int i = 0;
            bool in_token = false;

            while (i < line.Length)
            {
                char c = line[i];

                // Whitespace characters always end a token)
                if (Char.IsWhiteSpace(c))
                {
                    if (in_token)
                    {
                        tokens.Enqueue(sb.ToString());
                        sb.Length = 0;
                        in_token = false;
                    }
                }

                // If this is one of the special chars (>, =, etc) end token and count char as its own token
                if (IsSpecialChar(c))
                {

                    // Signify end of the token preceding it
                    if (in_token)
                    {
                        tokens.Enqueue(sb.ToString());
                        in_token = false;
                    }

                    // If the token is actually part of a >= or <= block, add the equal sign to it.
                    string s = c.ToString();
                    if (c == '!')
                    {
                        if (line[i + 1] == '=')
                        {
                            s += line[i + 1];
                            i++;
                        }
                    }
                    if (c == '<')
                    {
                        if (line[i + 1] == '>')
                        {
                            s += line[i + 1];
                            i++;
                        }
                    }
                    if (c == '>' || c == '<')
                    {
                        if (line[i + 1] == '=')
                        {
                            s += line[i + 1];
                            i++;
                        }
                    }

                    tokens.Enqueue(s);
                    sb.Length = 0;
                }

                // Apostrophes trigger string mode
                if (c == StringConstants.SINGLE_QUOTE)
                {
                    bool in_string = true;

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
                                in_string = false;
                                break;
                            }

                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }

                    // If the string failed to end properly, throw an error
                    if (in_string)
                    {
                        throw new UnterminatedValueException(line);
                    }

                    // Normal characters just get added to the token
                }
                else
                {
                    if (Char.IsWhiteSpace(c) == false && !IsSpecialChar(c))
                    {
                        sb.Append(c);
                        in_token = true;
                    }
                }

                // Move to the next char
                i++;
            }

            // Allow strings to end normally
            if (in_token)
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
