/*
 * Content.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>Reads the contents of files and streams.</summary>
public class Content {
    /// <summary>Returns the contents of the specified text file, which is read as UTF-8.</summary>
    public static String OfTextFile(String fileName) {
        StringBuilder sb = new StringBuilder(4096);
        // UTF-8 only, as in the other ports: the reader does not look for
        // UTF-16 and UTF-32 byte order marks, and keeps a UTF-8 one.
        using (StreamReader reader = new StreamReader(fileName, new UTF8Encoding(false), false)) {
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
        }
        // A byte order mark at the start of the file is not part of the text.
        if (sb.Length > 0 && sb[0] == '\uFEFF') {
            sb.Remove(0, 1);
        }
        return sb.ToString();
    }

    /// <summary>Returns the contents of the specified file as bytes.</summary>
    public static byte[] OfBinaryFile(String fileName) {
        MemoryStream ms = new MemoryStream();
        using (BufferedStream stream = new BufferedStream(
                new FileStream(fileName, FileMode.Open, FileAccess.Read))) {
            byte[] buffer = new byte[4096];
            int count = 0;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
                ms.Write(buffer, 0, count);
            }
        }
        return ms.ToArray();
    }

    /// <summary>Returns all the bytes read from the stream, reading bufferSize bytes at a time.</summary>
    public static byte[] GetFromStream(Stream stream, int bufferSize) {
        MemoryStream ms = new MemoryStream();
        try {
            byte[] buffer = new byte[bufferSize];
            int count = 0;
            while ((count = stream.Read(buffer, 0, bufferSize)) > 0) {
                ms.Write(buffer, 0, count);
            }
        } finally {
            stream.Close();
        }
        return ms.ToArray();
    }

    /// <summary>Returns all the bytes read from the stream.</summary>
    /// <summary>Returns the lines of a UTF-8 text file, without a byte order mark and without the line separators; an empty line is an empty string.</summary>
    public static List<String> LinesOfTextFile(String fileName) {
        List<String> lines = new List<String>();
        String contents = OfTextFile(fileName);
        StringBuilder buffer = new StringBuilder();
        foreach (char ch in contents) {
            if (ch == '\n') {
                lines.Add(buffer.ToString());
                buffer.Length = 0;
            } else {
                buffer.Append(ch);
            }
        }
        if (buffer.Length > 0) {
            lines.Add(buffer.ToString());
        }
        return lines;
    }

    public static byte[] GetFromStream(Stream stream) {
        return GetFromStream(stream, 4096);
    }
}   // End of Content.cs
}   // End of namespace PDFjet.NET
