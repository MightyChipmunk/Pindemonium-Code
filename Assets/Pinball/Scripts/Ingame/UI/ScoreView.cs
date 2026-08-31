using System.Globalization;
using Pinball.UI;
using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScoreView : MonoBehaviour
    {
        const string NUMBER_FORMAT = "N0";
        const long INITIAL_VALUE = 0;

        [SerializeField] TMP_Text _valueLabel = null;
        [SerializeField] UIScalePunch _punch = null;

        long _lastValue = INITIAL_VALUE;

        public void SetValue(long value)
        {
            _valueLabel.text = value.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);

            bool isIncreased = value > _lastValue;
            _lastValue = value;

            if (!isIncreased)
            {
                _punch.Stop();
                return;
            }

            _punch.Play();
        }
    }
}
