namespace Obfuz.Utils
{
    public interface IRandom
    {
        int NextInt(int min, int max);

        int NextInt(int max);

        int NextInt();

        long NextLong();
    }
}
