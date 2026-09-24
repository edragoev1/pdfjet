/*
 * GS1Parser.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.internal;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Reads GS1 data as people write it, each Application Identifier in
 * parentheses and its data after it, such as
 * "(01)09506000134352(17)261231(10)ABC123", for the barcodes that carry it,
 * GS1 DataMatrix and GS1-128, and for com.pdfjet.GS1.digitalLink.
 *
 * <p>Not part of the API of PDFjet: it is public only because the classes that
 * use it are in other packages, and it may change in any release. The other
 * ports keep it internal, as Go's internal/gs1 is.
 */
public final class GS1Parser {
    /**
     * An Application Identifier and its data, and whether a separator follows
     * it in a barcode: after a field of no set length that another follows.
     */
    public static final class Field {
        /** The Application Identifier, two to four digits. */
        public final String ai;
        /** The data of the field. */
        public final String data;
        /** True if a separator follows the field in a barcode. */
        public final boolean separator;

        Field(String ai, String data, boolean separator) {
            this.ai = ai;
            this.data = data;
            this.separator = separator;
        }
    }

    private static final String FORMAT =
            "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!";

    // The lengths, of the Application Identifier and its data together, of the
    // fields that the first two digits of their Application Identifier give a
    // set length, as the GS1 General Specifications list them. Their data is
    // digits, and they need no separator after them.
    private static final Map<String, Integer> PREDEFINED_LENGTHS = new HashMap<String, Integer>();
    static {
        String[] prefixes = {
            "00", "01", "02", "03", "04", "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "20", "31", "32", "33", "34", "35", "36", "41"};
        int[] lengths = {20, 16, 16, 16, 18, 8, 8, 8, 8, 8, 8, 8, 8, 8, 4, 10, 10, 10, 10, 10, 10, 16};
        for (int i = 0; i < prefixes.length; i++) {
            PREDEFINED_LENGTHS.put(prefixes[i], lengths[i]);
        }
    }

    // The characters of the data of a field, GS1's character set 82, less the
    // parentheses, which enclose the Application Identifiers.
    private static final String CHARACTERS =
            "!\"%&'*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

    private GS1Parser() {
    }

    /**
     * Returns the fields of the GS1 data written as people read it.
     *
     * @param str the GS1 data.
     * @return the fields.
     * @throws IllegalArgumentException if the data is not GS1: an Application
     *     Identifier that is not two to four digits, or with no data; a
     *     character GS1 does not allow, a parenthesis among them; data longer
     *     than 90 characters; data of a field of set length, such as the GTIN
     *     of (01) or the date of (17), that is not that many digits; or a wrong
     *     check digit of an SSCC (00), a GTIN (01) or (02), or a GLN (410) to
     *     (417).
     */
    public static List<Field> parse(String str) {
        if (!str.startsWith("(")) {
            throw new IllegalArgumentException(FORMAT);
        }
        List<Field> fields = new ArrayList<Field>();
        String rest = str;
        while (!rest.isEmpty()) {
            int end = rest.indexOf(')');
            if (end < 0) {
                throw new IllegalArgumentException(FORMAT);
            }
            String ai = rest.substring(1, end);
            rest = rest.substring(end + 1);
            int next = rest.indexOf('(');
            if (next < 0) {
                next = rest.length();
            }
            String data = rest.substring(0, next);
            rest = rest.substring(next);
            checkField(ai, data);
            fields.add(new Field(ai, data, !PREDEFINED_LENGTHS.containsKey(ai.substring(0, 2)) && !rest.isEmpty()));
        }
        return fields;
    }

    // Throws if the field is not one GS1 allows.
    private static void checkField(String ai, String data) {
        if (ai.length() < 2 || ai.length() > 4 || !digits(ai)) {
            throw new IllegalArgumentException("The Application Identifier (" + ai + ") is not two to four digits!");
        }
        if (data.isEmpty()) {
            throw new IllegalArgumentException("The Application Identifier (" + ai + ") has no data!");
        }
        for (int i = 0; i < data.length(); i++) {
            if (CHARACTERS.indexOf(data.charAt(i)) < 0) {
                throw new IllegalArgumentException("The data of (" + ai + ") has a character that GS1 does not allow!");
            }
        }
        if (data.length() > 90) {
            throw new IllegalArgumentException("The data of (" + ai + ") is longer than 90 characters!");
        }
        Integer length = PREDEFINED_LENGTHS.get(ai.substring(0, 2));
        if (length != null && (ai.length() + data.length() != length || !digits(data))) {
            throw new IllegalArgumentException("The data of (" + ai + ") must be " + (length - ai.length()) + " digits!");
        }
        boolean gln = ai.length() == 3 && ai.startsWith("41") && ai.charAt(2) <= '7';
        if ((ai.equals("00") || ai.equals("01") || ai.equals("02") || gln) && !checkDigitIsRight(data)) {
            throw new IllegalArgumentException("The check digit of (" + ai + ") is wrong!");
        }
    }

    private static boolean digits(String s) {
        for (int i = 0; i < s.length(); i++) {
            if (s.charAt(i) < '0' || s.charAt(i) > '9') {
                return false;
            }
        }
        return true;
    }

    // Returns true if the last digit of the number is its check digit: the
    // digits before it weighted 3 and 1 in turn from the right, and the check
    // digit what takes their sum to a multiple of ten.
    private static boolean checkDigitIsRight(String number) {
        int sum = 0;
        for (int i = number.length() - 2; i >= 0; i--) {
            int digit = number.charAt(i) - '0';
            if ((number.length() - 2 - i) % 2 == 0) {
                digit *= 3;
            }
            sum += digit;
        }
        return number.charAt(number.length() - 1) - '0' == (10 - sum % 10) % 10;
    }
}
