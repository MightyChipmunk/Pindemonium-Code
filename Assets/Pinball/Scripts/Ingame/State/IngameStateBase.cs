using System;
using UnityEngine;

namespace Pinball.Ingame.State
{
    public abstract class IngameStateBase : IIngameState
    {
        const string NOT_BOUND_FORMAT = "[{0}] 상태머신에 등록되기 전에 전이를 요청했습니다: {1}";

        Action<EIngameState, object> _stateChanger = null;

        public abstract EIngameState Key { get; }

        public void Bind(Action<EIngameState, object> stateChanger)
        {
            _stateChanger = stateChanger;
        }

        public void Enter(object param)
        {
            OnEnter(param);
        }

        public void Exit()
        {
            OnExit();
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);
        }

        public void Dispose()
        {
            OnExit();
            OnDispose();
            _stateChanger = null;
        }

        protected abstract void OnEnter(object param);
        protected virtual void OnExit() { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnDispose() { }

        protected void ChangeState(EIngameState key, object param = null)
        {
            if (null == _stateChanger)
            {
                Debug.LogError(string.Format(NOT_BOUND_FORMAT, GetType().Name, key));
                return;
            }

            _stateChanger(key, param);
        }
    }
}
