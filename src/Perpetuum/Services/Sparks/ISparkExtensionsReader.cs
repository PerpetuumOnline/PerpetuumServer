using System.Collections.Generic;

namespace Perpetuum.Services.Sparks
{
    public interface ISparkExtensionsReader
    {
        IEnumerable<SparkExtension> GetAllBySparkID(int sparkID);
    }
}