/**
 *  SVG.java
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

import java.io.FileInputStream;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

public class SVG {
    public static List<String> getSVGPaths(String fileName) throws IOException {
        List<String> paths = new ArrayList<String>();
        StringBuilder buf = new StringBuilder();
        boolean inPath = false;
        FileInputStream stream = new FileInputStream(fileName);
        int ch;
        while ((ch = stream.read()) != -1) {
            if (!inPath && buf.toString().endsWith("<path d=")) {
                inPath = true;
                buf.setLength(0);
            } else if (inPath && ch == '\"') {
                inPath = false;
                paths.add(buf.toString());
                buf.setLength(0);
            } else {
                buf.append((char) ch);
            }
        }
        stream.close();
        return paths;
    }

    private static boolean isCommand(char ch) {
        // Please note:
        // Capital letter commands use absolute coordinates
        // Small letter commands use relative coordinates
        if (ch == 'M' || ch == 'm') {           // moveto
            return true;
        } else if (ch == 'L' || ch == 'l') {    // lineto
            return true;
        } else if (ch == 'H' || ch == 'h') {    // horizontal lineto
            return true;
        } else if (ch == 'V' || ch == 'v') {    // vertical lineto
            return true;
        } else if (ch == 'C' || ch == 'c') {    // cubic curveto
            return true;
        } else if (ch == 'S' || ch == 's') {    // smooth cubic curveto
            return true;
        } else if (ch == 'Q' || ch == 'q') {    // quadratic curveto
            return true;
        } else if (ch == 'T' || ch == 't') {    // smooth quadratic curveto
            return true;
        } else if (ch == 'A' || ch == 'a') {    // elliptical arc
            return true;
        } else if (ch == 'Z' || ch == 'z') {    // close path
            return true;
        }
        return false;
    }

    public static List<PathOperation> getPathOperations(List<String> paths) {
        List<PathOperation> operations = new ArrayList<PathOperation>();
        PathOperation op = null;
        for (String path : paths) {
            // Path example:
            // "M22.65 34h3v-8.3H34v-3h-8.35V14h-3v8.7H14v3h8.65ZM24 44z"
            System.out.println(path);
            System.out.println();
            StringBuilder buf = new StringBuilder();
            boolean token = false;
            for (int i = 0; i < path.length(); i++) {
                char ch = path.charAt(i);
                if (isCommand(ch)) {                    // open path
                    if (token) {
                        op.args.add(buf.toString());
                        buf.setLength(0);
                    }
                    token = false;
                    op = new PathOperation(ch);
                    operations.add(op);
                } else if (ch == ' ') {
                    if (token) {
                        op.args.add(buf.toString());
                        buf.setLength(0);
                    }
                    token = false;
                } else if (ch == '-') {
                    if (token) {
                        op.args.add(buf.toString());
                        buf.setLength(0);
                    }
                    token = true;
                    buf.append(ch);
                } else {
                    token = true;
                    buf.append(ch);
                }
            }
        }
        return operations;
    }

    public static List<PathOperation> getPDFPathOperations(List<PathOperation> operations) {
        float x = 0f;
        float y = 0f;
        PathOperation prevOperation = null;
        for (PathOperation op : operations) {
            if (op.cmd == 'M') {
                x = Float.valueOf(op.args.get(0));
                y = Float.valueOf(op.args.get(1));
            } else if (op.cmd == 'm') {
                op.cmd = 'M';
                x += Float.valueOf(op.args.get(0));
                y += Float.valueOf(op.args.get(1));
                op.args.clear();
                op.args.add(String.valueOf(x));
                op.args.add(String.valueOf(y));
            } else if (op.cmd == 'L') {
            } else if (op.cmd == 'l') {
                op.cmd = 'L';
                x += Float.valueOf(op.args.get(0));
                y += Float.valueOf(op.args.get(1));
                op.args.clear();
                op.args.add(String.valueOf(x));
                op.args.add(String.valueOf(y));
            } else if (op.cmd == 'H') {
                op.cmd = 'L';
                x = Float.valueOf(op.args.get(0));
                op.args.clear();
                op.args.add(String.valueOf(x));
                op.args.add(String.valueOf(y));
            } else if (op.cmd == 'h') {
                op.cmd = 'L';
                x += Float.valueOf(op.args.get(0));
                op.args.clear();
                op.args.add(String.valueOf(x));
                op.args.add(String.valueOf(y));
            } else if (op.cmd == 'V') {
                op.cmd = 'L';
                y = Float.valueOf(op.args.get(0));
                op.args.clear();
                op.args.add(String.valueOf(x));
                op.args.add(String.valueOf(y));
            } else if (op.cmd == 'v') {
                op.cmd = 'L';
                y += Float.valueOf(op.args.get(0));
                op.args.clear();
                op.args.add(String.valueOf(x));
                op.args.add(String.valueOf(y));
            } else if (op.cmd == 'Q') {
            } else if (op.cmd == 'q') {
                op.cmd = 'Q';
                List<String> temp = new ArrayList<String>();
                for (int i = 0; i <= op.args.size() - 4; i += 4) {
                    float x1 = x + Float.valueOf(op.args.get(i));
                    float y1 = y + Float.valueOf(op.args.get(i + 1));
                    temp.add(String.valueOf(x1));
                    temp.add(String.valueOf(y1));
                    x += Float.valueOf(op.args.get(i + 2));
                    y += Float.valueOf(op.args.get(i + 3));
                    temp.add(String.valueOf(x));
                    temp.add(String.valueOf(y));
                }
                op.args.clear();
                op.args.addAll(temp);
            } else if (op.cmd == 'T') {
            } else if (op.cmd == 'X') { // 't'
                op.cmd = 'Q';
                if (prevOperation.cmd == 'Q' || prevOperation.cmd == 'q') {

                } else {

                }
                float cpx = Float.valueOf(prevOperation.args.get(0));
                float cpy = Float.valueOf(prevOperation.args.get(1));

                List<String> temp = new ArrayList<String>();
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x += Float.valueOf(op.args.get(i));
                    y += Float.valueOf(op.args.get(i + 1));
                    temp.add(String.valueOf(x));
                    temp.add(String.valueOf(y));
                }
                op.args.clear();
                op.args.addAll(temp);
            } else if (op.cmd == 'Z' || op.cmd == 'z') {
                // TODO:
            }
            prevOperation = op;
        }
        return operations;
    }

    public static void main(String[] args) throws IOException {
        FileWriter writer = new FileWriter("test73.svg");
        writer.write("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"100\" width=\"100\">\n");
        writer.write("  <path d=\"");
        // writer.write("M 20 20 q 0 60 60 60 0 -60 -60 -60 Z");
        List<String> svgPaths = getSVGPaths(args[0]);
        List<PathOperation> pathOperations = getPathOperations(svgPaths);
        List<PathOperation> pdfPathOperations = getPDFPathOperations(pathOperations);
        for (PathOperation operation : pdfPathOperations) {
            System.out.print(operation.cmd + " ");
            writer.write(operation.cmd + " ");
            for (String argument : operation.args) {
                System.out.print(argument + " ");
                writer.write(argument + " ");
            }
        }
        System.out.println();
        writer.write("\"/>\n");
        writer.write("</svg>\n");
        writer.flush();
        writer.close();
    }
}
