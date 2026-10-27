/*
 * Markup.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Makes paragraphs of text with the inline markup of Markdown: **bold**,
/// *italic*, ***bold italic***, `code` and [links](https://pdfjet.com), in the
/// fonts given for each, and as GitHub's Markdown has them, __bold__ and
/// _italic_, ~~strikethrough~~ of two ~, one being text here, as in ~5, and
/// links that are their address,
/// &lt;https://pdfjet.com&gt; or https://pdfjet.com alone. An _ emphasizes
/// only at the edge of a word, so that snake_case is text; an address alone
/// ends before the punctuation at its end, as the period of a sentence, and
/// before a ) that closes no ( of its own. A backslash before a punctuation
/// character, as in \*, makes it plain text, and so is a mark that has no
/// match, such as the * of 2 * 3. Emphasis can be inside a link, and neither
/// inside code. A word keeps its punctuation next to it in another style, as
/// in **bold**, with no space between them: the paragraph joins its text
/// lines with Paragraph.AddJoined.
/// <para>
/// A line that ends in two spaces or a backslash, and a &lt;br&gt;, are hard
/// line breaks, which TextFrame draws as the end of a row; any other line
/// break is a space. The entities &amp;amp;, &amp;lt;, &amp;gt;, &amp;quot;,
/// &amp;apos;, &amp;nbsp;, &amp;copy;, &amp;reg;, &amp;trade;, &amp;mdash;,
/// &amp;ndash;, &amp;hellip;, the quotes and a few more, and the numbers
/// &amp;#8212; and &amp;#x2014;, are the characters they name.
/// </para>
/// <para>
/// Headings, lists, block quotes and the rest of Markdown's blocks are not
/// read: Paragraphs splits a text into paragraphs at its empty lines. The markup is read in time linear in the length of the
/// text, so it can come from anyone. Please see Example_18.
/// </para>
/// </summary>
public class Markup {
    private const int BOLD = 1;
    private const int ITALIC = 2;
    private const int CODE = 4;
    private const int STRIKE = 8;
    private const int BREAK = 16;       // A hard line break

    // The named entities that are read, as GitHub's Markdown reads them: the
    // markup's own characters, the spaces and the punctuation of typeset text.
    private static readonly Dictionary<String, int> ENTITIES = MakeEntities();

    private static Dictionary<String, int> MakeEntities() {
        String[] names = {
            "amp", "lt", "gt", "quot", "apos", "nbsp", "copy", "reg", "trade",
            "mdash", "ndash", "hellip", "lsquo", "rsquo", "ldquo", "rdquo",
            "laquo", "raquo", "bull", "middot", "deg", "times", "divide",
            "plusmn", "sect", "para", "euro", "pound", "yen", "cent", "shy"};
        int[] codes = {
            '&', '<', '>', '"', '\'', 0xA0, 0xA9, 0xAE, 0x2122,
            0x2014, 0x2013, 0x2026, 0x2018, 0x2019, 0x201C, 0x201D,
            0xAB, 0xBB, 0x2022, 0xB7, 0xB0, 0xD7, 0xF7,
            0xB1, 0xA7, 0xB6, 0x20AC, 0xA3, 0xA5, 0xA2, 0xAD};
        Dictionary<String, int> entities = new Dictionary<String, int>();
        for (int i = 0; i < names.Length; i++) {
            entities[names[i]] = codes[i];
        }
        return entities;
    }

    private readonly Font regular;
    private readonly Font bold;
    private readonly Font italic;
    private readonly Font boldItalic;
    private readonly Font code;
    private int linkColor = Color.blue;
    private bool linkUnderline = true;

    /// <summary>
    /// Creates the markup of paragraphs in the fonts, each at its size.
    /// </summary>
    /// <param name="regular">the font of the text.</param>
    /// <param name="bold">the font of **bold** text.</param>
    /// <param name="italic">the font of *italic* text.</param>
    /// <param name="boldItalic">the font of ***bold italic*** text.</param>
    /// <param name="code">the font of `code`, usually a monospaced font.</param>
    public Markup(Font regular, Font bold, Font italic, Font boldItalic, Font code) {
        this.regular = regular;
        this.bold = bold;
        this.italic = italic;
        this.boldItalic = boldItalic;
        this.code = code;
    }

    /// <summary>
    /// Sets the color of the text of links. The default is Color.blue.
    /// Color.transparent leaves it unchanged.
    /// </summary>
    /// <param name="color">the color as a 0xRRGGBB value.</param>
    /// <returns>this Markup.</returns>
    public Markup SetLinkColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        this.linkColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the text of links is underlined, as it is by default.
    /// </summary>
    /// <param name="underline">true to underline the links.</param>
    /// <returns>this Markup.</returns>
    public Markup SetLinkUnderline(bool underline) {
        this.linkUnderline = underline;
        return this;
    }

    /// <summary>
    /// Returns the paragraphs of the text, which empty lines separate.
    /// </summary>
    /// <param name="text">the text with its markup.</param>
    /// <returns>the paragraphs, none for a text with no words.</returns>
    public List<Paragraph> Paragraphs(String text) {
        List<Paragraph> paragraphs = new List<Paragraph>();
        StringBuilder buf = new StringBuilder();
        foreach (String part in text.Split('\n')) {
            String line = part;
            if (line.EndsWith('\r')) {
                line = line.Substring(0, line.Length - 1);
            }
            if (Util.Trim(line).Length == 0) {
                AddParagraph(paragraphs, buf);
            } else {
                buf.Append(line).Append('\n');
            }
        }
        AddParagraph(paragraphs, buf);
        return paragraphs;
    }

    private void AddParagraph(List<Paragraph> paragraphs, StringBuilder buf) {
        if (buf.Length > 0) {
            // Without the line break after the last line, which made a
            // backslash at the end of the paragraph a hard line break, and
            // dropped it
            buf.Length = buf.Length - 1;
            Paragraph paragraph = Paragraph(buf.ToString());
            if (paragraph.lines.Count > 0) {
                paragraphs.Add(paragraph);
            }
            buf.Length = 0;
        }
    }

    /// <summary>
    /// Returns the text as one paragraph: a text line for each run of text in
    /// one style, each joined to the text before it.
    /// </summary>
    /// <param name="text">the text with its markup.</param>
    /// <returns>the paragraph.</returns>
    public Paragraph Paragraph(String text) {
        Parser parser = new Parser(text);
        List<Node> nodes = new List<Node>();
        parser.Parse(0, text.Length, null, nodes);
        Paragraph paragraph = new Paragraph();
        // The runs of one style and link, in order.
        StringBuilder buf = new StringBuilder();
        int flags = 0;
        String link = null;
        bool joined = false;
        foreach (Node node in nodes) {
            if ((node.flags & BREAK) != 0) {
                // A hard line break: the row ends after the text before it,
                // and the text after it is not joined to it
                if (buf.Length > 0) {
                    AddRun(paragraph, buf.ToString(), flags, link, joined);
                    buf.Length = 0;
                }
                if (paragraph.lines.Count > 0) {
                    paragraph.lines[paragraph.lines.Count - 1].lineBreaks++;
                }
                joined = false;
                continue;
            }
            if (buf.Length > 0 && (node.flags != flags || !String.Equals(node.link, link))) {
                joined = AddRun(paragraph, buf.ToString(), flags, link, joined);
                buf.Length = 0;
            }
            flags = node.flags;
            link = node.link;
            buf.Append(node.text);
        }
        if (buf.Length > 0) {
            AddRun(paragraph, buf.ToString(), flags, link, joined);
        }
        return paragraph;
    }

    // Adds a run of text in one style. A run of spaces only is not added: it
    // puts a space before the next run, as between two words. Returns whether
    // the next run is joined to this one.
    private bool AddRun(Paragraph paragraph, String text, int flags, String link, bool joined) {
        if (Util.Trim(text).Length == 0) {
            return false;
        }
        Font font = ((flags & CODE) != 0) ? code
                : ((flags & BOLD) != 0 && (flags & ITALIC) != 0) ? boldItalic
                : ((flags & BOLD) != 0) ? bold
                : ((flags & ITALIC) != 0) ? italic : regular;
        TextLine textLine = new TextLine(font, text);
        if ((flags & STRIKE) != 0) {
            textLine.SetStrikeout(true);
        }
        if (link != null) {
            textLine.SetURIAction(link);
            textLine.SetTextColor(linkColor);
            // The underline in the color of the link: it was black under the
            // blue (review of 15 October 2026)
            textLine.SetDecorationColor(linkColor);
            textLine.SetUnderline(linkUnderline);
        }
        if (joined) {
            paragraph.AddJoined(textLine);
        } else {
            paragraph.Add(textLine);
        }
        return true;
    }

    // Text in one style, or a run of *, of _ or of ~~ that may open or close
    // emphasis or a strikethrough, which is text too as far as it does not.
    // The text is null while the run's emphasis is not matched yet.
    private sealed class Node {
        internal String text;
        internal int flags;
        internal String link;
        // Of a run: its character, *, _ or ~, where it is among the nodes, how
        // many of its characters are not matched yet, whether they can open
        // and close emphasis, and the emphasis the run opens and closes,
        // counted in levels of bold, italic and strikethrough.
        internal char ch;
        internal int index;
        internal int count;
        internal bool canOpen;
        internal bool canClose;
        internal int[] opens;
        internal int[] closes;

        internal Node(String text, int flags, String link) {
            this.text = text;
            this.flags = flags;
            this.link = link;
        }
    }

    private sealed class Parser {
        private readonly String text;
        // Where each ] that closes a [ is, by the index of the [.
        private readonly int[] closingBracket;
        // The index of the next ) and of the next space at or after each index.
        private readonly int[] nextParenthesis;
        private readonly int[] nextSpace;
        // The runs of backticks, by their length: where each starts, in order,
        // and the first one of each length that can still close a code span.
        private readonly Dictionary<int, List<int>> backtickRuns = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, int> backtickCursor = new Dictionary<int, int>();

        internal Parser(String text) {
            this.text = text;
            int n = text.Length;
            closingBracket = new int[n];
            Array.Fill(closingBracket, -1);
            Stack<int> open = new Stack<int>();
            for (int i = 0; i < n; i++) {
                char ch = text[i];
                if (ch == '\\' && i + 1 < n) {
                    i++;
                } else if (ch == '[') {
                    open.Push(i);
                } else if (ch == ']' && open.Count > 0) {
                    closingBracket[open.Pop()] = i;
                }
            }
            nextParenthesis = new int[n + 1];
            nextSpace = new int[n + 1];
            nextParenthesis[n] = n;
            nextSpace[n] = n;
            for (int i = n - 1; i >= 0; i--) {
                char ch = text[i];
                nextParenthesis[i] = (ch == ')') ? i : nextParenthesis[i + 1];
                nextSpace[i] = Util.IsJavaWhitespace(ch) ? i : nextSpace[i + 1];
            }
            for (int i = 0; i < n; ) {
                if (text[i] == '\\' && i + 1 < n && text[i + 1] != '`') {
                    i += 2;
                } else if (text[i] == '`') {
                    // A backtick after a backslash is text where it would open
                    // code, as Parse reads it, but closes code, in which a
                    // backslash is text, as in GitHub's Markdown: `foo\` was
                    // not closed
                    int start = i;
                    while (i < n && text[i] == '`') {
                        i++;
                    }
                    int length = i - start;
                    List<int> runs;
                    if (!backtickRuns.TryGetValue(length, out runs)) {
                        runs = new List<int>();
                        backtickRuns[length] = runs;
                        backtickCursor[length] = 0;
                    }
                    runs.Add(start);
                } else {
                    i++;
                }
            }
        }

        // Reads the text from start to end into nodes, all in the link when it
        // is not null, with the emphasis of the runs of *, _ and ~~ matched.
        internal void Parse(int start, int end, String link, List<Node> nodes) {
            int first = nodes.Count;
            StringBuilder buf = new StringBuilder();
            int i = start;
            while (i < end) {
                char ch = text[i];
                int autolinkEnd;
                int breakEnd = BreakEnd(i, end);
                int[] entity;
                if (breakEnd > i) {
                    Flush(buf, link, nodes);
                    nodes.Add(new Node("", BREAK, link));
                    i = breakEnd;
                } else if ((entity = Entity(i, end)) != null) {
                    AppendCodePoint(buf, entity[0]);
                    i = entity[1];
                } else if (ch == '\\' && i + 1 < end && IsASCIIPunctuation(text[i + 1])) {
                    buf.Append(text[i + 1]);
                    i += 2;
                } else if (ch == '`') {
                    int length = 1;
                    while (i + length < end && text[i + length] == '`') {
                        length++;
                    }
                    int close = ClosingBackticks(length, i + length, end);
                    if (close == -1) {
                        buf.Append(text, i, length);
                    } else {
                        Flush(buf, link, nodes);
                        nodes.Add(new Node(CodeText(text.Substring(i + length, close - (i + length))), CODE, link));
                    }
                    i = (close == -1) ? i + length : close + length;
                } else if (ch == '*' || ch == '_' || ch == '~') {
                    int length = 1;
                    while (i + length < end && text[i + length] == ch) {
                        length++;
                    }
                    if (ch == '~' && length != 2) {
                        // A strikethrough is two ~, and one or three are text, as in ~5
                        buf.Append(text, i, length);
                        i += length;
                        continue;
                    }
                    Flush(buf, link, nodes);
                    Node run = new Node(null, 0, link);
                    run.ch = ch;
                    run.index = nodes.Count;
                    run.count = length;
                    // Before the run, or after it, a space, or the start or the end
                    bool left = i + length < end && !Util.IsJavaWhitespace(text[i + length]);
                    bool right = i > start && !Util.IsJavaWhitespace(text[i - 1]);
                    run.canOpen = left;
                    run.canClose = right;
                    if (ch == '_') {
                        // Inside a word, as in snake_case, an _ is text
                        run.canOpen = left && (!right || IsASCIIPunctuation(text[i - 1]));
                        run.canClose = right && (!left || IsASCIIPunctuation(text[i + length]));
                    }
                    nodes.Add(run);
                    i += length;
                } else if ((autolinkEnd = AutolinkEnd(i, start, end, link)) > i) {
                    Flush(buf, link, nodes);
                    String url = (ch == '<')
                            ? text.Substring(i + 1, autolinkEnd - 1 - (i + 1))
                            : text.Substring(i, autolinkEnd - i);
                    nodes.Add(new Node(url, 0, url));
                    i = autolinkEnd;
                } else if (ch == '[' && link == null && IsLink(i, end)) {
                    int close = closingBracket[i];
                    int urlEnd = nextParenthesis[close + 2];
                    Flush(buf, link, nodes);
                    Parse(i + 1, close, Decoded(close + 2, urlEnd), nodes);
                    i = urlEnd + 1;
                } else {
                    buf.Append(Util.IsJavaWhitespace(ch) ? ' ' : ch);
                    i++;
                }
            }
            Flush(buf, link, nodes);
            MatchEmphasis(nodes, first);
        }

        // Returns where a hard line break at i ends, or i when there is none:
        // a line break after two spaces, a backslash and a line break, or a
        // <br>, <br/> or <br /> in capitals or not.
        private int BreakEnd(int i, int end) {
            char ch = text[i];
            // The line break of a text of \r\n line ends too
            int newline = (ch == '\r' && i + 1 < end && text[i + 1] == '\n') ? i + 1 : i;
            if (text[newline] == '\n' && i >= 2 && text[i - 1] == ' ' && text[i - 2] == ' ') {
                return newline + 1;
            }
            if (ch == '\\' && i + 1 < end && text[i + 1] == '\n') {
                return i + 2;
            }
            if (ch == '\\' && i + 2 < end && text[i + 1] == '\r' && text[i + 2] == '\n') {
                return i + 3;
            }
            if (ch == '<' && i + 3 < end && (text[i + 1] == 'b' || text[i + 1] == 'B')
                    && (text[i + 2] == 'r' || text[i + 2] == 'R')) {
                int j = i + 3;
                while (j < end && text[j] == ' ') {
                    j++;
                }
                if (j < end && text[j] == '/') {
                    j++;
                }
                if (j < end && text[j] == '>') {
                    return j + 1;
                }
            }
            return i;
        }

        // Returns the character of the entity at i and where the entity ends,
        // or null when there is none: a name of ENTITIES, or a number of up to
        // seven digits, or of six in hexadecimal after an x, that is a
        // character, between & and ;.
        private int[] Entity(int i, int end) {
            if (text[i] != '&') {
                return null;
            }
            int semicolon = -1;
            for (int j = i + 1; j < end && j <= i + 10; j++) {
                if (text[j] == ';') {
                    semicolon = j;
                    break;
                }
            }
            if (semicolon == -1) {
                return null;
            }
            String name = text.Substring(i + 1, semicolon - (i + 1));
            int code;
            if (ENTITIES.TryGetValue(name, out code)) {
                return new int[] {code, semicolon + 1};
            }
            if (name.Length < 2 || name[0] != '#') {
                return null;
            }
            bool hex = name[1] == 'x' || name[1] == 'X';
            String digits = name.Substring(hex ? 2 : 1);
            if (digits.Length == 0 || digits.Length > (hex ? 6 : 7)) {
                return null;
            }
            int value = 0;
            for (int k = 0; k < digits.Length; k++) {
                int digit = AsciiDigit(digits[k], hex);
                if (digit < 0) {
                    return null;
                }
                value = value * (hex ? 16 : 10) + digit;
            }
            if (value == 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF)) {
                return null;
            }
            if (value < 0x20 || (value >= 0x7F && value <= 0x9F)) {
                value = ' ';    // A control character is a space: &#10; broke the line of a table cell
            }
            return new int[] {value, semicolon + 1};
        }

        // The text from start to end with its entities the characters they
        // are: the address of a link, where &amp; was the five characters.
        private String Decoded(int start, int end) {
            StringBuilder buf = new StringBuilder();
            int i = start;
            while (i < end) {
                int[] entity = Entity(i, end);
                if (entity != null) {
                    AppendCodePoint(buf, entity[0]);
                    i = entity[1];
                } else {
                    buf.Append(text[i]);
                    i++;
                }
            }
            return buf.ToString();
        }

        // The value of an ASCII digit, of 0 to 9, or of a to f and A to F in
        // hexadecimal, or -1.
        private static int AsciiDigit(char ch, bool hex) {
            if (ch >= '0' && ch <= '9') {
                return ch - '0';
            }
            if (hex && ch >= 'a' && ch <= 'f') {
                return ch - 'a' + 10;
            }
            if (hex && ch >= 'A' && ch <= 'F') {
                return ch - 'A' + 10;
            }
            return -1;
        }

        // Appends the code point, as its surrogate pair above U+FFFF.
        private static void AppendCodePoint(StringBuilder buf, int codePoint) {
            if (codePoint > 0xFFFF) {
                buf.Append(Char.ConvertFromUtf32(codePoint));
            } else {
                buf.Append((char) codePoint);
            }
        }

        // A [ starts a link when its ] is before the end, a ( follows the ]
        // at once, and a ) ends the URL before any space: [text](url).
        private bool IsLink(int i, int end) {
            int close = closingBracket[i];
            if (close == -1 || close + 1 >= end || text[close + 1] != '(') {
                return false;
            }
            int urlEnd = nextParenthesis[close + 2];
            return urlEnd < end && urlEnd > close + 2 && nextSpace[close + 2] > urlEnd;
        }

        // Returns where a link that is its address ends, after its > when it is
        // in < and >, or i when there is none at i, or the text is in a link
        // already: an address of http:// or https:// and a host, in < and >
        // with no space in it, or alone, after a space, the start, a (, or a *,
        // _ or ~ of emphasis. An address alone ends at a space or a <, and then
        // before the punctuation at its end, ? ! . , : * _ and ~, and before
        // each ) at its end that closes no ( in it, as GitHub's Markdown reads it.
        private int AutolinkEnd(int i, int start, int end, String link) {
            if (link != null || (text[i] != '<' && text[i] != 'h' && text[i] != 'H')) {
                return i;
            }
            int from = i;
            if (text[i] == '<') {
                from = i + 1;
            } else if (i > start && !Util.IsJavaWhitespace(text[i - 1]) && "(*_~".IndexOf(text[i - 1]) == -1) {
                return i;
            }
            int host = SchemeEnd(from, end);
            if (host == -1 || host >= end || !IsHostChar(text[host])) {
                return i;
            }
            int j = host;
            while (j < end && !Util.IsJavaWhitespace(text[j]) && text[j] != '<' && text[j] != '>') {
                j++;
            }
            if (text[i] == '<') {
                return (j < end && text[j] == '>') ? j + 1 : i;
            }
            int opening = Count(from, j, '(');
            int closing = Count(from, j, ')');
            while (j > host) {
                char last = text[j - 1];
                if ("?!.,:*_~".IndexOf(last) != -1) {
                    j--;
                } else if (last == ')' && opening < closing) {
                    j--;
                    closing--;
                } else {
                    break;
                }
            }
            return (j == host) ? i : j;
        }

        // Returns where the host starts after http:// or https:// at the
        // index, in capitals or not, or -1. Only the ASCII letters are folded,
        // so that the four ports agree.
        private int SchemeEnd(int i, int end) {
            foreach (String scheme in new String[] {"https://", "http://"}) {
                int n = scheme.Length;
                if (i + n > end) {
                    continue;
                }
                int k = 0;
                while (k < n) {
                    char ch = text[i + k];
                    if (ch >= 'A' && ch <= 'Z') {
                        ch = (char) (ch + ('a' - 'A'));
                    }
                    if (ch != scheme[k]) {
                        break;
                    }
                    k++;
                }
                if (k == n) {
                    return i + n;
                }
            }
            return -1;
        }

        // Returns true for a letter, a digit or a character that is not ASCII,
        // as the first character of a host is.
        private static bool IsHostChar(char ch) {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch > 0x7F;
        }

        // Returns how many of the character are in the text from start to end.
        private int Count(int start, int end, char ch) {
            int n = 0;
            for (int i = start; i < end; i++) {
                if (text[i] == ch) {
                    n++;
                }
            }
            return n;
        }

        // Returns where the next run of backticks of the length starts, after
        // from and before end, or -1.
        private int ClosingBackticks(int length, int from, int end) {
            List<int> runs;
            if (!backtickRuns.TryGetValue(length, out runs)) {
                return -1;
            }
            int cursor = backtickCursor[length];
            while (cursor < runs.Count && runs[cursor] < from) {
                cursor++;
            }
            backtickCursor[length] = cursor;
            if (cursor < runs.Count && runs[cursor] + length <= end) {
                return runs[cursor];
            }
            return -1;
        }

        // The text of a code span: a line break is a space, and one space at
        // each end is taken away when both ends have one, as in `` `a` ``.
        private static String CodeText(String code) {
            code = code.Replace('\n', ' ').Replace('\r', ' ');
            if (code.Length >= 2 && code[0] == ' ' && code[code.Length - 1] == ' '
                    && Util.Trim(code).Length > 0) {
                code = code.Substring(1, code.Length - 2);
            }
            return code;
        }

        private static void Flush(StringBuilder buf, String link, List<Node> nodes) {
            if (buf.Length > 0) {
                nodes.Add(new Node(buf.ToString(), 0, link));
                buf.Length = 0;
            }
        }

        // Matches the runs from the index on, each closer with the nearest
        // opener of its character before it: two * or _ of each make bold and
        // one italic, and ~~ a strikethrough. The runs of the other characters
        // between a closer and its opener can no longer match, so that emphasis
        // and strikethrough nest and do not cross. Then gives every node the
        // emphasis it is in, and turns what is left of each run into text.
        private static void MatchEmphasis(List<Node> nodes, int first) {
            // The runs that can still open, of *, of _ and of ~
            Stack<Node> stars = new Stack<Node>();
            Stack<Node> underscores = new Stack<Node>();
            Stack<Node> tildes = new Stack<Node>();
            for (int i = first; i < nodes.Count; i++) {
                Node node = nodes[i];
                if (node.text != null) {
                    continue;
                }
                if (node.ch == '~') {
                    if (node.canClose && tildes.Count > 0) {
                        Node opener = tildes.Pop();
                        DropAfter(stars, opener.index);
                        DropAfter(underscores, opener.index);
                        opener.opens = Add(opener.opens, STRIKE);
                        node.closes = Add(node.closes, STRIKE);
                        opener.count = 0;
                        node.count = 0;
                    }
                    if (node.count > 0 && node.canOpen) {
                        tildes.Push(node);
                    }
                    continue;
                }
                Stack<Node> own = (node.ch == '_') ? underscores : stars;
                Stack<Node> other = (node.ch == '_') ? stars : underscores;
                if (node.canClose) {
                    while (node.count > 0 && own.Count > 0) {
                        Node opener = own.Peek();
                        DropAfter(other, opener.index);
                        DropAfter(tildes, opener.index);
                        int use = (node.count >= 2 && opener.count >= 2) ? 2 : 1;
                        int flag = (use == 2) ? BOLD : ITALIC;
                        opener.opens = Add(opener.opens, flag);
                        node.closes = Add(node.closes, flag);
                        opener.count -= use;
                        node.count -= use;
                        if (opener.count == 0) {
                            own.Pop();
                        }
                    }
                }
                if (node.count > 0 && node.canOpen) {
                    own.Push(node);
                }
            }
            // The emphasis in effect, in levels, since bold can be in bold.
            int[] levels = new int[3];
            for (int i = first; i < nodes.Count; i++) {
                Node node = nodes[i];
                if (node.text == null) {
                    for (int k = 0; k < levels.Length; k++) {
                        levels[k] -= Level(node.closes, k);
                    }
                    node.text = Repeat(node.ch, node.count);
                    node.flags = Emphasis(levels);
                    for (int k = 0; k < levels.Length; k++) {
                        levels[k] += Level(node.opens, k);
                    }
                } else {
                    node.flags |= Emphasis(levels);
                }
            }
        }

        // Drops the openers after the index from the stack.
        private static void DropAfter(Stack<Node> openers, int index) {
            while (openers.Count > 0 && openers.Peek().index > index) {
                openers.Pop();
            }
        }

        // The flags of the levels of bold, italic and strikethrough in effect.
        private static int Emphasis(int[] levels) {
            return ((levels[0] > 0) ? BOLD : 0) | ((levels[1] > 0) ? ITALIC : 0)
                    | ((levels[2] > 0) ? STRIKE : 0);
        }

        // The levels of bold, italic and strikethrough with one more of the flag.
        private static int[] Add(int[] levels, int flag) {
            if (levels == null) {
                levels = new int[3];
            }
            levels[(flag == BOLD) ? 0 : (flag == ITALIC) ? 1 : 2]++;
            return levels;
        }

        // The level of bold (0), italic (1) or strikethrough (2).
        private static int Level(int[] levels, int k) {
            return (levels == null) ? 0 : levels[k];
        }

        private static String Repeat(char ch, int count) {
            return new String(ch, count);
        }

        private static bool IsASCIIPunctuation(char ch) {
            return (ch >= '!' && ch <= '/') || (ch >= ':' && ch <= '@')
                    || (ch >= '[' && ch <= '`') || (ch >= '{' && ch <= '~');
        }
    }
}   // End of Markup.cs
}   // End of namespace PDFjet.NET
