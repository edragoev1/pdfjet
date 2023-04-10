package com.pdfjet;

public class Token {
    public static byte[] stream = "\nstream\n".getBytes();
    public static byte[] endstream = "\nendstream\n".getBytes();
    public static byte[] space = " ".getBytes();
    public static byte[] newline = "\n".getBytes();
    public static byte[] endobj = "endobj\n".getBytes();
    public static byte[] beginDictionary = "<<\n".getBytes();
    public static byte[] endDictionary = ">>\n".getBytes();
}
