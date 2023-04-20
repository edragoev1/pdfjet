package com.pdfjet;

public class Token {
    public static byte[] beginDictionary = "<<\n".getBytes();
    public static byte[] endDictionary = ">>\n".getBytes();
    public static byte[] stream = "stream\n".getBytes();
    public static byte[] endstream = "\nendstream\n".getBytes();
    public static byte[] endobj = "endobj\n".getBytes();
    public static byte[] count = "/Count ".getBytes();
    public static byte[] length = "/Length ".getBytes();
    public static byte space = ' ';
    public static byte newline = '\n';
}
