using System;
using System.Text;

namespace PDFjet.NET {
public class Token {
    public static byte[] beginDictionary = Encoding.ASCII.GetBytes("<<\n");
    public static byte[] endDictionary = Encoding.ASCII.GetBytes(">>\n");
    public static byte[] stream = Encoding.ASCII.GetBytes("stream\n");
    public static byte[] endstream = Encoding.ASCII.GetBytes("\nendstream\n");
    public static byte[] endobj = Encoding.ASCII.GetBytes("endobj\n");
    public static byte[] count = Encoding.ASCII.GetBytes("/Count ");
    public static byte[] length = Encoding.ASCII.GetBytes("/Length ");
    public static byte space = Convert.ToByte(' ');
    public static byte newline = Convert.ToByte('\n');
}
}
