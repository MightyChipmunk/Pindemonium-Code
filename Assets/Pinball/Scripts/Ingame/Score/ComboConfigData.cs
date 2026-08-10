using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "ComboConfigData", menuName = "Pinball/Ingame/ComboConfigData")]
    public class ComboConfigData : ScriptableObject
    {
        [SerializeField] int _comboCountPerStep = 10;
        [SerializeField] float _stepMultiplierAdd = 0.1f;
        [SerializeField] float _timeoutSeconds = 3f;

        public int ComboCountPerStep => _comboCountPerStep;
        public float StepMultiplierAdd => _stepMultiplierAdd;
        public float TimeoutSeconds => _timeoutSeconds;
    }
}
