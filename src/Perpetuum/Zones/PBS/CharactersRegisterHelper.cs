using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Units;
using Perpetuum.Zones.ProximityProbes;

namespace Perpetuum.Zones.PBS
{
    public class CharactersRegisterHelper<T> : ICharactersRegistered where T : Unit
    {
        private readonly T _sourceUnit;
        private Character[] _registeredCharacters;

        public CharactersRegisterHelper(T sourceUnit)
        {
            _sourceUnit = sourceUnit;
        }

        //ezt kell hivogatni requestbol, ha valtozott
        public void ReloadRegistration()
        {
            _registeredCharacters = null;
        }

        public Character[] GetRegisteredCharacters()
        {
            return LazyInitializer.EnsureInitialized(ref _registeredCharacters, () => PBSRegisterHelper.GetRegisteredMembers(_sourceUnit.Eid).ToArray());
        }

        public int GetMaxRegisteredCount()
        {
            var corporation = Corporation.GetOrThrow(_sourceUnit.Owner);
            return corporation.GetMaximumRegisteredProbesAmount();
        }
    }
}