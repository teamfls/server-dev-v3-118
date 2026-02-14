using Executable.Utility;
using Plugin.Core;
using Plugin.Core.Filters;
using Plugin.Core.JSON;
using Plugin.Core.Managers;
using Plugin.Core.RAW;
using Plugin.Core.Settings;
using Plugin.Core.XML;
using Server.Game;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Executable.Forms
{
    public partial class FormConfig : Form
    {
        private readonly DirectoryInfo ServerDir;
        public FormConfig(DirectoryInfo ServerDir)
        {
            InitializeComponent();
            this.ServerDir = ServerDir;
        }
        #region Attachments
        private void MoreInfoStatus(bool High, bool Default)
        {
            HighRB.Checked = High;
            DefaultRB.Checked = Default;
        }
        private static void PrintSection(string Name, string Type)
        {
            StringBuilder ST = new StringBuilder(80);
            if (Name != null)
            {
                ST.Append("---[").Append(Name).Append(']');
            }
            string End = (Type == null) ? "" : ($"[{Type}]---");
            int Count = 79 - End.Length;
            while (ST.Length != Count)
            {
                ST.Append('-');
            }
            ST.Append(End);
            Console.WriteLine($"{(Type.Equals("Ended") ? $"{ST}\n" : $"\n{ST}")}");
        }
        #endregion Attachments
        private void FormConfig_Load(object sender, EventArgs e)
        {
            float OpacityValue = float.Parse("10") / 100;
            BackgroundImage = ImageUtility.GetInstance().ChangeOpacity(Properties.Resources.PBLOGO_256, OpacityValue);
            BackgroundImageLayout = ImageLayout.Center;
            if (ConfigLoader.ShowMoreInfo)
            {
                MoreInfoStatus(true, false);
            }
            else
            {
                MoreInfoStatus(false, true);
            }
        }
        private void OpenConfigBTN_Click(object sender, EventArgs e)
        {
            new Thread(() => MemoryUtility.ShellMode(@"Config/Settings.ini", "notepad.exe", "open")).Start();
        }
        private void ChangeLogBTN_Click(object sender, EventArgs e)
        {
            ConfigEngine CFG = new ConfigEngine("Config/Settings.ini");
            if (!ConfigLoader.ShowMoreInfo)
            {
                ConfigLoader.ShowMoreInfo = true;
                MoreInfoStatus(true, false);
            }
            else
            {
                ConfigLoader.ShowMoreInfo = false;
                MoreInfoStatus(false, true);
            }
            new Thread(() =>
            {
                string Key = "MoreInfo", Section = "Server";
                if (!CFG.KeyExists(Key, Section))
                {
                    MessageBox.Show($"Key: '{Key}' on Section '{Section}' doesn't exist!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                CFG.WriteX(Key, ConfigLoader.ShowMoreInfo, Section);
            }).Start();
            MessageBox.Show($"Logs mode changed to {(ConfigLoader.ShowMoreInfo ? "High." : "Default.")}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ClearLogBTN_Click(object sender, EventArgs e)
        {
            new Thread(() => MemoryUtility.ClearCache(ServerDir)).Start();
            MessageBox.Show("Logs has been cleared!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void Reload1BTN_Click(object sender, EventArgs e)
        {
            new Thread(() => 
            {
                PrintSection("Config", "Begin");
                ServerConfigJSON.Reload();
                CommandHelperJSON.Reload();
                ResolutionJSON.Reload();
                PrintSection("Config", "Ended");
            }).Start();
            MessageBox.Show("Config successfully Reloaded!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void Reload2BTN_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                PrintSection("Shop Data", "Begin");
                ShopManager.Reset();
                ShopManager.Load(1);
                ShopManager.Load(2);
                PrintSection("Shop Data", "Ended");
                GameXender.UpdateShop();
            }).Start();
            MessageBox.Show("Shop Sucessfully reloaded.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void Reload3BTN_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                PrintSection("Events Data", "Begin");
                //EventLoginJSON.Reload();
                EventLoginXML.Reload();
                //EventBoostJSON.Reload();
                EventBoostXML.Reload();
                //EventPlaytimeJSON.Reload();
                EventPlaytimeJSON.Reload();
                //EventQuestJSON.Reload();
                EventQuestXML.Reload();
                //EventRankUpJSON.Reload();
                EventRankUpXML.Reload();
                //EventVisitJSON.Reload();
                EventVisitXML.Reload();
                //EventXmasJSON.Reload();
                EventXmasXML.Reload();
                PrintSection("Events Data", "Ended");
                GameXender.UpdateEvents();
            }).Start();
            MessageBox.Show("Events Sucessfully reloaded.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void Reload4BTN_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                PrintSection("Classic Mode", "Begin");
                GameRuleXML.Reload();
                PrintSection("Classic Mode", "Ended");
            }).Start();
            MessageBox.Show("Tournament Rule Sucessfully reloaded.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void Reload5BTN_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                PrintSection("Server Data", "Begin");
                TemplatePackXML.Reload();
                TitleSystemXML.Reload();
                TitleAwardXML.Reload();
                MissionAwardXML.Reload();
                MissionConfigXML.Reload();
                SChannelXML.Reload();
                SynchronizeXML.Reload();
                SystemMapXML.Reload();
                ClanRankXML.Reload();
                PlayerRankXML.Reload();
                CouponEffectXML.Reload();
                PermissionXML.Reload();
                RandomBoxXML.Reload();
                DirectLibraryXML.Reload();
                InternetCafeXML.Reload();
                RedeemCodeXML.Reload();
                NickFilter.Reload();
                Server.Auth.Data.XML.ChannelsXML.Reload();
                Server.Game.Data.XML.ChannelsXML.Reload();
                Server.Match.Data.XML.MapStructureXML.Reload();
                Server.Match.Data.XML.CharaStructureXML.Reload();
                Server.Match.Data.XML.ItemStatisticXML.Reload();
                PrintSection("Server Data", "Ended");
            }).Start();
            MessageBox.Show("Server Data Sucessfully reloaded.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
