namespace Perpetuum.Items.Templates
{
    public interface IRobotTemplateRelations : IReadOnlyRepository<int,IRobotTemplateRelation>
    {
        RobotTemplate EquippedDefault { get; }
        RobotTemplate UnequippedDefault { get; }
    }
}