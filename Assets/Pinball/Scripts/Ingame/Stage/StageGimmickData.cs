using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageGimmickData", menuName = "Pinball/Ingame/StageGimmickData")]
    public class StageGimmickData : ScriptableObject
    {
        public enum EType
        {
            BumperUnbreakable = 0,
            TargetScoreMultiplier = 1,
            TimeLimit = 2,
            GimmickAbsent = 3,
            GimmickScoreMultiplier = 4,
            GimmickScoreGainLimit = 5,
        }

        [SerializeField] EType _type = EType.BumperUnbreakable;
        [SerializeField, Tooltip("TargetScoreMultiplier: 요구 점수 배율 / TimeLimit: 제한 시간(초) / GimmickScoreMultiplier: 점수 배율 / GimmickScoreGainLimit: 점수 획득 허용 횟수. 그 외 종류는 사용하지 않음")]
        float _value = 0f;
        [SerializeField, Tooltip("GimmickAbsent / GimmickScoreMultiplier / GimmickScoreGainLimit 종류에서만 사용하는 대상 맵 기믹. Lane은 비활성화(Absent / GainLimit) 대상이 될 수 없음")]
        MapInteractable.EGimmickType _targetGimmickType = MapInteractable.EGimmickType.Bumper;

        public EType Type => _type;
        public float Value => _value;
        public MapInteractable.EGimmickType TargetGimmickType => _targetGimmickType;
    }
}
