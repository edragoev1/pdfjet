/*
 * Chunk.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

class Chunk {
    protected long length;
    protected byte[] type;
    protected byte[] data;
    protected long crc;

    public byte[] getData() {
        return this.data;
    }

    public void setData(byte[] data) {
        this.data = data;
    }
}   // End of Chunk.java
