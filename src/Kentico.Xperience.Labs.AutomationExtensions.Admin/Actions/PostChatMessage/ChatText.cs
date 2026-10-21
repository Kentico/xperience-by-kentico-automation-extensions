using System.Text;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Text helpers shared by the chat payload builders.
/// </summary>
internal static class ChatText
{
    /// <summary>
    /// Appended to shortened texts.
    /// </summary>
    public const string ELLIPSIS = "…";


    /// <summary>
    /// Converts <c>\r\n</c> and <c>\r</c> to <c>\n</c>.
    /// </summary>
    public static string NormalizeLineBreaks(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');


    /// <summary>
    /// Returns <paramref name="text"/>, or its start followed by <see cref="ELLIPSIS"/> when it's longer than
    /// <paramref name="maxLength"/> characters. Never splits a surrogate pair.
    /// </summary>
    public static string Truncate(string text, int maxLength) => EncodeAndTruncate(text, maxLength, encode: null);


    /// <summary>
    /// Escapes <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> as Slack requires for text that isn't markup, and shortens the
    /// escaped result to <paramref name="maxLength"/> characters without cutting an entity.
    /// </summary>
    /// <remarks>
    /// See https://docs.slack.dev/messaging/formatting-message-text (escaping text).
    /// </remarks>
    public static string EscapeSlackAndTruncate(string text, int maxLength) => EncodeAndTruncate(text, maxLength, EscapeSlack);


    private static string? EscapeSlack(char character) => character switch
    {
        '&' => "&amp;",
        '<' => "&lt;",
        '>' => "&gt;",
        _ => null
    };


    private static string EncodeAndTruncate(string text, int maxLength, Func<char, string?>? encode)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string encoded = encode is null ? text : Encode(text, encode, int.MaxValue);
        if (encoded.Length <= maxLength)
        {
            return encoded;
        }

        return Encode(text, encode, maxLength - ELLIPSIS.Length) + ELLIPSIS;
    }


    private static string Encode(string text, Func<char, string?>? encode, int maxLength)
    {
        var builder = new StringBuilder(Math.Min(text.Length, maxLength));

        int index = 0;
        while (index < text.Length)
        {
            bool surrogatePair = char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]);
            string piece = surrogatePair
                ? text.Substring(index, 2)
                : encode?.Invoke(text[index]) ?? text[index].ToString();

            if (builder.Length + piece.Length > maxLength)
            {
                return builder.ToString();
            }

            builder.Append(piece);
            index += surrogatePair ? 2 : 1;
        }

        return builder.ToString();
    }
}
