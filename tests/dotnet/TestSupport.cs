/*
 * TestSupport.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// Helpers shared by the unit tests, as in the Java TestSupport. The tests read
/// the PngSuite images and the fonts from the repository root, found by walking
/// up from the test assembly, or from the PDFJET_ROOT environment variable.
/// </summary>
public static class TestSupport {
    /// <summary>The tolerance for coordinates and widths, a hundredth of a point.</summary>
    public const float DELTA = 0.01f;

    private static readonly string Root = FindRoot();

    private static string FindRoot() {
        string env = Environment.GetEnvironmentVariable("PDFJET_ROOT");
        if (!string.IsNullOrEmpty(env)) {
            return env;
        }
        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(System.IO.Path.Combine(dir.FullName, "PngSuite"))) {
            dir = dir.Parent;
        }
        return dir != null ? dir.FullName : Directory.GetCurrentDirectory();
    }

    public static string RepoPath(string path) {
        return System.IO.Path.Combine(Root, path);
    }

    /// <summary>
    /// Opens a file of the repository, or a font PDFjet carries, as the Font
    /// constructor that takes a path reads it.
    /// </summary>
    public static Stream Open(string path) {
        string file = RepoPath(path);
        if (!File.Exists(file) && path.StartsWith("fonts/", StringComparison.Ordinal)) {
            return Font.Open(path);
        }
        return new FileStream(file, FileMode.Open, FileAccess.Read);
    }

    /// <summary>
    /// The bytes of a file of the repository, or of a font PDFjet carries, as
    /// Open reads them: for the tests that read or patch a font file itself.
    /// </summary>
    public static byte[] FontBytes(string path) {
        using (Stream stream = Open(path)) {
            MemoryStream bytes = new MemoryStream();
            stream.CopyTo(bytes);
            return bytes.ToArray();
        }
    }

    /// <summary>
    /// Returns the font with its character map rewritten so that the second
    /// character of each pair is drawn with the glyph of the first, as Noto
    /// Sans Arabic draws ١ and ۱, and ل and its isolated form ﻝ, with one
    /// glyph, which no font PDFjet carries does in Arabic. The map is a format
    /// 12 subtable of the Windows platform, of every character of the font to
    /// its glyph, put at the end of the font in place of its cmap table, which
    /// PDFjet reads as it reads any font's.
    /// </summary>
    public static byte[] WithSharedGlyphs(byte[] font, int[][] pairs) {
        SortedDictionary<int, int> map = new SortedDictionary<int, int>();
        new OTF(new MemoryStream(font)).unicodeToGID.Each((c, gid) => map[c] = gid);
        foreach (int[] pair in pairs) {
            int gid;
            if (!map.TryGetValue(pair[0], out gid)) {
                throw new ArgumentException(string.Format("the font has no U+{0:X4}", pair[0]));
            }
            map[pair[1]] = gid;
        }
        List<byte> cmap = new List<byte>();
        Action<int> u16 = v => { cmap.Add((byte) (v >> 8)); cmap.Add((byte) v); };
        Action<int> u32 = v => { u16(v >> 16); u16(v & 0xFFFF); };
        u16(0); u16(1);                     // The version, one record
        u16(3); u16(10); u32(12);           // Windows, Unicode full
        u16(12); u16(0); u32(16 + 12 * map.Count); u32(0); u32(map.Count);
        foreach (KeyValuePair<int, int> e in map) {
            u32(e.Key); u32(e.Key); u32(e.Value);
        }
        byte[] patched = new byte[font.Length + cmap.Count];
        Array.Copy(font, patched, font.Length);
        cmap.CopyTo(patched, font.Length);
        int tables = (font[4] & 0xFF) << 8 | (font[5] & 0xFF);
        for (int i = 0; i < tables; i++) {
            int entry = 12 + 16 * i;
            if (Encoding.Latin1.GetString(font, entry, 4) == "cmap") {
                int[] values = {font.Length, cmap.Count};
                for (int k = 0; k < 2; k++) {
                    int at = entry + 8 + 4 * k;
                    patched[at] = (byte) (values[k] >> 24);
                    patched[at + 1] = (byte) (values[k] >> 16);
                    patched[at + 2] = (byte) (values[k] >> 8);
                    patched[at + 3] = (byte) values[k];
                }
                return patched;
            }
        }
        throw new ArgumentException("the font has no cmap table");
    }

    public static PDF NewPDF() {
        return new PDF(new MemoryStream());
    }

    public static Font Helvetica(PDF pdf) {
        return new Font(pdf, CoreFont.HELVETICA);
    }

    /// <summary>
    /// Lets the test draw in a core font in a document of PDF/A or PDF/UA,
    /// which Complete refuses, until the result is disposed: for the tests of
    /// what such a document tags, which find the text of a core font by its
    /// encoding. The flag is of the thread, as the test classes run in parallel.
    /// </summary>
    public static IDisposable AllowCoreFonts() {
        PDF.coreFontsInCompliance = true;
        return new CoreFontsAllowed();
    }

    private sealed class CoreFontsAllowed : IDisposable {
        public void Dispose() {
            PDF.coreFontsInCompliance = false;
        }
    }

    /// <summary>
    /// Returns IBM Plex Sans, a font that is embedded, as a document of PDF/A
    /// or PDF/UA needs: a core font is refused in one (review of
    /// 11 October 2026, #6).
    /// </summary>
    public static Font PlexSans(PDF pdf) {
        return new Font(pdf, RepoPath("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
    }

    public static string Latin1(byte[] bytes) {
        return Encoding.Latin1.GetString(bytes);
    }

    /// <summary>Returns the content stream of the page, which is not compressed yet.</summary>
    public static string Content(Page page) {
        return Latin1(page.GetContent());
    }

    /// <summary>
    /// Returns the text as PDF writes a text string: UTF-16 with its byte order
    /// mark, in lower case hexadecimal, as AppendTextString writes it.
    /// </summary>
    public static string TextString(string text) {
        StringBuilder sb = new StringBuilder("<feff");
        foreach (char unit in text) {
            sb.Append(((int) unit).ToString("x4"));
        }
        return sb.Append('>').ToString();
    }

    /// <summary>Returns the text as a core font draws it: a byte per character in upper case hexadecimal.</summary>
    public static string Hex(string text) {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in Encoding.Latin1.GetBytes(text)) {
            sb.Append("0123456789ABCDEF"[b >> 4]).Append("0123456789ABCDEF"[b & 0x0F]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns the x and the y of the Td before the first text, drawn in a core
    /// font, that starts with the text.
    /// </summary>
    public static float[] PositionOf(string content, string text) {
        int i = content.IndexOf("<" + Hex(text), StringComparison.Ordinal);
        if (i == -1) {
            throw new Xunit.Sdk.XunitException("\"" + text + "\" is not drawn:\n" + content);
        }
        int td = content.LastIndexOf(" Td\n", i, StringComparison.Ordinal);
        int start = content.LastIndexOf('\n', td) + 1;
        string[] parts = content.Substring(start, td - start).Split(' ');
        return new float[] {
            float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)};
    }

    /// <summary>
    /// Returns the fill color set last before the text, drawn in a core font, as
    /// its operator, for example "1 1 1 rg".
    /// </summary>
    public static string FillColorBefore(string content, string text) {
        int rg = content.LastIndexOf(" rg\n", content.IndexOf("<" + Hex(text) + ">"), System.StringComparison.Ordinal);
        int start = content.LastIndexOf('\n', rg) + 1;
        return content.Substring(start, rg + 3 - start);
    }

    /// <summary>Decodes a PDF text string written as a hexadecimal string with a UTF-16BE byte order mark.</summary>
    public static string Utf16Hex(string value) {
        string digits = value.Replace("<", "").Replace(">", "").Replace(" ", "").Replace("\n", "");
        byte[] bytes = new byte[digits.Length / 2];
        for (int i = 0; i < bytes.Length; i++) {
            bytes[i] = Convert.ToByte(digits.Substring(2 * i, 2), 16);
        }
        string text = Encoding.BigEndianUnicode.GetString(bytes);
        return text.StartsWith("﻿", StringComparison.Ordinal) ? text.Substring(1) : text;
    }

    public static List<PDFobj> Read(byte[] pdf) {
        return new PDF().Read(new MemoryStream(pdf));
    }

    public static List<PDFobj> Read(byte[] pdf, string password) {
        return new PDF().Read(new MemoryStream(pdf), password);
    }

    /// <summary>Returns the first /ID of the trailer.</summary>
    public static string TrailerID(byte[] pdf) {
        string raw = Latin1(pdf);
        int start = raw.LastIndexOf("/ID[<", StringComparison.Ordinal) + 5;
        return raw.Substring(start, raw.IndexOf('>', start) - start);
    }

    /// <summary>Returns the object that holds the key, or null.</summary>
    public static PDFobj FindObject(List<PDFobj> objects, string key) {
        foreach (PDFobj obj in objects) {
            if (obj.GetValue(key).Length > 0) {
                return obj;
            }
        }
        return null;
    }

    public static string Crc32(byte[] data) {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data) {
            crc ^= b;
            for (int k = 0; k < 8; k++) {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }
        return (~crc).ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static void AssertNear(float expected, float actual, float delta, string what = "") {
        Assert.True(Math.Abs(expected - actual) <= delta, what + ": expected " + expected + " but was " + actual);
    }

    public static void AssertXY(float x, float y, float[] xy) {
        AssertNear(x, xy[0], DELTA, "x");
        AssertNear(y, xy[1], DELTA, "y");
    }

    public static void AssertRGB(float r, float g, float b, float[] rgb) {
        Assert.Equal(3, rgb.Length);
        AssertNear(r, rgb[0], 0.0001f, "red");
        AssertNear(g, rgb[1], 0.0001f, "green");
        AssertNear(b, rgb[2], 0.0001f, "blue");
    }

    /// <summary>A directory that is deleted when the test is disposed, like the JUnit TempDir.</summary>
    public sealed class TempDir : IDisposable {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pdfjet-test-" + Guid.NewGuid().ToString("N"));

        public TempDir() {
            Directory.CreateDirectory(Path);
        }

        public string Write(string name, byte[] bytes) {
            string file = System.IO.Path.Combine(Path, name);
            File.WriteAllBytes(file, bytes);
            return file;
        }

        public void Dispose() {
            Directory.Delete(Path, true);
        }
    }

    // Returns the milliseconds the work takes, the shortest of up to three
    // runs: a run over the limit is run again, as a machine under load can make
    // a linear run slow once, where a quadratic one is slow every time (review
    // of 12 October 2026, C6).
    public static long ShortestMillis(long limit, Action work) {
        long shortest = long.MaxValue;
        for (int run = 0; run < 3 && shortest >= limit; run++) {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            work();
            shortest = Math.Min(shortest, watch.ElapsedMilliseconds);
        }
        return shortest;
    }
}
}
