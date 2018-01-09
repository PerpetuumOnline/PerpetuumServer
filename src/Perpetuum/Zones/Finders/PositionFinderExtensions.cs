namespace Perpetuum.Zones.Finders
{
    public static class PositionFinderExtensions
    {
        public static Position FindOrThrow(this IPositionFinder finder)
        {
            Position result;
            finder.Find(out result).ThrowIfFalse(ErrorCodes.InvalidPosition);
            return result;
        }
    }
}