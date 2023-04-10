package com.pdfjet;

public class Token {
    public static byte[] stream = "\nstream\n".getBytes();
    public static byte[] endstream = "\nendstream\n".getBytes();
    public static byte[] space = " ".getBytes();
    public static byte[] newline = "\n".getBytes();
    public static byte[] endobj = "endobj\n".getBytes();
    public static byte[] beginDictionary = "<<\n".getBytes();
    public static byte[] endDictionary = ">>\n".getBytes();
    public static byte[] length = "/Length ".getBytes();
    public static byte[] first = "/First ".getBytes();
    public static byte[] last = "/Last ".getBytes();
    public static byte[] count = "/Count ".getBytes();
}
