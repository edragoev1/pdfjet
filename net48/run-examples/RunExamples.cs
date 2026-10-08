/*
 * RunExamples.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

/// <summary>
/// Runs Example_01 to Example_57, compiled as they are, on .NET Framework 4.8
/// against the net48 PDFjet.dll, in the folder it is started in, which has
/// the folders the examples read, fonts, data, images, PngSuite and examples:
/// each example's Main, its time, whether its PDF was made whole, and what it
/// threw. The PDFs are made in that folder, as build-dotnet.sh makes them;
/// the results also go to net48-results.txt there.
///
///     RunExamples.exe            all of them
///     RunExamples.exe 7 29 46    those
/// </summary>
public static class RunExamples {
    public static int Main(string[] args) {
        if (!Directory.Exists("fonts") || !Directory.Exists("images")) {
            Console.Error.WriteLine("Start it in the folder that has fonts, data, images, PngSuite and examples.");
            return 2;
        }
        var report = new StringBuilder();
        Say(report, "PDFjet " + typeof(PDFjet.NET.PDF).Assembly.GetName().Version + " on "
                + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + ", "
                + System.Runtime.InteropServices.RuntimeInformation.OSDescription + ", "
                + (Environment.Is64BitProcess ? "64-bit" : "32-bit"));
        int failed = 0;
        int ran = 0;
        for (int i = 1; i <= 57; i++) {
            if (args.Length > 0 && Array.IndexOf(args, i.ToString()) < 0) {
                continue;
            }
            string name = "Example_" + i.ToString("00");
            string pdf = name + ".pdf";
            if (File.Exists(pdf)) {
                File.Delete(pdf);
            }
            var watch = Stopwatch.StartNew();
            string error = null;
            try {
                Type type = Type.GetType(name, true);
                MethodInfo main = type.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                main.Invoke(null, new object[] {new string[0]});
            } catch (TargetInvocationException e) {
                error = e.InnerException.GetType().Name + ": " + e.InnerException.Message;
            } catch (Exception e) {
                error = e.GetType().Name + ": " + e.Message;
            }
            watch.Stop();
            ran++;
            // A PDF made whole ends with %%EOF
            bool whole = false;
            if (File.Exists(pdf)) {
                byte[] bytes = File.ReadAllBytes(pdf);
                string end = Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length));
                whole = end.Contains("%%EOF");
            }
            if (error != null || !whole) {
                failed++;
            }
            Say(report, name + " => " + watch.ElapsedMilliseconds.ToString().PadLeft(5) + " ms  "
                    + (error != null ? "FAILED: " + error : whole ? "ok" : "FAILED: " + pdf + " not made whole"));
        }
        Say(report, ran - failed + " of " + ran + " made their PDF" + (failed == 0 ? "." : "; " + failed + " failed."));
        File.WriteAllText("net48-results.txt", report.ToString());
        return failed == 0 ? 0 : 1;
    }

    private static void Say(StringBuilder report, string line) {
        Console.WriteLine(line);
        report.AppendLine(line);
    }
}
