using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class DropTargetBankInteractable : MapInteractable
    {
        const float NO_ORDER_MULTIPLIER = 1f;

        [SerializeField] DropTarget[] _targets = null;
        [SerializeField] int _dropScoreReward = 0;
        [SerializeField] int _clearScoreReward = 0;
        [SerializeField] float _resetDelay = 1.5f;

        CancellationTokenSource _cancellationTokenSource = null;
        int[] _order = null;
        int _droppedCount = 0;
        bool _isOrderKept = true;

        public override EGimmickType GimmickType => EGimmickType.DropTarget;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            if (null == _targets)
                return;

            _order = new int[_targets.Length];
            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].Init(i, IsBallLayer, OnTargetDropped);

            ResetOrder();
        }

        protected override void OnRelease()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _order = null;

            if (null == _targets)
                return;

            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].Release();
        }

        void ResetOrder()
        {
            _droppedCount = 0;
            _isOrderKept = true;
            ShuffleOrder();
            RefreshOrderHighlight();
        }

        void ShuffleOrder()
        {
            for (int i = 0; i < _order.Length; ++i)
                _order[i] = i;

            for (int i = _order.Length - 1; i > 0; --i)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                int swapped = _order[i];
                _order[i] = _order[swapIndex];
                _order[swapIndex] = swapped;
            }
        }

        void RefreshOrderHighlight()
        {
            bool hasOrder = StatProvider.HasDropTargetOrder() && _droppedCount < _order.Length;
            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].SetHighlighted(hasOrder && i == _order[_droppedCount]);
        }

        void OnTargetDropped(int targetIndex, Collider2D ballCollider)
        {
            NotifyBallHit(ballCollider);
            PlayGimmickHit();
            GainScore(_dropScoreReward);

            if (targetIndex != _order[_droppedCount])
                _isOrderKept = false;

            ++_droppedCount;
            RefreshOrderHighlight();

            if (_droppedCount < _targets.Length)
                return;

            GainScore(Mathf.RoundToInt(_clearScoreReward * GetClearScoreMultiplier()));
            PlayBankClearedSound();
            PlayClearShake();

            if (null != _cancellationTokenSource)
                ResetBankAsync(_cancellationTokenSource.Token).Forget();
        }

        float GetClearScoreMultiplier()
        {
            if (!_isOrderKept)
                return NO_ORDER_MULTIPLIER;

            return StatProvider.GetDropTargetOrderMultiplier();
        }

        void PlayClearShake()
        {
            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].PlayShake();
        }

        async UniTaskVoid ResetBankAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_resetDelay), cancellationToken: cancellationToken);

            ResetOrder();
            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].Raise();
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _targets = GetComponentsInChildren<DropTarget>(true);
        }
#endif // UNITY_EDITOR
    }
}
