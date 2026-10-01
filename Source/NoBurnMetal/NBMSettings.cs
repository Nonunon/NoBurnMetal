using System.Collections.Generic;
using Verse;

namespace NoBurnMetal
{
    public class NBMSettings : ModSettings
    {
        public HashSet<string> enabled = new HashSet<string>();
        public HashSet<string> disabled = new HashSet<string>();
        public HashSet<string> collapsed = new HashSet<string>();
        public bool hideInactiveMods = true;

        public bool IsOn(string defName, bool defaultOn)
        {
            if (enabled.Contains(defName)) return true;
            if (disabled.Contains(defName)) return false;
            return defaultOn;
        }

        public void Set(string defName, bool on)
        {
            if (on)
            {
                disabled.Remove(defName);
                enabled.Add(defName);
            }
            else
            {
                enabled.Remove(defName);
                disabled.Add(defName);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref enabled, "enabled", LookMode.Value);
            Scribe_Collections.Look(ref disabled, "disabled", LookMode.Value);
            Scribe_Collections.Look(ref collapsed, "collapsed", LookMode.Value);
            Scribe_Values.Look(ref hideInactiveMods, "hideInactiveMods", true);
            enabled ??= new HashSet<string>();
            disabled ??= new HashSet<string>();
            collapsed ??= new HashSet<string>();
        }
    }
}
