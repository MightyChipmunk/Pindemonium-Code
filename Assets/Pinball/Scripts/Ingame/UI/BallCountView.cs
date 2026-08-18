using Pinball.Localization;
using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class BallCountView : MonoBehaviour
    {
        const string BALL_COUNT_FORMAT = "Ball : {0}";
        const string BALL_COUNT_KEY = "ui.ingame.ball_count";

        [SerializeField] TMP_Text _text = null;

        public void SetValue(int value)
        {
            _text.text = LocalizationTable.Format(BALL_COUNT_KEY, BALL_COUNT_FORMAT, value);
        }
    }
}
