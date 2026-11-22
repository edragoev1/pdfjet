/*
 * DictionaryBreaksTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public class DictionaryBreaksTest {
    // Every run of Thai, Lao, Khmer and Burmese in the UDHR is broken into the
    // words ICU breaks it into (tests/data/dictionarybreaks/expected.tsv): the
    // same dictionaries, ICU's engines ported.
    [Fact]
    public void WordEndsAreICUs() {
        Dictionary<string, int> runs = new Dictionary<string, int>();
        Dictionary<string, int> differ = new Dictionary<string, int>();
        List<string> examples = new List<string>();
        foreach (string row in File.ReadAllLines(TestSupport.RepoPath("tests/data/dictionarybreaks/expected.tsv"), Encoding.UTF8)) {
            if (row.Length == 0 || row.StartsWith("#", StringComparison.Ordinal)) {
                continue;
            }
            string[] fields = row.Split('\t');
            List<int> want = fields[2].Split(',').Where(e => e.Length > 0).Select(int.Parse).ToList();
            List<int> got = DictionaryBreaks.WordEnds(DictionaryBreaks.CodePoints(fields[1]));
            runs[fields[0]] = runs.TryGetValue(fields[0], out int r) ? r + 1 : 1;
            if (!got.SequenceEqual(want)) {
                differ[fields[0]] = differ.TryGetValue(fields[0], out int d) ? d + 1 : 1;
                if (examples.Count < 3) {
                    examples.Add(fields[0] + " " + fields[1] + ": " + string.Join(",", got) + ", ICU " + string.Join(",", want));
                }
            }
        }
        foreach (string code in new[] {"tha", "lao", "khm", "mya"}) {
            Assert.True(runs.TryGetValue(code, out int n) && n >= 300, code);
            Assert.True(!differ.ContainsKey(code), code + " " + string.Join(" | ", examples));
        }
    }

    // Thai text without zero width spaces breaks between its words: the Thai
    // UDHR, each paragraph 300 points wide in IBM Plex Sans Thai 12, in the
    // lines Chrome breaks it into (tests/data/dictionarybreaks/thai-textblock-300.txt).
    [Fact]
    public void ThaiTextBlockBreaksBetweenWordsAsChrome() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath("resources/fonts/world/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"));
        font.SetSize(12f);
        StringBuilder got = new StringBuilder();
        foreach (string paragraph in File.ReadAllLines(TestSupport.RepoPath("resources/data/udhr/udhr_tha.txt"), Encoding.UTF8)) {
            if (paragraph.Length == 0) {
                continue;
            }
            TextBlock block = new TextBlock(font, paragraph);
            block.SetWidth(300f);
            foreach (TextLine line in block.GetTextLines()) {
                got.Append(line.GetText().TrimEnd(' ')).Append('\n');
            }
            got.Append('\n');
        }
        string[] g = got.ToString().Trim().Split('\n');
        string[] w = File.ReadAllText(TestSupport.RepoPath("tests/data/dictionarybreaks/thai-textblock-300.txt"), Encoding.UTF8)
                .Trim().Split('\n');
        for (int i = 0; i < Math.Min(g.Length, w.Length); i++) {
            Assert.True(w[i] == g[i], "line " + (i + 1) + ": " + g[i] + ", Chrome " + w[i]);
        }
        Assert.Equal(w.Length, g.Length);
    }
}
}
