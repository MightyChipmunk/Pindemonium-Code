using System;
using DG.Tweening;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class DropTarget : BehaviourBase
    {
        const float DROP_DURATION = 0.1f;
        const float RAISE_DURATION = 0.18f;

        [SerializeField] Collider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] float _dropDistance = 0.25f;

        Func<int, bool> _ballLayerChecker = null;
        Action _onDropped = null;
        Tween _moveTween = null;
        Vector3 _raisedLocalPosition = Vector3.zero;
        bool _isDropped = false;

        public void Init(Func<int, bool> ballLayerChecker, Action onDropped)
        {
            _ballLayerChecker = ballLayerChecker;
            _onDropped = onDropped;
            _isDropped = false;
            _collider.enabled = true;

            if (null == _visual)
                return;

            _raisedLocalPosition = _visual.localPosition;
        }

        public void Release()
        {
            KillMoveTween();
            _onDropped = null;
            _ballLayerChecker = null;
        }

        public void Raise()
        {
            if (!_isDropped)
                return;

            _isDropped = false;
            _collider.enabled = true;
            KillMoveTween();

            if (null == _visual)
                return;

            _moveTween = _visual.DOLocalMove(_raisedLocalPosition, RAISE_DURATION);
        }

        void Drop()
        {
            _isDropped = true;
            _collider.enabled = false;
            KillMoveTween();

            if (null != _visual)
                _moveTween = _visual.DOLocalMove(_raisedLocalPosition + Vector3.down * _dropDistance, DROP_DURATION);

            _onDropped?.Invoke();
        }

        void KillMoveTween()
        {
            if (null == _moveTween)
                return;

            _moveTween.Kill();
            _moveTween = null;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDropped)
                return;

            if (null == _ballLayerChecker)
                return;

            if (!_ballLayerChecker(collision.gameObject.layer))
                return;

            Drop();
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _collider = GetComponent<Collider2D>();
        }
#endif // UNITY_EDITOR
    }
}
