namespace Pinball.Ingame
{
    public class MoneyWallet
    {
        int _current = 0;

        public int Current => _current;

        public MoneyWallet(int initialAmount)
        {
            _current = initialAmount;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _current += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
                return false;

            if (_current < amount)
                return false;

            _current -= amount;
            return true;
        }
    }
}
