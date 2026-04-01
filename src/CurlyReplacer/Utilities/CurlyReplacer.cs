using System;
using System.Collections.Generic;
using System.Text;

public sealed record CurlyCapture(string Content, int StartIndex, int EndIndex);

public static class CurlyReplacer
{

    /// <summary>
    /// Tries to parse all top-level {{ ... }} captures.
    /// </summary>
    public static IEnumerable<CurlyCapture> Parse(string input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (!ContainsOpenToken(input)) yield break;

        int i = 0;

        while (i < input.Length - 1)
        {
            if (!IsOpen(input, i))
            {
                i++;
                continue;
            }

            int start = i;   // first '{' of outer "{{"
            i += 2;          // consume outer "{{"
            int depth = 1;
            var content = new StringBuilder();

            while (i < input.Length)
            {
                if (i < input.Length - 1 && IsOpen(input, i))
                {
                    depth++;
                    content.Append("{{");
                    i += 2;
                    continue;
                }

                if (i < input.Length - 1 && IsClose(input, i))
                {
                    depth--;

                    if (depth == 0)
                    {
                        int end = i + 1; // last '}' of outer "}}"
                        i += 2;          // consume outer "}}"
                        yield return new CurlyCapture(content.ToString(), start, end);
                        break;
                    }

                    content.Append("}}");
                    i += 2;
                    continue;
                }

                content.Append(input[i]);
                i++;
            }
        }
    }

    /// <summary>
    /// Replaces each top-level {{ ... }} block using a replacement based on content.
    /// </summary>
    public static string Replace(string input, Func<string, string> replacement)
    {
        return Replace(input, c => replacement(c.Content));
    }

    /// <summary>
    /// Replaces each top-level {{ ... }} block using a replacement based on full capture metadata.
    /// </summary>
    private static string Replace(string input, Func<CurlyCapture, string> replacement)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (!ContainsOpenToken(input)) return input;

        var sb = new StringBuilder(input.Length);
        int cursor = 0;

        foreach (var capture in Parse(input))
        {
            // Text before the capture.
            sb.Append(input, cursor, capture.StartIndex - cursor);

            // Replacement text for this capture.
            sb.Append(replacement?.Invoke(capture) ?? string.Empty);

            // Move past the full "{{ ... }}" span.
            cursor = capture.EndIndex + 1;
        }

        // Remaining text after last capture.
        sb.Append(input, cursor, input.Length - cursor);
        return sb.ToString();
    }

    private static bool IsOpen(string s, int i) => s[i] == '{' && s[i + 1] == '{';
    private static bool IsClose(string s, int i) => s[i] == '}' && s[i + 1] == '}';
    private static bool ContainsOpenToken(string s) => s.Length > 1 && s.IndexOf("{{", StringComparison.Ordinal) >= 0;
}