using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.UI
{
    public class BallCountView : MonoBehaviour
    {
        const string OVERFLOW_FORMAT = "x{0}";
        const int OVERFLOW_ICON_COUNT = 1;
        const int MIN_BALL_COUNT = 0;

        [SerializeField] Image[] _ballIcons = null;
        [SerializeField] TMP_Text _overflowLabel = null;

        public void SetValue(int value)
        {
            int ballCount = Mathf.Max(MIN_BALL_COUNT, value);
            bool isOverflow = ballCount > _ballIcons.Length;
            int visibleIconCount = isOverflow ? OVERFLOW_ICON_COUNT : ballCount;

            for (int i = 0; i < _ballIcons.Length; ++i)
                _ballIcons[i].gameObject.SetActive(i < visibleIconCount);

            _overflowLabel.gameObject.SetActive(isOverflow);

            if (isOverflow)
                _overflowLabel.text = string.Format(OVERFLOW_FORMAT, ballCount);
        }
    }
}
