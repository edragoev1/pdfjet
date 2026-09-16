/*
 * OCG.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
