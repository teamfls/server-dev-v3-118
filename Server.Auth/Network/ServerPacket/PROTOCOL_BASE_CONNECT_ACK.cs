using Plugin.Core;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;
using Plugin.Core.Enums;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_BASE_CONNECT_ACK : AuthServerPacket
    {
        private readonly int SessionId;
        private readonly ushort SessionSeed;
        private readonly byte[] Modulus;
        private readonly byte[] Exponent;

        public PROTOCOL_BASE_CONNECT_ACK(AuthClient client)
        {
            this.SessionId = client.SessionId;
            this.SessionSeed = client.SessionSeed;

            byte[] privateKeyObj;
            Bitwise.GenerateRSAKeyPairRaw(1360, out Modulus, out Exponent, out privateKeyObj);
            client.RSAPrivateKey = privateKeyObj;

            byte[] sharedKey = new byte[16];
            Array.Copy(Modulus, 16, sharedKey, 0, 16);

            client.CMessEncryptKey = sharedKey; 
            client.CMessDecryptKey = sharedKey; 
            client.IsCMessReady = false;

            if (ConfigLoader.DebugMode)
            {
                Console.WriteLine("\n[+] ====== CONNECT_ACK SENT ======");
                Console.WriteLine($"[+] SessionId   : {this.SessionId}");
                Console.WriteLine($"[+] RSA Modulus : {Modulus.Length} bytes");
                Console.WriteLine($"[+] SharedKey   : {BitConverter.ToString(sharedKey).Replace("-", " ")}");
                Console.WriteLine("==================================\n");
            }
        }

        public override void Write()
        {
            WriteH((short)2306);
            WriteH((short)0);
            WriteC((byte)11);
            WriteB(new byte[11]);
            WriteH((ushort)(Modulus.Length + Exponent.Length));
            WriteH((ushort)Modulus.Length);
            WriteB(Modulus);
            WriteB(Exponent);
            WriteH((short)117);
            WriteC((byte)0);
            WriteH(SessionSeed);
            WriteD(SessionId);
            CLogger.Print($"[+] CONNECT_ACK: SessionId={SessionId}, SessionSeed={SessionSeed}, Modulus={Modulus.Length}b", LoggerType.Debug);
        }
    }
}