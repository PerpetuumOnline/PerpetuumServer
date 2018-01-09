namespace Perpetuum.Units
{
    public interface IOptionalProperty
    {
        UnitDataType Type { get; }
        object Value { get; }
    }
}