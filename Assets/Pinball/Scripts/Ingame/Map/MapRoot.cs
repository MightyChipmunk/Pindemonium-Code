using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class MapRoot : BehaviourBase
    {
        [SerializeField] MapInteractable[] _interactables = null;
        [SerializeField] Flipper _leftFlipper = null;
        [SerializeField] Flipper _rightFlipper = null;
        [SerializeField] Spring _spring = null;

        Action<Flipper.EHitGrade, Vector2> _onBallStruck = null;

        public void Init(Action<int> onScoreGained, Func<float, float> damageEvaluator, Func<float> flipPowerGetter, Action<Flipper.EHitGrade, Vector2> onBallStruck)
        {
            if (null != _interactables)
            {
                for (int i = 0; i < _interactables.Length; ++i)
                    _interactables[i].Init(onScoreGained, damageEvaluator);
            }

            _onBallStruck = onBallStruck;
            _leftFlipper.Init(flipPowerGetter);
            _rightFlipper.Init(flipPowerGetter);
            _leftFlipper.OnBallStruck += OnFlipperBallStruck;
            _rightFlipper.OnBallStruck += OnFlipperBallStruck;
            _spring.Init();
        }

        public void Release()
        {
            _spring.Release();
            _rightFlipper.OnBallStruck -= OnFlipperBallStruck;
            _leftFlipper.OnBallStruck -= OnFlipperBallStruck;
            _rightFlipper.Release();
            _leftFlipper.Release();
            _onBallStruck = null;

            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

        public void SetFlipperPressed(Flipper.ESide side, bool isPressed)
        {
            if (side == Flipper.ESide.Left)
                _leftFlipper.SetPressed(isPressed);
            else
                _rightFlipper.SetPressed(isPressed);
        }

        public void LoadBall(Ball ball)
        {
            _spring.LoadBall(ball);
        }

        public bool TryLaunch()
        {
            return _spring.TryLaunch();
        }

        void OnFlipperBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            _onBallStruck?.Invoke(grade, worldPoint);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _interactables = GetComponentsInChildren<MapInteractable>(true);
            _spring = GetComponentInChildren<Spring>(true);

            Flipper[] flippers = GetComponentsInChildren<Flipper>(true);
            for (int i = 0; i < flippers.Length; ++i)
            {
                if (flippers[i].Side == Flipper.ESide.Left)
                    _leftFlipper = flippers[i];
                else
                    _rightFlipper = flippers[i];
            }

            for (int i = 0; i < _interactables.Length; ++i)
            {
                LaneInteractable lane = _interactables[i] as LaneInteractable;
                if (null != lane)
                    lane.BakePath();
            }
        }
#endif // UNITY_EDITOR
    }
}
