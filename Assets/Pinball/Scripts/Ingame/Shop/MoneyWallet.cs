using System;

namespace Pinball.Ingame.Shop
{
    public class MoneyWallet
    {
        int _current = 0;

        public int Current => _current;

        public event Action<int> OnChanged;

        public MoneyWallet(int initialAmount)
        {
            _current = initialAmount;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _current += amount;
            OnChanged?.Invoke(_current);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
                return false;

            if (_current < amount)
                return false;

            _current -= amount;
            OnChanged?.Invoke(_current);
            return true;
        }
    }
}
