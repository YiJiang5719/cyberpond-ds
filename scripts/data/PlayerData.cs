using Godot;
using Godot.Collections;

namespace CyberPond;

public class PlayerData
{
	public int Coins { get; set; }

	public PlayerData(int coins = 0)
	{
		Coins = coins;
	}

	public Dictionary ToDict()
	{
		return new Dictionary
		{
			{ "coins", Coins }
		};
	}

	public static PlayerData FromDict(Dictionary dict)
	{
		return new PlayerData(
			coins: dict["coins"].AsInt32()
		);
	}
}
