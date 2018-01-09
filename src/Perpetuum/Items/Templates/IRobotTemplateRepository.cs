namespace Perpetuum.Items.Templates
{
    public interface IRobotTemplateRepository : IRobotTemplateReader,IRepository<int,RobotTemplate>
    {
        void DeleteByID(int templateID);
    }
}