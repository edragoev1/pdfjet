/*
 * GS1.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.internal.GS1Parser;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * The GS1 Digital Link of GS1 data written as people read it, each Application
 * Identifier in parentheses and its data after it, such as
 * "(01)09506000134352(17)261231(10)ABC123".
 */
public final class GS1 {
    private GS1() {
    }

    // The primary keys of GS1 Digital Link and their qualifiers, in the order
    // the path has them: each array is a place in the path, and the
    // qualifiers in it are alternatives.
    private static final Map<String, String[][]> DIGITAL_LINK_KEYS = new HashMap<String, String[][]>();
    static {
        String[] alone = {"00", "253", "255", "401", "402", "8003", "8004", "8013"};
        for (String key : alone) {
            DIGITAL_LINK_KEYS.put(key, new String[][] {});
        }
        DIGITAL_LINK_KEYS.put("01", new String[][] {{"22"}, {"10"}, {"21"}});
        DIGITAL_LINK_KEYS.put("8006", new String[][] {{"22"}, {"10"}, {"21"}});
        DIGITAL_LINK_KEYS.put("414", new String[][] {{"254", "7040"}});
        DIGITAL_LINK_KEYS.put("417", new String[][] {{"7040"}});
        DIGITAL_LINK_KEYS.put("8010", new String[][] {{"8011"}});
        DIGITAL_LINK_KEYS.put("8017", new String[][] {{"8019"}});
        DIGITAL_LINK_KEYS.put("8018", new String[][] {{"8019"}});
    }

    /**
     * Returns the GS1 Digital Link of the GS1 data, the web address an
     * ordinary QR code carries, at the domain: a brand's own, or GS1's
     * resolver, https://id.gs1.org. digitalLink("https://id.gs1.org",
     * "(01)09506000134352(10)ABC123(17)261231") is
     * "https://id.gs1.org/01/09506000134352/10/ABC123?17=261231". The primary
     * key, such as a GTIN (01), an SSCC (00) or a GLN (414), is first in the
     * path, then its qualifiers in the order of the standard, such as (22),
     * the batch (10) and the serial (21) of a GTIN, and the other fields are in
     * the query, in their order. A value is percent-encoded where a web
     * address needs it.
     *
     * @param domain the domain, starting with https:// or http://.
     * @param data the GS1 data.
     * @return the GS1 Digital Link.
     * @throws IllegalArgumentException if the domain does not start with
     *     https:// or http://; if the data is not GS1, as
     *     {@link com.pdfjet.datamatrix.DataMatrix#fromGS1(String)} has it; or
     *     if it has no primary key or two, or both (254) and (7040) of a GLN.
     */
    public static String digitalLink(String domain, String data) {
        String rest = domain;
        if (rest.startsWith("https://")) {
            rest = rest.substring(8);
        } else if (rest.startsWith("http://")) {
            rest = rest.substring(7);
        }
        if (rest.equals(domain) || rest.isEmpty() || rest.equals("/")) {
            throw new IllegalArgumentException(
                    "The domain of a GS1 Digital Link starts with https:// or http://, such as https://id.gs1.org!");
        }
        List<GS1Parser.Field> fields = GS1Parser.parse(data);
        int key = -1;
        for (int i = 0; i < fields.size(); i++) {
            if (DIGITAL_LINK_KEYS.containsKey(fields.get(i).ai)) {
                if (key >= 0) {
                    throw new IllegalArgumentException("A GS1 Digital Link has one primary key, not (" +
                            fields.get(key).ai + ") and (" + fields.get(i).ai + ")!");
                }
                key = i;
            }
        }
        if (key < 0) {
            throw new IllegalArgumentException(
                    "A GS1 Digital Link needs a primary key, such as a GTIN (01), an SSCC (00) or a GLN (414)!");
        }

        StringBuilder sb = new StringBuilder(domain.endsWith("/") ? domain.substring(0, domain.length() - 1) : domain);
        sb.append('/').append(fields.get(key).ai).append('/').append(percentEncode(fields.get(key).data));
        boolean[] used = new boolean[fields.size()];
        used[key] = true;
        for (String[] place : DIGITAL_LINK_KEYS.get(fields.get(key).ai)) {
            int found = -1;
            for (int i = 0; i < fields.size(); i++) {
                for (String qualifier : place) {
                    if (!fields.get(i).ai.equals(qualifier)) {
                        continue;
                    }
                    if (found >= 0) {
                        throw new IllegalArgumentException("The qualifiers (" + fields.get(found).ai + ") and (" +
                                fields.get(i).ai + ") of (" + fields.get(key).ai + ") cannot be together!");
                    }
                    found = i;
                }
            }
            if (found >= 0) {
                sb.append('/').append(fields.get(found).ai).append('/').append(percentEncode(fields.get(found).data));
                used[found] = true;
            }
        }
        char separator = '?';
        for (int i = 0; i < fields.size(); i++) {
            if (!used[i]) {
                sb.append(separator).append(fields.get(i).ai).append('=').append(percentEncode(fields.get(i).data));
                separator = '&';
            }
        }
        return sb.toString();
    }

    // Returns the value with each character but the unreserved ones of a web
    // address, the letters, the digits and "-._~", as % and its two
    // hexadecimal digits.
    private static String percentEncode(String value) {
        final String hex = "0123456789ABCDEF";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || "-._~".indexOf(c) >= 0) {
                sb.append(c);
            } else {
                sb.append('%').append(hex.charAt(c >> 4)).append(hex.charAt(c & 15));
            }
        }
        return sb.toString();
    }
}
