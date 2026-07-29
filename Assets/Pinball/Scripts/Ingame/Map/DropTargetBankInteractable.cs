using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class DropTargetBankInteractable : MapInteractable
    {
        [SerializeField] DropTarget[] _targets = null;
        [SerializeField] int _dropScoreReward = 0;
        [SerializeField] int _clearScoreReward = 0;
        [SerializeField] float _resetDelay = 1.5f;

        CancellationTokenSource _cancellationTokenSource = null;
        int _droppedCount = 0;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _droppedCount = 0;

            if (null == _targets)
                return;

            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].Init(IsBallLayer, OnTargetDropped);
        }

        protected override void OnRelease()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (null == _targets)
                return;

            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].Release();
        }

        void OnTargetDropped()
        {
            GainScore(_dropScoreReward);
            ++_droppedCount;

            if (_droppedCount < _targets.Length)
                return;

            GainScore(_clearScoreReward);
            ResetBankAsync(_cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid ResetBankAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_resetDelay), cancellationToken: cancellationToken);

            _droppedCount = 0;
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
