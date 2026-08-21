using System;

namespace Pinball.Ingame.Score
{
    public class BallStockCounter
    {
        int _remaining = 0;

        public int Remaining => _remaining;
        public event Action<int> OnChanged;

        public BallStockCounter(int initialCount)
        {
            _remaining = initialCount;
        }

        public void Reset(int count)
        {
            _remaining = count;
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
