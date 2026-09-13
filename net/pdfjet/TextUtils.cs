/*
 * TextUtils.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;

namespace PDFjet.NET {
/// <summary>The helper that the examples use to print how long each one took.</summary>
public class TextUtils {
    /// <summary>Prints the name of an example and how long it took.</summary>
    public static void PrintDuration(String example, long time0, long time1) {
        String duration = ((time1 - time0)/1.0).ToString("F1", CultureInfo.InvariantCulture);
        if (duration.Length == 3) {
            duration = "    " + duration;
        } else if (duration.Length == 4) {
            duration = "   " + duration;
        } else if (duration.Length == 5) {
            duration = "  " + duration;
        } else if (duration.Length == 6) {
            duration = " " + duration;
        }
        Console.WriteLine(example + " => " + duration);
    }
}   // End of TextUtils.cs
}   // End of namespace PDFjet.NET
