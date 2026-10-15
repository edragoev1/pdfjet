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
    // loose, I[...] for an item, T[...] for a task and X[...] for a task that is
    // done, and TABLE(a,b;c,d|L,C) with its rows and alignments.
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
                    buf.Append(new String[] {"I", "T", "X"}[(int) block.task]).Append('[').Append(Describe(block.children)).Append(']');
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
    public void TaskLists() {
        Assert.Equal("UL[X[P(done)] T[P(open)] I[P(plain)]]", Parse("- [x] done\n- [ ] open\n- plain"));
        Assert.Equal("UL[X[P(Done)]]", Parse("* [X] Done"));
        Assert.Equal("OL1[T[P(first)]]", Parse("1. [ ] first"));
        Assert.Equal("UL[T[P(a) UL[X[P(b)]]]]", Parse("- [ ] a\n  - [x] b"));
        Assert.Equal("UL[T[P(tab)]]", Parse("- [ ]\ttab"));
        // Not tasks: no space after the box, no text, another letter
        Assert.Equal("UL[I[P([ ]no space)]]", Parse("- [ ]no space"));
        Assert.Equal("UL[I[P([ ])]]", Parse("- [ ]"));
        Assert.Equal("UL[I[P([ ])]]", Parse("- [ ]   "));
        Assert.Equal("UL[I[P([y] other)]]", Parse("- [y] other"));
        Assert.Equal("P([x] not in a list)", Parse("[x] not in a list"));
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

    // Eight times as long a text is read in about eight times the time, not
    // sixty-four; the two times are measured in the same run, the shortest of
    // three each, so that a busy computer slows both (a limit of 3 s failed
    // under a fuzzer's load, 9 October 2026, and a limit of 10 for four times
    // the text, too near both, failed in CI at 10.1 on 12 October).
    [Fact]
    public void LongInputsAreReadQuickly() {
        TimeSpan small = ParseTime(2500);
        TimeSpan large = ParseTime(20000);
        Assert.True(large.Ticks <= 24 * small.Ticks, "20000 repeats took "
                + large.TotalMilliseconds + " ms, and 2500 " + small.TotalMilliseconds + " ms");
    }

    // A table of more than 64 columns is marked too wide, and its rows are not
    // padded: thousands of columns over thousands of rows made cells as the
    // square of the text (review of 11 October 2026, #33); drawn, it is an
    // error (review of 12 October 2026, R33).
    [Fact]
    public void ATableHasAtMost64Columns() {
        string Table(int columns) {
            StringBuilder text = new StringBuilder();
            text.Insert(0, "| a ", columns).Append("|\n");
            for (int i = 0; i < columns; i++) {
                text.Append("| - ");
            }
            return text.Append("|\n|\n|\n").ToString();
        }
        string got = Parse(Table(64));
        Assert.True(got.StartsWith("TABLE(", StringComparison.Ordinal), "64 columns: " + got);
        List<MarkdownParser.Block> blocks = MarkdownParser.Parse(Table(65));
        Assert.True(blocks.Count == 1 && blocks[0].tooWide && blocks[0].rows.Count == 1, "65 columns: " + Parse(Table(65)));
    }

    private static TimeSpan ParseTime(int count) {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < count; i++) {
            text.Append("> - a | b\n>   ---|---\n>   ```\n\n    x\n- [ ]\n");
        }
        TimeSpan shortest = TimeSpan.MaxValue;
        for (int run = 0; run < 3; run++) {
            Stopwatch watch = Stopwatch.StartNew();
            List<MarkdownParser.Block> blocks = MarkdownParser.Parse(text.ToString());
            if (watch.Elapsed < shortest) {
                shortest = watch.Elapsed;
            }
            Assert.True(blocks.Count > 0);
        }
        return shortest;
    }
}
}
