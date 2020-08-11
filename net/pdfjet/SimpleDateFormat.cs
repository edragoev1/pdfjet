/**
 * SimpleDateFormat.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
