/**
 *  Paragraph.cs
 *
Copyright 2023 Innovatics Inc.
*/

using System;
using System.Collections.Generic;


namespace PDFjet.NET {
/**
 *  Used to create paragraph objects.
 *  See the TextColumn class for more information.
 *
 */
public class Paragraph {

    internal List<TextLine> list = null;
    internal int alignment = Align.LEFT;


    /**
     *  Constructor for creating paragraph objects.
     *
     */
    public Paragraph() {
        this.list = new List<TextLine>();
    }


    public Paragraph(TextLine text) {
        this.list = new List<TextLine>();
        this.list.Add(text);
    }


    /**
     *  Adds a text line to this paragraph.
     *
     *  @param text the text line to add to this paragraph.
     *  @return this paragraph.
     */
    public Paragraph Add(TextLine text) {
        list.Add(text);
        return this;
    }


    /**
     *  Sets the alignment of the text in this paragraph.
     *
     *  @param alignment the alignment code.
     *  @return this paragraph.
     *
     *  <pre>Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.</pre>
     */
    public Paragraph SetAlignment(int alignment) {
        this.alignment = alignment;
        return this;
    }

}   // End of Paragraph.cs
}   // End of namespace PDFjet.NET
