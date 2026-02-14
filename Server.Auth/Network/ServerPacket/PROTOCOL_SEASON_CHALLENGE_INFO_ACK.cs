// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_SEASON_CHALLENGE_INFO_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Managers;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.XML;
using Server.Auth.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_SEASON_CHALLENGE_INFO_ACK : AuthServerPacket
    {
        private readonly List<BattlePassCardData> cardDataList;
        private readonly BattlePassSeason activeSeason;
        private readonly Account Player;
        private readonly string seasonName;
        private readonly uint startDate;
        private readonly uint endDate;
        private readonly int isPremium;

        public PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Account A_1)
        {
            if (A_1 == null)
                return;

            this.Player = A_1;

            // Cargar los datos del pase de batalla actual
            this.cardDataList = BattlePassManager.GetCards();

            // Obtener la temporada activa
            this.activeSeason = BattlePassManager.GetActiveSeason();

            if (activeSeason != null)
            {
                // Usar datos de la temporada activa
                seasonName = activeSeason.SeasonName;
                startDate = uint.Parse(activeSeason.SeasonStartDate);
                endDate = uint.Parse(activeSeason.SeasonEndDate);
            }

            isPremium = (A_1.Battlepass.HavePremium) ? 1 : 0;
        }

        public override void Write()
        {
            this.WriteH((short)8450);
            this.WriteH((short)0);

            if (Player == null || cardDataList == null || cardDataList.Count == 0)
            {
                CLogger.Print("Error: Datos inválidos para el paquete BattlePass", LoggerType.Error);
                return;
            }

            // Calcular información de nivel del jugador
            var levelInfo = BattlePass.GetLevelInfoForSeason((int)Player.Battlepass.EarnedPoints);
            int currentLevel = levelInfo.currentLevel;
            int completedLevel = levelInfo.completedLevel;

            // Cálculo del estado premium
            int premiumComplete = (Player.Battlepass.HavePremium) ? completedLevel : 0;

            // Niveles restantes para 99 niveles totales
            int maxLevels = 99;
            int remainingLevels = Math.Max(0, maxLevels - cardDataList.Count);

            if (activeSeason != null)
            {
                this.WriteD(activeSeason.SeasonEnabled);
                this.WriteC((byte)currentLevel);
                this.WriteD(Player.Battlepass.EarnedPoints);
                this.WriteC((byte)completedLevel);
                this.WriteC((byte)premiumComplete);
                this.WriteC((byte)isPremium);
                this.WriteC((byte)0);
                this.WriteD(20250307);
                this.WriteD(0);
                this.WriteD(1);

                // Estado de la temporada
                byte seasonStatus = (byte)(Player.Battlepass.BattlepassNormalLevel >= cardDataList.Count ? 0 :
                                          activeSeason.SeasonEnabled == 1 ? 1 : 2);
                this.WriteC(seasonStatus);

                // Nombre de la temporada (limitado a 21 caracteres)
                string displayName = seasonName;
                if (displayName.Length > 21)
                {
                    displayName = displayName.Substring(0, 21);
                }
                this.WriteU(displayName, 42);

                this.WriteH((short)cardDataList.Count);
                this.WriteD(500);
                this.WriteD(0);

                // Escribir las cartas del pase de batalla
                foreach (var card in cardDataList.OrderBy(c => c.Number))
                {
                    // Escribir las cartas según la configuración
                    if (activeSeason.SeasonEnabledForFree && activeSeason.SeasonEnabledForPremium)
                    {
                        this.WriteD(card.NormalCard);
                        this.WriteD(card.PremiumCardA);
                        this.WriteD(card.PremiumCardB);
                    }
                    else if (activeSeason.SeasonEnabledForFree && !activeSeason.SeasonEnabledForPremium)
                    {
                        this.WriteD(card.NormalCard);
                        this.WriteD(0x00000000);
                        this.WriteD(0x00000000);
                    }
                    else if (!activeSeason.SeasonEnabledForFree && activeSeason.SeasonEnabledForPremium)
                    {
                        this.WriteD(0x00000000);
                        this.WriteD(card.PremiumCardA);
                        this.WriteD(card.PremiumCardB);
                    }
                    else
                    {
                        this.WriteD(0x00000000);
                        this.WriteD(0x00000000);
                        this.WriteD(0x00000000);
                    }

                    this.WriteD(card.RequiredExp);
                }

                // Rellenar niveles restantes
                for (int i = 0; i < remainingLevels; i++)
                {
                    this.WriteB(new byte[16]);
                }

                this.WriteD(0);
                this.WriteD(0);
                this.WriteD(0);

                this.WriteD((int)startDate);
                this.WriteD((int)endDate);

                // Calcular progreso del nivel actual
                var currentLevelCard = cardDataList.OrderBy(c => c.Number).ElementAtOrDefault(completedLevel);
                var nextLevelCard = cardDataList.OrderBy(c => c.Number).ElementAtOrDefault(completedLevel + 1);

                int currentLevelPoints = 0;
                int pointsForNextLevel = 0;

                if (currentLevelCard != null)
                {
                    currentLevelPoints = Player.Battlepass.EarnedPoints - currentLevelCard.RequiredExp;
                }

                if (nextLevelCard != null && currentLevelCard != null)
                {
                    pointsForNextLevel = nextLevelCard.RequiredExp - currentLevelCard.RequiredExp;
                }

                this.WriteD(currentLevelPoints);
                this.WriteD(pointsForNextLevel);
            }
        }
    }
}