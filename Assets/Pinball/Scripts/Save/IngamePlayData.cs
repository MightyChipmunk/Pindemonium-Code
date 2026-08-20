using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinball.Save
{
    [Serializable]
    public class IngamePlayData : SaveDataBase
    {
        [Serializable]
        public struct UpgradeEntry
        {
            [SerializeField, FormerlySerializedAs("Id")] string _id;
            [SerializeField, FormerlySerializedAs("Count")] int _count;

            public string Id => _id;
            public int Count => _count;

            public UpgradeEntry(string id, int count)
            {
                _id = id;
                _count = count;
            }
        }

        [SerializeField, FormerlySerializedAs("StageNumber")] int _stageNumber = 1;
        [SerializeField, FormerlySerializedAs("IsShopPending")] bool _isShopPending = false;
        [SerializeField, FormerlySerializedAs("Money")] int _money = 0;
        [SerializeField, FormerlySerializedAs("TotalScore")] int _totalScore = 0;
        [SerializeField, FormerlySerializedAs("GimmickIndices")] int[] _gimmickIndices = null;
        [SerializeField, FormerlySerializedAs("FlipperId")] string _flipperId = string.Empty;
        [SerializeField, FormerlySerializedAs("PassiveIds")] string[] _passiveIds = null;
        [SerializeField, FormerlySerializedAs("Upgrades")] UpgradeEntry[] _upgrades = null;
        [SerializeField] string[] _voucherIds = null;
        [SerializeField] int _voucherPurchasedRound = -1;

        public int StageNumber => _stageNumber;
        public bool IsShopPending => _isShopPending;
        public int VoucherPurchasedRound => _voucherPurchasedRound;
        public int Money => _money;
        public int TotalScore => _totalScore;
        public string FlipperId => _flipperId;

        public IngamePlayData() { }

        public IngamePlayData(int stageNumber, bool isShopPending, int money, int totalScore, int[] gimmickIndices, string flipperId, string[] passiveIds, UpgradeEntry[] upgrades, string[] voucherIds, int voucherPurchasedRound)
        {
            _stageNumber = stageNumber;
            _isShopPending = isShopPending;
            _money = money;
            _totalScore = totalScore;
            _gimmickIndices = gimmickIndices;
            _flipperId = flipperId;
            _passiveIds = passiveIds;
            _upgrades = upgrades;
            _voucherIds = voucherIds;
            _voucherPurchasedRound = voucherPurchasedRound;
        }

        public int[] GetGimmickIndices()
        {
            return _gimmickIndices;
        }

        public string[] GetPassiveIds()
        {
            return _passiveIds;
        }

        public UpgradeEntry[] GetUpgrades()
        {
            return _upgrades;
        }

        public string[] GetVoucherIds()
        {
            return _voucherIds;
        }
    }
}
