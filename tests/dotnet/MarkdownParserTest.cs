/*
 * MarkdownParserTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public class MarkdownParserTest {
    // The blocks as text: H1(text), P(text), CODE(text), RULE, IMG(alt|source),
    // QUOTE[...], UL[...] or OL3[...] with its start, * after a list that is
    // loose, I[...] for an item, and TABLE(a,b;c,d|L,C) with its rows and alignments.
    internal static String Describe(List<MarkdownParser.Block> blocks) {
        StringBuilder buf = new StringBuilder();
        foreach (MarkdownParser.Block block in blocks) {
            if (buf.Length > 0) {
                buf.Append(' ');
            }
            switch (block.kind) {
                case MarkdownParser.Kind.HEADING:
                    buf.Append('H').Append(block.level).Append('(').Append(block.text).Append(')');
                    break;
                case MarkdownParser.Kind.PARAGRAPH:
                    buf.Append("P(").Append(block.text).Append(')');
                    break;
                case MarkdownParser.Kind.CODE:
                    buf.Append("CODE(").Append(block.text).Append(')');
                    break;
                case MarkdownParser.Kind.RULE:
                    buf.Append("RULE");
                    break;
                case MarkdownParser.Kind.IMAGE:
                    buf.Append("IMG(").Append(block.text).Append('|').Append(block.source).Append(')');
                    break;
                case MarkdownParser.Kind.QUOTE:
                    buf.Append("QUOTE[").Append(Describe(block.children)).Append(']');
                    break;
                case MarkdownParser.Kind.LIST:
                    buf.Append(block.ordered ? "OL" + block.start : "UL").Append(block.loose ? "*" : "")
                            .Append('[').Append(Describe(block.children)).Append(']');
                    break;
                case MarkdownParser.Kind.ITEM:
                    buf.Append("I[").Append(Describe(block.children)).Append(']');
                    break;
                case MarkdownParser.Kind.TABLE:
                    StringBuilder rows = new StringBuilder();
                    foreach (List<String> row in block.rows) {
                        if (rows.Length > 0) {
                            rows.Append(';');
                        }
                        rows.Append(String.Join(",", row));
                    }
                    StringBuilder alignments = new StringBuilder();
                    foreach (Alignment alignment in block.alignments) {
                        alignments.Append(alignment.ToString()[0]);
                    }
                    buf.Append("TABLE(").Append(rows).Append('|').Append(alignments).Append(')');
                    break;
            }
        }
        return buf.ToString();
    }

    private static String Parse(String text) {
        return Describe(MarkdownParser.Parse(text));
    }

    // The number of times the text has the part, as Java's split(part, -1).length - 1 counts them.
    internal static int Occurrences(String text, String part) {
        int count = 0;
        int i = text.IndexOf(part, StringComparison.Ordinal);
        while (i != -1) {
            count++;
            i = text.IndexOf(part, i + part.Length, StringComparison.Ordinal);
        }
        return count;
    }

    [Fact]
    public void HeadingsOfHashesAndOfUnderlines() {
        Assert.Equal("H1(Title) H3(Three) H2(Closed)", Parse("# Title\n### Three\n## Closed ##"));
        Assert.Equal("P(#NoSpace) P(####### Seven)", Parse("#NoSpace\n\n####### Seven"));
        Assert.Equal("H1(Big\nTitle) H2(Small)", Parse("Big\nTitle\n===\n\nSmall\n---"));
    }

    [Fact]
    public void ParagraphsAreSeparatedByEmptyLines() {
        Assert.Equal("P(one\ntwo) P(three)", Parse("one\ntwo\n\n  \nthree"));
        Assert.Equal("P(a) P(b)", Parse("a\r\n\r\nb"));
        Assert.Equal("", Parse(""));
        Assert.Equal("", Parse("\n \n"));
    }

    [Fact]
    public void FencedAndIndentedCode() {
        Assert.Equal("CODE(int x = 1;\n  y();)", Parse("```java\nint x = 1;\n  y();\n```"));
        Assert.Equal("CODE(a\n```\nb)", Parse("~~~~\na\n```\nb\n~~~~"));
        Assert.Equal("CODE(unclosed)", Parse("```\nunclosed"));
        Assert.Equal("P(text) CODE(code\n\n  more)", Parse("text\n\n    code\n\n      more\n\n"));
        // A paragraph goes on over an indented line.
        Assert.Equal("P(text\ncontinued)", Parse("text\n    continued"));
    }

    [Fact]
    public void ThematicBreaks() {
        Assert.Equal("P(a) RULE P(b) RULE RULE", Parse("a\n\n***\nb\n\n- - -\n___"));
        Assert.Equal("P(-- not)", Parse("-- not"));
    }

    [Fact]
    public void QuotesNestAndGoOnWithoutTheirMark() {
        Assert.Equal("QUOTE[P(a\nb)]", Parse("> a\n> b"));
        Assert.Equal("QUOTE[P(a\nlazy)]", Parse("> a\nlazy"));
        Assert.Equal("QUOTE[H1(T) QUOTE[P(deep)]]", Parse("> # T\n> > deep"));
        Assert.Equal("QUOTE[P(a)] P(b)", Parse("> a\n\nb"));
    }

    [Fact]
    public void BulletAndNumberedLists() {
        Assert.Equal("UL[I[P(a)] I[P(b)]]", Parse("- a\n- b"));
        Assert.Equal("OL3[I[P(x)] I[P(y)]]", Parse("3. x\n4. y"));
        Assert.Equal("UL[I[P(a)]] UL[I[P(b)]]", Parse("- a\n+ b"));
        Assert.Equal("UL[I[P(a\nlazy)]]", Parse("- a\nlazy"));
        Assert.Equal("UL[I[P(a) UL[I[P(b)]]] I[P(c)]]", Parse("- a\n  - b\n- c"));
        Assert.Equal("UL*[I[P(a)] I[P(b)]]", Parse("- a\n\n- b"));
        Assert.Equal("UL*[I[P(a) P(more)]]", Parse("- a\n\n  more"));
        Assert.Equal("UL[I[P(a)]] P(after)", Parse("- a\n\nafter"));
    }

    [Fact]
    public void ANumberedListInterruptsAParagraphOnlyWhenItStartsAtOne() {
        Assert.Equal("P(The year\n2026. was good)", Parse("The year\n2026. was good"));
        Assert.Equal("P(Steps:) OL1[I[P(go)]]", Parse("Steps:\n1. go"));
    }

    [Fact]
    public void TablesOfPipes() {
        Assert.Equal("TABLE(A,B;1,2;3,|LR)", Parse("| A | B |\n|---|--:|\n| 1 | 2 |\n| 3 |"));
        Assert.Equal("TABLE(a,b;x\\|y,z|CL)", Parse("a | b\n:-:|---\nx\\|y | z"));
        // The header and the delimiter row need the same number of cells.
        Assert.Equal("P(a | b\n--- | --- | ---)", Parse("a | b\n--- | --- | ---"));
    }

    [Fact]
    public void AnImageAloneInItsParagraph() {
        Assert.Equal("IMG(A map|images/map.png)", Parse("![A map](images/map.png)"));
        Assert.Equal("P(See ![a](b.png) here)", Parse("See ![a](b.png) here"));
        Assert.Equal("P(![a](b c.png))", Parse("![a](b c.png)"));
    }

    [Fact]
    public void TabsAreSpacesToTheNextStopOfFour() {
        Assert.Equal("CODE(x)", Parse("\tx"));
        Assert.Equal("UL*[I[P(a) CODE(b)]]", Parse("-\ta\n\n\t\tb"));
    }

    [Fact]
    public void ContainersNestAtMostMaxDepthLevels() {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 1000; i++) {
            text.Append('>');
        }
        text.Append(" deep");
        String tree = Parse(text.ToString());
        int quotes = Occurrences(tree, "QUOTE[");
        Assert.Equal(MarkdownParser.MAX_DEPTH, quotes);
    }

    [Fact]
    public void LongInputsAreReadQuickly() {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 20000; i++) {
            text.Append("> - a | b\n>   ---|---\n>   ```\n\n    x\n- [ ]\n");
        }
        Stopwatch watch = Stopwatch.StartNew();
        List<MarkdownParser.Block> blocks = MarkdownParser.Parse(text.ToString());
        long milliseconds = watch.ElapsedMilliseconds;
        Assert.True(blocks.Count > 0);
        Assert.True(milliseconds < 3000, milliseconds + " ms");
    }
}
}
