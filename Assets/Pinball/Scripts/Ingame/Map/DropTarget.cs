using System;
using DG.Tweening;
using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame.Map
{
    public class DropTarget : BehaviourBase
    {
        const float DROP_DURATION = 0.1f;
        const float RAISE_DURATION = 0.18f;
        const float SHAKE_DURATION = 0.5f;
        const float SHAKE_STRENGTH = 0.14f;
        const int SHAKE_VIBRATO = 20;
        const float RAISED_ALPHA = 1f;
        const float DROPPED_ALPHA = 0.4f;

        static readonly Color HIGHLIGHT_TINT = new Color(1f, 0.72f, 0.24f, 1f);

        [SerializeField] Collider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;
        [SerializeField] float _dropDistance = 0.25f;

        Func<int, bool> _ballLayerChecker = null;
        Action<int, Collider2D> _onDropped = null;
        Tween _moveTween = null;
        Vector3 _raisedLocalPosition = Vector3.zero;
        Color _baseColor = Color.white;
        int _index = 0;
        bool _isDropped = false;

        public void Init(int index, Func<int, bool> ballLayerChecker, Action<int, Collider2D> onDropped)
        {
            _index = index;
            _ballLayerChecker = ballLayerChecker;
            _onDropped = onDropped;
            _isDropped = false;
            _collider.enabled = true;

            if (null != _visualRenderer)
                _baseColor = _visualRenderer.color;

            SetHighlighted(false);
            SetAlpha(RAISED_ALPHA);

            if (null == _visual)
                return;

            _raisedLocalPosition = _visual.localPosition;
        }

        public void Release()
        {
            KillMoveTween(false);
            SetHighlighted(false);
            _onDropped = null;
            _ballLayerChecker = null;
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (null == _visualRenderer)
                return;

            Color tint = isHighlighted ? HIGHLIGHT_TINT : _baseColor;
            Color color = _visualRenderer.color;
            color.r = tint.r;
            color.g = tint.g;
            color.b = tint.b;
            _visualRenderer.color = color;
        }

        public void Raise()
        {
            if (!_isDropped)
                return;

            _isDropped = false;
            _collider.enabled = true;
            KillMoveTween(false);
            SetAlpha(RAISED_ALPHA);

            if (null == _visual)
                return;

            _moveTween = _visual.DOLocalMove(_raisedLocalPosition, RAISE_DURATION);
        }

        public void PlayShake()
        {
            if (null == _visual)
                return;

            KillMoveTween(false);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_visual.DOLocalMove(GetDroppedLocalPosition(), DROP_DURATION));
            sequence.Append(_visual.DOShakePosition(SHAKE_DURATION, SHAKE_STRENGTH, SHAKE_VIBRATO));
            _moveTween = sequence;
        }

        public bool TryDropByEffect()
        {
            if (_isDropped)
                return false;

            Drop(null);
            return true;
        }

        public bool IsDropped()
        {
            return _isDropped;
        }

        void Drop(Collider2D ballCollider)
        {
            _isDropped = true;
            _collider.enabled = false;
            KillMoveTween(false);
            SetAlpha(DROPPED_ALPHA);

            if (null != _visual)
                _moveTween = _visual.DOLocalMove(GetDroppedLocalPosition(), DROP_DURATION);

            _onDropped?.Invoke(_index, ballCollider);
        }

        Vector3 GetDroppedLocalPosition()
        {
            return _raisedLocalPosition + Vector3.down * _dropDistance;
        }

        void SetAlpha(float alpha)
        {
            if (null == _visualRenderer)
                return;

            Color color = _visualRenderer.color;
            color.a = alpha;
            _visualRenderer.color = color;
        }

        void KillMoveTween(bool isComplete)
        {
            if (null == _moveTween)
                return;

            _moveTween.Kill(isComplete);
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

            Drop(collision.collider);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _collider = GetComponent<Collider2D>();
            _visualRenderer = GetComponentInChildren<SpriteRenderer>();
        }
#endif // UNITY_EDITOR
    }
}
