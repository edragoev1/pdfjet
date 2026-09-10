package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;

/**
 * A named block of content, written as a PDF form XObject.
 */
public class ContentBlock {
    // XObject internal representation
    private final String xObjectName;
    private final byte[] xObjectData; // This will store the raw stream data (e.g., image, graphic commands, etc.)

    // A map to keep track of the resources (for embedding in the page)
    private static final Map<String, ContentBlock> xObjectResources = new HashMap<>();

    /**
     * Creates a content block.
     *
     * @param name the name of the XObject.
     * @param data the content stream data.
     */
    public ContentBlock(String name, byte[] data) {
        this.xObjectName = name;
        this.xObjectData = data;
    }

    /**
     * Adds this content block to the shared XObject resources.
     */
    public void addToResources() {
        xObjectResources.put(xObjectName, this);
    }

    /**
     * Returns this content block as a PDF form XObject.
     *
     * @return the XObject dictionary and stream.
     * @throws IOException if the content cannot be written.
     */
    public String serialize() throws IOException {
        StringBuilder sb = new StringBuilder();
        sb.append("<<\n");
        sb.append("/Type /XObject\n");
        sb.append("/Subtype /Form\n");
        sb.append("/Name /");
        sb.append(xObjectName);
        sb.append("\n");
        sb.append("/Length ");
        sb.append(xObjectData.length);
        sb.append("\n");
        sb.append(">>\n");
        sb.append("stream\n");
        // Write the actual stream (the data of the image or graphic)
        sb.append(new String(xObjectData, StandardCharsets.ISO_8859_1));
        sb.append("\nendstream\n");
        return sb.toString();
    }

    /**
     * Returns the /Resources dictionary that lists the content blocks added with addToResources.
     *
     * @return the resources dictionary.
     */
    public static String generateResourcesSection() {
        StringBuilder resources = new StringBuilder();
        resources.append("/Resources <<\n");
        for (Map.Entry<String, ContentBlock> entry : xObjectResources.entrySet()) {
            resources.append("/");
            resources.append(entry.getKey());
            resources.append(" ");
            resources.append(entry.getValue());
            resources.append("\n");
        }
        resources.append(">>\n");
        return resources.toString();
    }
}
