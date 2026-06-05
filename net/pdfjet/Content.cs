/**
 * Content.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;

namespace PDFjet.NET {
public class Content {
    public static String OfTextFile(String fileName) {
        StringBuilder sb = new StringBuilder(4096);
        StreamReader reader = null;
        try {
            reader = new StreamReader(fileName);
            int ch;
            while ((ch = reader.Read()) != -1) {
                if (ch == '\r') {
                    // Skip it
                } else if (ch == '"') {
                    sb.Append("\"");
                } else {
                    sb.Append((char) ch);
                }
            }
        } finally {
            reader.Close();
        }
        return sb.ToString();
    }

    public static byte[] OfBinaryFile(String fileName) {
        MemoryStream ms = new MemoryStream();
        BufferedStream stream = null;
        try {
            stream = new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            byte[] buffer = new byte[4096];
            int count = 0;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
                ms.Write(buffer, 0, count);
            }
        } finally {
            stream.Close();
        }
        return ms.ToArray();
    }

    public static byte[] GetFromStream(Stream stream) {
        MemoryStream ms = new MemoryStream();
        try {
            byte[] buffer = new byte[4096];
            int count = 0;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
                ms.Write(buffer, 0, count);
            }
        } finally {
            stream.Close();
        }
        return ms.ToArray();
    }
}   // End of Content.cs
}   // End of namespace PDFjet.NET
