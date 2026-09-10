using System;

namespace PDFjet.NET {
/// <summary>Holds the alpha values of stroking and non-stroking operations.</summary>
public class GraphicsState {
    // Default values
    private float CA = 1f;
    private float ca = 1f;

    /// <summary>Sets the alpha of stroking operations, from 0.0 to 1.0. Other values are ignored.</summary>
    public GraphicsState SetAlphaStroking(float CA) {
        if (CA >= 0f && CA <= 1f) {
            this.CA = CA;
        }
        return this;
    }

    /// <summary>Returns the alpha of stroking operations.</summary>
    public float GetAlphaStroking() {
        return this.CA;
    }

    /// <summary>Sets the alpha of non-stroking operations, such as fills, from 0.0 to 1.0. Other values are ignored.</summary>
    public GraphicsState SetAlphaNonStroking(float ca) {
        if (ca >= 0f && ca <= 1f) {
            this.ca = ca;
        }
        return this;
    }

    /// <summary>Returns the alpha of non-stroking operations.</summary>
    public float GetAlphaNonStroking() {
        return this.ca;
    }
}
}
