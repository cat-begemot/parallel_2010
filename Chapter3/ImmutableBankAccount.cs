namespace Chapter3 {
	internal class ImmutableBankAccount
	{
		public const int AccountNumber = 123456;
		public readonly int Balance;

		public ImmutableBankAccount()
		{
			Balance = 0;
		}

		public ImmutableBankAccount(int initialBalance)
		{
			Balance = initialBalance;
		}
	}
}