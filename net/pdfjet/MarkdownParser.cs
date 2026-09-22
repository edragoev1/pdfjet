/*
 * MarkdownParser.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace PDFjet.NET {
/// <summary>
/// Reads the blocks of a Markdown text into a tree, for Markdown to draw:
/// headings, paragraphs, code, block quotes, lists, thematic breaks, tables and
/// images. The text of the headings, the paragraphs and the table cells keeps
/// its inline markup, which Markup reads. Containers nest to MAX_DEPTH levels;
/// deeper, their lines are text, so that no input makes the reading slower
/// than linear in its length times that depth.
/// </summary>
internal sealed class MarkdownParser {
    internal const int MAX_DEPTH = 32;

    internal enum Kind { HEADING, PARAGRAPH, CODE, QUOTE, LIST, ITEM, RULE, TABLE, IMAGE }

    internal sealed class Block {
        internal readonly Kind kind;
        internal int level;             // Of a heading, 1 to 6
        internal String text;           // Of a heading or a paragraph, the code, or the alt text of an image
        internal String source;         // Of an image
        internal bool ordered;          // Of a list
        internal int start = 1;         // Of an ordered list
        internal bool loose;            // Of a list whose items are apart, with an empty line between them
        internal readonly List<Block> children = new List<Block>();    // Of a quote, a list or an item
        internal List<List<String>> rows;       // Of a table, the header row first
        internal List<Alignment> alignments;    // Of a table, of each column, or null

        internal Block(Kind kind) {
            this.kind = kind;
        }
    }

    private MarkdownParser() {
    }

    // The blocks of the text.
    internal static List<Block> Parse(String text) {
        List<String> lines = new List<String>();
        int start = 0;
        for (int i = 0; i < text.Length; i++) {
            char ch = text[i];
            if (ch == '\n' || ch == '\r') {
                lines.Add(ExpandTabs(text.Substring(start, i - start)));
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') {
                    i++;
                }
                start = i + 1;
            }
        }
        lines.Add(ExpandTabs(text.Substring(start)));
        return ParseBlocks(lines, 0);
    }

    // A tab is spaces to the next column that is a multiple of 4.
    private static String ExpandTabs(String line) {
        if (line.IndexOf('\t') == -1) {
            return line;
        }
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < line.Length; i++) {
            char ch = line[i];
            if (ch == '\t') {
                do {
                    buf.Append(' ');
                } while (buf.Length % 4 != 0);
            } else {
                buf.Append(ch);
            }
        }
        return buf.ToString();
    }

    private static List<Block> ParseBlocks(List<String> lines, int depth) {
        List<Block> blocks = new List<Block>();
        int i = 0;
        while (i < lines.Count) {
            String line = lines[i];
            if (IsBlank(line)) {
                i++;
                continue;
            }
            int[] fence = Fence(line);
            if (fence != null) {
                i = ParseFencedCode(lines, i, fence, blocks);
            } else if (AtxLevel(line) > 0) {
                Block heading = new Block(Kind.HEADING);
                heading.level = AtxLevel(line);
                heading.text = AtxText(line);
                blocks.Add(heading);
                i++;
            } else if (IsThematicBreak(line)) {
                blocks.Add(new Block(Kind.RULE));
                i++;
            } else if (depth < MAX_DEPTH && IsQuote(line)) {
                i = ParseQuote(lines, i, depth, blocks);
            } else if (depth < MAX_DEPTH && ListMarker(line) != null) {
                i = ParseList(lines, i, depth, blocks);
            } else if (IsTableStart(lines, i)) {
                i = ParseTable(lines, i, blocks);
            } else if (Indent(line) >= 4) {
                i = ParseIndentedCode(lines, i, blocks);
            } else {
                i = ParseParagraph(lines, i, blocks);
            }
        }
        return blocks;
    }

    // Code between two fences of ``` or ~~~, the closing one as long or longer.
    private static int ParseFencedCode(List<String> lines, int i, int[] fence, List<Block> blocks) {
        StringBuilder code = new StringBuilder();
        int j = i + 1;
        for (; j < lines.Count; j++) {
            String line = lines[j];
            if (IsClosingFence(line, fence)) {
                j++;
                break;
            }
            if (code.Length > 0 || j > i + 1) {
                code.Append('\n');
            }
            // The content loses as much of its indent as the opening fence has.
            int strip = Math.Min(fence[2], Indent(line));
            code.Append(line.Substring(strip));
        }
        Block block = new Block(Kind.CODE);
        block.text = code.ToString();
        blocks.Add(block);
        return j;
    }

    // Lines indented 4 spaces or more, with the empty lines between them.
    private static int ParseIndentedCode(List<String> lines, int i, List<Block> blocks) {
        List<String> code = new List<String>();
        int j = i;
        while (j < lines.Count && (IsBlank(lines[j]) || Indent(lines[j]) >= 4)) {
            String line = lines[j];
            code.Add(IsBlank(line) ? "" : line.Substring(4));
            j++;
        }
        while (code.Count > 0 && code[code.Count - 1].Length == 0) {
            code.RemoveAt(code.Count - 1);
        }
        Block block = new Block(Kind.CODE);
        block.text = Join(code, "\n");
        blocks.Add(block);
        return j;
    }

    // The lines of a quote, without their >, and the lines that go on from
    // the text of the quote without one, as Markdown allows.
    private static int ParseQuote(List<String> lines, int i, int depth, List<Block> blocks) {
        List<String> quoted = new List<String>();
        int j = i;
        while (j < lines.Count) {
            String line = lines[j];
            if (IsQuote(line)) {
                quoted.Add(StripQuote(line));
            } else if (!IsBlank(line) && quoted.Count > 0 && !IsBlank(quoted[quoted.Count - 1])
                    && !Interrupts(line)) {
                quoted.Add(line);
            } else {
                break;
            }
            j++;
        }
        Block quote = new Block(Kind.QUOTE);
        quote.children.AddRange(ParseBlocks(quoted, depth + 1));
        blocks.Add(quote);
        return j;
    }

    // The items of a list: each is its first line after the marker and the
    // lines indented to its text, with what goes on from its text without an
    // indent. A list ends at an item of another kind, or at a line that is not
    // indented after an empty one.
    private static int ParseList(List<String> lines, int i, int depth, List<Block> blocks) {
        int[] first = ListMarker(lines[i]);
        Block list = new Block(Kind.LIST);
        list.ordered = first[1] < 0;
        list.start = list.ordered ? first[3] : 1;
        while (i < lines.Count) {
            int[] marker = ListMarker(lines[i]);
            if (marker == null || marker[1] != first[1] || IsThematicBreak(lines[i])) {
                break;
            }
            int column = marker[2];
            String line = lines[i];
            List<String> itemLines = new List<String>();
            itemLines.Add(line.Length > column ? line.Substring(column) : "");
            int j = i + 1;
            while (j < lines.Count) {
                String next = lines[j];
                String last = itemLines[itemLines.Count - 1];
                if (IsBlank(next)) {
                    itemLines.Add("");
                } else if (Indent(next) >= column) {
                    itemLines.Add(next.Substring(column));
                } else if (!IsBlank(last) && !Interrupts(next) && ListMarker(next) == null) {
                    itemLines.Add(next);
                } else {
                    break;
                }
                j++;
            }
            // The empty lines after the item are between it and the next.
            bool blankAfter = false;
            while (itemLines.Count > 1 && itemLines[itemLines.Count - 1].Length == 0) {
                itemLines.RemoveAt(itemLines.Count - 1);
                blankAfter = true;
            }
            if (itemLines.Contains("") || (blankAfter && j < lines.Count && SameList(lines[j], first))) {
                list.loose = true;
            }
            Block item = new Block(Kind.ITEM);
            item.children.AddRange(ParseBlocks(itemLines, depth + 1));
            list.children.Add(item);
            i = j;
        }
        blocks.Add(list);
        return i;
    }

    private static bool SameList(String line, int[] first) {
        int[] marker = ListMarker(line);
        return marker != null && marker[1] == first[1];
    }

    // A table of GitHub's Markdown: a header row, a row of dashes that sets the
    // alignment of each column, and the rows up to an empty line.
    private static int ParseTable(List<String> lines, int i, List<Block> blocks) {
        List<String> header = Cells(lines[i]);
        List<String> delimiters = Cells(lines[i + 1]);
        Block table = new Block(Kind.TABLE);
        table.rows = new List<List<String>>();
        table.alignments = new List<Alignment>();
        foreach (String delimiter in delimiters) {
            String d = Util.Trim(delimiter);
            bool left = d.StartsWith(":", StringComparison.Ordinal);
            bool right = d.EndsWith(":", StringComparison.Ordinal);
            table.alignments.Add((left && right) ? Alignment.CENTER : right ? Alignment.RIGHT : Alignment.LEFT);
        }
        table.rows.Add(header);
        int j = i + 2;
        while (j < lines.Count && !IsBlank(lines[j]) && lines[j].IndexOf('|') != -1
                && !Interrupts(lines[j])) {
            List<String> row = Cells(lines[j]);
            // Every row has a cell for each column.
            while (row.Count < header.Count) {
                row.Add("");
            }
            table.rows.Add(row.GetRange(0, header.Count));
            j++;
        }
        blocks.Add(table);
        return j;
    }

    private static bool IsTableStart(List<String> lines, int i) {
        if (i + 1 >= lines.Count || lines[i].IndexOf('|') == -1 || Indent(lines[i]) >= 4) {
            return false;
        }
        String delimiterRow = lines[i + 1];
        if (delimiterRow.IndexOf('-') == -1 || Indent(delimiterRow) >= 4) {
            return false;
        }
        List<String> delimiters = Cells(delimiterRow);
        foreach (String delimiter in delimiters) {
            String d = Util.Trim(delimiter);
            int from = d.StartsWith(":", StringComparison.Ordinal) ? 1 : 0;
            int to = d.EndsWith(":", StringComparison.Ordinal) && d.Length > from ? d.Length - 1 : d.Length;
            if (to <= from) {
                return false;
            }
            for (int k = from; k < to; k++) {
                if (d[k] != '-') {
                    return false;
                }
            }
        }
        return Cells(lines[i]).Count == delimiters.Count;
    }

    // The cells of a row, split at the | that no backslash escapes, without
    // the | at the start and at the end of the row.
    private static List<String> Cells(String line) {
        String row = Util.Trim(line);
        if (row.StartsWith("|", StringComparison.Ordinal)) {
            row = row.Substring(1);
        }
        if (row.EndsWith("|", StringComparison.Ordinal) && !row.EndsWith("\\|", StringComparison.Ordinal)) {
            row = row.Substring(0, row.Length - 1);
        }
        List<String> cells = new List<String>();
        StringBuilder cell = new StringBuilder();
        for (int k = 0; k < row.Length; k++) {
            char ch = row[k];
            if (ch == '\\' && k + 1 < row.Length) {
                cell.Append(ch).Append(row[k + 1]);
                k++;
            } else if (ch == '|') {
                cells.Add(Util.Trim(cell.ToString()));
                cell.Length = 0;
            } else {
                cell.Append(ch);
            }
        }
        cells.Add(Util.Trim(cell.ToString()));
        return cells;
    }

    // The lines of a paragraph, up to an empty line or a line that starts
    // another block. A line of = or - under them makes them a heading.
    private static int ParseParagraph(List<String> lines, int i, List<Block> blocks) {
        List<String> text = new List<String>();
        int j = i;
        while (j < lines.Count && !IsBlank(lines[j])) {
            String line = lines[j];
            if (j > i) {
                int level = SetextLevel(line);
                if (level > 0) {
                    Block heading = new Block(Kind.HEADING);
                    heading.level = level;
                    heading.text = Join(text, "\n");
                    blocks.Add(heading);
                    return j + 1;
                }
                if (Interrupts(line)) {
                    break;
                }
            }
            text.Add(Util.Trim(line));
            j++;
        }
        String paragraph = Join(text, "\n");
        Block image = Image(paragraph);
        blocks.Add(image != null ? image : Paragraph(paragraph));
        return j;
    }

    private static Block Paragraph(String text) {
        Block block = new Block(Kind.PARAGRAPH);
        block.text = text;
        return block;
    }

    // An image alone in its paragraph: ![alt text](source), with no space in the source.
    private static Block Image(String text) {
        if (!text.StartsWith("![", StringComparison.Ordinal) || !text.EndsWith(")", StringComparison.Ordinal)) {
            return null;
        }
        int close = text.IndexOf("](", StringComparison.Ordinal);
        if (close == -1 || text.IndexOf(']', 2) != close) {
            return null;
        }
        String source = text.Substring(close + 2, text.Length - 1 - (close + 2));
        if (source.Length == 0) {
            return null;
        }
        for (int k = 0; k < source.Length; k++) {
            char ch = source[k];
            if (Util.IsJavaWhitespace(ch) || ch == '(' || ch == ')') {
                return null;
            }
        }
        Block block = new Block(Kind.IMAGE);
        block.text = text.Substring(2, close - 2);
        block.source = source;
        return block;
    }

    // A line that ends a paragraph and starts another block, with no empty
    // line before it: a fence, a heading, a thematic break, a quote, or a list
    // item with text, of a numbered list only when it starts at 1.
    private static bool Interrupts(String line) {
        if (Fence(line) != null || AtxLevel(line) > 0 || IsThematicBreak(line) || IsQuote(line)) {
            return true;
        }
        int[] marker = ListMarker(line);
        return marker != null && !IsBlank(line.Substring(Math.Min(marker[2], line.Length)))
                && (marker[1] >= 0 || marker[3] == 1);
    }

    private static bool IsBlank(String line) {
        return Util.Trim(line).Length == 0;
    }

    private static int Indent(String line) {
        int n = 0;
        while (n < line.Length && line[n] == ' ') {
            n++;
        }
        return n;
    }

    // A fence of three or more ` or ~, indented 3 spaces at most, as
    // {the character, its count, the indent}; a ` fence has no ` after it.
    private static int[] Fence(String line) {
        int n = Indent(line);
        if (n > 3 || n >= line.Length) {
            return null;
        }
        char ch = line[n];
        if (ch != '`' && ch != '~') {
            return null;
        }
        int count = 0;
        while (n + count < line.Length && line[n + count] == ch) {
            count++;
        }
        if (count < 3 || (ch == '`' && line.IndexOf('`', n + count) != -1)) {
            return null;
        }
        return new int[] {ch, count, n};
    }

    private static bool IsClosingFence(String line, int[] fence) {
        int n = Indent(line);
        if (n > 3) {
            return false;
        }
        int count = 0;
        while (n + count < line.Length && line[n + count] == fence[0]) {
            count++;
        }
        return count >= fence[1] && Util.Trim(line.Substring(n + count)).Length == 0;
    }

    // The level of a heading of # to ######, or 0.
    private static int AtxLevel(String line) {
        int n = Indent(line);
        if (n > 3) {
            return 0;
        }
        int level = 0;
        while (n + level < line.Length && line[n + level] == '#') {
            level++;
        }
        if (level < 1 || level > 6) {
            return 0;
        }
        int after = n + level;
        return (after == line.Length || line[after] == ' ') ? level : 0;
    }

    // The text of a heading of #, without the #s that close it.
    private static String AtxText(String line) {
        String text = Util.Trim(line);
        int k = 0;
        while (k < text.Length && text[k] == '#') {
            k++;
        }
        text = Util.Trim(text.Substring(k));
        int end = text.Length;
        while (end > 0 && text[end - 1] == '#') {
            end--;
        }
        if (end == 0 || text[end - 1] == ' ') {
            text = Util.Trim(text.Substring(0, end));
        }
        return text;
    }

    // A line of three or more *, - or _, all the same, with spaces between them.
    private static bool IsThematicBreak(String line) {
        if (Indent(line) > 3) {
            return false;
        }
        char mark = (char) 0;
        int count = 0;
        for (int k = 0; k < line.Length; k++) {
            char ch = line[k];
            if (ch == ' ') {
                continue;
            }
            if ((ch != '*' && ch != '-' && ch != '_') || (mark != 0 && ch != mark)) {
                return false;
            }
            mark = ch;
            count++;
        }
        return count >= 3;
    }

    // The level of the heading that a line of = (1) or of - (2) under a paragraph makes, or 0.
    private static int SetextLevel(String line) {
        if (Indent(line) > 3) {
            return 0;
        }
        String text = Util.Trim(line);
        if (text.Length == 0) {
            return 0;
        }
        char ch = text[0];
        if (ch != '=' && ch != '-') {
            return 0;
        }
        for (int k = 0; k < text.Length; k++) {
            if (text[k] != ch) {
                return 0;
            }
        }
        return (ch == '=') ? 1 : 2;
    }

    private static bool IsQuote(String line) {
        int n = Indent(line);
        return n <= 3 && n < line.Length && line[n] == '>';
    }

    // The line without its > and the space after it.
    private static String StripQuote(String line) {
        int n = Indent(line) + 1;
        if (n < line.Length && line[n] == ' ') {
            n++;
        }
        return line.Substring(n);
    }

    // The marker of a list item: -, + or * and a space, or 1 to 9 digits, . or
    // ) and a space, indented 3 spaces at most. Returns {the indent, the kind:
    // the bullet, or -1 after . and -2 after ), the column of the text, the
    // number}, or null. The text is 1 to 4 spaces after the marker; with more
    // the item starts with indented code, one space after it.
    internal static int[] ListMarker(String line) {
        int n = Indent(line);
        if (n > 3 || n >= line.Length) {
            return null;
        }
        char ch = line[n];
        int end;
        int kind;
        int number = 0;
        if (ch == '-' || ch == '+' || ch == '*') {
            end = n + 1;
            kind = ch;
        } else {
            int digits = 0;
            while (n + digits < line.Length && digits < 10
                    && line[n + digits] >= '0' && line[n + digits] <= '9') {
                digits++;
            }
            if (digits == 0 || digits > 9 || n + digits >= line.Length) {
                return null;
            }
            char delimiter = line[n + digits];
            if (delimiter != '.' && delimiter != ')') {
                return null;
            }
            number = Int32.Parse(line.Substring(n, digits), NumberStyles.None, CultureInfo.InvariantCulture);
            end = n + digits + 1;
            kind = (delimiter == '.') ? -1 : -2;
        }
        if (end < line.Length && line[end] != ' ') {
            return null;
        }
        int spaces = 0;
        while (end + spaces < line.Length && line[end + spaces] == ' ') {
            spaces++;
        }
        int column = (spaces >= 1 && spaces <= 4 && end + spaces < line.Length) ? end + spaces : end + 1;
        return new int[] {n, kind, column, number};
    }

    private static String Join(List<String> list, String separator) {
        StringBuilder buf = new StringBuilder();
        for (int k = 0; k < list.Count; k++) {
            if (k > 0) {
                buf.Append(separator);
            }
            buf.Append(list[k]);
        }
        return buf.ToString();
    }
}   // End of MarkdownParser.cs
}   // End of namespace PDFjet.NET
