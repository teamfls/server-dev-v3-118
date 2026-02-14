// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Settings.ConfigEngine
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Plugin.Core.Settings
{
    public class ConfigEngine
    {
        private readonly FileInfo Field0;
        private readonly FileAccess Field1;
        private readonly string Field2 = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(
          string A_0,
          string A_1,
          string A_2,
          string A_3);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
          string A_0,
          string A_1,
          string A_2,
          StringBuilder A_3,
          int A_4,
          string A_5);

        public ConfigEngine(string A_1 = null, FileAccess A_2 = FileAccess.ReadWrite)
        {
            this.Field1 = A_2;
            this.Field0 = new FileInfo(A_1 ?? this.Field2);
        }

        
        public byte ReadC(string Key, byte Defaultprop, string Section = null)
        {
            try
            {
                return byte.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public short ReadH(string Key, short Defaultprop, string Section = null)
        {
            try
            {
                return short.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public ushort ReadUH(string Key, ushort Defaultprop, string Section = null)
        {
            try
            {
                return ushort.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public int ReadD(string Key, int Defaultprop, string Section = null)
        {
            try
            {
                return int.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public uint ReadUD(string Key, uint Defaultprop, string Section = null)
        {
            try
            {
                return uint.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public long ReadQ(string Key, long Defaultprop, string Section = null)
        {
            try
            {
                return long.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public ulong ReadUQ(string Key, ulong Defaultprop, string Section = null)
        {
            try
            {
                return ulong.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public double ReadF(string Key, double Defaultprop, string Section = null)
        {
            try
            {
                return double.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public float ReadT(string Key, float Defaultprop, string Section = null)
        {
            try
            {
                return float.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public bool ReadX(string Key, bool Defaultprop, string Section = null)
        {
            try
            {
                return bool.Parse(this.Method0(Key, Section));
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        public string ReadS(string Key, string Defaultprop, string Section = null)
        {
            try
            {
                return this.Method0(Key, Section);
            }
            catch
            {
                CLogger.Print("Read Parameter Failure: " + Key, LoggerType.Warning);
                return Defaultprop;
            }
        }

        
        private string Method0(string A_1, string A_2 = null)
        {
            StringBuilder A_3 = new StringBuilder(65025);
            if (this.Field1 == FileAccess.Write)
                throw new Exception("Can`t read the file! No access!");
            ConfigEngine.GetPrivateProfileString(A_2 ?? this.Field2, A_1, "", A_3, 65025, this.Field0.FullName);
            return A_3.ToString();
        }

        
        public void WriteC(string Key, byte Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteH(string Key, short Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteH(string Key, ushort Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteD(string Key, int Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteD(string Key, uint Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteQ(string Key, long Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteQ(string Key, ulong Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteF(string Key, double Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteT(string Key, float Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteX(string Key, bool Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value.ToString(), Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        public void WriteS(string Key, string Value, string Section = null)
        {
            try
            {
                this.Method1(Key, Value, Section);
            }
            catch
            {
                CLogger.Print("Write Parameter Failure: " + Key, LoggerType.Warning);
            }
        }

        
        private void Method1(string A_1, string A_2, string A_3 = null)
        {
            if (this.Field1 == FileAccess.Read)
                throw new Exception("Can`t write to file! No access!");
            ConfigEngine.WritePrivateProfileString(A_3 ?? this.Field2, A_1, " " + A_2, this.Field0.FullName);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            this.Method1(Key, (string)null, Section ?? this.Field2);
        }

        public void DeleteSection(string Section = null)
        {
            this.Method1((string)null, (string)null, Section ?? this.Field2);
        }

        public bool KeyExists(string Key, string Section = null) => this.Method0(Key, Section).Length > 0;
    }
}