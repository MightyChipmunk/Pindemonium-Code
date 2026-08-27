using System;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    [CreateAssetMenu(fileName = "SaucerGradeData", menuName = "Pinball/Ingame/SaucerGradeData")]
    public class SaucerGradeData : ScriptableObject
    {
        public enum EGrade
        {
            Normal = 0,
            Giant = 1,
        }

        [Serializable]
        public struct GradeSpec
        {
            public EGrade Grade;
            public float RootScale;
            public int ScoreReward;
            public int ScoreRewardIncrement;
            public int MaxRewardStep;
            public float HoldDuration;
            public float CooldownDuration;
            public float EjectSpeed;
        }

        [SerializeField] GradeSpec[] _specs = null;

        public GradeSpec GetSpec(EGrade grade)
        {
            if (null == _specs)
                return default;

            for (int i = 0; i < _specs.Length; ++i)
                if (_specs[i].Grade == grade)
                    return _specs[i];

            Debug.LogError($"[SaucerGradeData] 등록되지 않은 등급입니다: {grade}");
            return default;
        }
    }
}
