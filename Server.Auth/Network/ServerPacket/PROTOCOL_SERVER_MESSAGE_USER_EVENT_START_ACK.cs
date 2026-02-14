// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_SERVER_MESSAGE_USER_EVENT_START_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll


namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_SERVER_MESSAGE_USER_EVENT_START_ACK : AuthServerPacket
    {
        public override void Write() => this.WriteH((short)3086);
    }
}