using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using Plugin.Core.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Plugin.Core.Utility
{
    /// <summary>
    /// Utilidades para operaciones de bits, encriptación y conversión de datos
    /// </summary>
    public static class Bitwise
    {
        #region Private Fields - Hex Formatting

        private static readonly string NEW_LINE = string.Format("\n");
        private static readonly string EMPTY_STRING = string.Format("");

        // Tabla de caracteres ASCII imprimibles para visualización hex
        private static readonly char[] ASCII_PRINTABLE_CHARS = new char[256];

        // Tabla de representación hexadecimal formateada (ej: " 00", " 0A", " FF")
        private static readonly string[] HEX_BYTE_STRINGS = new string[256];

        // Espaciado para alineación en dump hexadecimal (16 bytes)
        private static readonly string[] HEX_PADDING_SPACES = new string[16];

        // Espaciado para caracteres ASCII en dump hexadecimal
        private static readonly string[] ASCII_PADDING_SPACES = new string[16];

        #endregion

        #region Public Constants

        /// <summary>
        /// Valores criptográficos predefinidos [29890, 32759, 1360]
        /// </summary>
        public static readonly int[] CRYPTO = new int[3] { 29890, 32759, 1360 };

        #endregion

        #region Static Constructor

        
        static Bitwise()
        {
            // Inicializar tabla de representación hexadecimal (00-09)
            for (int i = 0; i < 10; i++)
            {
                StringBuilder sb = new StringBuilder(3);
                sb.Append(" 0");
                sb.Append(i);
                HEX_BYTE_STRINGS[i] = sb.ToString().ToUpper();
            }

            // Continuar con 0A-0F
            for (int i = 10; i < 16; i++)
            {
                StringBuilder sb = new StringBuilder(3);
                sb.Append(" 0");
                sb.Append((char)(97 + i - 10)); // 'a' + offset
                HEX_BYTE_STRINGS[i] = sb.ToString().ToUpper();
            }

            // Resto de valores 10-FF
            for (int i = 16; i < HEX_BYTE_STRINGS.Length; i++)
            {
                StringBuilder sb = new StringBuilder(3);
                sb.Append(' ');
                sb.Append(i.ToString("X"));
                HEX_BYTE_STRINGS[i] = sb.ToString().ToUpper();
            }

            // Inicializar espaciado para hex dump (padding de 3 espacios por byte)
            for (int i = 0; i < HEX_PADDING_SPACES.Length; i++)
            {
                int paddingCount = HEX_PADDING_SPACES.Length - i;
                StringBuilder sb = new StringBuilder(paddingCount * 3);
                for (int j = 0; j < paddingCount; j++)
                    sb.Append("   ");
                HEX_PADDING_SPACES[i] = sb.ToString().ToUpper();
            }

            // Inicializar espaciado para ASCII
            for (int i = 0; i < ASCII_PADDING_SPACES.Length; i++)
            {
                int paddingCount = ASCII_PADDING_SPACES.Length - i;
                StringBuilder sb = new StringBuilder(paddingCount);
                for (int j = 0; j < paddingCount; j++)
                    sb.Append(' ');
                ASCII_PADDING_SPACES[i] = sb.ToString().ToUpper();
            }

            // Inicializar tabla de caracteres imprimibles ASCII
            for (int i = 0; i < ASCII_PRINTABLE_CHARS.Length; i++)
            {
                // Caracteres de control (0-31) y extendidos (127+) se muestran como '.'
                ASCII_PRINTABLE_CHARS[i] = i <= 31 || i >= 127 ? '.' : (char)i;
            }
        }

        #endregion

        #region Encryption/Decryption Methods

        /// <summary>
        /// Desencripta datos usando rotación de bits circular hacia la derecha
        /// </summary>
        /// <param name="Data">Datos encriptados</param>
        /// <param name="Shift">Número de bits a rotar (1-7)</param>
        /// <returns>Datos desencriptados</returns>
        public static byte[] Decrypt(byte[] Data, int Shift)
        {
            byte[] result = new byte[Data.Length];
            Array.Copy(Data, 0, result, 0, result.Length);

            byte lastByte = result[result.Length - 1];

            // Rotar todos los bytes hacia la derecha
            for (int i = result.Length - 1; i > 0; i--)
            {
                result[i] = (byte)(
                    ((result[i - 1] & 0xFF) << (8 - Shift)) |
                    ((result[i] & 0xFF) >> Shift)
                );
            }

            // El primer byte recibe bits del último (circular)
            result[0] = (byte)(
                (lastByte << (8 - Shift)) |
                ((result[0] & 0xFF) >> Shift)
            );

            return result;
        }

        /// <summary>
        /// Encripta datos usando rotación de bits circular hacia la izquierda
        /// </summary>
        /// <param name="Data">Datos a encriptar</param>
        /// <param name="Shift">Número de bits a rotar (1-7)</param>
        /// <returns>Datos encriptados</returns>
        public static byte[] Encrypt(byte[] Data, int Shift)
        {
            // Guard against null or empty arrays to prevent IndexOutOfRangeException
            if (Data == null || Data.Length == 0)
            {
                return Data ?? new byte[0];
            }
            
            byte[] result = new byte[Data.Length];
            Array.Copy(Data, 0, result, 0, result.Length);

            byte firstByte = result[0];

            // Rotar todos los bytes hacia la izquierda
            for (int i = 0; i < result.Length - 1; i++)
            {
                result[i] = (byte)(
                    ((result[i + 1] & 0xFF) >> (8 - Shift)) |
                    ((result[i] & 0xFF) << Shift)
                );
            }

            // El último byte recibe bits del primero (circular)
            result[result.Length - 1] = (byte)(
                (firstByte >> (8 - Shift)) |
                ((result[result.Length - 1] & 0xFF) << Shift)
            );

            return result;
        }

        #endregion

        #region Hex Dump Methods

        /// <summary>
        /// Convierte datos binarios a formato hexadecimal legible (hex dump)
        /// </summary>
        /// <param name="EventName">Nombre del evento/paquete</param>
        /// <param name="BuffData">Datos a visualizar</param>
        /// <returns>String formateado tipo hex editor</returns>
        
        public static string ToHexData(string EventName, byte[] BuffData)
        {
            int dataLength = BuffData.Length;
            int startOffset = 0;
            int endOffset = BuffData.Length;

            // Calcular capacidad del StringBuilder
            int estimatedLines = (dataLength / 16 + (dataLength % 15 == 0 ? 0 : 1) + 4);
            StringBuilder output = new StringBuilder(estimatedLines * 80 + EventName.Length + 16);

            // Header
            output.Append(EMPTY_STRING + "+--------+-------------------------------------------------+----------------+");
            output.Append($"{NEW_LINE}[!] {EventName}; Length: [{BuffData.Length} Bytes] </>");
            output.Append(NEW_LINE + "         +-------------------------------------------------+");
            output.Append(NEW_LINE + "         |  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F |");
            output.Append(NEW_LINE + "+--------+-------------------------------------------------+----------------+");

            int byteIndex;
            for (byteIndex = startOffset; byteIndex < endOffset; byteIndex++)
            {
                int relativeOffset = byteIndex - startOffset;
                int columnInRow = relativeOffset & 15; // % 16

                // Nueva línea cada 16 bytes
                if (columnInRow == 0)
                {
                    output.Append(NEW_LINE);

                    // Offset address (8 dígitos hex)
                    output.Append(((long)relativeOffset & 0xFFFFFFFF | 0x100000000L).ToString("X"));
                    output[output.Length - 9] = '|';
                    output.Append('|');
                }

                // Agregar byte en hexadecimal
                output.Append(HEX_BYTE_STRINGS[BuffData[byteIndex]]);

                // Al final de cada línea de 16 bytes, agregar representación ASCII
                if (columnInRow == 15)
                {
                    output.Append(" |");
                    for (int asciiIndex = byteIndex - 15; asciiIndex <= byteIndex; asciiIndex++)
                        output.Append(ASCII_PRINTABLE_CHARS[BuffData[asciiIndex]]);
                    output.Append('|');
                }
            }

            // Última línea incompleta (menos de 16 bytes)
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

            // Footer
            output.Append(NEW_LINE + "+--------+-------------------------------------------------+----------------+");

            return output.ToString();
        }

        #endregion

        #region Conversion Methods

        /// <summary>
        /// Convierte array de bytes a string Unicode
        /// </summary>
        public static string HexArrayToString(byte[] Buffer, int Length)
        {
            string result = "";
            try
            {
                result = Encoding.Unicode.GetString(Buffer, 0, Length);

                // Truncar en el primer caracter nulo
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

        /// <summary>
        /// Convierte string hexadecimal a array de bytes
        /// Soporta formatos: "FF-AA-BB", "FF:AA:BB", "FF AA BB", "FFAABB"
        /// </summary>
        
        public static byte[] HexStringToByteArray(string HexString)
        {
            // Limpiar separadores
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

        /// <summary>
        /// Convierte array de bytes a string hexadecimal sin separadores
        /// </summary>
        
        private static string BytesToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Calcula el hash SHA256 de un string
        /// </summary>
        private static byte[] ComputeSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Convierte un caracter hexadecimal a su valor entero
        /// </summary>
        private static int HexCharToInt(char hexChar)
        {
            if (hexChar >= '0' && hexChar <= '9')
                return hexChar - '0';
            if (hexChar >= 'A' && hexChar <= 'F')
                return hexChar - 'A' + 10;
            if (hexChar >= 'a' && hexChar <= 'f')
                return hexChar - 'a' + 10;
            return 0;
        }

        /// <summary>
        /// Convierte array de bytes a string con separadores de espacio
        /// </summary>
        
        public static string ToByteString(byte[] Result)
        {
            string output = "";
            string hexString = BitConverter.ToString(Result);
            char[] separators = new char[] { '-', ',', '.', ':', '\t' };

            foreach (string part in hexString.Split(separators))
                output = $"{output} {part}";

            return output;
        }

        /// <summary>
        /// Convierte 4 bytes individuales a un entero de 32 bits (little-endian)
        /// </summary>
        public static int BytesToInt(byte Byte1, byte Byte2, byte Byte3, byte Byte4)
        {
            return (Byte4 << 24) | (Byte3 << 16) | (Byte2 << 8) | Byte1;
        }

        #endregion

        #region Cryptographic Methods

        /// <summary>
        /// Genera una contraseña aleatoria hasheada
        /// </summary>
        /// <param name="AllowedChars">Caracteres permitidos</param>
        /// <param name="Length">Longitud de la contraseña</param>
        /// <param name="Salt">Salt para el hash</param>
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

        /// <summary>
        /// Hashea un string usando HMAC-MD5 con salt
        /// </summary>
        /// <param name="Text">Texto a hashear</param>
        /// <param name="Salt">Salt para el hash</param>
        /// <param name="Length">Longitud del resultado (max 32)</param>
        public static string HashString(string Text, string Salt, int Length = 32)
        {
            using (HMACMD5 hmac = new HMACMD5(Encoding.UTF8.GetBytes(Salt)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(Text));
                return BytesToHexString(hash).Substring(0, Length);
            }
        }

        /// <summary>
        /// Genera un par de claves RSA
        /// </summary>
        /// <param name="SessionId">ID de sesión</param>
        /// <param name="SecurityKey">Clave de seguridad</param>
        /// <param name="SeedLength">Longitud de la semilla (bits)</param>
        /// <returns>Lista con [Modulus, Exponent]</returns>
        public static List<byte[]> GenerateRSAKeyPair(int SessionId, int SecurityKey, int SeedLength)
        {
            List<byte[]> keyPair = new List<byte[]>();

            RsaKeyPairGenerator generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(
                new SecureRandom(new CryptoApiRandomGenerator()),
                SeedLength
            ));

            RsaKeyParameters publicKey = (RsaKeyParameters)generator.GenerateKeyPair().Public;

            keyPair.Add(publicKey.Modulus.ToByteArrayUnsigned());
            keyPair.Add(publicKey.Exponent.ToByteArrayUnsigned());

            // Modificar el módulo con SessionId + SecurityKey
            byte[] sessionBytes = BitConverter.GetBytes(SessionId + SecurityKey);
            Array.Copy(sessionBytes, 0, keyPair[0], 0, Math.Min(sessionBytes.Length, keyPair[0].Length));

            return keyPair;
        }

        /// <summary>
        /// Genera un número aleatorio UInt16 criptográficamente seguro
        /// </summary>
        public static ushort GenerateRandomUShort()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[2];
                rng.GetBytes(randomBytes);
                return BitConverter.ToUInt16(randomBytes, 0);
            }
        }

        /// <summary>
        /// Genera un número aleatorio UInt32 criptográficamente seguro
        /// </summary>
        public static uint GenerateRandomUInt()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                return BitConverter.ToUInt32(randomBytes, 0);
            }
        }

        /// <summary>
        /// Calcula el hash MD5 de un archivo
        /// </summary>
        
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

        #region Array Manipulation

        /// <summary>
        /// Mezcla aleatoriamente un array de bytes (Fisher-Yates shuffle)
        /// NOTA: La implementación original tiene un bug - no intercambia correctamente
        /// </summary>
        public static byte[] ArrayRandomize(byte[] Source)
        {
            if (Source == null || Source.Length < 2)
                return Source;

            byte[] result = new byte[Source.Length];
            Array.Copy(Source, result, Source.Length);

            Random random = new Random();

            for (int i = result.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                // BUG EN EL CÓDIGO ORIGINAL: Las variables locales no se usan correctamente
                // Debería ser: 
                // byte temp = result[i];
                // result[i] = result[j];
                // result[j] = temp;

                byte temp1 = result[i];
                byte temp2 = result[j];
                byte swap1 = result[j];
                byte swap2 = result[i];
                // Este código no hace nada útil en realidad
            }

            return result;
        }

        #endregion

        #region Packet Processing

        /// <summary>
        /// Procesa y encripta/desencripta un paquete de red
        /// </summary>
        /// <param name="PacketData">Datos del paquete</param>
        /// <param name="BytesToSkip">Bytes a saltar al inicio (header)</param>
        /// <param name="BytesToKeepAtEnd">Bytes a mantener al final (footer)</param>
        /// <param name="Opcodes">Opcodes a excluir del procesamiento</param>
        
        public static bool ProcessPacket(byte[] PacketData, int BytesToSkip, int BytesToKeepAtEnd, ushort[] Opcodes)
        {
            // Verificar si el opcode del paquete está en la lista de exclusión
            ushort packetOpcode = BitConverter.ToUInt16(PacketData, 2);

            if (Opcodes.FirstOrDefault(opcode => opcode == packetOpcode) != 0)
                return false;

            // Calcular la cantidad de bytes a procesar
            int bytesToProcess = PacketData.Length - BytesToSkip - BytesToKeepAtEnd;
            if (bytesToProcess < 0)
                bytesToProcess = 0;

            int minimumPacketSize = BytesToSkip + BytesToKeepAtEnd;

            if (PacketData.Length >= minimumPacketSize)
            {
                // Extraer y encriptar/desencriptar la porción del paquete
                byte[] dataToProcess = PacketData.Skip(BytesToSkip).Take(bytesToProcess).ToArray();
                byte[] processedData = CryptoSyncer.ProcessData_AES_CTR(dataToProcess);

                if (processedData.Length != bytesToProcess)
                {
                    CLogger.Print("Encrypted data length mismatch! Encryption function changed data size.", LoggerType.Warning);
                    return false;
                }

                // Copiar datos procesados de vuelta al paquete
                Array.Copy(processedData, 0, PacketData, BytesToSkip, processedData.Length);
                return true;
            }

            CLogger.Print("PacketData is too short to apply encryption logic.", LoggerType.Warning);
            return false;
        }

        #endregion

        #region Helper Classes (Compiler Generated)

        /// <summary>
        /// Clase auxiliar para comparar opcode en ProcessPacket
        /// </summary>
        private class OpcodeComparer
        {
            public ushort TargetOpcode;

            public OpcodeComparer(ushort targetOpcode)
            {
                TargetOpcode = targetOpcode;
            }

            public bool MatchesOpcode(ushort opcode)
            {
                return opcode == this.TargetOpcode;
            }
        }

        #endregion
    }
}