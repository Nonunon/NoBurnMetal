using System.Collections.Generic;
using Verse;

namespace NoBurnMetal
{
    public class NBMTargetsDef : Def
    {
        public List<string> defNames = new List<string>();
        public List<NBMKnownMod> knownMods = new List<NBMKnownMod>();
    }

    public class NBMKnownMod
    {
        public string packageId;
        public string label;
    }
}
