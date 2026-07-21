using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class BallCountView : MonoBehaviour
    {
        const string BALL_COUNT_FORMAT = "Ball : {0}";

        [SerializeField] TMP_Text _text = null;

        public void SetValue(int value)
        {
            _text.text = string.Format(BALL_COUNT_FORMAT, value);
        }
    }
}
