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
 * fonts given for each. A backslash before a punctuation character, as in \*,
 * makes it plain text, and so is a mark that has no match, such as the * of
 * 2 * 3. Emphasis can be inside a link, and neither inside code. A word keeps
 * its punctuation next to it in another style, as in **bold**, with no space
 * between them: the paragraph joins its text lines with Paragraph.addJoined.
 * <p>
 * Headings, lists, block quotes and the rest of Markdown's blocks are not
 * read: a line break is a space, and paragraphs splits a text into paragraphs
 * at its empty lines. The markup is read in time linear in the length of the
 * text, so it can come from anyone. Please see Example_53.
 */
public class Markup {
    private static final int BOLD = 1;
    private static final int ITALIC = 2;
    private static final int CODE = 4;

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
     *
     * @param color the color as a 0xRRGGBB value.
     * @return this Markup.
     */
    public Markup setLinkColor(int color) {
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
        for (String line : text.split("\n", -1)) {
            if (line.endsWith("\r")) {
                line = line.substring(0, line.length() - 1);
            }
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
            Paragraph paragraph = paragraph(buf.toString());
            if (!paragraph.lines.isEmpty()) {
                paragraphs.add(paragraph);
            }
            buf.setLength(0);
        }
    }

    /**
     * Returns the text as one paragraph: a text line for each run of text in
     * one style, each joined to the text before it.
     *
     * @param text the text with its markup.
     * @return the paragraph.
     */
    public Paragraph paragraph(String text) {
        Parser parser = new Parser(text);
        List<Node> nodes = new ArrayList<Node>();
        parser.parse(0, text.length(), null, nodes);
        Paragraph paragraph = new Paragraph();
        // The runs of one style and link, in order.
        StringBuilder buf = new StringBuilder();
        int flags = 0;
        String link = null;
        boolean joined = false;
        for (Node node : nodes) {
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
        if (link != null) {
            textLine.setURIAction(link);
            textLine.setTextColor(linkColor);
            textLine.setUnderline(linkUnderline);
        }
        if (joined) {
            paragraph.addJoined(textLine);
        } else {
            paragraph.add(textLine);
        }
        return true;
    }

    // Text in one style, or a run of * that may open or close emphasis, which
    // is text too as far as it does not.
    private static final class Node {
        String text;
        int flags;
        String link;
        // Of a run of *: how many of them are not matched yet, whether they can
        // open and close emphasis, and the emphasis the run opens and closes,
        // counted in levels of the flags.
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
        // The index of the next ) and of the next space at or after each index.
        private final int[] nextParenthesis;
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
            nextParenthesis = new int[n + 1];
            nextSpace = new int[n + 1];
            nextParenthesis[n] = n;
            nextSpace[n] = n;
            for (int i = n - 1; i >= 0; i--) {
                char ch = text.charAt(i);
                nextParenthesis[i] = (ch == ')') ? i : nextParenthesis[i + 1];
                nextSpace[i] = Character.isWhitespace(ch) ? i : nextSpace[i + 1];
            }
            for (int i = 0; i < n; ) {
                if (text.charAt(i) == '\\' && i + 1 < n) {
                    i += 2;
                } else if (text.charAt(i) == '`') {
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
        // is not null, with the emphasis of the runs of * matched.
        void parse(int start, int end, String link, List<Node> nodes) {
            int first = nodes.size();
            StringBuilder buf = new StringBuilder();
            int i = start;
            while (i < end) {
                char ch = text.charAt(i);
                if (ch == '\\' && i + 1 < end && isASCIIPunctuation(text.charAt(i + 1))) {
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
                } else if (ch == '*') {
                    int length = 1;
                    while (i + length < end && text.charAt(i + length) == '*') {
                        length++;
                    }
                    flush(buf, link, nodes);
                    Node run = new Node(null, 0, link);
                    run.count = length;
                    run.canOpen = i + length < end && !Character.isWhitespace(text.charAt(i + length));
                    run.canClose = i > start && !Character.isWhitespace(text.charAt(i - 1));
                    nodes.add(run);
                    i += length;
                } else if (ch == '[' && link == null && isLink(i, end)) {
                    int close = closingBracket[i];
                    int urlEnd = nextParenthesis[close + 2];
                    flush(buf, link, nodes);
                    parse(i + 1, close, text.substring(close + 2, urlEnd), nodes);
                    i = urlEnd + 1;
                } else {
                    buf.append(Character.isWhitespace(ch) ? ' ' : ch);
                    i++;
                }
            }
            flush(buf, link, nodes);
            matchEmphasis(nodes, first);
        }

        // A [ starts a link when its ] is before the end, a ( follows the ]
        // at once, and a ) ends the URL before any space: [text](url).
        private boolean isLink(int i, int end) {
            int close = closingBracket[i];
            if (close == -1 || close + 1 >= end || text.charAt(close + 1) != '(') {
                return false;
            }
            int urlEnd = nextParenthesis[close + 2];
            return urlEnd < end && urlEnd > close + 2 && nextSpace[close + 2] > urlEnd;
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
            code = code.replace('\n', ' ').replace('\r', ' ');
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

        // Matches the runs of * from the index on, each closer with the nearest
        // opener before it: two of each make bold and one italic, and the runs
        // between them can no longer match. Then gives every node the emphasis
        // it is in, and turns what is left of each run into text.
        private static void matchEmphasis(List<Node> nodes, int first) {
            Deque<Node> openers = new ArrayDeque<Node>();
            for (int i = first; i < nodes.size(); i++) {
                Node node = nodes.get(i);
                if (node.text != null) {
                    continue;
                }
                if (node.canClose) {
                    while (node.count > 0 && !openers.isEmpty()) {
                        Node opener = openers.peek();
                        int use = (node.count >= 2 && opener.count >= 2) ? 2 : 1;
                        int flag = (use == 2) ? BOLD : ITALIC;
                        opener.opens = add(opener.opens, flag);
                        node.closes = add(node.closes, flag);
                        opener.count -= use;
                        node.count -= use;
                        if (opener.count == 0) {
                            openers.pop();
                        }
                    }
                }
                if (node.count > 0 && node.canOpen) {
                    openers.push(node);
                }
            }
            // The emphasis in effect, in levels, since bold can be in bold.
            int boldLevel = 0;
            int italicLevel = 0;
            for (int i = first; i < nodes.size(); i++) {
                Node node = nodes.get(i);
                if (node.text == null) {
                    boldLevel -= level(node.closes, BOLD);
                    italicLevel -= level(node.closes, ITALIC);
                    node.text = repeat('*', node.count);
                    node.flags = emphasis(boldLevel, italicLevel);
                    boldLevel += level(node.opens, BOLD);
                    italicLevel += level(node.opens, ITALIC);
                } else {
                    node.flags |= emphasis(boldLevel, italicLevel);
                }
            }
        }

        private static int emphasis(int boldLevel, int italicLevel) {
            return ((boldLevel > 0) ? BOLD : 0) | ((italicLevel > 0) ? ITALIC : 0);
        }

        // The levels of bold and of italic, [bold, italic], with one more of the flag.
        private static int[] add(int[] levels, int flag) {
            if (levels == null) {
                levels = new int[2];
            }
            levels[(flag == BOLD) ? 0 : 1]++;
            return levels;
        }

        private static int level(int[] levels, int flag) {
            return (levels == null) ? 0 : levels[(flag == BOLD) ? 0 : 1];
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
