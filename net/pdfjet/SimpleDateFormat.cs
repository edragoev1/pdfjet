/**
 * SimpleDateFormat.cs
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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
        }
        else {
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
