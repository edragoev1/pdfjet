/*
 * Markup.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Makes paragraphs of text with the inline markup of Markdown: **bold**,
 * *italic*, ***bold italic***, `code` and [links](https://pdfjet.com), in the
 * fonts given for each, and as GitHub's Markdown has them, __bold__ and
 * _italic_, ~~strikethrough~~ of two ~, one being text here, as in ~5, and
 * links that are their address,
 * &lt;https://pdfjet.com&gt; or https://pdfjet.com alone. An _ emphasizes only
 * at the edge of a word, so that snake_case is text; an address alone ends
 * before the punctuation at its end, as the period of a sentence, and before a
 * ) that closes no ( of its own. A backslash before a punctuation character,
 * as in \*, makes it plain text, and so is a mark that has no match, such as the * of
 * 2 * 3. Emphasis can be inside a link, and neither inside code. A word keeps
 * its punctuation next to it in another style, as in **bold**, with no space
 * between them: the paragraph joins its text lines with Paragraph.addJoined.
 * <p>
 * A line that ends in two spaces or a backslash, and a &lt;br&gt;, are hard
 * line breaks, which TextFrame draws as the end of a row, and the spaces
 * before the line break are not in the text; two spaces and a line break at
 * the end of the text are not, and are dropped, and a backslash at the end of
 * the text is a backslash, as in CommonMark. Any other line break is a space,
 * none at the start of a row. \n, \r\n and \r are each a line break. The
 * spaces and tabs around a line break, hard or not, are not in the text, as
 * in CommonMark, and a space written &amp;#32; is. The entities &amp;amp;, &amp;lt;, &amp;gt;, &amp;quot;,
 * &amp;apos;, &amp;nbsp;, &amp;copy;, &amp;reg;, &amp;trade;, &amp;mdash;,
 * &amp;ndash;, &amp;hellip;, the quotes and a few more, and the numbers
 * &amp;#8212; and &amp;#x2014;, are the characters they name.
 * <p>
 * Headings, lists, block quotes and the rest of Markdown's blocks are not
 * read: paragraphs splits a text into paragraphs at its empty lines. The markup is read in time linear in the length of the
 * text, so it can come from anyone. Please see Example_18.
 */
public class Markup {
    private static final int BOLD = 1;
    private static final int ITALIC = 2;
    private static final int CODE = 4;
    private static final int STRIKE = 8;
    private static final int BREAK = 16;        // A hard line break

    // The named entities that are read, as GitHub's Markdown reads them: the
    // markup's own characters, the spaces and the punctuation of typeset text.
    private static final Map<String, Integer> ENTITIES = new HashMap<String, Integer>();
    static {
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
        for (int i = 0; i < names.length; i++) {
            ENTITIES.put(names[i], codes[i]);
        }
    }

    private final Font regular;
    private final Font bold;
    private final Font italic;
    private final Font boldItalic;
    private final Font code;
    private int linkColor = Color.blue;
    private boolean linkUnderline = true;

    /**
     * Creates the markup of paragraphs in the fonts, each at its size.
     *
     * @param regular the font of the text.
     * @param bold the font of **bold** text.
     * @param italic the font of *italic* text.
     * @param boldItalic the font of ***bold italic*** text.
     * @param code the font of `code`, usually a monospaced font.
     */
    public Markup(Font regular, Font bold, Font italic, Font boldItalic, Font code) {
        this.regular = regular;
        this.bold = bold;
        this.italic = italic;
        this.boldItalic = boldItalic;
        this.code = code;
    }

