using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Social
{
    public interface ISocialService
    {
        [NotNull]
        ICharacterSocial GetCharacterSocial(Character character);
    }
}