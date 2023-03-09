/**
 *  PathOperation.java
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

import java.util.*;


class PathOperation {
    char command;
    List<String> arguments = null;

    PathOperation(char command) {
        this.command = command;
        this.arguments = new ArrayList<>();
    }

    List<PathOperation> getPathOperations() {
        List<PathOperation> operations = new ArrayList<>();
        int n = getNumberOfArguments();
        PathOperation operation = new PathOperation(command);
        for (String argument : arguments) {
            operation.arguments.add(argument);
            if (operation.arguments.size() % n == 0) {
                operations.add(operation);
                operation = new PathOperation(command);
            }
        }
        if (operation.arguments.size() == n) {
            operations.add(operation);
        }
        return operations;
    }

    int getNumberOfArguments() {
        if (command == 'M' || command == 'm') {         // moveto
            return 2;
        }
        else if (command == 'L' || command == 'l') {    // lineto
            return 2;
        }
        else if (command == 'H' || command == 'h') {    // horizontal lineto
            return 1;
        }
        else if (command == 'V' || command == 'v') {    // vertical lineto
            return 1;
        }
        else if (command == 'C' || command == 'c') {    // curveto
            return 6;
        }
        else if (command == 'S' || command == 's') {    // smooth curveto
            return 4;
        }
        else if (command == 'Q' || command == 'q') {    // quadratic curve
            return 4;
        }
        else if (command == 'T' || command == 't') {    // smooth quadratic curveto
            return 2;
        }
        else if (command == 'A' || command == 'a') {    // elliptical arc
            return 7;
        }
        else if (command == 'Z' || command == 'z') {    // closepath
            return 0;
        }
        return 0;
    }

}
