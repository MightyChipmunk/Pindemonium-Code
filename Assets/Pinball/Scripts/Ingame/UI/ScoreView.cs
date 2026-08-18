using Pinball.Localization;
using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScoreView : MonoBehaviour
    {
        const string SCORE_FORMAT = "Score : {0}";
        const string SCORE_KEY = "ui.ingame.score";

        [SerializeField] TMP_Text _text = null;

        public void SetValue(int value)
        {
            _text.text = LocalizationTable.Format(SCORE_KEY, SCORE_FORMAT, value);
        }
    }
}
