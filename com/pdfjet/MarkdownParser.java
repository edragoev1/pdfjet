/*
 * MarkdownParser.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Reads the blocks of a Markdown text into a tree, for Markdown to draw:
 * headings, paragraphs, code, block quotes, lists, thematic breaks, tables and
 * images. The text of the headings, the paragraphs and the table cells keeps
 * its inline markup, which Markup reads. Containers nest to MAX_DEPTH levels;
 * deeper, their lines are text, so that no input makes the reading slower
 * than linear in its length times that depth.
 */
final class MarkdownParser {
    static final int MAX_DEPTH = 32;

    enum Kind { HEADING, PARAGRAPH, CODE, QUOTE, LIST, ITEM, RULE, TABLE, IMAGE }

    static final class Block {
        final Kind kind;
        int level;              // Of a heading, 1 to 6
        String text;            // Of a heading or a paragraph, the code, or the alt text of an image
        String source;          // Of an image
        boolean ordered;        // Of a list
        int start = 1;          // Of an ordered list
        boolean loose;          // Of a list whose items are apart, with an empty line between them
        final List<Block> children = new ArrayList<Block>();    // Of a quote, a list or an item
        List<List<String>> rows;        // Of a table, the header row first
        List<Alignment> alignments;     // Of a table, of each column, or null

        Block(Kind kind) {
            this.kind = kind;
        }
    }

    private MarkdownParser() {
    }