    /**
     * Sets the color of the text of links. The default is Color.blue.
     * Color.transparent leaves it unchanged.
     *
     * @param color the color as a 0xRRGGBB value.
     * @return this Markup.
     */
    public Markup setLinkColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        this.linkColor = color;
        return this;
    }

    /**
     * Sets whether the text of links is underlined, as it is by default.
     *
     * @param underline true to underline the links.
     * @return this Markup.
     */
    public Markup setLinkUnderline(boolean underline) {
        this.linkUnderline = underline;
        return this;
    }

    /**
     * Returns the paragraphs of the text, which empty lines separate.
     *
     * @param text the text with its markup.
     * @return the paragraphs, none for a text with no words.
     */
    public List<Paragraph> paragraphs(String text) {
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        StringBuilder buf = new StringBuilder();
        for (String line : lineFeeds(text).split("\n", -1)) {
            if (line.trim().isEmpty()) {
                addParagraph(paragraphs, buf);
            } else {
                buf.append(line).append('\n');
            }
        }
        addParagraph(paragraphs, buf);
        return paragraphs;
    }

    private void addParagraph(List<Paragraph> paragraphs, StringBuilder buf) {
        if (buf.length() > 0) {
            // Without the line break after the last line, which made a
            // backslash at the end of the paragraph a hard line break, and
            // dropped it, and without the whitespace at its end, as
            // CommonMark strips the final spaces of a paragraph: "0  \n" is
            // "0"
            buf.setLength(buf.length() - 1);
            while (buf.length() > 0 && Util.isWhitespace(buf.charAt(buf.length() - 1))) {
                buf.setLength(buf.length() - 1);
            }
            Paragraph paragraph = paragraph(buf.toString());
            if (!paragraph.lines.isEmpty()) {
                paragraphs.add(paragraph);
            }
            buf.setLength(0);
        }
    }

    // Returns the text with its line endings \n: the parser sees no \r\n or
    // \r.
    private static String lineFeeds(String text) {
        return text.replace("\r\n", "\n").replace('\r', '\n');
    }

    /**
     * Returns the text as one paragraph: a text line for each run of text in
     * one style, each joined to the text before it.
     *
     * @param text the text with its markup.
     * @return the paragraph.
     */
    public Paragraph paragraph(String text) {
        text = lineFeeds(text);
        Parser parser = new Parser(text);
        int end = parser.trailingBreak();
        if (end < text.length()) {
            // A hard line break at the end of the text is no break, as in
            // CommonMark, and the whitespace at the end is dropped: "0  \n"
            // is the text line "0"
            text = text.substring(0, end);
            parser = new Parser(text);
        }
        List<Node> nodes = new ArrayList<Node>();
        parser.parse(0, text.length(), null, nodes);
        Paragraph paragraph = new Paragraph();
        // The runs of one style and link, in order.
        StringBuilder buf = new StringBuilder();
        int flags = 0;
        String link = null;
        boolean joined = false;
        for (Node node : nodes) {
            if ((node.flags & BREAK) != 0) {
                // A hard line break: the row ends after the text before it,
                // and the text after it is not joined to it
                if (buf.length() > 0) {
                    addRun(paragraph, buf.toString(), flags, link, joined);
                    buf.setLength(0);
                }
                if (!paragraph.lines.isEmpty()) {
                    paragraph.lines.get(paragraph.lines.size() - 1).lineBreaks++;
                }
                joined = false;
                continue;
            }
            if (buf.length() > 0 && (node.flags != flags || !Objects.equals(node.link, link))) {
                joined = addRun(paragraph, buf.toString(), flags, link, joined);
                buf.setLength(0);
            }
            flags = node.flags;
            link = node.link;
            buf.append(node.text);
        }
        if (buf.length() > 0) {
            addRun(paragraph, buf.toString(), flags, link, joined);
        }
        return paragraph;
    }

    // Adds a run of text in one style. A run of spaces only is not added: it
    // puts a space before the next run, as between two words. Returns whether
    // the next run is joined to this one.
    private boolean addRun(Paragraph paragraph, String text, int flags, String link, boolean joined) {
        if (text.trim().isEmpty()) {
            return false;
        }
        Font font = ((flags & CODE) != 0) ? code
                : ((flags & BOLD) != 0 && (flags & ITALIC) != 0) ? boldItalic
                : ((flags & BOLD) != 0) ? bold
                : ((flags & ITALIC) != 0) ? italic : regular;
        TextLine textLine = new TextLine(font, text);
        textLine.keepSpaces = (flags & CODE) != 0;
        if ((flags & STRIKE) != 0) {
            textLine.setStrikeout(true);
        }
        if (link != null) {
            textLine.setURIAction(link);
            textLine.setTextColor(linkColor);
            // The underline in the color of the link: it was black under the
            // blue (review of 15 October 2026)
            textLine.setDecorationColor(linkColor);
            textLine.setUnderline(linkUnderline);
        }
        if (joined) {
            paragraph.addJoined(textLine);
        } else {
            paragraph.add(textLine);
        }
        return true;
    }

    // Text in one style, or a run of *, of _ or of ~~ that may open or close
    // emphasis or a strikethrough, which is text too as far as it does not.
    // The text is null while the node is a run whose emphasis is not matched
    // yet.
    private static final class Node {
        String text;
        int flags;
        String link;
        // Of a run: its character, *, _ or ~, where it is among the nodes, how
        // many of its characters are not matched yet, whether they can open and
        // close emphasis, and the emphasis the run opens and closes, counted in
        // levels of bold, italic and strikethrough.
        char ch;
        int index;
        int count;
        boolean canOpen;
        boolean canClose;
        int[] opens;
        int[] closes;

        Node(String text, int flags, String link) {
            this.text = text;
            this.flags = flags;
            this.link = link;
        }
    }

    private static final class Parser {
        private final String text;
        // Where each ] that closes a [ is, by the index of the [.
        private final int[] closingBracket;
        // Where each ) that closes a ( is, by the index of the (, and the index
        // of the next space at or after each index.
        private final int[] closingParenthesis;
        private final int[] nextSpace;
        // The runs of backticks, by their length: where each starts, in order,
        // and the first one of each length that can still close a code span.
        private final Map<Integer, List<Integer>> backtickRuns = new HashMap<Integer, List<Integer>>();
        private final Map<Integer, Integer> backtickCursor = new HashMap<Integer, Integer>();

        Parser(String text) {
            this.text = text;
            int n = text.length();
            closingBracket = new int[n];
            Arrays.fill(closingBracket, -1);
            Deque<Integer> open = new ArrayDeque<Integer>();
            for (int i = 0; i < n; i++) {
                char ch = text.charAt(i);
                if (ch == '\\' && i + 1 < n) {
                    i++;
                } else if (ch == '[') {
                    open.push(i);
                } else if (ch == ']' && !open.isEmpty()) {
                    closingBracket[open.pop()] = i;
                }
            }
            closingParenthesis = new int[n];
            Arrays.fill(closingParenthesis, -1);
            open.clear();
            for (int i = 0; i < n; i++) {
                char ch = text.charAt(i);
                if (ch == '(') {
                    open.push(i);
                } else if (ch == ')' && !open.isEmpty()) {
                    closingParenthesis[open.pop()] = i;
                }
            }
            nextSpace = new int[n + 1];
            nextSpace[n] = n;
            for (int i = n - 1; i >= 0; i--) {
                nextSpace[i] = Util.isWhitespace(text.charAt(i)) ? i : nextSpace[i + 1];
            }
            for (int i = 0; i < n; ) {
                if (text.charAt(i) == '\\' && i + 1 < n && text.charAt(i + 1) != '`') {
                    i += 2;
                } else if (text.charAt(i) == '`') {
                    // A backtick after a backslash is text where it would open
                    // code, as parse reads it, but closes code, in which a
                    // backslash is text, as in GitHub's Markdown: `foo\` was
                    // not closed
                    int start = i;
                    while (i < n && text.charAt(i) == '`') {
                        i++;
                    }
                    Integer length = i - start;
                    List<Integer> runs = backtickRuns.get(length);
                    if (runs == null) {
                        runs = new ArrayList<Integer>();
                        backtickRuns.put(length, runs);
                        backtickCursor.put(length, 0);
                    }
                    runs.add(start);
                } else {
                    i++;
                }
            }
        }

        // Reads the text from start to end into nodes, all in the link when it
        // is not null, with the emphasis of the runs of *, _ and ~~ matched.
        void parse(int start, int end, String link, List<Node> nodes) {
            int first = nodes.size();
            StringBuilder buf = new StringBuilder();
            // The spaces and tabs that end at spaceEnd, each one space in the
            // buffer.
            int spaces = 0;
            int spaceEnd = start;
            int i = start;
            while (i < end) {
                char ch = text.charAt(i);
                int addressEnd;
                int breakEnd = breakEnd(i, end);
                int[] entity;
                if (breakEnd > i && ch == '\\' && onlySpaceFrom(breakEnd)) {
                    // A backslash at the end of the text is text, and no hard
                    // line break, as in CommonMark: "0\\\n" is the text line
                    // "0\\"
                    buf.append(ch);
                    i = end;
                } else if (breakEnd > i || ch == '\n') {
                    if (ch == '\n' && spaceEnd == i) {
                        // The spaces before a line break are not in the text,
                        // as in CommonMark: "one  \ntwo" is "one", a break
                        // and "two"
                        buf.setLength(buf.length() - spaces);
                    }
                    spaces = 0;
                    if (breakEnd > i) {
                        flush(buf, link, nodes);
                        nodes.add(new Node("", BREAK, link));
                        i = breakEnd;
                    } else {
                        // A line break is a space, but for one at the start of
                        // a row
                        if (buf.length() > 0 || (!nodes.isEmpty()
                                && (nodes.get(nodes.size() - 1).flags & BREAK) == 0)) {
                            buf.append(' ');
                            spaces = 1;
                        }
                        i++;
                    }
                    if (text.charAt(i - 1) == '\n') {
                        i = lineStart(i, end);
                        spaceEnd = i;
                    }
                } else if ((entity = entity(i, end)) != null) {
                    buf.appendCodePoint(entity[0]);
                    i = entity[1];
                } else if (ch == '\\' && i + 1 < end && isASCIIPunctuation(text.charAt(i + 1))) {
                    buf.append(text.charAt(i + 1));
                    i += 2;
                } else if (ch == '`') {
                    int length = 1;
                    while (i + length < end && text.charAt(i + length) == '`') {
                        length++;
                    }
                    int close = closingBackticks(length, i + length, end);
                    if (close == -1) {
                        buf.append(text, i, i + length);
                    } else {
                        flush(buf, link, nodes);
                        nodes.add(new Node(codeText(text.substring(i + length, close)), CODE, link));
                    }
                    i = (close == -1) ? i + length : close + length;
                } else if (ch == '*' || ch == '_' || ch == '~') {
                    int length = 1;
                    while (i + length < end && text.charAt(i + length) == ch) {
                        length++;
                    }
                    if (ch == '~' && length != 2) {
                        // A strikethrough is two ~, and one or three are text, as in ~5
                        buf.append(text, i, i + length);
                        i += length;
                        continue;
                    }
                    flush(buf, link, nodes);
                    Node run = new Node(null, 0, link);
                    run.ch = ch;
                    run.index = nodes.size();
                    run.count = length;
                    // Before the run, or after it, a space, or the start or the end
                    boolean left = i + length < end && !Util.isWhitespace(text.charAt(i + length));
                    boolean right = i > start && !Util.isWhitespace(text.charAt(i - 1));
                    run.canOpen = left;
                    run.canClose = right;
                    if (ch == '_') {
                        // Inside a word, as in snake_case, an _ is text
                        run.canOpen = left && (!right || isASCIIPunctuation(text.charAt(i - 1)));
                        run.canClose = right && (!left || isASCIIPunctuation(text.charAt(i + length)));
                    }
                    nodes.add(run);
                    i += length;
                } else if ((addressEnd = autolinkEnd(i, start, end, link)) > i) {
                    flush(buf, link, nodes);
                    String url = (ch == '<') ? text.substring(i + 1, addressEnd - 1) : text.substring(i, addressEnd);
                    nodes.add(new Node(url, 0, url));
                    i = addressEnd;
                } else if (ch == '[' && link == null && isLink(i, end)) {
                    int close = closingBracket[i];
                    int urlEnd = closingParenthesis[close + 1];
                    flush(buf, link, nodes);
                    parse(i + 1, close, decoded(close + 2, urlEnd), nodes);
                    i = urlEnd + 1;
                } else {
                    if (ch == ' ' || ch == '\t') {
                        if (spaceEnd != i) {
                            spaces = 0;
                        }
                        spaces++;
                        spaceEnd = i + 1;
                    }
                    buf.append(Util.isWhitespace(ch) ? ' ' : ch);
                    i++;
                }
            }
            flush(buf, link, nodes);
            matchEmphasis(nodes, first);
        }

        // Returns where the whitespace at the end of the text starts when a
        // hard line break is in it, two spaces before a line break, or the
        // length of the text when none is.
        int trailingBreak() {
            int n = text.length();
            int start = n;
            while (start > 0 && Util.isWhitespace(text.charAt(start - 1))) {
                start--;
            }
            for (int i = start; i < n; i++) {
                if (breakEnd(i, n) > i) {
                    return start;
                }
            }
            return n;
        }

        // Returns whether the text from i to its end is whitespace, or
        // nothing.
        private boolean onlySpaceFrom(int i) {
            for (; i < text.length(); i++) {
                if (!Util.isWhitespace(text.charAt(i))) {
                    return false;
                }
            }
            return true;
        }

        // Returns where the text of the line at i starts before the end:
        // after its spaces and tabs, which are not in the text after a line
        // break, hard or not, as in CommonMark: "one  \n  two" is "one", a
        // break and "two". A space written &#32; is text.
        private int lineStart(int i, int end) {
            while (i < end && (text.charAt(i) == ' ' || text.charAt(i) == '\t')) {
                i++;
            }
            return i;
        }

        // Returns where a hard line break at i ends, or i when there is none:
        // a line break after two spaces, a backslash and a line break, or a
        // <br>, <br/> or <br /> in capitals or not.
        private int breakEnd(int i, int end) {
            char ch = text.charAt(i);
            if (ch == '\n' && i >= 2 && text.charAt(i - 1) == ' ' && text.charAt(i - 2) == ' ') {
                return i + 1;
            }
            if (ch == '\\' && i + 1 < end && text.charAt(i + 1) == '\n') {
                return i + 2;
            }
            if (ch == '<' && i + 3 < end && (text.charAt(i + 1) == 'b' || text.charAt(i + 1) == 'B')
                    && (text.charAt(i + 2) == 'r' || text.charAt(i + 2) == 'R')) {
                int j = i + 3;
                while (j < end && text.charAt(j) == ' ') {
                    j++;
                }
                if (j < end && text.charAt(j) == '/') {
                    j++;
                }
                if (j < end && text.charAt(j) == '>') {
                    return j + 1;
                }
            }
            return i;
        }

        // Returns the character of the entity at i and where the entity ends,
        // or null when there is none: a name of ENTITIES, or a number of up to
        // seven digits, or of six in hexadecimal after an x, that is a
        // character, between & and ;.
        private int[] entity(int i, int end) {
            if (text.charAt(i) != '&') {
                return null;
            }
            int semicolon = -1;
            for (int j = i + 1; j < end && j <= i + 10; j++) {
                if (text.charAt(j) == ';') {
                    semicolon = j;
                    break;
                }
            }
            if (semicolon == -1) {
                return null;
            }
            String name = text.substring(i + 1, semicolon);
            Integer code = ENTITIES.get(name);
            if (code != null) {
                return new int[] {code, semicolon + 1};
            }
            if (name.length() < 2 || name.charAt(0) != '#') {
                return null;
            }
            boolean hex = name.charAt(1) == 'x' || name.charAt(1) == 'X';
            String digits = name.substring(hex ? 2 : 1);
            if (digits.isEmpty() || digits.length() > (hex ? 6 : 7)) {
                return null;
            }
            int value = 0;
            for (int k = 0; k < digits.length(); k++) {
                int digit = asciiDigit(digits.charAt(k), hex);
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
        private String decoded(int start, int end) {
            StringBuilder buf = new StringBuilder();
            int i = start;
            while (i < end) {
                int[] entity = entity(i, end);
                if (entity != null) {
                    buf.appendCodePoint(entity[0]);
                    i = entity[1];
                } else {
                    buf.append(text.charAt(i));
                    i++;
                }
            }
            return buf.toString();
        }

        // The value of an ASCII digit, of 0 to 9, or of a to f and A to F in
        // hexadecimal, or -1.
        private static int asciiDigit(char ch, boolean hex) {
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

        // A [ starts a link when its ] is before the end, a ( follows the ]
        // at once, and the ) that closes it ends the URL before any space:
        // [text](url), with the parentheses in the URL in pairs, as in
        // CommonMark: [Foo](https://x/Foo_(bar)).
        private boolean isLink(int i, int end) {
            int close = closingBracket[i];
            if (close == -1 || close + 1 >= end || text.charAt(close + 1) != '(') {
                return false;
            }
            int urlEnd = closingParenthesis[close + 1];
            return urlEnd < end && urlEnd > close + 2 && nextSpace[close + 2] > urlEnd;
        }

        // Returns where a link that is its address ends, after its > when it is
        // in < and >, or i when there is none at i, or the text is in a link
        // already: an address of http:// or https:// and a host, in < and >
        // with no space in it, or alone, after a space, the start, a (, or a *,
        // _ or ~ of emphasis. An address alone ends at a space or a <, and then
        // before the punctuation at its end, ? ! . , : * _ and ~, and before
        // each ) at its end that closes no ( in it, as GitHub's Markdown reads it.
        private int autolinkEnd(int i, int start, int end, String link) {
            if (link != null || (text.charAt(i) != '<' && text.charAt(i) != 'h' && text.charAt(i) != 'H')) {
                return i;
            }
            int from = i;
            if (text.charAt(i) == '<') {
                from = i + 1;
            } else if (i > start && !Util.isWhitespace(text.charAt(i - 1))
                    && "(*_~".indexOf(text.charAt(i - 1)) == -1) {
                return i;
            }
            int host = schemeEnd(from, end);
            if (host == -1 || host >= end || !isHostChar(text.charAt(host))) {
                return i;
            }
            int j = host;
            while (j < end && !Util.isWhitespace(text.charAt(j))
                    && text.charAt(j) != '<' && text.charAt(j) != '>') {
                j++;
            }
            if (text.charAt(i) == '<') {
                return (j < end && text.charAt(j) == '>') ? j + 1 : i;
            }
            int opening = count(from, j, '(');
            int closing = count(from, j, ')');
            while (j > host) {
                char last = text.charAt(j - 1);
                if ("?!.,:*_~".indexOf(last) != -1) {
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
        private int schemeEnd(int i, int end) {
            for (String scheme : new String[] {"https://", "http://"}) {
                int n = scheme.length();
                if (i + n > end) {
                    continue;
                }
                int k = 0;
                while (k < n) {
                    char ch = text.charAt(i + k);
                    if (ch >= 'A' && ch <= 'Z') {
                        ch += 'a' - 'A';
                    }
                    if (ch != scheme.charAt(k)) {
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
        private static boolean isHostChar(char ch) {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch > 0x7F;
        }

        private int count(int from, int to, char ch) {
            int n = 0;
            for (int k = from; k < to; k++) {
                if (text.charAt(k) == ch) {
                    n++;
                }
            }
            return n;
        }

        // Returns where the next run of backticks of the length starts, after
        // from and before end, or -1.
        private int closingBackticks(int length, int from, int end) {
            List<Integer> runs = backtickRuns.get(length);
            if (runs == null) {
                return -1;
            }
            int cursor = backtickCursor.get(length);
            while (cursor < runs.size() && runs.get(cursor) < from) {
                cursor++;
            }
            backtickCursor.put(length, cursor);
            if (cursor < runs.size() && runs.get(cursor) + length <= end) {
                return runs.get(cursor);
            }
            return -1;
        }

        // The text of a code span: a line break is a space, and one space at
        // each end is taken away when both ends have one, as in `` `a` ``.
        private static String codeText(String code) {
            code = code.replace('\n', ' ');
            if (code.length() >= 2 && code.charAt(0) == ' ' && code.charAt(code.length() - 1) == ' '
                    && !code.trim().isEmpty()) {
                code = code.substring(1, code.length() - 1);
            }
            return code;
        }

        private static void flush(StringBuilder buf, String link, List<Node> nodes) {
            if (buf.length() > 0) {
                nodes.add(new Node(buf.toString(), 0, link));
                buf.setLength(0);
            }
        }

        // Matches the runs from the index on, each closer with the nearest
        // opener of its character before it: two * or _ of each make bold and
        // one italic, and ~~ a strikethrough. The runs of the other characters
        // between a closer and its opener can no longer match, so that emphasis
        // and strikethrough nest and do not cross. Then gives every node the
        // emphasis it is in, and turns what is left of each run into text.
        private static void matchEmphasis(List<Node> nodes, int first) {
            // The runs that can still open, of *, of _ and of ~
            Deque<Node> stars = new ArrayDeque<Node>();
            Deque<Node> underscores = new ArrayDeque<Node>();
            Deque<Node> tildes = new ArrayDeque<Node>();
            for (int i = first; i < nodes.size(); i++) {
                Node node = nodes.get(i);
                if (node.text != null) {
                    continue;
                }
                if (node.ch == '~') {
                    if (node.canClose && !tildes.isEmpty()) {
                        Node opener = tildes.pop();
                        dropAfter(stars, opener.index);
                        dropAfter(underscores, opener.index);
                        opener.opens = add(opener.opens, STRIKE);
                        node.closes = add(node.closes, STRIKE);
                        opener.count = 0;
                        node.count = 0;
                    }
                    if (node.count > 0 && node.canOpen) {
                        tildes.push(node);
                    }
                    continue;
                }
                Deque<Node> own = (node.ch == '_') ? underscores : stars;
                Deque<Node> other = (node.ch == '_') ? stars : underscores;
                if (node.canClose) {
                    while (node.count > 0 && !own.isEmpty()) {
                        Node opener = own.peek();
                        dropAfter(other, opener.index);
                        dropAfter(tildes, opener.index);
                        int use = (node.count >= 2 && opener.count >= 2) ? 2 : 1;
                        int flag = (use == 2) ? BOLD : ITALIC;
                        opener.opens = add(opener.opens, flag);
                        node.closes = add(node.closes, flag);
                        opener.count -= use;
                        node.count -= use;
                        if (opener.count == 0) {
                            own.pop();
                        }
                    }
                }
                if (node.count > 0 && node.canOpen) {
                    own.push(node);
                }
            }
            // The emphasis in effect, in levels, since bold can be in bold.
            int[] levels = new int[3];
            for (int i = first; i < nodes.size(); i++) {
                Node node = nodes.get(i);
                if (node.text == null) {
                    for (int k = 0; k < levels.length; k++) {
                        levels[k] -= level(node.closes, k);
                    }
                    node.text = repeat(node.ch, node.count);
                    node.flags = emphasis(levels);
                    for (int k = 0; k < levels.length; k++) {
                        levels[k] += level(node.opens, k);
                    }
                } else {
                    node.flags |= emphasis(levels);
                }
            }
        }

        // Drops the openers after the index from the stack.
        private static void dropAfter(Deque<Node> openers, int index) {
            while (!openers.isEmpty() && openers.peek().index > index) {
                openers.pop();
            }
        }

        // The flags of the levels of bold, italic and strikethrough in effect.
        private static int emphasis(int[] levels) {
            return ((levels[0] > 0) ? BOLD : 0) | ((levels[1] > 0) ? ITALIC : 0)
                    | ((levels[2] > 0) ? STRIKE : 0);
        }

        // The levels of bold, italic and strikethrough with one more of the flag.
        private static int[] add(int[] levels, int flag) {
            if (levels == null) {
                levels = new int[3];
            }
            levels[(flag == BOLD) ? 0 : (flag == ITALIC) ? 1 : 2]++;
            return levels;
        }

        // The level of bold (0), italic (1) or strikethrough (2).
        private static int level(int[] levels, int k) {
            return (levels == null) ? 0 : levels[k];
        }

        private static String repeat(char ch, int count) {
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < count; i++) {
                buf.append(ch);
            }
            return buf.toString();
        }

        private static boolean isASCIIPunctuation(char ch) {
            return (ch >= '!' && ch <= '/') || (ch >= ':' && ch <= '@')
                    || (ch >= '[' && ch <= '`') || (ch >= '{' && ch <= '~');
        }
    }
}   // End of Markup.java
