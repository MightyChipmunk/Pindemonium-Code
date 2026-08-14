using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallAura : BehaviourBase
    {
        public enum EAuraType
        {
            Fire = 0,
            Lightning = 1,
            Frost = 2,
            Gold = 3,
        }

        [SerializeField] EAuraType _auraType = EAuraType.Fire;
        [SerializeField] ParticleSystem _particle = null;
        [SerializeField] Color _trailColor = Color.white;

        bool _isActive = false;

        public EAuraType AuraType => _auraType;
        public Color TrailColor => _trailColor;
        public bool IsActive => _isActive;

        public void Init()
        {
            SetActive(false);
        }

        public void Release()
        {
            SetActive(false);
        }

        public void SetActive(bool isActive)
        {
            _isActive = isActive;

            if (isActive)
            {
                _particle.Play(true);
                return;
            }

            _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Clear()
        {
            _particle.Clear(true);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _particle = GetComponent<ParticleSystem>();
        }
#endif // UNITY_EDITOR
    }
}
