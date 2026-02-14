// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CHAR_CHANGE_EQUIP_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CHAR_CHANGE_EQUIP_REQ : GameClientPacket
    {
        private int Field0;
        private bool Field1;
        private bool Field2;
        private bool Field3;
        private bool Field4;
        private bool Field5;
        private readonly int[] Field6 = new int[2];
        private readonly int[] Field7 = new int[2];
        private readonly int[] Field8 = new int[13];
        private readonly SortedList<int, int> Field9 = new SortedList<int, int>();
        private readonly SortedList<int, int> Field10 = new SortedList<int, int>();
        private readonly SortedList<int, int> Field11 = new SortedList<int, int>();

        public override void Read()
        {
            this.Field0 = this.ReadD();
            int num1 = (int)this.ReadUD();
            this.Field1 = this.ReadC() == (byte)1;
            byte num2 = this.ReadC();
            for (byte key = 0; (int)key < (int)num2; ++key)
            {
                int num3 = this.ReadD();
                this.Field9.Add((int)key, num3);
            }
            this.Field2 = this.ReadC() == (byte)1;
            int num4 = (int)this.ReadC();
            byte num5 = this.ReadC();
            for (byte key = 0; (int)key < (int)num5; ++key)
            {
                int num6 = this.ReadD();
                this.Field10.Add((int)key, num6);
            }
            this.Field3 = this.ReadC() == (byte)1;
            int num7 = (int)this.ReadC();
            int num8 = (int)this.ReadC();
            this.Field8[0] = this.ReadD();
            int num9 = (int)this.ReadUD();
            this.Field8[1] = this.ReadD();
            int num10 = (int)this.ReadUD();
            this.Field8[2] = this.ReadD();
            int num11 = (int)this.ReadUD();
            this.Field8[3] = this.ReadD();
            int num12 = (int)this.ReadUD();
            this.Field8[4] = this.ReadD();
            int num13 = (int)this.ReadUD();
            this.Field6[0] = this.ReadD();
            int num14 = (int)this.ReadUD();
            this.Field8[5] = this.ReadD();
            int num15 = (int)this.ReadUD();
            this.Field8[6] = this.ReadD();
            int num16 = (int)this.ReadUD();
            this.Field8[7] = this.ReadD();
            int num17 = (int)this.ReadUD();
            this.Field8[8] = this.ReadD();
            int num18 = (int)this.ReadUD();
            this.Field8[9] = this.ReadD();
            int num19 = (int)this.ReadUD();
            this.Field8[10] = this.ReadD();
            int num20 = (int)this.ReadUD();
            this.Field8[11] = this.ReadD();
            int num21 = (int)this.ReadUD();
            this.Field8[12] = this.ReadD();
            int num22 = (int)this.ReadUD();
            this.Field6[1] = this.ReadD();
            int num23 = (int)this.ReadUD();
            this.Field4 = this.ReadC() == (byte)1;
            byte num24 = this.ReadC();
            for (byte key = 0; (int)key < (int)num24; ++key)
            {
                int num25 = this.ReadD();
                int num26 = (int)this.ReadUD();
                this.Field11.Add((int)key, num25);
            }
            this.Field5 = this.ReadC() == (byte)1;
            int num27 = (int)this.ReadC();
            this.Field7[0] = (int)this.ReadC();
            this.Field7[1] = (int)this.ReadC();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                if (player.Character.Characters.Count > 0)
                {
                    if (this.Field1)
                        AllUtils.ValidateAccesoryEquipment(player, this.Field0);
                    if (this.Field2)
                        AllUtils.ValidateDisabledCoupon(player, this.Field9);
                    if (this.Field3)
                        AllUtils.ValidateEnabledCoupon(player, this.Field10);
                    if (this.Field4)
                        AllUtils.ValidateCharacterEquipment(player, player.Equipment, this.Field8, this.Field6, this.Field7);
                    if (this.Field5)
                        AllUtils.ValidateItemEquipment(player, this.Field11);
                    AllUtils.ValidateCharacterSlot(player, player.Equipment, this.Field7);
                }
                RoomModel room = player.Room;
                if (room != null)
                    AllUtils.UpdateSlotEquips(player, room);
                this.Client.SendPacket(new PROTOCOL_CHAR_CHANGE_EQUIP_ACK(0U));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}