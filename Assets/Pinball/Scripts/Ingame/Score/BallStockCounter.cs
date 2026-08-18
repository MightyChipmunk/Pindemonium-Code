using System;

namespace Pinball.Ingame
{
    public class BallStockCounter
    {
        int _remaining = 0;

        public int Remaining => _remaining;
        public event Action<int> OnChanged;

        public void Init(int initialCount)
        {
            _remaining = initialCount;
        }

        public void Release()
        {
            OnChanged = null;
        }

        public void Consume()
        {
            if (_remaining <= 0)
                return;

            --_remaining;
            OnChanged?.Invoke(_remaining);
        }
    }
}
