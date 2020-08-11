/**
 *
 * The Salsa20 encryption function was designed and originally implemented
 * by Daniel J. Bernstein:
 *
 * http://cr.yp.to/salsa20.html
 * http://cr.yp.to/snuffle/ip.pdf
 *
 * The following Swift implementation encrypts the system time and returns
 * the first 32 characters of the hash.
 *
 */

import Foundation

public class Salsa20 {

    private var id: String = ""

    public init() {

/*
Test input:

        let a_in: [UInt32] = [
                0x61707865, 0x04030201, 0x08070605, 0x0c0b0a09,
                0x100f0e0d, 0x3320646e, 0x01040103, 0x06020905,
                0x00000007, 0x00000000, 0x79622d32, 0x14131211,
                0x18171615, 0x1c1b1a19, 0x201f1e1d, 0x6b206574]

The expected output:
0xb9a205a3
0x0695e150
0xaa94881a
0xadb7b12c
0x798942d4
0x26107016
0x64edb1a4
0x2d27173f
0xb1c7f1fa
0x62066edc
0xe035fa23
0xc4496f04
0x2131e6b3
0x810bde28
0xf62cb407
0x6bdede3d
*/

        var a_in: [UInt32] = [
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00]

        let currentTimeMillis = UInt32(Int64(Date().timeIntervalSince1970 * 1000) & 0xffffffff)
        a_in[0] = currentTimeMillis
        id = bin2hex(salsa20_word_specification(a_in))
    }


    private func R(_ a: UInt32, _ b: UInt32) -> UInt32 {
        return (a << b) | (a >> (32 - b))
    }


    private func salsa20_word_specification(_ a_in: [UInt32]) -> [UInt32] {

        var a_out = [UInt32](repeating: 0, count: 16)
        var x = [UInt32](repeating: 0, count: 16)

        for i in 0..<16 {
            x[i] = a_in[i]
        }

        for _ in stride(from: 20, to: 0, by: -2) {
            x[ 4] ^= R(x[ 0] &+ x[12], 7)
            x[ 8] ^= R(x[ 4] &+ x[ 0], 9)
            x[12] ^= R(x[ 8] &+ x[ 4],13)
            x[ 0] ^= R(x[12] &+ x[ 8],18)
            x[ 9] ^= R(x[ 5] &+ x[ 1], 7)
            x[13] ^= R(x[ 9] &+ x[ 5], 9)
            x[ 1] ^= R(x[13] &+ x[ 9],13)
            x[ 5] ^= R(x[ 1] &+ x[13],18)
            x[14] ^= R(x[10] &+ x[ 6], 7)
            x[ 2] ^= R(x[14] &+ x[10], 9)
            x[ 6] ^= R(x[ 2] &+ x[14],13)
            x[10] ^= R(x[ 6] &+ x[ 2],18)
            x[ 3] ^= R(x[15] &+ x[11], 7)
            x[ 7] ^= R(x[ 3] &+ x[15], 9)
            x[11] ^= R(x[ 7] &+ x[ 3],13)
            x[15] ^= R(x[11] &+ x[ 7],18)
            x[ 1] ^= R(x[ 0] &+ x[ 3], 7)
            x[ 2] ^= R(x[ 1] &+ x[ 0], 9)
            x[ 3] ^= R(x[ 2] &+ x[ 1],13)
            x[ 0] ^= R(x[ 3] &+ x[ 2],18)
            x[ 6] ^= R(x[ 5] &+ x[ 4], 7)
            x[ 7] ^= R(x[ 6] &+ x[ 5], 9)
            x[ 4] ^= R(x[ 7] &+ x[ 6],13)
            x[ 5] ^= R(x[ 4] &+ x[ 7],18)
            x[11] ^= R(x[10] &+ x[ 9], 7)
            x[ 8] ^= R(x[11] &+ x[10], 9)
            x[ 9] ^= R(x[ 8] &+ x[11],13)
            x[10] ^= R(x[ 9] &+ x[ 8],18)
            x[12] ^= R(x[15] &+ x[14], 7)
            x[13] ^= R(x[12] &+ x[15], 9)
            x[14] ^= R(x[13] &+ x[12],13)
            x[15] ^= R(x[14] &+ x[13],18)
        }

        for i in 0..<16 {
            a_out[i] = x[i] &+ a_in[i]
        }

        return a_out
    }


    private func bin2hex(_ binArray: [UInt32]) -> String {
        let hexString =
                String(format: "%08x", binArray[0]) +
                String(format: "%08x", binArray[1]) +
                String(format: "%08x", binArray[2]) +
                String(format: "%08x", binArray[3])
        return hexString
    }


    public func getID() -> String {
        return self.id
    }

}

/*
_ = Salsa20()
*/
