#!/bin/bash
# Checks the ZLibStream of the .NET Framework 4.8 build against .NET 8's own,
# on .NET 8, where both run: see check-zlib/Check.cs. Run after a change of
# net/pdfjet/Decompressor.cs or of Compat/ZLibStream.cs or Compat/Puff.cs.
cd "$(dirname "$0")" || exit 1
mkdir -p check-zlib/obj/copied
python3 - <<'PY' || exit 1
import re
src = open('../net/pdfjet/Decompressor.cs', encoding='utf-8').read()
def method(sig):
    i = src.index('    internal static byte[] ' + sig)
    return src[i:src.index('\n    }\n', i) + 7]
def private(name):
    i = src.index('    private static void ' + name)
    return src[i:src.index('\n    }\n', i) + 7]
body = method('Inflate(byte[] data, int maxLength)') + method('InflatePrefix(') + method('InflateExact(') \
    + private('CheckLength')
body = body.replace('new ZLibStream(inStream, CompressionMode.Decompress)', 'Make(inStream)') \
           .replace('new EndOfInputStream(data)', 'new EndOfInputStream(data, false)')
open('check-zlib/obj/copied/Decoding.cs', 'w', encoding='utf-8').write(
    'using System; using System.IO; using System.IO.Compression;\nnamespace Check {\n'
    'static class Decompressor { internal const int MAX_DECODED_LENGTH = 256 * 1024 * 1024; }\n'
    'static class Decoding {\n    internal static Func<Stream, Stream> Make;\n' + body + '}\n}\n')
puff = open('Compat/Puff.cs', encoding='utf-8').read().replace('namespace PDFjet.NET {', 'namespace Check {')
open('check-zlib/obj/copied/Puff.cs', 'w', encoding='utf-8').write(puff)
z = open('Compat/ZLibStream.cs', encoding='utf-8').read()
z = z.replace('using PDFjet.NET;', 'using System; using System.IO; using System.IO.Compression;') \
     .replace('namespace System.IO.Compression {', 'namespace Check {') \
     .replace('class ZLibStream', 'class CompatZLibStream').replace('internal ZLibStream(', 'internal CompatZLibStream(')
open('check-zlib/obj/copied/CompatZLibStream.cs', 'w', encoding='utf-8').write(z)
PY
dotnet run --project check-zlib/check-zlib.csproj -c release
