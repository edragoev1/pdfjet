/*
 * GraphicsState.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

using System;
using System.Text;

namespace PDFjet.NET {
/// <summary>Holds the alpha values of stroking and non-stroking operations.</summary>
public class GraphicsState {
    // Default values
    private float CA = 1f;
    private float ca = 1f;

    /// <summary>Creates a graphics state with both alpha values set to 1.0.</summary>
    public GraphicsState() {
    }

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

    // Null writes no /BM entry, as before there was a blend mode (suggested
    // by Stefan Ostermann, of his fork of PDFjet, 19 October 2026).
    private BlendMode? blendMode = null;

    /// <summary>
    /// Sets the blend mode: how what is drawn is mixed with what is already on
    /// the page. BlendMode.MULTIPLY draws like a highlighter pen. The blend
    /// mode stays until a graphics state sets another one, so set
    /// BlendMode.NORMAL to go back to painting over. A PDF/A-1 document takes
    /// only BlendMode.NORMAL. Null sets none.
    /// </summary>
    public GraphicsState SetBlendMode(BlendMode? blendMode) {
        this.blendMode = blendMode;
        return this;
    }

    /// <summary>Returns the blend mode, or null if none was set.</summary>
    public BlendMode? GetBlendMode() {
        return this.blendMode;
    }

    // The entries of the state's /ExtGState dictionary, as the page and an
    // imported page write them: the alphas with a dot whatever the locale, at
    // most two decimals and no exponent, then the blend mode if it has one.
    internal String Entries() {
        String state = "/CA " +
                Encoding.ASCII.GetString(FastFloat.ToByteArray(CA)) +
                " /ca " +
                Encoding.ASCII.GetString(FastFloat.ToByteArray(ca));
        if (blendMode != null) {
            state += " /BM /" + blendMode.Value.ToPDFName();
        }
        return state;
    }
}
}
