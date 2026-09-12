using System;

namespace PDFjet.NET {
/// <summary>Holds the object number and the name of an optional content group.</summary>
internal class OCG {
    // Fields to hold object number and name
    internal int objNumber;
    internal String name;

    // Constructor to initialize the OCG object with objNumber and name
    internal OCG(int objNumber, String name) {
        this.objNumber = objNumber;
        this.name = name;
    }
}
}
