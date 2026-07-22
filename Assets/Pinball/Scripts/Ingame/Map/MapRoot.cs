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

        public void Init(Action<int> onScoreGained, Func<float, float> damageEvaluator, Func<float> flipPowerGetter)
        {
            if (null != _interactables)
            {
                for (int i = 0; i < _interactables.Length; ++i)
                    _interactables[i].Init(onScoreGained, damageEvaluator);
            }

            _leftFlipper.Init(flipPowerGetter);
            _rightFlipper.Init(flipPowerGetter);
            _spring.Init();
        }

        public void Release()
        {
            _spring.Release();
            _rightFlipper.Release();
            _leftFlipper.Release();

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
        }
#endif // UNITY_EDITOR
    }
}
