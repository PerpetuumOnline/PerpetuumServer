using System;
using System.Collections.Generic;


namespace Perpetuum.Services.Relics
{
    /// <summary>
    /// Specification interface for RelicManagers
    /// </summary>
    public interface IRelicManager
    {
        void Start();
        void Stop();
        bool ForceSpawnRelicAt(int x, int y);
        List<Dictionary<string, object>> GetRelicListDictionary();
        void Update(TimeSpan time);
    }
}
