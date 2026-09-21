/*
 * EncryptionTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
/// <summary>AES-256 encryption with the standard security handler, read back with the passwords.</summary>
public class EncryptionTest {
    private static byte[] Encrypted(Compliance compliance, Passwords passwords, Permissions permissions) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, compliance);
        pdf.SetTitle("Encrypted title");
        pdf.SetEncryption(new Encryption(pdf, passwords, permissions));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(new Font(pdf, CoreFont.HELVETICA), "Secret text").SetLocation(50f, 50f).DrawOn(page);
        pdf.Complete();
        return stream.ToArray();
    }

    private static byte[] Encrypted(string user, string owner) {
        return Encrypted(Compliance.PDF_1_7,
                new Passwords().SetUserPassword(user).SetOwnerPassword(owner),
                new Permissions().Grant(UserAccess.PRINT));
    }

    private static int AccessValue(byte[] pdf) {
        Match match = Regex.Match(TestSupport.Latin1(pdf), "/P (-?\\d+)");
        Assert.True(match.Success);
        return int.Parse(match.Groups[1].Value);
    }

    private static string Title(List<PDFobj> objects) {
        return TestSupport.Utf16Hex(TestSupport.FindObject(objects, "/Producer").GetValue("/Title"));
    }

    [Fact]
    public void ReadsBackWithTheUserOrTheOwnerPassword() {
        byte[] pdf = Encrypted("hello", "world");
        foreach (string password in new[] {"hello", "world"}) {
            List<PDFobj> objects = TestSupport.Read(pdf, password);
            Assert.Single(new PDF().GetPageObjects(objects));
            Assert.Equal("Encrypted title", Title(objects));
        }
    }

    [Fact]
    public void TheTitleIsNotInTheFileInPlainText() {
        string raw = TestSupport.Latin1(Encrypted("hello", "world"));
        Assert.DoesNotContain("feff0045006e0063", raw);
        Assert.Contains("/Encrypt ", raw);
    }

    [Fact]
    public void AWrongOrMissingPasswordFailsWithAMessage() {
        byte[] pdf = Encrypted("hello", "world");
        Exception wrong = Assert.ThrowsAny<Exception>(() => TestSupport.Read(pdf, "wrong"));
        Assert.Equal("The password of the PDF is not correct.", wrong.Message);
        Exception missing = Assert.ThrowsAny<Exception>(() => TestSupport.Read(pdf));
        Assert.Equal("The PDF needs a password.", missing.Message);
    }

    [Fact]
    public void AnEmptyUserPasswordOpensWithoutAPassword() {
        Assert.Equal("Encrypted title", Title(TestSupport.Read(Encrypted("", "owner"))));
    }

    [Fact]
    public void ANonAsciiPasswordIsUtf8() {
        byte[] pdf = Encrypted("пароль", "owner");
        Assert.Equal("Encrypted title", Title(TestSupport.Read(pdf, "пароль")));
    }

    [Fact]
    public void PasswordsAreCutAt127Bytes() {
        string password = new string('a', 200);
        byte[] pdf = Encrypted(password, "owner");
        Assert.Equal("Encrypted title", Title(TestSupport.Read(pdf, password.Substring(0, 127))));
        Assert.ThrowsAny<Exception>(() => TestSupport.Read(pdf, password.Substring(0, 126)));
    }

    [Fact]
    public void PermissionsAreANegativeNumberWithTheReservedBitsSet() {
        Assert.Equal(-3900, AccessValue(Encrypted("hello", "world")));
    }

    [Fact]
    public void PdfUaGrantsExtractionForAccessibility() {
        byte[] pdf = Encrypted(Compliance.PDF_UA_1,
                new Passwords().SetUserPassword("hello").SetOwnerPassword("world"),
                new Permissions().Grant(UserAccess.PRINT));
        int access = AccessValue(pdf);
        Assert.Equal(-3388, access);
        Assert.True(((UserAccess) access).HasFlag(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY));
    }

    [Fact]
    public void ACryptFilterThatIsAnObjectOfItsOwnIsFollowed() {
        // The /CF dictionary and the filter in it as objects of their own, as
        // pdf.js tests them in issue7665: the method was not found, and the
        // streams and the strings of the PDF were left encrypted.
        Func<int, string, PDFobj> obj = (number, raw) => {
            PDFobj o = new PDF().GetObject(System.Text.Encoding.Latin1.GetBytes(raw), 0);
            o.number = number;
            return o;
        };
        List<PDFobj> objects = new List<PDFobj> {
            obj(7, "7 0 obj << /StdCF 8 0 R >> endobj"),
            obj(8, "8 0 obj << /AuthEvent /DocOpen /CFM /AESV3 /Length 32 >> endobj"),
            obj(9, "9 0 obj << /StdCF << /CFM /AESV2 >> >> endobj"),
        };
        var cases = new (string cf, int want)[] {
            ("7 0 R", Decryptor.AES_256),
            ("9 0 R", Decryptor.AES_128),
            ("<< /StdCF 8 0 R >>", Decryptor.AES_256),
            ("<< /StdCF << /CFM /V2 >> >>", Decryptor.RC4),
            ("99 0 R", Decryptor.NONE),
        };
        foreach (var c in cases) {
            PDFobj encrypt = obj(6, "6 0 obj << /Filter /Standard /V 5 /CF " + c.cf + " /StmF /StdCF >> endobj");
            Assert.Equal((c.cf, c.want), (c.cf, Decryptor.GetMethod(encrypt, "/StdCF", objects)));
        }
    }
}
}
