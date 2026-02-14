// Decompiled with JetBrains decompiler
// Type: Server.Match.Data.Utils.AllUtils
// Assembly: Server.Match, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: CE18A1E1-67C7-4FA9-8510-2DD553448D5A
// Assembly location: C:\Users\home\Desktop\dll\Server.Match-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Network;
using Plugin.Core.SharpDX;
using Plugin.Core.Utility;
using Server.Match.Data.Enums;
using Server.Match.Data.Models;
using Server.Match.Data.Models.Event;
using Server.Match.Data.XML;
using System;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;


namespace Server.Match.Data.Utils
{
    public static class AllUtils
    {
        public static float GetDuration(DateTime Date)
        {
            return (float)(DateTimeUtil.Now() - Date).TotalSeconds;
        }

        public static ItemClass ItemClassified(ClassType ClassWeapon)
        {
            ItemClass itemClass = ItemClass.Unknown;
            switch (ClassWeapon)
            {
                case ClassType.Knife:
                case ClassType.DualKnife:
                case ClassType.Knuckle:
                    itemClass = ItemClass.Melee;
                    break;
                case ClassType.HandGun:
                case ClassType.CIC:
                case ClassType.DualHandGun:
                    itemClass = ItemClass.Secondary;
                    break;
                case ClassType.Assault:
                    itemClass = ItemClass.Primary;
                    break;
                case ClassType.SMG:
                case ClassType.DualSMG:
                    itemClass = ItemClass.Primary;
                    break;
                case ClassType.Sniper:
                    itemClass = ItemClass.Primary;
                    break;
                case ClassType.Shotgun:
                case ClassType.DualShotgun:
                    itemClass = ItemClass.Primary;
                    break;
                case ClassType.ThrowingGrenade:
                    itemClass = ItemClass.Explosive;
                    break;
                case ClassType.ThrowingSpecial:
                    itemClass = ItemClass.Special;
                    break;
                case ClassType.Machinegun:
                    itemClass = ItemClass.Primary;
                    break;
                case ClassType.Dino:
                    itemClass = ItemClass.Unknown;
                    break;
            }
            return itemClass;
        }

        public static ObjectType GetHitType(uint HitInfo) => (ObjectType)((int)HitInfo & 3);

        public static int GetHitWho(uint HitInfo) => (int)(HitInfo >> 2) & 511 /*0x01FF*/;

        public static CharaHitPart GetHitPart(uint HitInfo)
        {
            return (CharaHitPart)((int)(HitInfo >> 11) & 63 /*0x3F*/);
        }

        public static int GetHitDamageBot(uint HitInfo) => (int)(HitInfo >> 20);

        public static int GetHitDamageNormal(uint HitInfo) => (int)(HitInfo >> 21);

        public static int GetHitHelmet(uint info) => (int)(info >> 17) & 7;

        public static CharaDeath GetCharaDeath(uint HitInfo) => (CharaDeath)((int)HitInfo & 15);

        public static int GetKillerId(uint HitInfo) => (int)(HitInfo >> 11) & 511 /*0x01FF*/;

        public static int GetObjectType(uint HitInfo) => (int)(HitInfo >> 10) & 1;

        public static int GetRoomInfo(uint UniqueRoomId, int Type)
        {
            switch (Type)
            {
                case 0:
                    return (int)UniqueRoomId & 4095 /*0x0FFF*/;
                case 1:
                    return (int)(UniqueRoomId >> 12) & (int)byte.MaxValue;
                case 2:
                    return (int)(UniqueRoomId >> 20) & 4095 /*0x0FFF*/;
                default:
                    return 0;
            }
        }

        public static int GetSeedInfo(uint Seed, int Type)
        {
            switch (Type)
            {
                case 0:
                    return (int)Seed & 4095 /*0x0FFF*/;
                case 1:
                    return (int)(Seed >> 12) & (int)byte.MaxValue;
                case 2:
                    return (int)(Seed >> 20) & 4095 /*0x0FFF*/;
                default:
                    return 0;
            }
        }

