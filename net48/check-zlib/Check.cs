using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
// The ZLibStream of the .NET Framework 4.8 build (Compat/ZLibStream.cs, on
// Compat/Puff.cs) against .NET 8's own, run on .NET 8: the three functions of
// the Decompressor that read zlib streams, Inflate, InflatePrefix and
// InflateExact, as net/pdfjet/Decompressor.cs has them, copied by
// check-zlib.sh, run with each, on streams whole, cut short, with a wrong
// checksum, bytes after their end, or corrupted; what they return, and the
// kind of error they throw, must be the same. And what the compat stream
// compresses, .NET reads.

namespace Check {
sealed class EndOfInputStream : MemoryStream {
    internal bool ReadAtEnd;
    readonly bool oneByte;
    internal EndOfInputStream(byte[] d, bool oneByte) : base(d, false) { this.oneByte = oneByte; }
    int Limit(int count) { if (!oneByte) return count; long left = Length - Position; return (int)((left <= 64) ? Math.Min(count, 1) : Math.Min(count, left - 64)); }
    public override int Read(byte[] b, int o, int c) { int r = base.Read(b, o, Limit(c)); if (r == 0 && c > 0) ReadAtEnd = true; return r; }
    public override int Read(Span<byte> b) { int r = base.Read(b.Slice(0, Limit(b.Length))); if (r == 0 && b.Length > 0) ReadAtEnd = true; return r; }
    public override int ReadByte() { int v = base.ReadByte(); if (v == -1) ReadAtEnd = true; return v; }
}

static class Program {
    static byte[] Zlib(byte[] raw, CompressionLevel level) { var ms = new MemoryStream(); using (var z = new ZLibStream(ms, level, true)) z.Write(raw); return ms.ToArray(); }
    static int Main(string[] args) {
        var rnd = new Random(1);
        var cases = new List<(string, byte[])>();
        foreach (var level in new[] { CompressionLevel.Optimal, CompressionLevel.Fastest, CompressionLevel.NoCompression, CompressionLevel.SmallestSize }) {
            foreach (int size in new[] { 0, 1, 100, 5000, 70000, 300000 }) {
                var raw = new byte[size];
                for (int i = 0; i < size; i++) raw[i] = (byte)(rnd.Next(4) == 0 ? rnd.Next(256) : (i % 7) + 'a');
                var z = Zlib(raw, level);
                cases.Add(($"{level} {size}", z));
                for (int cut = 1; cut <= 6 && cut < z.Length; cut++) cases.Add(($"{level} {size} cut {cut}", z[..^cut]));
                if (z.Length > 10) cases.Add(($"{level} {size} cut half", z[..(z.Length / 2)]));
                var bad = (byte[])z.Clone(); bad[^1] ^= 1; cases.Add(($"{level} {size} bad checksum", bad));
                var extra = new byte[z.Length + 5]; z.CopyTo(extra, 0); cases.Add(($"{level} {size} bytes after", extra));
                if (z.Length > 8) { var flip = (byte[])z.Clone(); flip[z.Length / 2] ^= 0x55; cases.Add(($"{level} {size} corrupted", flip)); }
            }
        }
        cases.Add(("bad header", new byte[] { 0x12, 0x34, 0, 0 }));
        cases.Add(("empty", new byte[0]));
        cases.Add(("one byte", new byte[] { 0x78 }));
        int same = 0, differ = 0;
        string Outcome(System.Func<string> f) { try { return f(); } catch (Exception e) { return e.GetType().Name; } }
        string Hash(byte[] b) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(b))[..12] + "/" + b.Length;
        foreach (var (name, data) in cases) {
            foreach (int limit in new[] { 1 << 28, 50, 4999, 0 }) {
                string Both(bool compat) {
                    Decoding.Make = s => compat ? new CompatZLibStream(s, CompressionMode.Decompress) : new ZLibStream(s, CompressionMode.Decompress);
                    return "inflate " + Outcome(() => Hash(Decoding.Inflate(data, limit)))
                        + " | prefix " + Outcome(() => Hash(Decoding.InflatePrefix(data, limit)))
                        + " | exact " + Outcome(() => { var r = Decoding.InflateExact(data, limit, out bool ex); return Hash(r) + " exact=" + ex; });
                }
                string a = Both(false), b = Both(true);
                if (a == b) same++; else { differ++; if (differ <= 12) Console.WriteLine($"{name} limit={limit}\n  real:   {a}\n  compat: {b}"); }
            }
        }
        Console.WriteLine($"functions: same {same}, differ {differ}");
        int roundTrips = 0, identical = 0;
        foreach (int size in new[] { 0, 1, 100, 5000, 70000, 300000 }) {
            var raw = new byte[size];
            for (int i = 0; i < size; i++) raw[i] = (byte)(rnd.Next(4) == 0 ? rnd.Next(256) : (i % 7) + 'a');
            var ms = new MemoryStream();
            using (var z = new CompatZLibStream(ms, CompressionMode.Compress, true)) z.Write(raw, 0, raw.Length);
            var back = new MemoryStream();
            using (var z = new ZLibStream(new MemoryStream(ms.ToArray()), CompressionMode.Decompress)) z.CopyTo(back);
            if (back.ToArray().AsSpan().SequenceEqual(raw)) roundTrips++;
            if (ms.ToArray().AsSpan().SequenceEqual(Zlib(raw, CompressionLevel.Optimal))) identical++;
        }
        Console.WriteLine($"compressed by the compat stream, read by .NET: {roundTrips} of 6; the same bytes as .NET 8 wrote: {identical} of 6");
        return differ == 0 && roundTrips == 6 ? 0 : 1;
    }
}
}
