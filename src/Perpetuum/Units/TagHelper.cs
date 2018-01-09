using System;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;

namespace Perpetuum.Units
{
    public class TagHelper
    {
        public void DoTagging<T>(T target,Player tagger,TimeSpan duration) where T:Unit,ITaggable
        {
            var currentTagger = target.GetTagger();
            if (currentTagger != null)
                return;

            var builder = target.NewEffectBuilder().SetType(EffectType.effect_tag).SetSource(tagger).WithDuration(duration);
            target.ApplyEffect(builder);
        }

        [CanBeNull]
        public static Player GetTagger<T>(T tagable) where T:Unit,ITaggable
        {
            var zone = tagable.Zone;
            if (zone == null)
                return null;

            var tag = tagable.EffectHandler.GetEffectsByType(EffectType.effect_tag).FirstOrDefault();
            return tag?.Source as Player;
        }
    }
}