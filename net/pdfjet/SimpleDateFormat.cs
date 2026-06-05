/**
 * SimpleDateFormat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class SimpleDateFormat {
    private String format = null;

    // SimpleDateFormat sdf1 = new SimpleDateFormat("yyyyMMddHHmmss'Z'");
    // SimpleDateFormat sdf2 = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
    public SimpleDateFormat(String format) {
        this.format = format;
    }

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
            for (int i = 0; i < list.Count; i++) {
                String str = list[i];
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
            for (int i = 0; i < list.Count; i++) {
                String str = list[i].ToString();
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
