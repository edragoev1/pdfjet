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
/// fonts given for each. A backslash before a punctuation character, as in \*,
/// makes it plain text, and so is a mark that has no match, such as the * of
/// 2 * 3. Emphasis can be inside a link, and neither inside code. A word keeps
/// its punctuation next to it in another style, as in **bold**, with no space
/// between them: the paragraph joins its text lines with Paragraph.AddJoined.
/// <para>
/// Headings, lists, block quotes and the rest of Markdown's blocks are not
/// read: a line break is a space, and Paragraphs splits a text into paragraphs
/// at its empty lines. The markup is read in time linear in the length of the
/// text, so it can come from anyone. Please see Example_53.
/// </para>
/// </summary>
public class Markup {
    private const int BOLD = 1;
    private const int ITALIC = 2;
    private const int CODE = 4;

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
    /// </summary>
    /// <param name="color">the color as a 0xRRGGBB value.</param>
    /// <returns>this Markup.</returns>
    public Markup SetLinkColor(int color) {
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
        if (link != null) {
            textLine.SetURIAction(link);
            textLine.SetTextColor(linkColor);
            textLine.SetUnderline(linkUnderline);
        }
        if (joined) {
            paragraph.AddJoined(textLine);
        } else {
            paragraph.Add(textLine);
        }
        return true;
    }

    // Text in one style, or a run of * that may open or close emphasis, which
    // is text too as far as it does not.
    private sealed class Node {
        internal String text;
        internal int flags;
        internal String link;
        // Of a run of *: how many of them are not matched yet, whether they can
        // open and close emphasis, and the emphasis the run opens and closes,
        // counted in levels of the flags.
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
                if (text[i] == '\\' && i + 1 < n) {
                    i += 2;
                } else if (text[i] == '`') {
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
        // is not null, with the emphasis of the runs of * matched.
        internal void Parse(int start, int end, String link, List<Node> nodes) {
            int first = nodes.Count;
            StringBuilder buf = new StringBuilder();
            int i = start;
            while (i < end) {
                char ch = text[i];
                if (ch == '\\' && i + 1 < end && IsASCIIPunctuation(text[i + 1])) {
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
                } else if (ch == '*') {
                    int length = 1;
                    while (i + length < end && text[i + length] == '*') {
                        length++;
                    }
                    Flush(buf, link, nodes);
                    Node run = new Node(null, 0, link);
                    run.count = length;
                    run.canOpen = i + length < end && !Util.IsJavaWhitespace(text[i + length]);
                    run.canClose = i > start && !Util.IsJavaWhitespace(text[i - 1]);
                    nodes.Add(run);
                    i += length;
                } else if (ch == '[' && link == null && IsLink(i, end)) {
                    int close = closingBracket[i];
                    int urlEnd = nextParenthesis[close + 2];
                    Flush(buf, link, nodes);
                    Parse(i + 1, close, text.Substring(close + 2, urlEnd - (close + 2)), nodes);
                    i = urlEnd + 1;
                } else {
                    buf.Append(Util.IsJavaWhitespace(ch) ? ' ' : ch);
                    i++;
                }
            }
            Flush(buf, link, nodes);
            MatchEmphasis(nodes, first);
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

        // Matches the runs of * from the index on, each closer with the nearest
        // opener before it: two of each make bold and one italic, and the runs
        // between them can no longer match. Then gives every node the emphasis
        // it is in, and turns what is left of each run into text.
        private static void MatchEmphasis(List<Node> nodes, int first) {
            Stack<Node> openers = new Stack<Node>();
            for (int i = first; i < nodes.Count; i++) {
                Node node = nodes[i];
                if (node.text != null) {
                    continue;
                }
                if (node.canClose) {
                    while (node.count > 0 && openers.Count > 0) {
                        Node opener = openers.Peek();
                        int use = (node.count >= 2 && opener.count >= 2) ? 2 : 1;
                        int flag = (use == 2) ? BOLD : ITALIC;
                        opener.opens = Add(opener.opens, flag);
                        node.closes = Add(node.closes, flag);
                        opener.count -= use;
                        node.count -= use;
                        if (opener.count == 0) {
                            openers.Pop();
                        }
                    }
                }
                if (node.count > 0 && node.canOpen) {
                    openers.Push(node);
                }
            }
            // The emphasis in effect, in levels, since bold can be in bold.
            int boldLevel = 0;
            int italicLevel = 0;
            for (int i = first; i < nodes.Count; i++) {
                Node node = nodes[i];
                if (node.text == null) {
                    boldLevel -= Level(node.closes, BOLD);
                    italicLevel -= Level(node.closes, ITALIC);
                    node.text = Repeat('*', node.count);
                    node.flags = Emphasis(boldLevel, italicLevel);
                    boldLevel += Level(node.opens, BOLD);
                    italicLevel += Level(node.opens, ITALIC);
                } else {
                    node.flags |= Emphasis(boldLevel, italicLevel);
                }
            }
        }

        private static int Emphasis(int boldLevel, int italicLevel) {
            return ((boldLevel > 0) ? BOLD : 0) | ((italicLevel > 0) ? ITALIC : 0);
        }

        // The levels of bold and of italic, [bold, italic], with one more of the flag.
        private static int[] Add(int[] levels, int flag) {
            if (levels == null) {
                levels = new int[2];
            }
            levels[(flag == BOLD) ? 0 : 1]++;
            return levels;
        }

        private static int Level(int[] levels, int flag) {
            return (levels == null) ? 0 : levels[(flag == BOLD) ? 0 : 1];
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
