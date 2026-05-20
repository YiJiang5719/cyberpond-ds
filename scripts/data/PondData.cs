using System;
using System.Collections.Generic;
using Godot;

namespace CyberPond;

public class PondData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public List<FishData> Fishes { get; set; } = new();

    public PondData(string name, double lat, double lon)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        Lat = lat;
        Lon = lon;
    }

    public Godot.Collections.Dictionary ToDict()
    {
        var fishesArray = new Godot.Collections.Array();
        foreach (var fish in Fishes)
            fishesArray.Add(fish.ToDict());

        return new Godot.Collections.Dictionary
        {
            { "id", Id },
            { "name", Name },
            { "lat", Lat },
            { "lon", Lon },
            { "fishes", fishesArray }
        };
    }

    public static PondData FromDict(Godot.Collections.Dictionary dict)
    {
        var pond = new PondData(
            name: dict["name"].AsString(),
            lat: dict["lat"].AsDouble(),
            lon: dict["lon"].AsDouble()
        )
        {
            Id = dict.TryGetValue("id", out var id) ? id.AsString() : Guid.NewGuid().ToString()
        };

        if (dict.TryGetValue("fishes", out var fishesArray))
        {
            foreach (Godot.Collections.Dictionary fishDict in (Godot.Collections.Array)fishesArray)
                pond.Fishes.Add(FishData.FromDict(fishDict));
        }

        return pond;
    }
}
