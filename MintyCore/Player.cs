namespace MintyCore;

public sealed class Player
{
    internal Player(ushort gameId, ulong globalId, string name)
    {
        GameId = gameId;
        GlobalId = globalId;
        Name = name;
        IsConnected = true;
    }

    public bool IsConnected { get; internal set; }
    public ushort GameId { get;  }
    public ulong GlobalId { get;  }
    public string Name { get;  }
}