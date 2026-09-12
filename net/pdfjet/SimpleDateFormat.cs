/*
 * SimpleDateFormat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>Formats dates for the PDF metadata.</summary>
internal class SimpleDateFormat {
    private String format = null;

    /// <summary>Creates a formatter for the yyyyMMddHHmmss'Z' or the yyyy-MM-dd'T'HH:mm:ss pattern.</summary>
    public SimpleDateFormat(String format) {
        this.format = format;
    }

    /// <summary>Formats the date and time using the pattern of this formatter.</summary>
    public String Format(DateTime now) {
        String dateAndTime = now.Year.ToString();
        if (format[4] == '-') {
            List<String> list = new List<String>();
            list.Add("-");
            list.Add(now.Month.ToString());
            list.Add("-");
            list.Add(now.Day.ToString());
            list.Add("T");
            list.Add(now.Hour.ToString());
            list.Add(":");
            list.Add(now.Minute.ToString());
            list.Add(":");
            list.Add(now.Second.ToString());
            foreach (String str in list) {
                if (str.Length == 1 && Char.IsDigit(str[0])) {
                    dateAndTime += "0";
                }
                dateAndTime += str;
            }
        } else {
            List<int> list = new List<int>();
            list.Add(now.Month);
            list.Add(now.Day);
            list.Add(now.Hour);
            list.Add(now.Minute);
            list.Add(now.Second);
            foreach (int value in list) {
                String str = value.ToString();
                if (str.Length == 1) {
                    dateAndTime += "0";
                }
                dateAndTime += str;
            }
            dateAndTime += "Z";
        }

        return dateAndTime;
    }
}   // End of SimpleDateFormat.cs
}   // End of package PDFjet.NET
