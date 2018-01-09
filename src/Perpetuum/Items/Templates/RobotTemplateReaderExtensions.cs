using System.Linq;

namespace Perpetuum.Items.Templates
{
    public static class RobotTemplateReaderExtensions
    {
        [CanBeNull]
        public static RobotTemplate GetByName(this IRobotTemplateReader reader, string name)
        {
            return reader.GetAll().FirstOrDefault(t => t.Name == name);
        }
    }
}