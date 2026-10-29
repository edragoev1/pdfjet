/*
 * FontCoverageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// Every face of the fonts PDFjet carries for Latin text has every letter of
/// modern Greek and of the Cyrillic of Russian, Ukrainian, Belarusian,
/// Bulgarian, Serbian and Macedonian: a document in one of them never shows
/// an empty box (the owner, 29 October 2026, of ѝ, the Bulgarian "her", which
/// JetBrains Mono does not have).
/// </summary>
public class FontCoverageTest {
    private static readonly String[] FAMILIES = {"IBMPlexSans", "IBMPlexSerif", "SourceSerif4", "CascadiaMono"};

    // The letters of modern Greek, U+0386 to U+03CE, and of Cyrillic, U+0400
    // to U+045F; what the code points between them are not, as U+0387, a
    // punctuation mark, is skipped by the test.
    private static String Letters() {
        StringBuilder sb = new StringBuilder();
        for (int c = 0x0386; c <= 0x03CE; c++) {
            if (Char.IsLetter((char) c)) {
                sb.Append((char) c);
            }
        }
        for (int c = 0x0400; c <= 0x045F; c++) {
            if (Char.IsLetter((char) c)) {
                sb.Append((char) c);
            }
        }
        return sb.ToString();
    }

    [Fact]
    public void EveryFaceForLatinTextHasGreekAndCyrillic() {
        String letters = Letters();
        Assert.Equal(69 + 96, letters.Length);
        PDF pdf = new PDF(new MemoryStream());
        int faces = 0;
        foreach (String family in FAMILIES) {
            foreach (String file in Directory.GetFiles(TestSupport.RepoPath("resources/fonts/" + family))) {
                String name = System.IO.Path.GetFileName(file);
                if (!name.EndsWith(".ttf", StringComparison.Ordinal)) {
                    continue;
                }
                Font font;
                using (Stream stream = TestSupport.Open("resources/fonts/" + family + "/" + name)) {
                    font = new Font(pdf, stream);
                }
                StringBuilder missing = new StringBuilder();
                for (int i = 0; i < letters.Length; i++) {
                    if (font.unicodeToGID.Get(letters[i]) == 0) {
                        missing.Append(letters[i]);
                    }
                }
                Assert.True(missing.Length == 0, name + ": " + missing);
                faces++;
            }
        }
        Assert.Equal(14 + 14 + 10 + 10, faces);
    }
}
}   // End of FontCoverageTest.cs
