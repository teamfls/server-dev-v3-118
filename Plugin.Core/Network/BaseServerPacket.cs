// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Network.BaseServerPacket
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Plugin.Core.Network
{
    public abstract class BaseServerPacket
    {
        protected MemoryStream MStream;
        protected BinaryWriter BWriter;
        protected SafeHandle Handle;
        protected bool Disposed;
        protected int SECURITY_KEY;
        protected int HASH_CODE;
        protected int SEED_LENGTH;
        protected NationsEnum NATIONS;

        protected internal void WriteB(byte[] Value, int Offset, int Length)
        {
            this.BWriter.Write(Value, Offset, Length);
        }

        protected internal void WriteB(byte[] Value) => this.BWriter.Write(Value);

        protected internal void WriteC(byte Value) => this.BWriter.Write(Value);

        protected internal void WriteH(ushort Value) => this.BWriter.Write(Value);

        protected internal void WriteH(short Value) => this.BWriter.Write(Value);

        protected internal void WriteD(uint Value) => this.BWriter.Write(Value);

        protected internal void WriteD(int Value) => this.BWriter.Write(Value);

        protected internal void WriteT(float Value) => this.BWriter.Write(Value);

        protected internal void WriteF(double Value) => this.BWriter.Write(Value);

        protected internal void WriteQ(ulong Value) => this.BWriter.Write(Value);

        protected internal void WriteQ(long Value) => this.BWriter.Write(Value);

        protected internal void WriteN(string Text, int Count, string CodePage)
        {
            if (Text == null)
                return;
            this.WriteB(Encoding.GetEncoding(CodePage).GetBytes(Text));
            this.WriteB(new byte[Count - Text.Length]);
        }

        protected internal void WriteS(string Text, int Count)
        {
            if (Text == null)
                return;
            this.WriteB(Encoding.UTF8.GetBytes(Text));
            this.WriteB(new byte[Count - Text.Length]);
        }

        protected internal void WriteU(string Text, int Count)
        {
            if (Text == null)
                return;
            this.WriteB(Encoding.Unicode.GetBytes(Text));
            this.WriteB(new byte[Count - Text.Length * 2]);
        }
    }
}