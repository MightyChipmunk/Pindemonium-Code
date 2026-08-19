using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinball.Ingame.State
{
    public class IngameStateMachine : IDisposable
    {
        const string STATE_NOT_FOUND_FORMAT = "[IngameStateMachine] 등록되지 않은 상태입니다: {0}";

        readonly Dictionary<EIngameState, IIngameState> _states = new Dictionary<EIngameState, IIngameState>();
        readonly List<IIngameState> _registerOrder = new List<IIngameState>();
        IIngameState _currentState = null;

        public void Dispose()
        {
            if (null != _currentState)
            {
                _currentState.Exit();
                _currentState = null;
            }

            for (int i = _registerOrder.Count - 1; i >= 0; --i)
                _registerOrder[i].Dispose();

            _registerOrder.Clear();
            _states.Clear();
        }

        public void Register(IIngameState state)
        {
            if (null == state)
                return;

            _states[state.Key] = state;
            _registerOrder.Add(state);
            state.Bind(ChangeState);
        }

        public bool IsCurrent(EIngameState key)
        {
            return null != _currentState && _currentState.Key == key;
        }

        public void ChangeState(EIngameState key, object param)
        {
            if (!_states.TryGetValue(key, out IIngameState nextState))
            {
                Debug.LogError(string.Format(STATE_NOT_FOUND_FORMAT, key));
                return;
            }

            if (null != _currentState)
                _currentState.Exit();

            _currentState = nextState;
            _currentState.Enter(param);
        }

        public void Tick(float deltaTime)
        {
            if (null == _currentState)
                return;

            _currentState.Tick(deltaTime);
        }
    }
}
