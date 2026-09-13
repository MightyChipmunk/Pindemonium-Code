using Pinball.Ingame.Shop;
using GameCore.Localization;

namespace Pinball.UI
{
    public static class PassiveCategoryLabel
    {
        const string BALL_TEXT = "BALL";
        const string BALL_KEY = "ui.common.ball";
        const string MAP_TEXT = "MAP";
        const string MAP_KEY = "ui.passive.category_map";
        const string COMMON_TEXT = "COMMON";
        const string COMMON_KEY = "ui.passive.category_common";

        public static string Get(ShopItemData.EPassiveCategory category)
        {
            switch (category)
            {
                case ShopItemData.EPassiveCategory.Ball:
                    return LocalizationTable.Get(BALL_KEY, BALL_TEXT);
                case ShopItemData.EPassiveCategory.Map:
                    return LocalizationTable.Get(MAP_KEY, MAP_TEXT);
                default:
                    return LocalizationTable.Get(COMMON_KEY, COMMON_TEXT);
            }
        }
    }
}
