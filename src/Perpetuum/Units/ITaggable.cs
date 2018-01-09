using System;
using Perpetuum.Players;

namespace Perpetuum.Units
{
    public interface ITaggable
    {
        void Tag(Player tagger, TimeSpan duration);
        Player GetTagger();
    }
}