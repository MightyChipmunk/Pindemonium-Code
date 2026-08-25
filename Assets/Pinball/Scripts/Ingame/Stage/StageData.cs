using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Pinball/Ingame/StageData")]
    public class StageData : ScriptableObject
    {
        const int DEFAULT_CLEAR_MONEY = 3;

        [SerializeField] int _targetScore = 5000;
        [SerializeField, Tooltip("이 스테이지를 클리어할 때 지급하는 기본 돈")] int _clearMoney = DEFAULT_CLEAR_MONEY;
        [SerializeField, Tooltip("기믹 후보 목록에서 무작위로 뽑을 기믹 수")] int _gimmickCount = 0;
        [SerializeField, Tooltip("이 스테이지에 등장할 수 있는 기믹 후보 목록")] StageGimmickData[] _gimmickPool = null;

        public int TargetScore => _targetScore;
        public int GimmickRollCount => Mathf.Max(0, _gimmickCount);
        public int GimmickPoolCount => null == _gimmickPool ? 0 : _gimmickPool.Length;

        public static int CalculateClearMoney(StageData stage, int clearMoneyAdd)
        {
            if (null == stage)
                return 0;

            return Mathf.Max(0, stage._clearMoney + clearMoneyAdd);
        }

        public StageGimmickData GetPoolGimmick(int index)
        {
            if (index < 0 || index >= GimmickPoolCount)
                return null;

            return _gimmickPool[index];
        }
    }
}
