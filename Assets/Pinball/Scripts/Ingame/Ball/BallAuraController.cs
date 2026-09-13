using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame
{
    public class BallAuraController : BehaviourBase
    {
        [SerializeField] BallAura[] _auras = null;

        public void Init()
        {
            if (null == _auras)
                return;

            for (int i = 0; i < _auras.Length; ++i)
                _auras[i].Init();
        }

        public void Release()
        {
            if (null == _auras)
                return;

            for (int i = 0; i < _auras.Length; ++i)
                _auras[i].Release();
        }

        public void SetAuraActive(BallAura.EAuraType auraType, bool isActive)
        {
            if (null == _auras)
                return;

            for (int i = 0; i < _auras.Length; ++i)
                if (_auras[i].AuraType == auraType)
                    _auras[i].SetActive(isActive);
        }

        public void ClearParticles()
        {
            if (null == _auras)
                return;

            for (int i = 0; i < _auras.Length; ++i)
                _auras[i].Clear();
        }

        public bool TryGetDominantAuraType(out BallAura.EAuraType auraType)
        {
            auraType = BallAura.EAuraType.Fire;

            if (null == _auras)
                return false;

            bool isFound = false;
            for (int i = 0; i < _auras.Length; ++i)
            {
                if (!_auras[i].IsActive)
                    continue;

                if (isFound && _auras[i].AuraType <= auraType)
                    continue;

                auraType = _auras[i].AuraType;
                isFound = true;
            }

            return isFound;
        }

        public bool TryGetBlendedColor(out Color blendedColor)
        {
            blendedColor = Color.white;

            if (null == _auras)
                return false;

            float sumRed = 0f;
            float sumGreen = 0f;
            float sumBlue = 0f;
            int activeCount = 0;

            for (int i = 0; i < _auras.Length; ++i)
            {
                if (!_auras[i].IsActive)
                    continue;

                Color auraColor = _auras[i].TrailColor;
                sumRed += auraColor.r;
                sumGreen += auraColor.g;
                sumBlue += auraColor.b;
                ++activeCount;
            }

            if (activeCount <= 0)
                return false;

            blendedColor = new Color(sumRed / activeCount, sumGreen / activeCount, sumBlue / activeCount, 1f);
            return true;
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _auras = GetComponentsInChildren<BallAura>(true);
        }
#endif // UNITY_EDITOR
    }
}
