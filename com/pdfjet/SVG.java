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

    public static List<PathOperation> getPathOperations(List<String> svgPaths) {
        List<PathOperation> operations = new ArrayList<PathOperation>();
        PathOperation operation = null;
        for (String path : svgPaths) {
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
                        operation.arguments.add(buf.toString());
                        buf.setLength(0);
                    }
                    token = false;
                    operation = new PathOperation(ch);
                    operations.add(operation);
                } else if (ch == ' ') {
                    if (token) {
                        operation.arguments.add(buf.toString());
                        buf.setLength(0);
                    }
                    token = false;
                } else if (ch == '-') {
                    if (token) {
                        operation.arguments.add(buf.toString());
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
        for (PathOperation operation : operations) {
            if (operation.command == 'M') {
                x = Float.valueOf(operation.arguments.get(0));
                y = Float.valueOf(operation.arguments.get(1));
            } else if (operation.command == 'm') {
                operation.command = 'M';
                x += Float.valueOf(operation.arguments.get(0));
                y += Float.valueOf(operation.arguments.get(1));
                operation.arguments.clear();
                operation.arguments.add(String.valueOf(x));
                operation.arguments.add(String.valueOf(y));
            } else if (operation.command == 'L') {
            } else if (operation.command == 'l') {
                operation.command = 'L';
                x += Float.valueOf(operation.arguments.get(0));
                y += Float.valueOf(operation.arguments.get(1));
                operation.arguments.clear();
                operation.arguments.add(String.valueOf(x));
                operation.arguments.add(String.valueOf(y));
            } else if (operation.command == 'H') {
                operation.command = 'L';
                x = Float.valueOf(operation.arguments.get(0));
                operation.arguments.clear();
                operation.arguments.add(String.valueOf(x));
                operation.arguments.add(String.valueOf(y));
            } else if (operation.command == 'h') {
                operation.command = 'L';
                x += Float.valueOf(operation.arguments.get(0));
                operation.arguments.clear();
                operation.arguments.add(String.valueOf(x));
                operation.arguments.add(String.valueOf(y));
            } else if (operation.command == 'V') {
                operation.command = 'L';
                y = Float.valueOf(operation.arguments.get(0));
                operation.arguments.clear();
                operation.arguments.add(String.valueOf(x));
                operation.arguments.add(String.valueOf(y));
            } else if (operation.command == 'v') {
                operation.command = 'L';
                y += Float.valueOf(operation.arguments.get(0));
                operation.arguments.clear();
                operation.arguments.add(String.valueOf(x));
                operation.arguments.add(String.valueOf(y));
            } else if (operation.command == 'Q') {
            } else if (operation.command == 'q') {
                operation.command = 'Q';
                List<String> temp = new ArrayList<String>();
                for (int i = 0; i <= operation.arguments.size() - 4; i += 4) {
                    float x1 = x + Float.valueOf(operation.arguments.get(i));
                    float y1 = y + Float.valueOf(operation.arguments.get(i + 1));
                    temp.add(String.valueOf(x1));
                    temp.add(String.valueOf(y1));
                    x += Float.valueOf(operation.arguments.get(i + 2));
                    y += Float.valueOf(operation.arguments.get(i + 3));
                    temp.add(String.valueOf(x));
                    temp.add(String.valueOf(y));
                }
                operation.arguments.clear();
                operation.arguments.addAll(temp);
            }
        }
        return operations;
    }

    public static void main(String[] args) throws IOException {
        FileWriter writer = new FileWriter("test73.svg");
        writer.write("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"100\" width=\"100\">\n");
        writer.write("  <path d=\"M 20 20 q 0 60 60 60 0 -60 -60 -60 Z\"/>\n");
        writer.write("</svg>\n");
        List<String> svgPaths = getSVGPaths(args[0]);
        List<PathOperation> pathOperations = getPathOperations(svgPaths);
        List<PathOperation> pdfPathOperations = getPDFPathOperations(pathOperations);
        for (PathOperation operation : pdfPathOperations) {
            System.out.print(operation.command + " ");
            for (String argument : operation.arguments) {
                System.out.print(argument + " ");
            }
            // System.out.println();
        }
        writer.flush();
        writer.close();
    }
}
