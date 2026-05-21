using System;
using Godot;

namespace CyberPond;

public class FishData
{
	public string Id { get; set; }
	public string FishType { get; set; }
	public DateTime SpawnedAt { get; set; }
	public int UncollectedRoe { get; set; }
	public DateTime LastRoeTime { get; set; }
	public bool IsAdult { get; set; }

	public FishData(string fishType)
	{
		Id = Guid.NewGuid().ToString();
		FishType = fishType;
		SpawnedAt = DateTime.UtcNow;
		UncollectedRoe = 0;
		LastRoeTime = DateTime.UtcNow;
		IsAdult = false;
	}

	public Godot.Collections.Dictionary ToDict()
	{
		return new Godot.Collections.Dictionary
		{
			{ "id", Id },
			{ "fish_type", FishType },
			{ "spawned_at", SpawnedAt.ToString("o") },
			{ "is_adult", IsAdult },
			{ "uncollected_roe", UncollectedRoe },
			{ "last_roe_time", LastRoeTime.ToString("o") }
		};
	}

	public static FishData FromDict(Godot.Collections.Dictionary dict)
	{
		return new FishData(dict["fish_type"].AsString())
		{
			Id = dict.TryGetValue("id", out var id) ? id.AsString() : Guid.NewGuid().ToString(),
			SpawnedAt = DateTime.Parse(dict["spawned_at"].AsString()),
			IsAdult = dict.TryGetValue("uncollected_roe", out var adult) && adult.AsBool(),
			UncollectedRoe = dict.TryGetValue("uncollected_roe", out var roe) ? roe.AsInt32() : 0,
			LastRoeTime = dict.TryGetValue("last_roe_time", out var lrt) ? DateTime.Parse(lrt.AsString()) : DateTime.UtcNow
		};
	}
}
