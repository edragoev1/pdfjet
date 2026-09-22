/*
 * Corpus.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// The C# port's side of the corpus check, tests/corpus/check-corpus.py, as
/// tests/corpus/go is the Go port's. It reads one PDF, merges the whole of it
/// into a document of its own, and splits its first and its last page into two
/// more:
///
///     dotnet Corpus.dll IN.pdf PASSWORD OUTDIR
///
/// It writes merged.pdf, first.pdf and last.pdf into OUTDIR, the ones it could
/// make, and prints what it read as JSON: the error, or the number of pages and
/// the size of each page.
///
/// The reader refuses a PDF it cannot read with an Exception, an
/// ArgumentException or an InvalidOperationException, of those very types,
/// with an IOException, or with the InvalidDataException of a stream that
/// cannot be decoded, which is not an IOException but a SystemException. Any other exception -- an index out of range, a null reference,
/// an invalid cast, an overflow, a FormatException of a number it parsed
/// without checking -- is a programming error of the port, and is reported as
/// a crash, as a Go runtime error is.
/// </summary>
public static class Corpus {
    private sealed class Report {
        internal string error;
        internal string crash;
        internal List<PageSize> sizes = new List<PageSize>();
        // The documents made, in the order they are made, and "ok" or the error.
        internal List<KeyValuePair<string, string>> made = new List<KeyValuePair<string, string>>();
    }

    // Returns whether the exception is the reader's own refusal of a PDF.
    private static bool IsRefusal(Exception e) {
        Type type = e.GetType();
        return type == typeof(Exception)
                || type == typeof(ArgumentException)
                || type == typeof(InvalidOperationException)
                || e is IOException
                || e is InvalidDataException;
    }

    // Describes the exception of a crash, with the place it was thrown.
    private static string CrashOf(Exception e) {
        string place = "";
        if (e.StackTrace != null) {
            place = " " + e.StackTrace.Split('\n')[0].Trim();
        }
        return e.GetType().Name + ": " + e.Message + place;
    }

    private static List<PDFobj> Read(byte[] data, string password) {
        return new PDF().Read(new MemoryStream(data), password);
    }

    // Merges the pages into path, and returns "ok" or the error.
    private static string Write(Report report, List<PDFobj> objects, string path, params int[] pages) {
        try {
            using (FileStream f = new FileStream(path, FileMode.Create)) {
                try {
                    PDF pdf = new PDF(new BufferedStream(f));
                    if (pages.Length == 0) {
                        pdf.Merge(objects);
                    } else {
                        pdf.Merge(objects, pages);
                    }
                    pdf.Complete();
                    return "ok";
                } catch (Exception) {
                    f.Dispose();
                    File.Delete(path);
                    throw;
                }
            }
        } catch (Exception e) {
            if (!IsRefusal(e)) {
                report.crash = System.IO.Path.GetFileName(path) + ": " + CrashOf(e);
            }
            return e.Message;
        }
    }

    private static void Run(Report report, string file, string password, string dir) {
        byte[] data;
        try {
            data = File.ReadAllBytes(file);
        } catch (IOException e) {
            report.error = e.Message;
            return;
        }
        List<PDFobj> objects;
        try {
            objects = Read(data, password);
        } catch (Exception e) {
            if (!IsRefusal(e)) {
                report.crash = CrashOf(e);
            }
            report.error = e.Message;
            return;
        }
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        foreach (PDFobj page in pages) {
            report.sizes.Add(page.GetPageSize());
        }
        // Each document is read again: a merge may change what it merges.
        report.made.Add(new KeyValuePair<string, string>("merged",
                Write(report, objects, System.IO.Path.Combine(dir, "merged.pdf"))));
        if (pages.Count > 0) {
            report.made.Add(new KeyValuePair<string, string>("first",
                    Write(report, Read(data, password), System.IO.Path.Combine(dir, "first.pdf"), 1)));
            report.made.Add(new KeyValuePair<string, string>("last",
                    Write(report, Read(data, password), System.IO.Path.Combine(dir, "last.pdf"), pages.Count)));
        }
    }

    public static int Main(string[] args) {
        if (args.Length != 3) {
            Console.Error.WriteLine("usage: dotnet Corpus.dll IN.pdf PASSWORD OUTDIR");
            return 2;
        }
        Report report = new Report();
        try {
            Run(report, args[0], args[1], args[2]);
        } catch (Exception e) {
            report.crash = CrashOf(e);
        }
        Console.Out.Write(Json(report));
        return 0;
    }

    // The report as JSON, as the Go harness prints it.
    private static string Json(Report report) {
        StringBuilder sb = new StringBuilder("{");
        if (report.error != null) {
            sb.Append("\"error\":").Append(Quote(report.error)).Append(',');
        }
        if (report.crash != null) {
            sb.Append("\"crash\":").Append(Quote(report.crash)).Append(',');
        }
        sb.Append("\"pages\":").Append(report.sizes.Count).Append(",\"sizes\":[");
        for (int i = 0; i < report.sizes.Count; i++) {
            sb.Append(i > 0 ? ",[" : "[")
                    .Append(Number(report.sizes[i].GetWidth())).Append(',')
                    .Append(Number(report.sizes[i].GetHeight())).Append(']');
        }
        sb.Append("],\"made\":{");
        for (int i = 0; i < report.made.Count; i++) {
            sb.Append(i > 0 ? "," : "").Append(Quote(report.made[i].Key)).Append(':')
                    .Append(Quote(report.made[i].Value));
        }
        return sb.Append("}}\n").ToString();
    }

    // A number as Python's json module reads it, NaN and Infinity too.
    private static string Number(float value) {
        if (float.IsNaN(value)) {
            return "NaN";
        }
        if (float.IsInfinity(value)) {
            return value > 0 ? "Infinity" : "-Infinity";
        }
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Quote(string str) {
        StringBuilder sb = new StringBuilder("\"");
        foreach (char c in str) {
            if (c == '"' || c == '\\') {
                sb.Append('\\').Append(c);
            } else if (c < ' ' || (c >= 0xD800 && c <= 0xDFFF)) {
                sb.Append("\\u").Append(((int) c).ToString("x4"));
            } else {
                sb.Append(c);
            }
        }
        return sb.Append('"').ToString();
    }
}
}   // End of namespace PDFjet.NET
