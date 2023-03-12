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

    public static List<PathOp> getSVGPathOps(List<String> paths) {
        List<PathOp> operations = new ArrayList<PathOp>();
        PathOp op = null;
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
                    op = new PathOp(ch);
                    operations.add(op);
                } else if (ch == ' ' || ch == ',') {
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
                } else if (ch == '.') {
                    if (buf.toString().contains(".")) {
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

    public static List<PathOp> getPDFPathOps(List<PathOp> list) {
        List<PathOp> operations = new ArrayList<PathOp>();
        float x0 = 0f;  // Subpath initial point x
        float y0 = 0f;  // Subpath initial point y
        float x = 0f;
        float y = 0f;
        PathOp pathOp = null;
        PathOp prevOp = null;
        for (PathOp op : list) {
            if (op.cmd == 'M') {
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x = Float.valueOf(op.args.get(i));
                    y = Float.valueOf(op.args.get(i + 1));
                    if (i == 0) {
                        x0 = x;
                        y0 = y;
                        pathOp = new PathOp('M', x, y);
                    } else {
                        pathOp = new PathOp('L', x, y);
                    }
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'm') {
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x += Float.valueOf(op.args.get(i));
                    y += Float.valueOf(op.args.get(i + 1));
                    if (i == 0) {
                        x0 = x;
                        y0 = y;
                        pathOp = new PathOp('M', x, y);
                    } else {
                        pathOp = new PathOp('L', x, y);
                    }
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'L') {
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x = Float.valueOf(op.args.get(i));
                    y = Float.valueOf(op.args.get(i + 1));
                    pathOp = new PathOp('L', x, y);
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'l') {
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x += Float.valueOf(op.args.get(i));
                    y += Float.valueOf(op.args.get(i + 1));
                    pathOp = new PathOp('L', x, y);
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'H') {
                for (int i = 0; i < op.args.size(); i++) {
                    x = Float.valueOf(op.args.get(i));
                    pathOp = new PathOp('L', x, y);
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'h') {
                for (int i = 0; i < op.args.size(); i++) {
                    x += Float.valueOf(op.args.get(i));
                    pathOp = new PathOp('L', x, y);
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'V') {
                for (int i = 0; i < op.args.size(); i++) {
                    y = Float.valueOf(op.args.get(i));
                    pathOp = new PathOp('L', x, y);
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'v') {
                for (int i = 0; i < op.args.size(); i++) {
                    y += Float.valueOf(op.args.get(i));
                    pathOp = new PathOp('L', x, y);
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'Q') {
                pathOp = new PathOp('C');
                for (int i = 0; i <= op.args.size() - 4; i += 4) {
                    float x1 = Float.valueOf(op.args.get(i));
                    float y1 = Float.valueOf(op.args.get(i + 1));
                    x = Float.valueOf(op.args.get(i + 2));
                    y = Float.valueOf(op.args.get(i + 3));
                    float cp1x = prevOp.x + (2f/3f)*(x1 - prevOp.x);
                    float cp1y = prevOp.y + (2f/3f)*(y1 - prevOp.y);
                    float cp2x = x + (2f/3f)*(x1 - x);
                    float cp2y = y + (2f/3f)*(y1 - y);
                    pathOp.appendArgs(cp1x, cp1y, cp2x, cp2y, x, y);
                }
                operations.add(pathOp);
                prevOp = pathOp;
            } else if (op.cmd == 'q') {
                pathOp = new PathOp('Q');
                for (int i = 0; i <= op.args.size() - 4; i += 4) {
                    float x1 = x + Float.valueOf(op.args.get(i));
                    float y1 = y + Float.valueOf(op.args.get(i + 1));
                    x += Float.valueOf(op.args.get(i + 2));
                    y += Float.valueOf(op.args.get(i + 3));
                    pathOp.appendArgs(x1, y1, x, y);
                }
                operations.add(pathOp);
                prevOp = pathOp;
            } else if (op.cmd == 'T') {
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x = Float.valueOf(op.args.get(i));
                    y = Float.valueOf(op.args.get(i + 1));
                    if (prevOp.cmd == 'Q' ||
                            prevOp.cmd == 'q' ||
                            prevOp.cmd == 'T' ||
                            prevOp.cmd == 't') {
                        float xr = 2*prevOp.x - prevOp.x1;
                        float yr = 2*prevOp.y - prevOp.y1;
                        pathOp = new PathOp('Q', xr, yr, x, y);
                    } else {
                        pathOp = new PathOp('Q', x, y, x, y);
                    }
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 't') {
                for (int i = 0; i <= op.args.size() - 2; i += 2) {
                    x += Float.valueOf(op.args.get(i));
                    y += Float.valueOf(op.args.get(i + 1));
                    if (prevOp.cmd == 'Q' ||
                            prevOp.cmd == 'q' ||
                            prevOp.cmd == 'T' ||
                            prevOp.cmd == 't') {
                        float xr = 2*prevOp.x - prevOp.x1;
                        float yr = 2*prevOp.y - prevOp.y1;
                        pathOp = new PathOp('Q', xr, yr, x, y);
                    } else {
                        pathOp = new PathOp('Q', x, y, x, y);
                    }
                    operations.add(pathOp);
                    prevOp = pathOp;
                }
            } else if (op.cmd == 'Z' || op.cmd == 'z') {
                x = x0;
                y = y0;
                pathOp = new PathOp('Z');
                operations.add(pathOp);
                prevOp = pathOp;
            }
        }
        return operations;
    }

    public static void main(String[] args) throws IOException {
        FileWriter writer = new FileWriter("test.svg");
        writer.write("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"48\" width=\"48\">\n");
        writer.write("  <path d=\"");
        List<String> paths = getSVGPaths(args[0]);
        List<PathOp> svgPathOps = getSVGPathOps(paths);
        List<PathOp> pdfPathOps = getPDFPathOps(svgPathOps);
        for (PathOp op : pdfPathOps) {
            System.out.print(op.cmd + " ");
            writer.write(op.cmd + " ");
            for (String argument : op.args) {
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

