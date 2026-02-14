
namespace Plugin.Core.Models
{
    public class PlayerBattlepass
    {
        public int BattlepassId { get; set; }
        public int BattlepassPremiumLevel { get; set; }
        public int BattlepassNormalLevel { get; set; }
        public bool HavePremium { get; set; }
        public int EarnedPoints { get; set; }
    }
}