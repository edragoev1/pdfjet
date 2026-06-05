/**
 * TextUtils.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
public class TextUtils {
    public static void PrintDuration(String example, long time0, long time1) {
        String duration = String.Format("{0:N1}", (time1 - time0)/1.0).Replace(",", "");
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
