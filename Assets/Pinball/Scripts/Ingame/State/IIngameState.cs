using System;

namespace Pinball.Ingame.State
{
    public interface IIngameState : IDisposable
    {
        EIngameState Key { get; }
        void Bind(Action<EIngameState, object> stateChanger);
        void Enter(object param);
        void Exit();
        void Tick(float deltaTime);
    }
}
