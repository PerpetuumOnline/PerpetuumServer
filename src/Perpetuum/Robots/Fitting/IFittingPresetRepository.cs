namespace Perpetuum.Robots.Fitting
{
    public interface IFittingPresetRepository : IRepository<int,FittingPreset>
    {
        void DeleteById(int id);
    }
}