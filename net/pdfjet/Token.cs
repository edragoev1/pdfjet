using System.Text;

namespace PDFjet.NET {
public class Token {
    public static byte[] stream = Encoding.ASCII.GetBytes("\nstream\n");
    public static byte[] endstream = Encoding.ASCII.GetBytes("\nendstream\n");
    public static byte[] space = Encoding.ASCII.GetBytes(" ");
    public static byte[] newline = Encoding.ASCII.GetBytes("\n");
    public static byte[] endobj = Encoding.ASCII.GetBytes("endobj\n");
    public static byte[] beginDictionary = Encoding.ASCII.GetBytes("<<\n");
    public static byte[] endDictionary = Encoding.ASCII.GetBytes(">>\n");
    public static byte[] length = Encoding.ASCII.GetBytes("/Length ");
    public static byte[] first = Encoding.ASCII.GetBytes("/First ");
    public static byte[] last = Encoding.ASCII.GetBytes("/Last ");
    public static byte[] count = Encoding.ASCII.GetBytes("/Count ");
}
}
