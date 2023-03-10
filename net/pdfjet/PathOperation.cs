using System;
using System.Collections.Generic;

namespace PDFjet.NET {
class PathOperation {
    Char command;
    List<String> arguments = null;

    PathOperation(Char command) {
        this.command = command;
        this.arguments = new List<String>();
    }

    List<PathOperation> GetPathOperations() {
        List<PathOperation> operations = new List<PathOperation>();
        int n = GetNumberOfArguments();
        PathOperation operation = new PathOperation(command);
        foreach (String argument in arguments) {
            operation.arguments.Add(argument);
            if (operation.arguments.Count % n == 0) {
                operations.Add(operation);
                operation = new PathOperation(command);
            }
        }
        if (operation.arguments.Count == n) {
            operations.Add(operation);
        }
        return operations;
    }

    int GetNumberOfArguments() {
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
}