/*
 * Relationship.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Says how a file attached to the document relates to what the document shows.
/// A document of PDF/A-3 names one for each file it carries, and a reader shows
/// it to the person who opens the document.
/// </summary>
public enum Relationship {
    /// <summary>The file is the source the document was made from.</summary>
    SOURCE,
    /// <summary>The file holds the data behind what the document shows, such as the numbers of a chart.</summary>
    DATA,
    /// <summary>The file is the same content in another form, such as the invoice of a bill in XML.</summary>
    ALTERNATIVE,
    /// <summary>The file adds to the document without being part of what it shows.</summary>
    SUPPLEMENT,
    /// <summary>The relationship is none of the others, or is not known.</summary>
    UNSPECIFIED
}

internal static class RelationshipExtensions {
    // The name written to the /AFRelationship entry.
    internal static String ToPDFName(this Relationship relationship) {
        switch (relationship) {
            case Relationship.SOURCE: return "Source";
            case Relationship.DATA: return "Data";
            case Relationship.ALTERNATIVE: return "Alternative";
            case Relationship.SUPPLEMENT: return "Supplement";
            case Relationship.UNSPECIFIED: return "Unspecified";
            default: throw new ArgumentException("Invalid relationship: " + relationship);
        }
    }
}
}   // End of namespace PDFjet.NET