        public static byte[] BaseWriteCode(
          int Opcode,
          byte[] Actions,
          int SlotId,
          float Time,
          int Round,
          int Respawn,
          int RoundNumber,
          int AccountId)
        {
            // Guard against null or empty Actions array to prevent IndexOutOfRangeException
            if (Actions == null || Actions.Length == 0)
            {
                return new byte[0];
            }
            
            int Shift = (17 + Actions.Length) % 6 + 1;
            byte[] numArray = Bitwise.Encrypt(Actions, Shift);
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)Opcode);
                syncServerPacket.WriteC((byte)SlotId);
                syncServerPacket.WriteT(Time);
                syncServerPacket.WriteC((byte)Round);
                syncServerPacket.WriteH((ushort)(17 + numArray.Length));
                syncServerPacket.WriteC((byte)Respawn);
                syncServerPacket.WriteC((byte)RoundNumber);
                syncServerPacket.WriteC((byte)AccountId);
                syncServerPacket.WriteC((byte)0);
                syncServerPacket.WriteD(0);
                syncServerPacket.WriteB(numArray);
                return syncServerPacket.ToArray();
            }
        }

        
        public static bool ValidateHitData(int RawDamage, HitDataInfo Hit, out int Damage)
        {
            if (!ConfigLoader.AntiScript)
            {
                Damage = RawDamage;
                return true;
            }
            ItemsStatistic itemStats = ItemStatisticXML.GetItemStats(Hit.WeaponId);
            if (itemStats == null)
            {
                CLogger.Print($"The Item Statistic was not found. Please add: {Hit.WeaponId} to config!", LoggerType.Warning);
                Damage = 0;
                return false;
            }
            ItemClass itemClass = AllUtils.ItemClassified(Hit.WeaponClass);
            float num1 = Vector3.Distance((Vector3)Hit.StartBullet, (Vector3)Hit.EndBullet);
            if (itemClass == ItemClass.Melee || (double)num1 <= (double)itemStats.Range)
            {
                if (itemClass == ItemClass.Melee && (double)num1 > (double)itemStats.Range)
                {
                    Damage = 0;
                    return false;
                }
                if (AllUtils.GetHitPart(Hit.HitIndex) != CharaHitPart.HEAD)
                {
                    int num2 = itemStats.Damage + itemStats.Damage * 30 / 100;
                    if (itemClass != ItemClass.Melee && RawDamage > num2)
                    {
                        Damage = 0;
                        return false;
                    }
                    if (itemClass == ItemClass.Melee && RawDamage > itemStats.Damage)
                    {
                        Damage = 0;
                        return false;
                    }
                }
                Damage = RawDamage;
                return true;
            }
            Damage = 0;
            return false;
        }

        
        public static bool ValidateGrenadeHit(int RawDamage, GrenadeHitInfo Hit, out int Damage)
        {
            if (!ConfigLoader.AntiScript)
            {
                Damage = RawDamage * 2;
                return true;
            }
            ItemsStatistic itemStats = ItemStatisticXML.GetItemStats(Hit.WeaponId);
            if (itemStats != null)
            {
                int num1 = (int)AllUtils.ItemClassified(Hit.WeaponClass);
                float num2 = Vector3.Distance((Vector3)Hit.FirePos, (Vector3)Hit.HitPos);
                if (num1 == 4)
                {
                    if ((double)num2 <= (double)itemStats.Range)
                    {
                        if (RawDamage > itemStats.Damage)
                        {
                            Damage = 0;
                            return false;
                        }
                    }
                    else
                    {
                        Damage = 0;
                        return false;
                    }
                }
                Damage = RawDamage * 2;
                return true;
            }
            CLogger.Print($"The Item Statistic was not found. Please add: {Hit.WeaponId} to config!", LoggerType.Warning);
            Damage = 0;
            return false;
        }

        
        public static void GetDecryptedData(PacketModel Packet)
        {
            try
            {
                if (Packet.Data.Length >= Packet.Length)
                {
                    byte[] numArray = new byte[Packet.Length - 17];
                    Array.Copy((Array)Packet.Data, 17, (Array)numArray, 0, numArray.Length);
                    byte[] sourceArray = Bitwise.Decrypt(numArray, Packet.Length % 6 + 1);
                    byte[] destinationArray = new byte[sourceArray.Length - 9];
                    Array.Copy((Array)sourceArray, (Array)destinationArray, destinationArray.Length);
                    Packet.WithEndData = sourceArray;
                    Packet.WithoutEndData = destinationArray;
                }
                else
                    CLogger.Print($"Invalid packet size. (Packet.Data.Length >= Packet.Length): [ {Packet.Data.Length} | {Packet.Length} ]", LoggerType.Warning);
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        public static void CheckDataFlags(ActionModel Action, PacketModel Packet)
        {
            UdpGameEvent flag = Action.Flag;
            if (!flag.HasFlag((Enum)UdpGameEvent.WeaponSync) || Packet.Opcode == 4 || (flag & (UdpGameEvent.DropWeapon | UdpGameEvent.GetWeaponForClient)) <= (UdpGameEvent)0)
                return;
            Action.Flag -= UdpGameEvent.WeaponSync;
        }

        public static int PingTime(
          string Address,
          byte[] Buffer,
          int TTL,
          int TimeOut,
          bool IsFragmented,
          out int Ping)
        {
            int A_0 = 0;
            try
            {
                PingOptions options = new PingOptions()
                {
                    Ttl = TTL,
                    DontFragment = IsFragmented
                };
                using (Ping ping = new Ping())
                {
                    PingReply pingReply = ping.Send(Address, TimeOut, Buffer, options);
                    if (pingReply.Status == IPStatus.Success)
                        A_0 = Convert.ToInt32(pingReply.RoundtripTime);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            Ping = (int)AllUtils.StaticMethod0(A_0);
            return A_0;
        }

        private static byte StaticMethod0(int A_0)
        {
            if (A_0 <= 100)
                return 5;
            if (A_0 >= 100 && A_0 <= 200)
                return 4;
            if (A_0 >= 200 && A_0 <= 300)
                return 3;
            if (A_0 >= 300 && A_0 <= 400)
                return 2;
            return A_0 >= 400 && A_0 <= 500 ? (byte)1 : (byte)0;
        }

        public static TeamEnum GetSwappedTeam(PlayerModel Player, RoomModel Room)
        {
            if (Player == null || Room == null)
                return TeamEnum.TEAM_DRAW;
            TeamEnum swappedTeam = Player.Team;
            if (Room.IsTeamSwap)
                swappedTeam = swappedTeam == TeamEnum.FR_TEAM ? TeamEnum.CT_TEAM : TeamEnum.FR_TEAM;
            return swappedTeam;
        }
    }
}