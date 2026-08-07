using System;

namespace Pinball.Ingame
{
    public class ScoreCounter
    {
        int _current = 0;

        public int Current => _current;
        public event Action<int> OnChanged;

        public void Init(int initialScore)
        {
            _current = initialScore;
        }

        public void Release()
        {
            OnChanged = null;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _current += amount;
            OnChanged?.Invoke(_current);
        }
    }
}
