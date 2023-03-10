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
        }
        return false;
    }

    public static List<PathOperation> getPathOperations(List<String> svgPaths) {
        List<PathOperation> operations = new ArrayList<PathOperation>();
        PathOperation operation = null;
        for (String svgPath : svgPaths) {
            // Path example:
            // "M22.65 34h3v-8.3H34v-3h-8.35V14h-3v8.7H14v3h8.65ZM24 44z"
            System.out.println(svgPath);
            StringBuilder argument = new StringBuilder();
            for (int i = 0; i < svgPath.length(); i++) {
                char ch = svgPath.charAt(i);
                if (SVG.isCommand(ch)) {                // open path
                    if (operation != null) {
                        operation.arguments.add(argument.toString());
                        operations.add(operation);                        
                    }
                    operation = new PathOperation(ch);
                    argument.setLength(0);
                } else if (ch == ' ') {
                    operation.arguments.add(argument.toString());
                    argument.setLength(0);
                } else if (ch == '-') {
                    if (!argument.toString().equals("")) {
                        operation.arguments.add(argument.toString());
                    }
                    argument.setLength(0);
                    argument.append(ch);
                } else if (ch == 'Z' || ch == 'z') {    // close path
                    operation.arguments.add(argument.toString());
                    argument.setLength(0);
                    operations.add(operation);
                    operations.add(new PathOperation(ch));
                    operation = null;
                } else {
                    argument.append(ch);
                }
            }
        }
        return operations;
    }

    public static void main(String[] args) throws IOException {
        List<String> svgPaths = getSVGPaths(args[0]);
        List<PathOperation> pathOperations = getPathOperations(svgPaths);
        for (PathOperation operation : pathOperations) {
            System.out.print(operation.command + " ");
            for (String argument : operation.arguments) {
                System.out.print(argument + " ");
            }
            System.out.println();
        }
    }
}
