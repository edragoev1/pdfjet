/*
 * GS1.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Reads GS1 data as people write it, each Application Identifier in
/// parentheses and its data after it, for the barcodes that carry it: GS1
/// DataMatrix and GS1-128, and makes its GS1 Digital Link.
/// </summary>
public static class GS1 {
    // An Application Identifier and its data, and whether a separator follows
    // it in a barcode: after a field of no set length that another follows.
    internal sealed class Field {
        internal readonly String ai;
        internal readonly String data;
        internal readonly bool separator;

        internal Field(String ai, String data, bool separator) {
            this.ai = ai;
            this.data = data;
            this.separator = separator;
        }
    }

    private const String FORMAT =
            "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!";

    // The lengths, of the Application Identifier and its data together, of the
    // fields that the first two digits of their Application Identifier give a
    // set length, as the GS1 General Specifications list them. Their data is
    // digits, and they need no separator after them.
    private static readonly Dictionary<String, int> PREDEFINED_LENGTHS = new Dictionary<String, int> {
        {"00", 20}, {"01", 16}, {"02", 16}, {"03", 16}, {"04", 18},
        {"11", 8}, {"12", 8}, {"13", 8}, {"14", 8}, {"15", 8}, {"16", 8}, {"17", 8}, {"18", 8}, {"19", 8},
        {"20", 4}, {"31", 10}, {"32", 10}, {"33", 10}, {"34", 10}, {"35", 10}, {"36", 10}, {"41", 16},
    };

    // The characters of the data of a field, GS1's character set 82, less the
    // parentheses, which enclose the Application Identifiers.
    private const String CHARACTERS =
            "!\"%&'*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

    // Returns the fields of the GS1 data written as people read it. Throws an
    // ArgumentException if the data is not GS1; see DataMatrix.FromGS1.
    internal static List<Field> Parse(String str) {
        if (!str.StartsWith("(", StringComparison.Ordinal)) {
            throw new ArgumentException(FORMAT);
        }
        List<Field> fields = new List<Field>();
        String rest = str;
        while (rest.Length > 0) {
            int end = rest.IndexOf(')');
            if (end < 0) {
                throw new ArgumentException(FORMAT);
            }
            String ai = rest.Substring(1, end - 1);
            rest = rest.Substring(end + 1);
            int next = rest.IndexOf('(');
            if (next < 0) {
                next = rest.Length;
            }
            String data = rest.Substring(0, next);
            rest = rest.Substring(next);
            CheckField(ai, data);
            fields.Add(new Field(ai, data, !PREDEFINED_LENGTHS.ContainsKey(ai.Substring(0, 2)) && rest.Length > 0));
        }
        return fields;
    }

    // Throws if the field is not one GS1 allows.
    private static void CheckField(String ai, String data) {
        if (ai.Length < 2 || ai.Length > 4 || !Digits(ai)) {
            throw new ArgumentException("The Application Identifier (" + ai + ") is not two to four digits!");
        }
        if (data.Length == 0) {
            throw new ArgumentException("The Application Identifier (" + ai + ") has no data!");
        }
        foreach (char c in data) {
            if (CHARACTERS.IndexOf(c) < 0) {
                throw new ArgumentException("The data of (" + ai + ") has a character that GS1 does not allow!");
            }
        }
        if (data.Length > 90) {
            throw new ArgumentException("The data of (" + ai + ") is longer than 90 characters!");
        }
        if (PREDEFINED_LENGTHS.TryGetValue(ai.Substring(0, 2), out int length) &&
                (ai.Length + data.Length != length || !Digits(data))) {
            throw new ArgumentException("The data of (" + ai + ") must be " + (length - ai.Length) + " digits!");
        }
        bool gln = ai.Length == 3 && ai.StartsWith("41", StringComparison.Ordinal) && ai[2] <= '7';
        if ((ai == "00" || ai == "01" || ai == "02" || gln) && !CheckDigitIsRight(data)) {
            throw new ArgumentException("The check digit of (" + ai + ") is wrong!");
        }
    }

    private static bool Digits(String s) {
        foreach (char c in s) {
            if (c < '0' || c > '9') {
                return false;
            }
        }
        return true;
    }

    // Returns true if the last digit of the number is its check digit: the
    // digits before it weighted 3 and 1 in turn from the right, and the check
    // digit what takes their sum to a multiple of ten.
    private static bool CheckDigitIsRight(String number) {
        int sum = 0;
        for (int i = number.Length - 2; i >= 0; i--) {
            int digit = number[i] - '0';
            if ((number.Length - 2 - i) % 2 == 0) {
                digit *= 3;
            }
            sum += digit;
        }
        return number[number.Length - 1] - '0' == (10 - sum % 10) % 10;
    }

    // The primary keys of GS1 Digital Link and their qualifiers, in the order
    // the path has them: each array is a place in the path, and the
    // qualifiers in it are alternatives.
    private static readonly Dictionary<String, String[][]> DIGITAL_LINK_KEYS = new Dictionary<String, String[][]> {
        {"00", new String[][] {}}, {"253", new String[][] {}}, {"255", new String[][] {}},
        {"401", new String[][] {}}, {"402", new String[][] {}}, {"8003", new String[][] {}},
        {"8004", new String[][] {}}, {"8013", new String[][] {}},
        {"01", new String[][] {new String[] {"22"}, new String[] {"10"}, new String[] {"21"}}},
        {"8006", new String[][] {new String[] {"22"}, new String[] {"10"}, new String[] {"21"}}},
        {"414", new String[][] {new String[] {"254", "7040"}}},
        {"417", new String[][] {new String[] {"7040"}}},
        {"8010", new String[][] {new String[] {"8011"}}},
        {"8017", new String[][] {new String[] {"8019"}}},
        {"8018", new String[][] {new String[] {"8019"}}},
    };

    /// <summary>
    /// Returns the GS1 Digital Link of the GS1 data, the web address an
    /// ordinary QR code carries, at the domain: a brand's own, or GS1's
    /// resolver, https://id.gs1.org. DigitalLink("https://id.gs1.org",
    /// "(01)09506000134352(10)ABC123(17)261231") is
    /// "https://id.gs1.org/01/09506000134352/10/ABC123?17=261231". The primary
    /// key, such as a GTIN (01), an SSCC (00) or a GLN (414), is first in the
    /// path, then its qualifiers in the order of the standard, such as (22),
    /// the batch (10) and the serial (21) of a GTIN, and the other fields are in
    /// the query, in their order. A value is percent-encoded where a web
    /// address needs it. Throws an ArgumentException if the domain does not
    /// start with https:// or http://; if the data is not GS1, as for
    /// DataMatrix.FromGS1; or if it has no primary key or two, or both (254)
    /// and (7040) of a GLN.
    /// </summary>
    public static String DigitalLink(String domain, String data) {
        String rest = domain;
        if (rest.StartsWith("https://", StringComparison.Ordinal)) {
            rest = rest.Substring(8);
        } else if (rest.StartsWith("http://", StringComparison.Ordinal)) {
            rest = rest.Substring(7);
        }
        if (rest == domain || rest.Length == 0 || rest == "/") {
            throw new ArgumentException(
                    "The domain of a GS1 Digital Link starts with https:// or http://, such as https://id.gs1.org!");
        }
        List<Field> fields = Parse(data);
        int key = -1;
        for (int i = 0; i < fields.Count; i++) {
            if (DIGITAL_LINK_KEYS.ContainsKey(fields[i].ai)) {
                if (key >= 0) {
                    throw new ArgumentException("A GS1 Digital Link has one primary key, not (" +
                            fields[key].ai + ") and (" + fields[i].ai + ")!");
                }
                key = i;
            }
        }
        if (key < 0) {
            throw new ArgumentException(
                    "A GS1 Digital Link needs a primary key, such as a GTIN (01), an SSCC (00) or a GLN (414)!");
        }

        StringBuilder sb = new StringBuilder(domain.EndsWith("/", StringComparison.Ordinal) ?
                domain.Substring(0, domain.Length - 1) : domain);
        sb.Append('/').Append(fields[key].ai).Append('/').Append(PercentEncode(fields[key].data));
        bool[] used = new bool[fields.Count];
        used[key] = true;
        foreach (String[] place in DIGITAL_LINK_KEYS[fields[key].ai]) {
            int found = -1;
            for (int i = 0; i < fields.Count; i++) {
                foreach (String qualifier in place) {
                    if (fields[i].ai != qualifier) {
                        continue;
                    }
                    if (found >= 0) {
                        throw new ArgumentException("The qualifiers (" + fields[found].ai + ") and (" +
                                fields[i].ai + ") of (" + fields[key].ai + ") cannot be together!");
                    }
                    found = i;
                }
            }
            if (found >= 0) {
                sb.Append('/').Append(fields[found].ai).Append('/').Append(PercentEncode(fields[found].data));
                used[found] = true;
            }
        }
        char separator = '?';
        for (int i = 0; i < fields.Count; i++) {
            if (!used[i]) {
                sb.Append(separator).Append(fields[i].ai).Append('=').Append(PercentEncode(fields[i].data));
                separator = '&';
            }
        }
        return sb.ToString();
    }

    // Returns the value with each character but the unreserved ones of a web
    // address, the letters, the digits and "-._~", as % and its two
    // hexadecimal digits.
    private static String PercentEncode(String value) {
        const String hex = "0123456789ABCDEF";
        StringBuilder sb = new StringBuilder();
        foreach (char c in value) {
            if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || "-._~".IndexOf(c) >= 0) {
                sb.Append(c);
            } else {
                sb.Append('%').Append(hex[c >> 4]).Append(hex[c & 15]);
            }
        }
        return sb.ToString();
    }
}
}
