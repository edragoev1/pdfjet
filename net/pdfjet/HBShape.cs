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
public class HBShape : IShaper, IHarfBuzz {
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
            throw new InvalidOperationException("ComplexText needs HarfBuzz to shape this text: add the NuGet "
                    + "package PDFjet.HarfBuzz, nothing to install, or install HarfBuzz's program hb-shape (Ubuntu: "
                    + "apt install libharfbuzz-bin; macOS: brew install harfbuzz; Windows: HarfBuzz's harfbuzz-win64 "
                    + "zip, its path given to HBShape), or give ComplexText a shaper of your own with SetShaper. "
                    + e.Message, e);
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
        return new HarfBuzzShaper(this).Shape(request);
    }

    /// <summary>
    /// Shapes each run in the font with hb-shape, all of them in one run of
    /// it, a run a line of its input file.
    /// </summary>
    /// <param name="font">the font file.</param>
    /// <param name="runs">the runs of text.</param>
    /// <param name="language">the language, or null.</param>
    /// <returns>the glyphs of each run, in font units.</returns>
    public IList<HarfBuzzGlyph[]> ShapeRuns(byte[] font, IList<String> runs, String language) {
        String dir = System.IO.Path.Combine(TempRoot(), "pdfjet-hbshape-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            StringBuilder input = new StringBuilder();
            foreach (String run in runs) {
                input.Append(run).Append('\n');
            }
            String fontPath = System.IO.Path.Combine(dir, "font");
            String inPath = System.IO.Path.Combine(dir, "in.txt");
            String outPath = System.IO.Path.Combine(dir, "out.txt");
            File.WriteAllBytes(fontPath, font);
            File.WriteAllBytes(inPath, new UTF8Encoding(false).GetBytes(input.ToString()));
            List<String> arguments = new List<String> {
                "--font-file=" + fontPath, "--text-file=" + inPath, "--output-file=" + outPath,
                "--no-glyph-names", "--output-format=json",
            };
            if (!String.IsNullOrEmpty(language)) {
                arguments.Add("--language=" + language);
            }
            Run(arguments.ToArray());
            List<String> lines = new List<String>();
            foreach (String line in File.ReadAllText(outPath, new UTF8Encoding(false)).Split('\n')) {
                String l = line.Replace("\r", "");
                if (l.Length > 0) {
                    lines.Add(l);
                }
            }
            if (lines.Count != runs.Count) {
                throw new InvalidOperationException("hb-shape gave " + lines.Count + " lines for "
                        + runs.Count + " runs of text");
            }
            List<HarfBuzzGlyph[]> shaped = new List<HarfBuzzGlyph[]>();
            foreach (String line in lines) {
                List<int[]> read = ReadGlyphs(line);
                HarfBuzzGlyph[] glyphs = new HarfBuzzGlyph[read.Count];
                for (int i = 0; i < glyphs.Length; i++) {
                    int[] g = read[i];
                    glyphs[i] = new HarfBuzzGlyph(g[0], g[1], g[2], g[3], g[4]);
                }
                shaped.Add(glyphs);
            }
            return shaped;
        } finally {
            try {
                Directory.Delete(dir, true);
            } catch (IOException) {
                // Left to the system's cleaning of its temporary files
            } catch (UnauthorizedAccessException) {
                // Left too
            }
        }
    }
}
}   // End of namespace PDFjet.NET
