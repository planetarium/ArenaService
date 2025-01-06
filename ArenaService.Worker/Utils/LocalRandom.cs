using Libplanet.Action;

namespace ArenaService.Worker.Utils;

public class LocalRandom : Random, IRandom
{
    public int Seed { get; }

    public LocalRandom(int seed)
        : base(seed)
    {
        Seed = seed;
    }
}
