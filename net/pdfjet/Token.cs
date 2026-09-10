using System;
using System.Text;

namespace PDFjet.NET {
/// <summary>Byte sequences of the PDF syntax.</summary>
public class Token {
    // Fundamental structural tokens
    /// <summary>A space.</summary>
    public static readonly byte Space = (byte) ' ';
    /// <summary>A line feed.</summary>
    public static readonly byte Newline = (byte) '\n';
    /// <summary>The start of a dictionary: &lt;&lt;</summary>
    public static readonly byte[] BeginDictionary = Encoding.ASCII.GetBytes("<<\n");
    /// <summary>The end of a dictionary: &gt;&gt;</summary>
    public static readonly byte[] EndDictionary = Encoding.ASCII.GetBytes(">>\n");
    /// <summary>The stream keyword.</summary>
    public static readonly byte[] Stream = Encoding.ASCII.GetBytes("stream\n");
    /// <summary>The endstream keyword.</summary>
    public static readonly byte[] EndStream = Encoding.ASCII.GetBytes("\nendstream\n");

    // Object management tokens
    /// <summary>The " 0 obj" that follows an object number.</summary>
    public static readonly byte[] NewObj = Encoding.ASCII.GetBytes(" 0 obj\n");
    /// <summary>The endobj keyword.</summary>
    public static readonly byte[] EndObj = Encoding.ASCII.GetBytes("endobj\n");
    /// <summary>The " 0 R" that follows the number of a referenced object.</summary>
    public static readonly byte[] ObjRef = Encoding.ASCII.GetBytes(" 0 R\n");

    // Text and content tokens
    /// <summary>The BT operator, which begins a text object.</summary>
    public static readonly byte[] BeginText = Encoding.ASCII.GetBytes("BT\n");
    /// <summary>The ET operator, which ends a text object.</summary>
    public static readonly byte[] EndText = Encoding.ASCII.GetBytes("ET\n");

    // Essential property tokens (used everywhere)
    /// <summary>The /Length key.</summary>
    public static readonly byte[] Length = Encoding.ASCII.GetBytes("/Length ");
    /// <summary>The /Type key.</summary>
    public static readonly byte[] Type = Encoding.ASCII.GetBytes("/Type ");
    /// <summary>The /Resources key.</summary>
    public static readonly byte[] Resources = Encoding.ASCII.GetBytes("/Resources ");
}
}