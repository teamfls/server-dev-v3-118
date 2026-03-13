using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Plugin.Core.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Plugin.Core.Utility
{
    public static class Bitwise
    {
        #region Private Fields - Hex Formatting

        private static readonly string NEW_LINE = string.Format("\n");
        private static readonly string EMPTY_STRING = string.Format("");
        private static readonly char[] ASCII_PRINTABLE_CHARS = new char[256];
        private static readonly string[] HEX_BYTE_STRINGS = new string[256];
        private static readonly string[] HEX_PADDING_SPACES = new string[16];
        private static readonly string[] ASCII_PADDING_SPACES = new string[16];

        #endregion

        #region Public Constants

        public static readonly int[] CRYPTO = new int[3] { 29890, 32759, 1360 };

        #endregion

        #region Static Constructor

        static Bitwise()
        {
            for (int i = 0; i < 10; i++)
            {
                StringBuilder sb = new StringBuilder(3);
                sb.Append(" 0");
                sb.Append(i);
                HEX_BYTE_STRINGS[i] = sb.ToString().ToUpper();
            }
            for (int i = 10; i < 16; i++)
            {
                StringBuilder sb = new StringBuilder(3);
                sb.Append(" 0");
                sb.Append((char)(97 + i - 10));
                HEX_BYTE_STRINGS[i] = sb.ToString().ToUpper();
            }
            for (int i = 16; i < HEX_BYTE_STRINGS.Length; i++)
            {
                StringBuilder sb = new StringBuilder(3);
                sb.Append(' ');
                sb.Append(i.ToString("X"));
                HEX_BYTE_STRINGS[i] = sb.ToString().ToUpper();
            }
            for (int i = 0; i < HEX_PADDING_SPACES.Length; i++)
            {
                int paddingCount = HEX_PADDING_SPACES.Length - i;
                StringBuilder sb = new StringBuilder(paddingCount * 3);
                for (int j = 0; j < paddingCount; j++)
                    sb.Append("   ");
                HEX_PADDING_SPACES[i] = sb.ToString().ToUpper();
            }
            for (int i = 0; i < ASCII_PADDING_SPACES.Length; i++)
            {
                int paddingCount = ASCII_PADDING_SPACES.Length - i;
                StringBuilder sb = new StringBuilder(paddingCount);
                for (int j = 0; j < paddingCount; j++)
                    sb.Append(' ');
                ASCII_PADDING_SPACES[i] = sb.ToString().ToUpper();
            }
            for (int i = 0; i < ASCII_PRINTABLE_CHARS.Length; i++)
            {
                ASCII_PRINTABLE_CHARS[i] = i <= 31 || i >= 127 ? '.' : (char)i;
            }
        }

        #endregion

        #region CMess Encryption (v108) - CMessEncryptor / CMessDecryptor
        public static byte[] Encrypt(byte[] data, byte[] key, int mode)
        {
            if (data == null || data.Length == 0)
                return data ?? new byte[0];
            if (key == null || key.Length < 16)
                throw new ArgumentException("CMess key must be 16 bytes");

            byte[] k1 = new byte[8];
            byte[] k2 = new byte[8];
            Array.Copy(key, 0, k1, 0, 8);
            Array.Copy(key, 8, k2, 0, 8);

            int dataLen = data.Length;
            int padLen = ((2 - dataLen) & 7) + 2; 

            byte[] output = new byte[4 + dataLen + padLen];

            Random rng = new Random();
            byte r0 = (byte)(rng.Next() % 64);
            byte r1 = (byte)(rng.Next() % 64);
            byte r2 = (byte)rng.Next();
            byte r3 = (byte)padLen;

            output[0] = r0;
            output[1] = r1;
            output[2] = r2;
            output[3] = r3;

            byte v63, v62, tmp = r2;
            if (mode == 1) { v63 = (byte)((tmp >> 4) + r0); tmp &= 0x0F; }
            else if (mode == 2) { v63 = (byte)((tmp & 0xF) + r0); tmp >>= 4; }
            else { v63 = (byte)(tmp + r0); }
            v62 = (byte)(tmp + r1);

            int v18 = 0;
            byte v19 = 0;
            int pos = 4;

            for (int i = 0; i < dataLen; i++)
            {
                byte s1 = MessRotateRead(k1, (uint)(v18 + v63));
                byte s2 = MessRotateRead(k2, (uint)(v18 + v62));
                v19 ^= (byte)(s1 ^ s2);
                v18 = MessStepAdvance(v19, mode, v18);

                byte outByte = (byte)(v19 ^ data[i]);
                output[pos++] = outByte;

                for (int j = 0; j < 8; j += 2) k1[j] ^= outByte;
                for (int j = 1; j < 8; j += 2) k2[j] ^= outByte;
            }


            for (int i = 0; i < padLen; i++)
            {
                byte s1 = MessRotateRead(k1, (uint)(v18 + v63));
                byte s2 = MessRotateRead(k2, (uint)(v18 + v62));
                v19 ^= (byte)(s1 ^ s2);
                v18 = MessStepAdvance(v19, mode, v18);

                byte padByte = (byte)(r3 ^ v19);
                output[pos++] = padByte;

                for (int j = 0; j < 8; j += 2) k1[j] ^= padByte;
                for (int j = 1; j < 8; j += 2) k2[j] ^= padByte;
            }

            return output;
        }

        public static byte[] Decrypt(byte[] encrypted, byte[] key, int mode)
        {
            if (encrypted == null || encrypted.Length < 4)
                return null;
            if (key == null || key.Length < 16)
                return null;

            byte[] k1 = new byte[8];
            byte[] k2 = new byte[8];
            Array.Copy(key, 0, k1, 0, 8);
            Array.Copy(key, 8, k2, 0, 8);

            byte r0 = encrypted[0];
            byte r1 = encrypted[1];
            byte r2 = encrypted[2];
            byte padLen = encrypted[3];

            if ((byte)(padLen - 2) > 7)
                return null;

            int plaintextLen = encrypted.Length - padLen - 4;
            if (plaintextLen < 0)
                return null;

            byte v63, v62, tmp = r2;
            if (mode == 1) { v63 = (byte)((tmp >> 4) + r0); tmp &= 0x0F; }
            else if (mode == 2) { v63 = (byte)((tmp & 0xF) + r0); tmp >>= 4; }
            else { v63 = (byte)(tmp + r0); }
            v62 = (byte)(tmp + r1);

            int v17 = 0;
            byte v16 = 0;
            int pos = 4;

            byte[] plaintext = new byte[plaintextLen];
            for (int i = 0; i < plaintextLen; i++)
            {
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
            for (int i = 0; i < padLen; i++)
            {
                byte s1 = MessRotateRead(k1, (uint)(v17 + v63));
                byte s2 = MessRotateRead(k2, (uint)(v17 + v62));
                v16 ^= (byte)(s1 ^ s2);
                v17 = MessStepAdvance(v16, mode, v17);

                byte cipherByte = encrypted[pos++];
                byte decryptedPad = (byte)(v16 ^ cipherByte);
                padDecrypted[i] = decryptedPad;

                for (int j = 0; j < 8; j += 2) k1[j] ^= cipherByte;
                for (int j = 1; j < 8; j += 2) k2[j] ^= cipherByte;
            }

            if (padDecrypted[0] != padLen || padDecrypted[1] != padLen)
                return null;

            return plaintext;
        }

        private static byte MessRotateRead(byte[] buf, uint bitPos)
        {
            uint byteIdx = (bitPos >> 3) & 7;
            int bitOff = (int)(bitPos & 7);
            if (bitOff != 0)
                return (byte)((buf[(byteIdx + 1) & 7] >> (8 - bitOff))
                            + ((buf[byteIdx] & (byte)(255 >> bitOff)) << bitOff));
            return buf[byteIdx];
        }

        private static int MessStepAdvance(byte val, int mode, int current)
        {
            if (mode == 1) return val + current + 8;
            if (mode == 2) return val + current + 14;
            return val + current + 12;
        }

        public static byte[] Encrypt(byte[] data, int shift)
        {
            if (data == null || data.Length == 0) return data ?? new byte[0];
            byte[] output = new byte[data.Length];
            byte key = (byte)(shift & 0xFF);
            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ key);
                key = (byte)((key + data[i] + shift) & 0xFF);
            }
            return output;
        }

        public static byte[] Decrypt(byte[] data, int shift)
        {
            if (data == null || data.Length == 0) return data ?? new byte[0];
            byte[] output = new byte[data.Length];
            byte key = (byte)(shift & 0xFF);
            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ key);
                key = (byte)((key + output[i] + shift) & 0xFF);
            }
            return output;
        }

        #endregion

        #region RSA Session Key (v108)
        public static List<byte[]> GenerateRSAKeyPair(int SessionId, int SecurityKey, int SeedLength,
            out byte[] sessionKey)
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(
                new SecureRandom(new CryptoApiRandomGenerator()),
                SeedLength
            ));

            AsymmetricCipherKeyPair keyPair = generator.GenerateKeyPair();
            var pubKey = (RsaKeyParameters)keyPair.Public;
            var privKey = (RsaPrivateCrtKeyParameters)keyPair.Private;

            byte[] modulus = pubKey.Modulus.ToByteArrayUnsigned();
            byte[] exponent = pubKey.Exponent.ToByteArrayUnsigned();
            byte[] sessionBytes = BitConverter.GetBytes(SessionId + SecurityKey);
            Array.Copy(sessionBytes, 0, modulus, 0, Math.Min(sessionBytes.Length, modulus.Length));

            sessionKey = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(sessionKey);

            byte[] encryptedSessionKey = RsaRawPrivateEncrypt(sessionKey, privKey);

            var result = new List<byte[]>();
            result.Add(modulus);
            result.Add(exponent);
            result.Add(encryptedSessionKey);
            return result;
        }

        public static List<byte[]> GenerateRSAKeyPair(int SessionId, int SecurityKey, int SeedLength)
        {
            byte[] dummy;
            return GenerateRSAKeyPair(SessionId, SecurityKey, SeedLength, out dummy);
        }


        private static byte[] RsaRawPrivateEncrypt(byte[] data, RsaPrivateCrtKeyParameters privKey)
        {
            int modulusLen = (privKey.Modulus.BitLength + 7) / 8;
            byte[] padded = new byte[modulusLen];
            int offset = modulusLen - data.Length;
            if (offset < 0) offset = 0;
            Array.Copy(data, 0, padded, offset, Math.Min(data.Length, modulusLen));
            var m = new BigInteger(1, padded);
            var c = m.ModPow(privKey.Exponent, privKey.Modulus);
            byte[] result = c.ToByteArrayUnsigned();
            if (result.Length == modulusLen)
                return result;

            byte[] fixed_ = new byte[modulusLen];
            Array.Copy(result, 0, fixed_, modulusLen - result.Length, result.Length);
            return fixed_;
        }

        public static void GenerateRSAKeyPairRaw(int keySizeInBits,
            out byte[] modulus,
            out byte[] exponent,
            out byte[] privateKeyObj)
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), keySizeInBits));
            var keyPair = generator.GenerateKeyPair();
            var pubKey = (RsaKeyParameters)keyPair.Public;
            var privKey = (RsaPrivateCrtKeyParameters)keyPair.Private;
            modulus = pubKey.Modulus.ToByteArrayUnsigned();
            exponent = pubKey.Exponent.ToByteArrayUnsigned();
            byte[] privMod = privKey.Modulus.ToByteArrayUnsigned();
            byte[] privExp = privKey.Exponent.ToByteArrayUnsigned();
            byte[] buf = new byte[2 + privMod.Length + 2 + privExp.Length];
            buf[0] = (byte)(privMod.Length >> 8);
            buf[1] = (byte)(privMod.Length & 0xFF);
            Array.Copy(privMod, 0, buf, 2, privMod.Length);
            buf[2 + privMod.Length] = (byte)(privExp.Length >> 8);
            buf[3 + privMod.Length] = (byte)(privExp.Length & 0xFF);
            Array.Copy(privExp, 0, buf, 4 + privMod.Length, privExp.Length);
            privateKeyObj = buf;
        }

        public static byte[] RsaDecryptWithStoredKey(byte[] data, byte[] privateKeyObj)
        {
            if (privateKeyObj == null || privateKeyObj.Length < 4) return null;

            // Deserialize private key
            int modLen = (privateKeyObj[0] << 8) | privateKeyObj[1];
            byte[] privModBytes = new byte[modLen];
            Array.Copy(privateKeyObj, 2, privModBytes, 0, modLen);

            int expLen = (privateKeyObj[2 + modLen] << 8) | privateKeyObj[3 + modLen];
            byte[] privExpBytes = new byte[expLen];
            Array.Copy(privateKeyObj, 4 + modLen, privExpBytes, 0, expLen);

            var privMod = new BigInteger(1, privModBytes);
            var privExp = new BigInteger(1, privExpBytes);

            // m = c^d mod n  (RSA decrypt)
            var c = new BigInteger(1, data);
            var m = c.ModPow(privExp, privMod);
            byte[] result = m.ToByteArrayUnsigned();

            return result;
        }

        #endregion

        #region Hex Dump Methods

        public static string ToHexData(string EventName, byte[] BuffData)
        {
            int dataLength = BuffData.Length;
            int startOffset = 0;
            int endOffset = BuffData.Length;
            int estimatedLines = (dataLength / 16 + (dataLength % 15 == 0 ? 0 : 1) + 4);
            StringBuilder output = new StringBuilder(estimatedLines * 80 + EventName.Length + 16);
            output.Append(EMPTY_STRING + "+--------+-------------------------------------------------+----------------+");
            output.Append($"{NEW_LINE}[!] {EventName}; Length: [{BuffData.Length} Bytes] </>");
            output.Append(NEW_LINE + "         +-------------------------------------------------+");
            output.Append(NEW_LINE + "         |  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F |");
            output.Append(NEW_LINE + "+--------+-------------------------------------------------+----------------+");
            int byteIndex;
            for (byteIndex = startOffset; byteIndex < endOffset; byteIndex++)
            {
                int relativeOffset = byteIndex - startOffset;
                int columnInRow = relativeOffset & 15;
                if (columnInRow == 0)
                {
                    output.Append(NEW_LINE);
                    output.Append(((long)relativeOffset & 0xFFFFFFFF | 0x100000000L).ToString("X"));
                    output[output.Length - 9] = '|';
                    output.Append('|');
                }
                output.Append(HEX_BYTE_STRINGS[BuffData[byteIndex]]);
                if (columnInRow == 15)
                {
                    output.Append(" |");
                    for (int asciiIndex = byteIndex - 15; asciiIndex <= byteIndex; asciiIndex++)
                        output.Append(ASCII_PRINTABLE_CHARS[BuffData[asciiIndex]]);
                    output.Append('|');
                }
            }
            if ((byteIndex - startOffset & 15) != 0)
            {
                int remainingBytes = dataLength & 15;
                output.Append(HEX_PADDING_SPACES[remainingBytes]);
                output.Append(" |");
                for (int asciiIndex = byteIndex - remainingBytes; asciiIndex < byteIndex; asciiIndex++)
                    output.Append(ASCII_PRINTABLE_CHARS[BuffData[asciiIndex]]);
                output.Append(ASCII_PADDING_SPACES[remainingBytes]);
                output.Append('|');
            }
            output.Append(NEW_LINE + "+--------+-------------------------------------------------+----------------+");
            return output.ToString();
        }

        #endregion

        #region Conversion Methods

        public static string HexArrayToString(byte[] Buffer, int Length)
        {
            string result = "";
            try
            {
                result = Encoding.Unicode.GetString(Buffer, 0, Length);
                int nullIndex = result.IndexOf(char.MinValue);
                if (nullIndex != -1)
                    result = result.Substring(0, nullIndex);
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return result;
        }

        public static byte[] HexStringToByteArray(string HexString)
        {
            string cleanHex = HexString.Replace(":", "").Replace("-", "").Replace(" ", "");
            byte[] result = new byte[cleanHex.Length / 2];
            for (int i = 0; i < cleanHex.Length; i += 2)
            {
                result[i / 2] = (byte)(
                    HexCharToInt(cleanHex.ElementAt(i)) << 4 |
                    HexCharToInt(cleanHex.ElementAt(i + 1))
                );
            }
            return result;
        }

        private static string BytesToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static int HexCharToInt(char hexChar)
        {
            if (hexChar >= '0' && hexChar <= '9') return hexChar - '0';
            if (hexChar >= 'A' && hexChar <= 'F') return hexChar - 'A' + 10;
            if (hexChar >= 'a' && hexChar <= 'f') return hexChar - 'a' + 10;
            return 0;
        }

        public static string ToByteString(byte[] Result)
        {
            string output = "";
            string hexString = BitConverter.ToString(Result);
            char[] separators = new char[] { '-', ',', '.', ':', '\t' };
            foreach (string part in hexString.Split(separators))
                output = $"{output} {part}";
            return output;
        }

        #endregion

        #region Cryptographic Methods

        public static string GenerateRandomPassword(string AllowedChars, int Length, string Salt)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[Length];
                rng.GetBytes(randomBytes);
                char[] passwordChars = new char[Length];
                for (int i = 0; i < Length; i++)
                    passwordChars[i] = AllowedChars[randomBytes[i] % AllowedChars.Length];
                return HashString(new string(passwordChars), Salt, Length);
            }
        }

        public static string HashString(string Text, string Salt, int Length = 32)
        {
            using (HMACMD5 hmac = new HMACMD5(Encoding.UTF8.GetBytes(Salt)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(Text));
                return BytesToHexString(hash).Substring(0, Length);
            }
        }

        public static string ReadFile(string Path)
        {
            string hashResult = "";
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fileStream = new FileInfo(Path).Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                {
                    hashResult = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", string.Empty);
                    fileStream.Close();
                }
            }
            return hashResult;
        }

        #endregion

        #region Helper Classes

        private class OpcodeComparer
        {
            public ushort TargetOpcode;
            public OpcodeComparer(ushort targetOpcode) { TargetOpcode = targetOpcode; }
            public bool MatchesOpcode(ushort opcode) { return opcode == this.TargetOpcode; }
        }

        #endregion
    }
}