using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Searchlight.Exceptions;
using Searchlight.Query;

namespace Searchlight.Parsing
{
    public static class Tokenizer
    {
        public static Queue<string> GenerateTokens(string filter)
        {
            Queue<string> tokens = new Queue<string>();
            StringBuilder sb = new StringBuilder();

            // Go through each character
            int i = 0;
            bool in_token = false;

            while (i < filter.Length) {
                char c = filter[i];

                // Whitespace characters always end a token)
                if (Char.IsWhiteSpace(c)) {
                    if (in_token) {
                        tokens.Enqueue(sb.ToString());
                        sb.Length = 0;
                        in_token = false;
                    }
                }

                // If this is one of the special chars (>, =, etc) end token and count char as its own token
                if (IsSpecialChar(c)) {

                    // Signify end of the token preceding it
                    if (in_token) {
                        tokens.Enqueue(sb.ToString());
                        in_token = false;
                    }

                    // If the token is actually part of a >= or <= block, add the equal sign to it.
                    string s = c.ToString();
                    if (c == '!') {
                        if (filter[i + 1] == '=') {
                            s += filter[i + 1];
                            i++;
                        }
                    }
                    if (c == '<') {
                        if (filter[i + 1] == '>') {
                            s += filter[i + 1];
                            i++;
                        }
                    }
                    if (c == '>' || c == '<') {
                        if (filter[i + 1] == '=') {
                            s += filter[i + 1];
                            i++;
                        }
                    }

                    tokens.Enqueue(s);
                    sb.Length = 0;
                }

                // Apostrophes trigger string mode
                if (c == StringConstants.SINGLE_QUOTE) {
                    bool in_string = true;

                    while (++i < filter.Length) {
                        c = filter[i];
                        if (c == StringConstants.SINGLE_QUOTE) {

                            // If there's a double apostrophe, treat it as a single one
                            if ((i + 1 <= filter.Length - 1) && (filter[i + 1] == StringConstants.SINGLE_QUOTE)) {
                                sb.Append(StringConstants.SINGLE_QUOTE);
                                i++;
                            } else {
                                tokens.Enqueue(sb.ToString());
                                sb.Length = 0;
                                in_string = false;
                                break;
                            }

                        } else {
                            sb.Append(c);
                        }
                    }

                    // If the string failed to end properly, throw an error
                    if (in_string) {
                        throw new UnterminatedValueException(filter);
                    }

                    // Normal characters just get added to the token
                } else {
                    if (Char.IsWhiteSpace(c) == false && !IsSpecialChar(c)) {
                        sb.Append(c);
                        in_token = true;
                    }
                }

                // Move to the next char
                i++;
            }

            // Allow strings to end normally
            if (in_token) {
                tokens.Enqueue(sb.ToString());
            }

            // Here's your tokenized list
            return tokens;
        }

        public static List<SortInfo> TokenizeOrderBy(string orderby_and_direction_string)
        {
            List<SortInfo> list = new List<SortInfo>();

            // Did the user give a sort string?  If so, break it apart by commas
            if (!String.IsNullOrEmpty(orderby_and_direction_string)) {
                foreach (var s in orderby_and_direction_string.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    string[] order_items = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (order_items.Length == 2) {
                        if (order_items[1].StartsWith("DESC", StringComparison.OrdinalIgnoreCase)) {
                            list.Add(new SortInfo(order_items[0], SortDirection.Descending));
                        } else if (order_items[1].StartsWith("ASC", StringComparison.OrdinalIgnoreCase)) {
                            list.Add(new SortInfo(order_items[0], SortDirection.Ascending));
                        } else {
                            throw new ParserSyntaxException(order_items[1], StringConstants.SAFE_SORT_BY, orderby_and_direction_string);
                        }

                        // Append this to the sort criteria
                    } else {
                        list.Add(new SortInfo(order_items[0], SortDirection.Ascending));
                    }
                }
            }
            return list;
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
