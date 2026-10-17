namespace Picknick.Creature
{
    /// <summary>
    /// False on an online client: the host simulates the world and the client only shows it,
    /// so gameplay code that makes decisions (like picking a Magnet target) stays off.
    /// </summary>
    public static class Authority
    {
        public static bool IsSimulating = true;
    }
}
