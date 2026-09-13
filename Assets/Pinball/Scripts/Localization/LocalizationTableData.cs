using GameCore.Localization;
using UnityEngine;

namespace Pinball.Localization
{
    [CreateAssetMenu(fileName = "LocalizationTableData", menuName = "Pinball/Localization/LocalizationTableData")]
    public class LocalizationTableData : LocalizationTableDataBase<ELanguage>
    {
        const string ENGLISH_DISPLAY_NAME = "ENGLISH";
        const string KOREAN_DISPLAY_NAME = "한국어";

        public override string GetLanguageName(int languageIndex)
        {
            switch ((ELanguage)languageIndex)
            {
                case ELanguage.Korean:
                    return KOREAN_DISPLAY_NAME;
                default:
                    return ENGLISH_DISPLAY_NAME;
            }
        }
    }
}
