package com.pdfjet;

public class Token {
    public static final byte[] beginDictionary = "<<\n".getBytes();
    public static final byte[] endDictionary = ">>\n".getBytes();
    public static final byte[] stream = "stream\n".getBytes();
    public static final byte[] endstream = "\nendstream\n".getBytes();
    public static final byte[] newobj = " 0 obj\n".getBytes();
    public static final byte[] endobj = "endobj\n".getBytes();
    public static final byte[] beginText = "BT\n".getBytes();
    public static final byte[] endText = "ET\n".getBytes();
    public static final byte[] count = "/Count ".getBytes();
    public static final byte[] length = "/Length ".getBytes();
    public static final byte space = ' ';
    public static final byte newline = '\n';
}
