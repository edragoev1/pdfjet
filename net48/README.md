# PDFjet for .NET Framework 4.8

`PDFjet.dll` for the applications still on .NET Framework 4.8, which cannot
load the net8.0 one. It is built from the sources of `net/pdfjet` **as they
are**: nothing there changes for it, and the .NET code goes on using what .NET 8
has. This folder fills the gaps around it.

    net48/build.sh                      bin/release/net48/PDFjet.dll, with the System.Memory DLLs
    net48/check-zlib.sh                 the ZLibStream of this build against .NET 8's own
    net48/make-examples-folder.sh <dir> the 57 examples to run on Windows, RunExamples.exe

It needs the .NET 10 SDK or later, for C# 14, beside the .NET 8 SDK of the rest:
`build.sh` takes the `dotnet` of `DOTNET`, else `~/.dotnet10/dotnet`, else the
one on the path. To install it in its own folder, which leaves the system's
.NET as it is:

    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir ~/.dotnet10

The DLL it makes needs .NET Framework 4.8 alone, and four small DLLs of
Microsoft beside it, System.Memory and what it uses: System.Buffers,
System.Numerics.Vectors and System.Runtime.CompilerServices.Unsafe.

## How

- `PDFjet.Net48.csproj` compiles `../net/pdfjet/**/*.cs`, with the same
  assembly name and version as the net8.0 DLL, `PDFjet, Version=9.0.0.0`, and
  the same strong name when it is given the key.
- Two methods .NET Framework cannot compile, overrides of `Stream` methods of
  spans that its `Stream` does not have, are cut from copies of their files in
  `obj/release/net48/cut`, by the task `CutMethods` of the project file:
  `Decompressor.EndOfInputStream.Read(Span<byte>)` and
  `Page.WrittenContent.Write(ReadOnlySpan<byte>)`. Each is found by the line
  that begins it, once, or the build stops: a change of one is looked at,
  never guessed.
- `Compat/Polyfills.cs` gives .NET Framework what the source calls and it does
  not have: static members such as `MD5.HashData`, `Array.Fill`,
  `Encoding.Latin1` and `Convert.ToHexString` as C# 14 extension members,
  which compile into calls of these methods and need nothing of the runtime;
  the newer overloads of `string`, `Stream`, `Encoding` and `Aes`; and classes
  of the names .NET Framework lacks, `MathF`, `BitOperations` and
  `CryptographicOperations`, in their namespaces.
- `Compat/ZLibStream.cs` is the zlib stream .NET Framework lacks: compressing
  with its `DeflateStream`, and decoding with `Compat/Puff.cs`, a C#
  translation of puff.c, Mark Adler's reference decoder of Deflate. It reads its
  input exactly to the end of the zlib stream and checks the checksum there,
  as the Decompressor needs to know a stream cut short or corrupted; and
  `check-zlib.sh` holds it to .NET 8's own on more than a thousand streams.

## When the .NET source changes

Nothing to do, but build: a change of `net/pdfjet` comes into this build as it
is. Should the source use another API of .NET 8, this build fails to compile,
and the gap is filled in `Compat`, never in the source. After a change of
`Decompressor.cs`, run `check-zlib.sh`.

What it cannot show on Linux: the examples run on .NET Framework, on Windows.
`make-examples-folder.sh` makes a folder for that, with nothing to install:
`run-examples/RunExamples.exe`, the 57 examples compiled as they are for .NET
Framework 4.8 against this PDFjet.dll, and what they read; run in it, it
makes the PDFs there and writes net48-results.txt.

Floating point of `MathF` in double precision, and the compression of .NET
Framework's own zlib, can make the bytes of a PDF differ from those of the
net8.0 build; the pages are to be the same.
