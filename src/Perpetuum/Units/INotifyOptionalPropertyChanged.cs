namespace Perpetuum.Units
{
    public interface INotifyOptionalPropertyChanged
    {
        event OptionalPropertyChangeEventHandler PropertyChanged;
    }
}