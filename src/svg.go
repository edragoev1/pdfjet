package pdfjet

/**
 * svg.go
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

import (
	"log"
	"os"
	"strings"
)

func GetSVGPaths(filename string) []string {
	var paths = make([]string, 0)
	contents, err := os.ReadFile(filename)
	if err != nil {
		log.Fatal(err)
	}
	var inPath = false
	var buffer = make([]rune, 0)
	for _, ch := range contents {
		if !inPath && strings.HasSuffix(string(buffer), "<path d=") {
			inPath = true
			buffer = nil
		} else if inPath && ch == '"' {
			inPath = false
			paths = append(paths, string(buffer))
			buffer = nil
		} else {
			paths = append(paths, string(buffer))
		}
	}
	return paths
}

func isCommand(ch byte) bool {
	// Please note:
	// Capital letter commands use absolute coordinates
	// Small letter commands use relative coordinates
	if ch == 'M' || ch == 'm' { // moveto
		return true
	} else if ch == 'L' || ch == 'l' { // lineto
		return true
	} else if ch == 'H' || ch == 'h' { // horizontal lineto
		return true
	} else if ch == 'V' || ch == 'v' { // vertical lineto
		return true
	} else if ch == 'C' || ch == 'c' { // cubic curveto
		return true
	} else if ch == 'S' || ch == 's' { // smooth cubic curveto
		return true
	} else if ch == 'Q' || ch == 'q' { // quadratic curveto
		return true
	} else if ch == 'T' || ch == 't' { // smooth quadratic curveto
		return true
	} else if ch == 'A' || ch == 'a' { // elliptical arc
		return true
	} else if ch == 'Z' || ch == 'z' { // close path
		return true
	}
	return false
}

func (pathOp *PathOp) getSVGPathOps(paths []string) []PathOp {
	operations := []PathOp{}
	var op *PathOp
	for _, path := range paths {
		// Path example:
		// "M22.65 34h3v-8.3H34v-3h-8.35V14h-3v8.7H14v3h8.65ZM24 44z"
		// System.out.println(path)
		// System.out.println()
		buf := []byte{}
		var token = false
		for i := 0; i < len(path); i++ {
			var ch = path[i]
			if isCommand(ch) { // open path
				if token {
					op.args = append(op.args, string(ch))
					buf = buf[:0]
				}
				token = false
				op = NewPathOp(ch)
				operations = append(operations, *op)
			} else if ch == ' ' || ch == ',' {
				if token {
					op.args = append(op.args, string(buf))
					buf = buf[:0]
				}
				token = false
			} else if ch == '-' {
				if token {
					op.args = append(op.args, string(buf))
					buf = buf[:0]
				}
				token = true
				buf = append(buf, ch)
			} else if ch == '.' {
				if strings.Contains(string(buf), ".") {
					op.args = append(op.args, string(buf))
					buf = buf[:0]
				}
				token = true
				buf = append(buf, ch)
			} else {
				token = true
				buf = append(buf, ch)
			}
		}
	}
	return operations
}

/*
public static List<PathOp> getPDFPathOps(List<PathOp> list) {
	List<PathOp> operations = new ArrayList<PathOp>();
	PathOp lastOp = null;
	PathOp pathOp = null;
	for (PathOp op : list) {
		System.out.print(op.cmd + " ");
		if (op.cmd == 'M' || op.cmd == 'm') {
			for (int i = 0; i <= op.args.size() - 2; i += 2) {
				float x = Float.valueOf(op.args.get(i));
				float y = Float.valueOf(op.args.get(i + 1));
				if (lastOp != null) {
					System.out.println("Hello" + lastOp.cmd);
				}
				if (op.cmd == 'm' && lastOp != null) {
					x += lastOp.x;
					y += lastOp.y;
				}
				if (i == 0) {
					pathOp = new PathOp('M', x, y);
				} else {
					pathOp = new PathOp('L', x, y);
				}
				operations.add(pathOp);
				lastOp = pathOp;
			}
		} else if (op.cmd == 'L' || op.cmd == 'l') {
			for (int i = 0; i <= op.args.size() - 2; i += 2) {
				float x = Float.valueOf(op.args.get(i));
				float y = Float.valueOf(op.args.get(i + 1));
				if (op.cmd == 'l' && lastOp != null) {
					x += lastOp.x;
					y += lastOp.y;
				}
				pathOp = new PathOp('L', x, y);
				operations.add(pathOp);
				lastOp = pathOp;
			}
		} else if (op.cmd == 'H' || op.cmd == 'h') {
			for (int i = 0; i < op.args.size(); i++) {
				float x = Float.valueOf(op.args.get(i));
				if (op.cmd == 'h' && lastOp != null) {
					x += lastOp.x;
				}
				pathOp = new PathOp('L', x, lastOp.y);
				operations.add(pathOp);
				lastOp = pathOp;
			}
		} else if (op.cmd == 'V' || op.cmd == 'v') {
			for (int i = 0; i < op.args.size(); i++) {
				float y = Float.valueOf(op.args.get(i));
				if (op.cmd == 'v' && lastOp != null) {
					y += lastOp.y;
				}
				pathOp = new PathOp('L', lastOp.x, y);
				operations.add(pathOp);
				lastOp = pathOp;
			}
		} else if (op.cmd == 'Q' || op.cmd == 'q') {
			for (int i = 0; i <= op.args.size() - 4; i += 4) {
				pathOp = new PathOp('C');
				float x1 = Float.valueOf(op.args.get(i));
				float y1 = Float.valueOf(op.args.get(i + 1));
				float x = Float.valueOf(op.args.get(i + 2));
				float y = Float.valueOf(op.args.get(i + 3));
				if (op.cmd == 'q') {
					x1 += lastOp.x;
					y1 += lastOp.y;
					x += lastOp.x;
					y += lastOp.y;
				}
				// Save the original control point
				pathOp.x1q = x1;
				pathOp.y1q = y1;
				// Calculate the coordinates of the cubic control points
				float x1c = lastOp.x + (2f/3f)*(x1 - lastOp.x);
				float y1c = lastOp.y + (2f/3f)*(y1 - lastOp.y);
				float x2c = x + (2f/3f)*(x1 - x);
				float y2c = y + (2f/3f)*(y1 - y);
				pathOp.addCubicPoints(x1c, y1c, x2c, y2c, x, y);
				operations.add(pathOp);
				lastOp = pathOp;
			}
		} else if (op.cmd == 'T' || op.cmd == 't') {
			for (int i = 0; i <= op.args.size() - 2; i += 2) {
				pathOp = new PathOp('C');
				float x1 = lastOp.x;
				float y1 = lastOp.y;
				if (lastOp.cmd == 'C') {
					// Find the reflection control point
					x1 = 2*lastOp.x - lastOp.x1q;
					y1 = 2*lastOp.y - lastOp.y1q;
				}
				float x = Float.valueOf(op.args.get(i));
				float y = Float.valueOf(op.args.get(i + 1));
				if (op.cmd == 't') {
					x += lastOp.x;
					y += lastOp.y;
				}
				// Calculate the coordinates of the cubic control points
				float x1c = lastOp.x + (2f/3f)*(x1 - lastOp.x);
				float y1c = lastOp.y + (2f/3f)*(y1 - lastOp.y);
				float x2c = x + (2f/3f)*(x1 - x);
				float y2c = y + (2f/3f)*(y1 - y);
				pathOp.addCubicPoints(x1c, y1c, x2c, y2c, x, y);
				operations.add(pathOp);
				lastOp = pathOp;
			}
		} else if (op.cmd == 'Z' || op.cmd == 'z') {
			operations.add(new PathOp('Z'));
		}
	}
	System.out.println();
	System.out.println();
	return operations;
}
*/
