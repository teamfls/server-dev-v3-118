// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Utility.ColorUtil
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using System.Drawing;
using System.Runtime.CompilerServices;

namespace Plugin.Core.Utility
{
    public class ColorUtil
    {
        public static Color White = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#FFFFFF"));
        public static Color Black = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#000000"));
        public static Color Red = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#FF0000"));
        public static Color Green = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#00FF00"));
        public static Color Blue = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#0000FF"));
        public static Color Yellow = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#FFFF00"));
        public static Color Fuchsia = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#FF00FF"));
        public static Color Cyan = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#00FFFF"));
        public static Color Silver = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#C0C0C0"));
        public static Color LightGrey = Color.FromArgb((int)byte.MaxValue, ColorTranslator.FromHtml("#D3D3D3"));

        
        static ColorUtil()
        {
        }
    }
}