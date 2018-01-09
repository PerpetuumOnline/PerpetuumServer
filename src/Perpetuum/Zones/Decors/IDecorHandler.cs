using System.Collections.Generic;

namespace Perpetuum.Zones.Decors
{
    public interface IDecorHandler
    {
        IDictionary<string,object> DecorObjectsToDictionary();
        ErrorCodes InsertDecorSql(DecorDescription decorDescription, out int newId);
        void SetDecor(DecorDescription decorDescription);
        void SpreadDecorChanges(DecorDescription decorDescription);
        IDictionary<int, DecorDescription> Decors { get; }
        ErrorCodes UpdateDecorSql(DecorDescription decorDescription);
        ErrorCodes DeleteDecorSql(int id);
        void DeleteDecor(int id);
        void SpreadDecorDelete(int id);
        ErrorCodes DrawDecorEnvironment(int decorId);
        ErrorCodes SampleDecorEnvironment(int decorId, int range, out Dictionary<string, object> dictionary);
    }
}