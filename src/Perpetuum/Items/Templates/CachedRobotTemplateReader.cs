using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Items.Templates
{
    public class CachedRobotTemplateReader : IRobotTemplateReader
    {
        private readonly IRobotTemplateReader _reader;
        private Dictionary<int, RobotTemplate> _templates;

        public CachedRobotTemplateReader(IRobotTemplateReader reader)
        {
            _reader = reader;
        }

        public void Init()
        {
            _templates = _reader.GetAll().ToDictionary(t => t.ID);
        }

        public RobotTemplate Get(int templateID)
        {
            return _templates.GetOrDefault(templateID);
        }

        public IEnumerable<RobotTemplate> GetAll()
        {
            return _templates.Values;
        }
    }
}