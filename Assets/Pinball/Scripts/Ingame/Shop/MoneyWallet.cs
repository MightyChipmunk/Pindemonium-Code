namespace Pinball.Ingame
{
    public class MoneyWallet
    {
        int _current = 0;

        public int Current => _current;

        public void Init()
        {
            _current = 0;
        }

        public void Release()
        {
            _current = 0;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _current += amount;
        }
    }
}
