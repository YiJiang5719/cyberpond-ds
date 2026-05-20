using Godot;

namespace CyberPond;

/// Manages coin balance with spend/add operations.
public partial class EconomyManager : Node
{
	public int Coins { get; private set; }

	public void SetCoins(int amount)
	{
		Coins = amount;
		GD.Print($"[EconomyManager] Coins set to {Coins}");
	}

	public bool SpendCoins(int amount)
	{
		if (Coins < amount)
		{
			GD.Print($"[EconomyManager] Not enough coins: have {Coins}, need {amount}");
			return false;
		}
		Coins -= amount;
		GD.Print($"[EconomyManager] Spent {amount} coins. Remaining: {Coins}");
		return true;
	}

	public void AddCoins(int amount)
	{
		Coins += amount;
		GD.Print($"[EconomyManager] Added {amount} coins. Total: {Coins}");
	}
}
