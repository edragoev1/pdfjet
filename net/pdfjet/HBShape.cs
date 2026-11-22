/*
 * HBShape.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// A shaper for ComplexText that runs hb-shape, the program of HarfBuzz, the
/// text engine of Chrome and Android: the same shaping on every server, with
/// nothing linked into the program. Lines break at spaces and, in Thai, Lao,
/// Khmer and Burmese, between words, by ICU's dictionaries of the world fonts.
/// hb-shape is installed on the server, HarfBuzz 15 or newer: on Ubuntu and
/// Debian apt install libharfbuzz-bin; on Fedora and RHEL dnf install
/// harfbuzz-utils; on macOS and Linux brew install harfbuzz; on Windows
/// harfbuzz-win64-&lt;version&gt;.zip of github.com/harfbuzz/harfbuzz/releases.
/// It talks with hb-shape through files, not pipes, the same on every system:
/// the text of each font in a temporary file, the glyphs back in another.
/// <code>
/// new ComplexText(hindi, text).SetFallbackFont(latin).SetShaper(new HBShape())
/// </code>
/// </summary>
public class HBShape : IShaper {
    private readonly String program;

    /// <summary>An HBShape that runs hb-shape of the PATH.</summary>
    public HBShape() : this("hb-shape") {
    }

    /// <summary>An HBShape that runs the hb-shape of the path.</summary>
    /// <param name="program">hb-shape's path, such as
    /// C:\Tools\harfbuzz-win64\hb-shape.exe, or its name looked for on the PATH.</param>
    public HBShape(String program) {
        this.program = program;
    }

    // A folder for the temporary files, of a plain ASCII path: a Windows
    // build of a Linux tool can mishandle a path like C:\Users\Евгени\...
    private static String TempRoot() {
        String temp = System.IO.Path.GetTempPath();
        foreach (char c in temp) {
            if (c > 0x7E) {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
        }
        return temp;
    }

    private static String Quote(String argument) {
        return "\"" + argument + "\"";
    }

    // Runs hb-shape with the arguments and waits for it to end; what it
    // writes to its standard output is returned, its errors read beside it
    private String Run(params String[] arguments) {
        StringBuilder args = new StringBuilder();
        foreach (String argument in arguments) {
            if (args.Length > 0) {
                args.Append(' ');
            }
            args.Append(Quote(argument));
        }
        ProcessStartInfo info = new ProcessStartInfo(program, args.ToString()) {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        Process process;
        try {
            process = Process.Start(info);
        } catch (System.ComponentModel.Win32Exception e) {
            throw new InvalidOperationException("ComplexText's HBShape could not run hb-shape, the program of HarfBuzz: "
                    + "install it (Ubuntu: apt install libharfbuzz-bin; macOS: brew install harfbuzz; Windows: "
                    + "HarfBuzz's harfbuzz-win64 zip), or give its path to HBShape. " + e.Message, e);
        }
        using (process) {
            // Both read at once, so that neither waits on a full pipe
            var error = process.StandardError.ReadToEndAsync();
            String output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) {
                String message = error.Result.Trim();
                throw new InvalidOperationException("hb-shape: exit " + process.ExitCode
                        + (message.Length == 0 ? "" : ": " + message));
            }
            return output;
        }
    }

    /// <summary>
    /// What hb-shape --version prints first, such as "hb-shape (HarfBuzz) 15.2.0".
    /// </summary>
    /// <returns>the version.</returns>
    public String GetVersion() {
        String text = Run("--version");
        int end = text.IndexOf('\n');
        return (end >= 0 ? text.Substring(0, end) : text).Trim();
    }

    // A glyph shaped, in points from the start of the text
    private sealed class Glyph {
        internal int id;
        internal int font;
        internal int cluster;   // The index in the text, in code points, of its cluster's first character
        internal float x;
        internal float y;
        internal float advance;
    }

    // hb-shape's line of JSON, [{"g":1,"cl":0,"dx":0,"dy":0,"ax":259,"ay":0},...]:
    // for each glyph its g, cl, ax, dx and dy, in font units
    private static List<int[]> ReadGlyphs(String line) {
        List<int[]> glyphs = new List<int[]>();
        int at = line.IndexOf('{');
        while (at >= 0) {
            int end = line.IndexOf('}', at);
            if (end < 0) {
                throw new InvalidOperationException("hb-shape's output: " + line);
            }
            Dictionary<String, int> values = new Dictionary<String, int>(StringComparer.Ordinal);
            foreach (String pair in line.Substring(at + 1, end - at - 1).Split(',')) {
                int colon = pair.IndexOf(':');
                String key = pair.Substring(0, colon).Trim().Replace("\"", "");
                values[key] = Int32.Parse(pair.Substring(colon + 1).Trim(), CultureInfo.InvariantCulture);
            }
            int g, cl, ax, dx, dy;
            if (!values.TryGetValue("g", out g) || !values.TryGetValue("cl", out cl) || !values.TryGetValue("ax", out ax)) {
                throw new InvalidOperationException("hb-shape's output: " + line);
            }
            values.TryGetValue("dx", out dx);
            values.TryGetValue("dy", out dy);
            glyphs.Add(new int[] {g, cl, ax, dx, dy});
            at = line.IndexOf('{', end);
        }
        return glyphs;
    }

    /// <summary>
    /// Shapes the text with hb-shape, each run of one font in that font, and
    /// breaks it into lines no wider than the width.
    /// </summary>
    /// <param name="request">the text, its language, the font files, the
    /// font size and the width of the lines.</param>
    /// <returns>the lines, in order.</returns>
    public List<ShapedLine> Shape(ShapingRequest request) {
        String text = request.Text;
        if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0) {
            throw new ArgumentException("HBShape shapes a paragraph: a text without line feeds");
        }
        byte[][] files = request.FontFiles;
        if (files.Length == 0) {
            throw new ArgumentException("HBShape needs a font file");
        }
        OTF[] fonts = new OTF[files.Length];
        for (int i = 0; i < files.Length; i++) {
            fonts[i] = new OTF(new MemoryStream(files[i]));
        }
        int[] cps = DictionaryBreaks.CodePoints(text);
        // The index in the text, in chars, of each code point, and of the end
        int[] offsets = new int[cps.Length + 1];
        for (int i = 0, at = 0; i < cps.Length; i++) {
            offsets[i] = at;
            at += (cps[i] > 0xFFFF) ? 2 : 1;
            offsets[i + 1] = at;
        }

        // Each character in the first font that has it, else in the first; a
        // mark in the font of the character it is on
        int[] fontOf = new int[cps.Length];
        for (int i = 0; i < cps.Length; i++) {
            int c = cps[i];
            UnicodeCategory category = (c <= 0xFFFF)
                    ? CharUnicodeInfo.GetUnicodeCategory((char) c)
                    : CharUnicodeInfo.GetUnicodeCategory(Char.ConvertFromUtf32(c), 0);
            if (i > 0 && (category == UnicodeCategory.NonSpacingMark
                    || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark)) {
                fontOf[i] = fontOf[i - 1];
                continue;
            }
            for (int k = 0; k < fonts.Length; k++) {
                OTF otf = fonts[k];
                if (category == UnicodeCategory.Control
                        || (c >= otf.firstChar && c <= otf.lastChar && otf.unicodeToGID.Get(c) != 0)) {
                    fontOf[i] = k;
                    break;
                }
            }
        }
        // The runs of one font: start and end
        List<int[]> runs = new List<int[]>();
        for (int start = 0; start < cps.Length;) {
            int end = start + 1;
            while (end < cps.Length && fontOf[end] == fontOf[start]) {
                end++;
            }
            runs.Add(new int[] {start, end});
            start = end;
        }

        // Each font's runs shaped in one run of hb-shape, a run a line
        String dir = System.IO.Path.Combine(TempRoot(), "pdfjet-hbshape-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Dictionary<int, List<int[]>> shapedRuns = new Dictionary<int, List<int[]>>();
        try {
            for (int k = 0; k < fonts.Length; k++) {
                List<int[]> list = new List<int[]>();
                StringBuilder input = new StringBuilder();
                foreach (int[] run in runs) {
                    if (fontOf[run[0]] == k) {
                        list.Add(run);
                        for (int i = run[0]; i < run[1]; i++) {
                            input.Append(Char.ConvertFromUtf32(cps[i]));
                        }
                        input.Append('\n');
                    }
                }
                if (list.Count == 0) {
                    continue;
                }
                String font = System.IO.Path.Combine(dir, "font" + k);
                String inPath = System.IO.Path.Combine(dir, "in" + k + ".txt");
                String outPath = System.IO.Path.Combine(dir, "out" + k + ".txt");
                File.WriteAllBytes(font, files[k]);
                File.WriteAllBytes(inPath, new UTF8Encoding(false).GetBytes(input.ToString()));
                List<String> arguments = new List<String> {
                    "--font-file=" + font, "--text-file=" + inPath, "--output-file=" + outPath,
                    "--no-glyph-names", "--output-format=json",
                };
                if (!String.IsNullOrEmpty(request.Language)) {
                    arguments.Add("--language=" + request.Language);
                }
                Run(arguments.ToArray());
                List<String> lines = new List<String>();
                foreach (String line in File.ReadAllText(outPath, new UTF8Encoding(false)).Split('\n')) {
                    String l = line.Replace("\r", "");
                    if (l.Length > 0) {
                        lines.Add(l);
                    }
                }
                if (lines.Count != list.Count) {
                    throw new InvalidOperationException("hb-shape gave " + lines.Count + " lines for "
                            + list.Count + " runs of text");
                }
                for (int i = 0; i < lines.Count; i++) {
                    shapedRuns[list[i][0]] = ReadGlyphs(lines[i]);
                }
            }
        } finally {
            try {
                Directory.Delete(dir, true);
            } catch (IOException) {
                // Left to the system's cleaning of its temporary files
            } catch (UnauthorizedAccessException) {
                // Left too
            }
        }

        // The glyphs of the runs in order, in points
        List<Glyph> glyphs = new List<Glyph>();
        float pen = 0f;
        foreach (int[] run in runs) {
            int k = fontOf[run[0]];
            float scale = request.FontSize / fonts[k].unitsPerEm;
            foreach (int[] g in shapedRuns[run[0]]) {
                if (run[0] + g[1] >= run[1]) {
                    throw new InvalidOperationException("hb-shape gave a glyph of a character past its run");
                }
                glyphs.Add(new Glyph {
                    id = g[0], font = k, cluster = run[0] + g[1],
                    x = pen + g[3] * scale, y = -g[4] * scale, advance = g[2] * scale,
                });
                pen += g[2] * scale;
            }
        }
        return Lines(glyphs, cps, offsets, text.Length, request.Width);
    }

    // The lines no wider than the width: broken after a space, or between two
    // words of Thai, Lao, Khmer or Burmese, the last place that fits; a word
    // wider than a line alone on its line
    private static List<ShapedLine> Lines(List<Glyph> glyphs, int[] cps, int[] offsets, int end, float width) {
        HashSet<int> wordEnd = new HashSet<int>(DictionaryBreaks.WordEnds(cps));
        List<ShapedLine> lines = new List<ShapedLine>();
        for (int first = 0; first < glyphs.Count;) {
            float lineStart = glyphs[first].x;
            int last = glyphs.Count;
            int lastBreak = -1;
            for (int i = first; i < glyphs.Count; i++) {
                Glyph g = glyphs[i];
                if (i > first && glyphs[i - 1].cluster != g.cluster
                        && (cps[g.cluster - 1] == ' ' || (wordEnd.Contains(g.cluster) && cps[g.cluster] != ' '))) {
                    lastBreak = i;
                }
                if (cps[g.cluster] != ' ' && g.x + g.advance - lineStart > width && lastBreak > first) {
                    last = lastBreak;
                    break;
                }
            }
            int lineEnd = (last < glyphs.Count) ? offsets[glyphs[last].cluster] : end;
            // The width of the line without the spaces at its end
            int visible = last;
            while (visible > first && cps[glyphs[visible - 1].cluster] == ' ') {
                visible--;
            }
            float lineWidth = 0f;
            if (visible > first) {
                Glyph g = glyphs[visible - 1];
                lineWidth = g.x + g.advance - lineStart;
            }
            int n = last - first;
            int[] fontIndexes = new int[n];
            int[] ids = new int[n];
            float[] positions = new float[2 * n];
            for (int i = 0; i < n; i++) {
                Glyph g = glyphs[first + i];
                fontIndexes[i] = g.font;
                ids[i] = g.id;
                positions[2 * i] = g.x - lineStart;
                positions[2 * i + 1] = g.y;
            }
            lines.Add(new ShapedLine(offsets[glyphs[first].cluster], lineEnd, fontIndexes, ids, positions, lineWidth));
            first = last;
        }
        return lines;
    }
}
}   // End of namespace PDFjet.NET
