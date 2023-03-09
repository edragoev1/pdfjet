
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace PDFjet.NET {
public class SVG {
    public static List<String> GetSVGPaths(String fileName) {
        List<String> paths = new List<String>();
        StringBuilder buf = new StringBuilder();
        bool inPath = false;
        byte[] bytes = File.ReadAllBytes(fileName);
        String str = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (!inPath && buf.ToString().EndsWith("<path d=")) {
                inPath = true;
                buf.Length = 0;
            } else if (inPath && ch == '\"') {
                inPath = false;
                paths.Add(buf.ToString());
                buf.Length = 0;
            } else {
                buf.Append(ch);
            }
        }
        return paths;
    }
  
}
}   // End of SVG.cs
