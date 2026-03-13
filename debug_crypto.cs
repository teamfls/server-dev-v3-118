using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DebugCrypto
{
    public class Program
    {
        public static void Main()
        {
            // Captured packet from user log:
            // 78 80 22 04 1E 03 C8 1C 8F 5F CF F6 71 67 96 F6 ...
            byte[] rawRecv = HexToBytes("78 80 22 04 1E 03 C8 1C 8F 5F CF F6 71 67 96 F6 C9 ED D9 60 43 90 61 39 BD 6B 85 72 59 D8 AA 6B A0 80 3C C1 8D 83 E5 41 CB E5 D0 06 47 33 58 FF 36 75 14 3E 3F E2 54 D2 9E 69 F6 F1 47 90 F9 B1 B6 73 37 E6 01 32 FD F5 5A C4 58 F2 50 BB 1D 3F 1B 03 9E C8 53 D5 3F 1E 7F 2B 5C 30 07 A7 6F 4E 77 F6 99 37 03 A9 0E 98 C1 D2 4D CF 28 12 8C 98 E8 66 B3 81 78 8F FE 5C 48 A4 00");
            
            int sessionId = 1;
            ushort sessionSeed = 12750;
            int sessionMode = 2; // derived from (1 + 29890) % 3
            
            // Derive k1 and k2 components
            int[] CRYPTO = { 29890, 32759, 1360 };
            byte[] bSeed1 = BitConverter.GetBytes(sessionId ^ CRYPTO[1]);
            byte[] bSeed2 = BitConverter.GetBytes(sessionId ^ CRYPTO[2]);
            
            byte[] k1 = new byte[8];
            Array.Copy(bSeed1, 0, k1, 0, 4);
            for (int i = 4; i < 8; i++) k1[i] = (byte)(sessionId >> (i % 4) & 0xFF);
            
            byte[] k2 = new byte[8];
            Array.Copy(bSeed2, 0, k2, 0, 4);
            for (int i = 4; i < 8; i++) k2[i] = (byte)(sessionSeed >> (i % 4) & 0xFF);
            
            Console.WriteLine("k1: " + BitConverter.ToString(k1).Replace("-", " "));
            Console.WriteLine("k2: " + BitConverter.ToString(k2).Replace("-", " "));
            
            // Extract payload for decryption: skip 2-byte header [len|0x8000]
            int payloadLen = BitConverter.ToUInt16(rawRecv, 0) & 0x7FFF;
            byte[] payload = new byte[payloadLen];
            Array.Copy(rawRecv, 2, payload, 0, payloadLen);
            
            // Try permutations
            TryDecrypt("k1+k2", Combine(k1, k2), payload, sessionMode);
            TryDecrypt("k2+k1", Combine(k2, k1), payload, sessionMode);
            TryDecrypt("k1+k1", Combine(k1, k1), payload, sessionMode);
            TryDecrypt("k2+k2", Combine(k2, k2), payload, sessionMode);
            
            // Try with SessionSeed in seed1?
            byte[] bSeed1Alt = BitConverter.GetBytes(sessionSeed ^ CRYPTO[1]);
            byte[] k1Alt = new byte[8];
            Array.Copy(bSeed1Alt, 0, k1Alt, 0, 4);
            for (int i = 4; i < 8; i++) k1Alt[i] = (byte)(sessionId >> (i % 4) & 0xFF);
            TryDecrypt("k1Alt+k2", Combine(k1Alt, k2), payload, sessionMode);
            
            // Try different modes
            for (int m = 0; m < 3; m++) {
                if (m == sessionMode) continue;
                TryDecrypt($"k1+k2 mode {m}", Combine(k1, k2), payload, m);
            }
        }
        
        static void TryDecrypt(string label, byte[] key, byte[] encrypted, int mode) {
            byte[] dec = Decrypt(encrypted, key, mode);
            if (dec != null) {
                Console.WriteLine($"[SUCCESS] {label}: {BitConverter.ToString(dec, 0, Math.Min(dec.Length, 10)).Replace("-", " ")}");
                if (dec.Length >= 2 && (dec[0] == 0x01 && dec[1] == 0x05)) {
                    Console.WriteLine("  !!! MATCHES LOGIN_REQ OPCODE 0x0501 !!!");
                }
            } else {
                // Console.WriteLine($"[FAIL] {label}");
            }
        }
        
        static byte[] Combine(byte[] a, byte[] b) {
            byte[] res = new byte[16];
            Array.Copy(a, 0, res, 0, 8);
            Array.Copy(b, 0, res, 8, 8);
            return res;
        }

        static byte[] HexToBytes(string hex) {
            string clean = hex.Replace(" ", "").Replace("-", "");
            byte[] res = new byte[clean.Length / 2];
            for (int i = 0; i < clean.Length; i += 2)
                res[i / 2] = Convert.ToByte(clean.Substring(i, 2), 16);
            return res;
        }

        // --- Implementation of MessRotateRead and Decrypt from Bitwise.cs ---
        static byte MessRotateRead(byte[] buf, uint bitPos) {
            uint byteIdx = (bitPos >> 3) & 7;
            int bitOff = (int)(bitPos & 7);
            if (bitOff != 0)
                return (byte)((buf[(byteIdx + 1) & 7] >> (8 - bitOff))
                            + ((buf[byteIdx] & (byte)(255 >> bitOff)) << bitOff));
            return buf[byteIdx];
        }

        static int MessStepAdvance(byte val, int mode, int current) {
            if (mode == 1) return val + current + 8;
            if (mode == 2) return val + current + 14;
            return val + current + 12;
        }

        static byte[] Decrypt(byte[] encrypted, byte[] key, int mode) {
            if (encrypted == null || encrypted.Length < 4 || key == null || key.Length < 16)
                return null;
            byte[] k1 = new byte[8]; byte[] k2 = new byte[8];
            Array.Copy(key, 0, k1, 0, 8); Array.Copy(key, 8, k2, 0, 8);
            byte r0 = encrypted[0], r1 = encrypted[1], r2 = encrypted[2], padLen = encrypted[3];
            if ((byte)(padLen - 2) > 7) return null;
            int plaintextLen = encrypted.Length - padLen - 4;
            if (plaintextLen < 0) return null;
            byte v63, v62, tmp = r2;
            if (mode == 1) { v63 = (byte)((tmp >> 4) + r0); tmp &= 0x0F; }
            else if (mode == 2) { v63 = (byte)((tmp & 0xF) + r0); tmp >>= 4; }
            else { v63 = (byte)(tmp + r0); }
            v62 = (byte)(tmp + r1);
            int v17 = 0; byte v16 = 0; int pos = 4;
            byte[] plaintext = new byte[plaintextLen];
            for (int i = 0; i < plaintextLen; i++) {
                byte s1 = MessRotateRead(k1, (uint)(v17 + v63));
                byte s2 = MessRotateRead(k2, (uint)(v17 + v62));
                v16 ^= (byte)(s1 ^ s2);
                v17 = MessStepAdvance(v16, mode, v17);
                byte cipherByte = encrypted[pos++];
                plaintext[i] = (byte)(v16 ^ cipherByte);
                for (int j = 0; j < 8; j += 2) k1[j] ^= cipherByte;
                for (int j = 1; j < 8; j += 2) k2[j] ^= cipherByte;
            }
            byte[] padDecrypted = new byte[padLen];
            for (int i = 0; i < padLen; i++) {
                byte s1 = MessRotateRead(k1, (uint)(v17 + v63));
                byte s2 = MessRotateRead(k2, (uint)(v17 + v62));
                v16 ^= (byte)(s1 ^ s2);
                v17 = MessStepAdvance(v16, mode, v17);
                byte cipherByte = encrypted[pos++];
                padDecrypted[i] = (byte)(v16 ^ cipherByte);
                for (int j = 0; j < 8; j += 2) k1[j] ^= cipherByte;
                for (int j = 1; j < 8; j += 2) k2[j] ^= cipherByte;
            }
            if (padDecrypted[0] != padLen || padDecrypted[1] != padLen) return null;
            return plaintext;
        }
    }
}