    // The blocks of the text.
    static List<Block> parse(String text) {
        List<String> lines = new ArrayList<String>();
        int start = 0;
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            if (ch == '\n' || ch == '\r') {
                lines.add(expandTabs(text.substring(start, i)));
                if (ch == '\r' && i + 1 < text.length() && text.charAt(i + 1) == '\n') {
                    i++;
                }
                start = i + 1;
            }
        }
        lines.add(expandTabs(text.substring(start)));
        return parseBlocks(lines, 0);
    }

    // A tab is spaces to the next column that is a multiple of 4.
    private static String expandTabs(String line) {
        if (line.indexOf('\t') == -1) {
            return line;
        }
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < line.length(); i++) {
            char ch = line.charAt(i);
            if (ch == '\t') {
                do {
                    buf.append(' ');
                } while (buf.length() % 4 != 0);
            } else {
                buf.append(ch);
            }
        }
        return buf.toString();
    }

    private static List<Block> parseBlocks(List<String> lines, int depth) {
        List<Block> blocks = new ArrayList<Block>();
        int i = 0;
        while (i < lines.size()) {
            String line = lines.get(i);
            if (isBlank(line)) {
                i++;
                continue;
            }
            int[] fence = fence(line);
            if (fence != null) {
                i = parseFencedCode(lines, i, fence, blocks);
            } else if (atxLevel(line) > 0) {
                Block heading = new Block(Kind.HEADING);
                heading.level = atxLevel(line);
                heading.text = atxText(line);
                blocks.add(heading);
                i++;
            } else if (isThematicBreak(line)) {
                blocks.add(new Block(Kind.RULE));
                i++;
            } else if (depth < MAX_DEPTH && isQuote(line)) {
                i = parseQuote(lines, i, depth, blocks);
            } else if (depth < MAX_DEPTH && listMarker(line) != null) {
                i = parseList(lines, i, depth, blocks);
            } else if (isTableStart(lines, i)) {
                i = parseTable(lines, i, blocks);
            } else if (indent(line) >= 4) {
                i = parseIndentedCode(lines, i, blocks);
            } else {
                i = parseParagraph(lines, i, blocks);
            }
        }
        return blocks;
    }

    // Code between two fences of ``` or ~~~, the closing one as long or longer.
    private static int parseFencedCode(List<String> lines, int i, int[] fence, List<Block> blocks) {
        StringBuilder code = new StringBuilder();
        int j = i + 1;
        for (; j < lines.size(); j++) {
            String line = lines.get(j);
            if (isClosingFence(line, fence)) {
                j++;
                break;
            }
            if (code.length() > 0 || j > i + 1) {
                code.append('\n');
            }
            // The content loses as much of its indent as the opening fence has.
            int strip = Math.min(fence[2], indent(line));
            code.append(line.substring(strip));
        }
        Block block = new Block(Kind.CODE);
        block.text = code.toString();
        blocks.add(block);
        return j;
    }

    // Lines indented 4 spaces or more, with the empty lines between them.
    private static int parseIndentedCode(List<String> lines, int i, List<Block> blocks) {
        List<String> code = new ArrayList<String>();
        int j = i;
        while (j < lines.size() && (isBlank(lines.get(j)) || indent(lines.get(j)) >= 4)) {
            String line = lines.get(j);
            code.add(isBlank(line) ? "" : line.substring(4));
            j++;
        }
        while (!code.isEmpty() && code.get(code.size() - 1).isEmpty()) {
            code.remove(code.size() - 1);
        }
        Block block = new Block(Kind.CODE);
        block.text = join(code, "\n");
        blocks.add(block);
        return j;
    }

    // The lines of a quote, without their >, and the lines that go on from
    // the text of the quote without one, as Markdown allows.
    private static int parseQuote(List<String> lines, int i, int depth, List<Block> blocks) {
        List<String> quoted = new ArrayList<String>();
        int j = i;
        while (j < lines.size()) {
            String line = lines.get(j);
            if (isQuote(line)) {
                quoted.add(stripQuote(line));
            } else if (!isBlank(line) && !quoted.isEmpty() && !isBlank(quoted.get(quoted.size() - 1))
                    && !interrupts(line)) {
                quoted.add(line);
            } else {
                break;
            }
            j++;
        }
        Block quote = new Block(Kind.QUOTE);
        quote.children.addAll(parseBlocks(quoted, depth + 1));
        blocks.add(quote);
        return j;
    }

    // The items of a list: each is its first line after the marker and the
    // lines indented to its text, with what goes on from its text without an
    // indent. A list ends at an item of another kind, or at a line that is not
    // indented after an empty one.
    private static int parseList(List<String> lines, int i, int depth, List<Block> blocks) {
        int[] first = listMarker(lines.get(i));
        Block list = new Block(Kind.LIST);
        list.ordered = first[1] < 0;
        list.start = list.ordered ? first[3] : 1;
        while (i < lines.size()) {
            int[] marker = listMarker(lines.get(i));
            if (marker == null || marker[1] != first[1] || isThematicBreak(lines.get(i))) {
                break;
            }
            int column = marker[2];
            String line = lines.get(i);
            List<String> itemLines = new ArrayList<String>();
            itemLines.add(line.length() > column ? line.substring(column) : "");
            int j = i + 1;
            while (j < lines.size()) {
                String next = lines.get(j);
                String last = itemLines.get(itemLines.size() - 1);
                if (isBlank(next)) {
                    itemLines.add("");
                } else if (indent(next) >= column) {
                    itemLines.add(next.substring(column));
                } else if (!isBlank(last) && !interrupts(next) && listMarker(next) == null) {
                    itemLines.add(next);
                } else {
                    break;
                }
                j++;
            }
            // The empty lines after the item are between it and the next.
            boolean blankAfter = false;
            while (itemLines.size() > 1 && itemLines.get(itemLines.size() - 1).isEmpty()) {
                itemLines.remove(itemLines.size() - 1);
                blankAfter = true;
            }
            if (itemLines.contains("") || (blankAfter && j < lines.size() && sameList(lines.get(j), first))) {
                list.loose = true;
            }
            Block item = new Block(Kind.ITEM);
            item.children.addAll(parseBlocks(itemLines, depth + 1));
            list.children.add(item);
            i = j;
        }
        blocks.add(list);
        return i;
    }

    private static boolean sameList(String line, int[] first) {
        int[] marker = listMarker(line);
        return marker != null && marker[1] == first[1];
    }

    // A table of GitHub's Markdown: a header row, a row of dashes that sets the
    // alignment of each column, and the rows up to an empty line.
    private static int parseTable(List<String> lines, int i, List<Block> blocks) {
        List<String> header = cells(lines.get(i));
        List<String> delimiters = cells(lines.get(i + 1));
        Block table = new Block(Kind.TABLE);
        table.rows = new ArrayList<List<String>>();
        table.alignments = new ArrayList<Alignment>();
        for (String delimiter : delimiters) {
            String d = delimiter.trim();
            boolean left = d.startsWith(":");
            boolean right = d.endsWith(":");
            table.alignments.add((left && right) ? Alignment.CENTER : right ? Alignment.RIGHT : Alignment.LEFT);
        }
        table.rows.add(header);
        int j = i + 2;
        while (j < lines.size() && !isBlank(lines.get(j)) && lines.get(j).indexOf('|') != -1
                && !interrupts(lines.get(j))) {
            List<String> row = cells(lines.get(j));
            // Every row has a cell for each column.
            while (row.size() < header.size()) {
                row.add("");
            }
            table.rows.add(new ArrayList<String>(row.subList(0, header.size())));
            j++;
        }
        blocks.add(table);
        return j;
    }

    private static boolean isTableStart(List<String> lines, int i) {
        if (i + 1 >= lines.size() || lines.get(i).indexOf('|') == -1 || indent(lines.get(i)) >= 4) {
            return false;
        }
        String delimiterRow = lines.get(i + 1);
        if (delimiterRow.indexOf('-') == -1 || indent(delimiterRow) >= 4) {
            return false;
        }
        List<String> delimiters = cells(delimiterRow);
        for (String delimiter : delimiters) {
            String d = delimiter.trim();
            int from = d.startsWith(":") ? 1 : 0;
            int to = d.endsWith(":") && d.length() > from ? d.length() - 1 : d.length();
            if (to <= from) {
                return false;
            }
            for (int k = from; k < to; k++) {
                if (d.charAt(k) != '-') {
                    return false;
                }
            }
        }
        return cells(lines.get(i)).size() == delimiters.size();
    }

    // The cells of a row, split at the | that no backslash escapes, without
    // the | at the start and at the end of the row.
    private static List<String> cells(String line) {
        String row = line.trim();
        if (row.startsWith("|")) {
            row = row.substring(1);
        }
        if (row.endsWith("|") && !row.endsWith("\\|")) {
            row = row.substring(0, row.length() - 1);
        }
        List<String> cells = new ArrayList<String>();
        StringBuilder cell = new StringBuilder();
        for (int k = 0; k < row.length(); k++) {
            char ch = row.charAt(k);
            if (ch == '\\' && k + 1 < row.length()) {
                cell.append(ch).append(row.charAt(k + 1));
                k++;
            } else if (ch == '|') {
                cells.add(cell.toString().trim());
                cell.setLength(0);
            } else {
                cell.append(ch);
            }
        }
        cells.add(cell.toString().trim());
        return cells;
    }

    // The lines of a paragraph, up to an empty line or a line that starts
    // another block. A line of = or - under them makes them a heading.
    private static int parseParagraph(List<String> lines, int i, List<Block> blocks) {
        List<String> text = new ArrayList<String>();
        int j = i;
        while (j < lines.size() && !isBlank(lines.get(j))) {
            String line = lines.get(j);
            if (j > i) {
                int level = setextLevel(line);
                if (level > 0) {
                    Block heading = new Block(Kind.HEADING);
                    heading.level = level;
                    heading.text = join(text, "\n");
                    blocks.add(heading);
                    return j + 1;
                }
                if (interrupts(line)) {
                    break;
                }
            }
            text.add(line.trim());
            j++;
        }
        String paragraph = join(text, "\n");
        Block image = image(paragraph);
        blocks.add(image != null ? image : paragraph(paragraph));
        return j;
    }

    private static Block paragraph(String text) {
        Block block = new Block(Kind.PARAGRAPH);
        block.text = text;
        return block;
    }

    // An image alone in its paragraph: ![alt text](source), with no space in the source.
    private static Block image(String text) {
        if (!text.startsWith("![") || !text.endsWith(")")) {
            return null;
        }
        int close = text.indexOf("](");
        if (close == -1 || text.indexOf(']', 2) != close) {
            return null;
        }
        String source = text.substring(close + 2, text.length() - 1);
        if (source.isEmpty()) {
            return null;
        }
        for (int k = 0; k < source.length(); k++) {
            char ch = source.charAt(k);
            if (Character.isWhitespace(ch) || ch == '(' || ch == ')') {
                return null;
            }
        }
        Block block = new Block(Kind.IMAGE);
        block.text = text.substring(2, close);
        block.source = source;
        return block;
    }

    // A line that ends a paragraph and starts another block, with no empty
    // line before it: a fence, a heading, a thematic break, a quote, or a list
    // item with text, of a numbered list only when it starts at 1.
    private static boolean interrupts(String line) {
        if (fence(line) != null || atxLevel(line) > 0 || isThematicBreak(line) || isQuote(line)) {
            return true;
        }
        int[] marker = listMarker(line);
        return marker != null && !isBlank(line.substring(Math.min(marker[2], line.length())))
                && (marker[1] >= 0 || marker[3] == 1);
    }

    private static boolean isBlank(String line) {
        return line.trim().isEmpty();
    }

    private static int indent(String line) {
        int n = 0;
        while (n < line.length() && line.charAt(n) == ' ') {
            n++;
        }
        return n;
    }

    // A fence of three or more ` or ~, indented 3 spaces at most, as
    // {the character, its count, the indent}; a ` fence has no ` after it.
    private static int[] fence(String line) {
        int n = indent(line);
        if (n > 3 || n >= line.length()) {
            return null;
        }
        char ch = line.charAt(n);
        if (ch != '`' && ch != '~') {
            return null;
        }
        int count = 0;
        while (n + count < line.length() && line.charAt(n + count) == ch) {
            count++;
        }
        if (count < 3 || (ch == '`' && line.indexOf('`', n + count) != -1)) {
            return null;
        }
        return new int[] {ch, count, n};
    }

    private static boolean isClosingFence(String line, int[] fence) {
        int n = indent(line);
        if (n > 3) {
            return false;
        }
        int count = 0;
        while (n + count < line.length() && line.charAt(n + count) == fence[0]) {
            count++;
        }
        return count >= fence[1] && line.substring(n + count).trim().isEmpty();
    }

    // The level of a heading of # to ######, or 0.
    private static int atxLevel(String line) {
        int n = indent(line);
        if (n > 3) {
            return 0;
        }
        int level = 0;
        while (n + level < line.length() && line.charAt(n + level) == '#') {
            level++;
        }
        if (level < 1 || level > 6) {
            return 0;
        }
        int after = n + level;
        return (after == line.length() || line.charAt(after) == ' ') ? level : 0;
    }

    // The text of a heading of #, without the #s that close it.
    private static String atxText(String line) {
        String text = line.trim();
        int k = 0;
        while (k < text.length() && text.charAt(k) == '#') {
            k++;
        }
        text = text.substring(k).trim();
        int end = text.length();
        while (end > 0 && text.charAt(end - 1) == '#') {
            end--;
        }
        if (end == 0 || text.charAt(end - 1) == ' ') {
            text = text.substring(0, end).trim();
        }
        return text;
    }

    // A line of three or more *, - or _, all the same, with spaces between them.
    private static boolean isThematicBreak(String line) {
        if (indent(line) > 3) {
            return false;
        }
        char mark = 0;
        int count = 0;
        for (int k = 0; k < line.length(); k++) {
            char ch = line.charAt(k);
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
    private static int setextLevel(String line) {
        if (indent(line) > 3) {
            return 0;
        }
        String text = line.trim();
        if (text.isEmpty()) {
            return 0;
        }
        char ch = text.charAt(0);
        if (ch != '=' && ch != '-') {
            return 0;
        }
        for (int k = 0; k < text.length(); k++) {
            if (text.charAt(k) != ch) {
                return 0;
            }
        }
        return (ch == '=') ? 1 : 2;
    }

    private static boolean isQuote(String line) {
        int n = indent(line);
        return n <= 3 && n < line.length() && line.charAt(n) == '>';
    }

    // The line without its > and the space after it.
    private static String stripQuote(String line) {
        int n = indent(line) + 1;
        if (n < line.length() && line.charAt(n) == ' ') {
            n++;
        }
        return line.substring(n);
    }

    // The marker of a list item: -, + or * and a space, or 1 to 9 digits, . or
    // ) and a space, indented 3 spaces at most. Returns {the indent, the kind:
    // the bullet, or -1 after . and -2 after ), the column of the text, the
    // number}, or null. The text is 1 to 4 spaces after the marker; with more
    // the item starts with indented code, one space after it.
    static int[] listMarker(String line) {
        int n = indent(line);
        if (n > 3 || n >= line.length()) {
            return null;
        }
        char ch = line.charAt(n);
        int end;
        int kind;
        int number = 0;
        if (ch == '-' || ch == '+' || ch == '*') {
            end = n + 1;
            kind = ch;
        } else {
            int digits = 0;
            while (n + digits < line.length() && digits < 10
                    && line.charAt(n + digits) >= '0' && line.charAt(n + digits) <= '9') {
                digits++;
            }
            if (digits == 0 || digits > 9 || n + digits >= line.length()) {
                return null;
            }
            char delimiter = line.charAt(n + digits);
            if (delimiter != '.' && delimiter != ')') {
                return null;
            }
            number = Integer.parseInt(line.substring(n, n + digits));
            end = n + digits + 1;
            kind = (delimiter == '.') ? -1 : -2;
        }
        if (end < line.length() && line.charAt(end) != ' ') {
            return null;
        }
        int spaces = 0;
        while (end + spaces < line.length() && line.charAt(end + spaces) == ' ') {
            spaces++;
        }
        int column = (spaces >= 1 && spaces <= 4 && end + spaces < line.length()) ? end + spaces : end + 1;
        return new int[] {n, kind, column, number};
    }

    private static String join(List<String> list, String separator) {
        StringBuilder buf = new StringBuilder();
        for (int k = 0; k < list.size(); k++) {
            if (k > 0) {
                buf.append(separator);
            }
            buf.append(list.get(k));
        }
        return buf.toString();
    }
}   // End of MarkdownParser.java
