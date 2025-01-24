namespace ArenaService.IntegrationTests;

public static class TestUtils
{
    private static readonly Random _random = new Random();

    public static byte[] GetRandomBytes(int size)
    {
        var bytes = new byte[size];
        _random.NextBytes(bytes);

        return bytes;
    }
}
