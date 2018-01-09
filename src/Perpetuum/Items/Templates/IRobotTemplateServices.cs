namespace Perpetuum.Items.Templates
{
    public interface IRobotTemplateServices
    {
        IRobotTemplateReader Reader { get; }
        IRobotTemplateRelations Relations { get; }
    }
}