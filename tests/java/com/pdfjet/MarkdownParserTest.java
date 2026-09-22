/*
 * MarkdownParserTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.List;
import org.junit.jupiter.api.Test;

class MarkdownParserTest {
    // The blocks as text: H1(text), P(text), CODE(text), RULE, IMG(alt|source),
    // QUOTE[...], UL[...] or OL3[...] with its start, * after a list that is
    // loose, I[...] for an item, and TABLE(a,b;c,d|L,C) with its rows and alignments.
    static String describe(List<MarkdownParser.Block> blocks) {
        StringBuilder buf = new StringBuilder();
        for (MarkdownParser.Block block : blocks) {
            if (buf.length() > 0) {
                buf.append(' ');
            }
            switch (block.kind) {
                case HEADING:
                    buf.append('H').append(block.level).append('(').append(block.text).append(')');
                    break;
                case PARAGRAPH:
                    buf.append("P(").append(block.text).append(')');
                    break;
                case CODE:
                    buf.append("CODE(").append(block.text).append(')');
                    break;
                case RULE:
                    buf.append("RULE");
                    break;
                case IMAGE:
                    buf.append("IMG(").append(block.text).append('|').append(block.source).append(')');
                    break;
                case QUOTE:
                    buf.append("QUOTE[").append(describe(block.children)).append(']');
                    break;
                case LIST:
                    buf.append(block.ordered ? "OL" + block.start : "UL").append(block.loose ? "*" : "")
                            .append('[').append(describe(block.children)).append(']');
                    break;
                case ITEM:
                    buf.append("I[").append(describe(block.children)).append(']');
                    break;
                case TABLE:
                    StringBuilder rows = new StringBuilder();
                    for (List<String> row : block.rows) {
                        if (rows.length() > 0) {
                            rows.append(';');
                        }
                        rows.append(String.join(",", row));
                    }
                    StringBuilder alignments = new StringBuilder();
                    for (Alignment alignment : block.alignments) {
                        alignments.append(alignment.toString().charAt(0));
                    }
                    buf.append("TABLE(").append(rows).append('|').append(alignments).append(')');
                    break;
            }
        }
        return buf.toString();
    }

    private static String parse(String text) {
        return describe(MarkdownParser.parse(text));
    }

    @Test
    void headingsOfHashesAndOfUnderlines() {
        assertEquals("H1(Title) H3(Three) H2(Closed)", parse("# Title\n### Three\n## Closed ##"));
        assertEquals("P(#NoSpace) P(####### Seven)", parse("#NoSpace\n\n####### Seven"));
        assertEquals("H1(Big\nTitle) H2(Small)", parse("Big\nTitle\n===\n\nSmall\n---"));
    }

    @Test
    void paragraphsAreSeparatedByEmptyLines() {
        assertEquals("P(one\ntwo) P(three)", parse("one\ntwo\n\n  \nthree"));
        assertEquals("P(a) P(b)", parse("a\r\n\r\nb"));
        assertEquals("", parse(""));
        assertEquals("", parse("\n \n"));
    }

    @Test
    void fencedAndIndentedCode() {
        assertEquals("CODE(int x = 1;\n  y();)", parse("```java\nint x = 1;\n  y();\n```"));
        assertEquals("CODE(a\n```\nb)", parse("~~~~\na\n```\nb\n~~~~"));
        assertEquals("CODE(unclosed)", parse("```\nunclosed"));
        assertEquals("P(text) CODE(code\n\n  more)", parse("text\n\n    code\n\n      more\n\n"));
        // A paragraph goes on over an indented line.
        assertEquals("P(text\ncontinued)", parse("text\n    continued"));
    }

    @Test
    void thematicBreaks() {
        assertEquals("P(a) RULE P(b) RULE RULE", parse("a\n\n***\nb\n\n- - -\n___"));
        assertEquals("P(-- not)", parse("-- not"));
    }

    @Test
    void quotesNestAndGoOnWithoutTheirMark() {
        assertEquals("QUOTE[P(a\nb)]", parse("> a\n> b"));
        assertEquals("QUOTE[P(a\nlazy)]", parse("> a\nlazy"));
        assertEquals("QUOTE[H1(T) QUOTE[P(deep)]]", parse("> # T\n> > deep"));
        assertEquals("QUOTE[P(a)] P(b)", parse("> a\n\nb"));
    }

    @Test
    void bulletAndNumberedLists() {
        assertEquals("UL[I[P(a)] I[P(b)]]", parse("- a\n- b"));
        assertEquals("OL3[I[P(x)] I[P(y)]]", parse("3. x\n4. y"));
        assertEquals("UL[I[P(a)]] UL[I[P(b)]]", parse("- a\n+ b"));
        assertEquals("UL[I[P(a\nlazy)]]", parse("- a\nlazy"));
        assertEquals("UL[I[P(a) UL[I[P(b)]]] I[P(c)]]", parse("- a\n  - b\n- c"));
        assertEquals("UL*[I[P(a)] I[P(b)]]", parse("- a\n\n- b"));
        assertEquals("UL*[I[P(a) P(more)]]", parse("- a\n\n  more"));
        assertEquals("UL[I[P(a)]] P(after)", parse("- a\n\nafter"));
    }

    @Test
    void aNumberedListInterruptsAParagraphOnlyWhenItStartsAtOne() {
        assertEquals("P(The year\n2026. was good)", parse("The year\n2026. was good"));
        assertEquals("P(Steps:) OL1[I[P(go)]]", parse("Steps:\n1. go"));
    }

    @Test
    void tablesOfPipes() {
        assertEquals("TABLE(A,B;1,2;3,|LR)", parse("| A | B |\n|---|--:|\n| 1 | 2 |\n| 3 |"));
        assertEquals("TABLE(a,b;x\\|y,z|CL)", parse("a | b\n:-:|---\nx\\|y | z"));
        // The header and the delimiter row need the same number of cells.
        assertEquals("P(a | b\n--- | --- | ---)", parse("a | b\n--- | --- | ---"));
    }

    @Test
    void anImageAloneInItsParagraph() {
        assertEquals("IMG(A map|images/map.png)", parse("![A map](images/map.png)"));
        assertEquals("P(See ![a](b.png) here)", parse("See ![a](b.png) here"));
        assertEquals("P(![a](b c.png))", parse("![a](b c.png)"));
    }

    @Test
    void tabsAreSpacesToTheNextStopOfFour() {
        assertEquals("CODE(x)", parse("\tx"));
        assertEquals("UL*[I[P(a) CODE(b)]]", parse("-\ta\n\n\t\tb"));
    }

    @Test
    void containersNestAtMostMaxDepthLevels() {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 1000; i++) {
            text.append('>');
        }
        text.append(" deep");
        String tree = parse(text.toString());
        int quotes = tree.split("QUOTE\\[", -1).length - 1;
        assertEquals(MarkdownParser.MAX_DEPTH, quotes);
    }

    @Test
    void longInputsAreReadQuickly() {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 20000; i++) {
            text.append("> - a | b\n>   ---|---\n>   ```\n\n    x\n- [ ]\n");
        }
        long time0 = System.nanoTime();
        List<MarkdownParser.Block> blocks = MarkdownParser.parse(text.toString());
        long milliseconds = (System.nanoTime() - time0) / 1000000;
        assertTrue(!blocks.isEmpty());
        assertTrue(milliseconds < 3000, milliseconds + " ms");
    }
}
