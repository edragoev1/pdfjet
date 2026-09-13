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

    public static Stream Open(string path) {
        return new FileStream(RepoPath(path), FileMode.Open, FileAccess.Read);
    }

    public static PDF NewPDF() {
        return new PDF(new MemoryStream());
    }

    public static Font Helvetica(PDF pdf) {
        return new Font(pdf, CoreFont.HELVETICA);
    }

    public static string Latin1(byte[] bytes) {
        return Encoding.Latin1.GetString(bytes);
    }

    /// <summary>Returns the content stream of the page, which is not compressed yet.</summary>
    public static string Content(Page page) {
        return Latin1(page.GetContent());
    }

    /// <summary>Returns the text as a core font draws it: a byte per character in upper case hexadecimal.</summary>
    public static string Hex(string text) {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in Encoding.Latin1.GetBytes(text)) {
            sb.Append("0123456789ABCDEF"[b >> 4]).Append("0123456789ABCDEF"[b & 0x0F]);
        }
        return sb.ToString();
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
}
}
