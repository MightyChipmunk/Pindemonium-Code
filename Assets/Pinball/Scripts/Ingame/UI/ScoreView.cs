using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScoreView : MonoBehaviour
    {
        const string SCORE_FORMAT = "Score : {0}";

        [SerializeField] TMP_Text _text = null;

        public void SetValue(int value)
        {
            _text.text = string.Format(SCORE_FORMAT, value);
        }
    }
}
