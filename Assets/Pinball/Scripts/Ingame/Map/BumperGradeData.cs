using System;
using Pinball.FX;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    [CreateAssetMenu(fileName = "BumperGradeData", menuName = "Pinball/Ingame/BumperGradeData")]
    public class BumperGradeData : ScriptableObject
    {
        public const EGrade MAX_GRADE = EGrade.Red;

        public enum EGrade
        {
            Yellow = 0,
            Orange = 1,
            Red = 2,
        }

        [Serializable]
        public struct GradeSpec
        {
            public EGrade Grade;
            public Sprite VisualSprite;
            public float RootScale;
            public float BounceForce;
            public float ScoreMultiplier;
            public float MaxHealth;
            public int BreakScore;
            public float RespawnDelay;
            public EFXType HitFX;
            public EFXType BreakFX;
        }

        [SerializeField] GradeSpec[] _specs = null;

        public GradeSpec GetSpec(EGrade grade)
        {
            if (null == _specs)
                return default;

            for (int i = 0; i < _specs.Length; ++i)
                if (_specs[i].Grade == grade)
                    return _specs[i];

            Debug.LogError($"[BumperGradeData] 등록되지 않은 등급입니다: {grade}");
            return default;
        }
    }
}
