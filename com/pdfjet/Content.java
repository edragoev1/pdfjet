/**
 *  Content.java
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package com.pdfjet;

import java.io.*;

public class Content {
    public static String ofTextFile(String fileName) throws IOException {
        StringBuilder sb = new StringBuilder(2048);
        FileInputStream stream = null;
        Reader reader = null;
        try {
            stream = new FileInputStream(fileName);
            reader = new InputStreamReader(stream, "UTF-8");
            int ch = 0;
            while ((ch = reader.read()) != -1) {
                sb.append((char) ch);
            }
        } finally {
            reader.close();
            stream.close();
        }
        return sb.toString();
    }

    public static byte[] ofBinaryFile(String fileName) throws Exception {
        FileInputStream stream = new FileInputStream(fileName);
        stream.close();
        return new byte[] {};
    }
}
