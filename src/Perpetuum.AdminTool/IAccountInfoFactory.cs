using System.Collections.Generic;

namespace Perpetuum.AdminTool
{
    public interface IAccountInfoFactory
    {
        AccountInfo Create(Dictionary<string,object> accountData,IEnumerable<CharacterInfo> characters);
        AccountInfo CreateEmpty();
    }
}