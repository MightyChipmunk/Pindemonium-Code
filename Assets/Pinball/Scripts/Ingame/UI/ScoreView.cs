using System.Globalization;
using Pinball.UI;
using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScoreView : MonoBehaviour
    {
        const string NUMBER_FORMAT = "N0";
        const int INITIAL_VALUE = 0;

        [SerializeField] TMP_Text _valueLabel = null;
        [SerializeField] UIScalePunch _punch = null;

        int _lastValue = INITIAL_VALUE;

        public void SetValue(int value)
        {
            _valueLabel.text = value.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);

            if (value == _lastValue)
                return;

            _lastValue = value;
            _punch.Play();
        }
    }
}
