using System;
using System.Text;

namespace PDFjet.NET {
/// <summary>The font, font size, location and text for Stamp.DrawText.</summary>
public class TextParameters {
    internal Font font;
    internal float fontSize;
    internal float x;
    internal float y;
    internal String text;

    // Constructor to initialize with default values (optional)
    /// <summary>Creates text parameters with a font size of 12, located at (0, 0).</summary>
    public TextParameters() {
        this.fontSize = 12f;    // Default font size
        this.x = 0f;            // Default X
        this.y = 0f;            // Default Y
    }

    // Method to set the font
    /// <summary>Sets the font.</summary>
    public TextParameters SetFont(Font font) {
        this.font = font;
        return this;
    }

    // Method to set the font size
    /// <summary>Sets the font size.</summary>
    public TextParameters SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    // Method to set the location (X, Y)
    /// <summary>Sets the location of the text.</summary>
    public TextParameters SetTextLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the text.</summary>
    public TextParameters SetText(String text) {
        this.text = text;
        return this;
    }
}   // End of TextParameters.cs
}   // End of namespace PDFjet.NET
