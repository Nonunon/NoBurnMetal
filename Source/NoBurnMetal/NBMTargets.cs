using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NoBurnMetal
{
    public class NBMTarget
    {
        public ThingDef def;
        public bool viaFactor;
        public bool curated;
        public float? original;

        public string Key => def.defName;

        private List<StatModifier> Mods => viaFactor ? def.stuffProps.statFactors : def.statBases;

        public StatModifier Entry => Mods?.Find(m => m.stat == StatDefOf.Flammability);

        public void Apply(bool fireproof)
        {
            var entry = Entry;
            if (fireproof)
            {
                if (entry != null)
                {
                    entry.value = 0f;
                    return;
                }
                var mod = new StatModifier { stat = StatDefOf.Flammability, value = 0f };
                if (viaFactor) (def.stuffProps.statFactors ??= new List<StatModifier>()).Add(mod);
                else (def.statBases ??= new List<StatModifier>()).Add(mod);
            }
            else if (entry != null)
            {
                if (original.HasValue) entry.value = original.Value;
                else Mods.Remove(entry);
            }
        }
    }

    public class NBMSection
    {
        public string id;
        public string label;
        public int order;
        public bool active = true;
        public List<NBMTarget> items = new List<NBMTarget>();
    }

    [StaticConstructorOnStartup]
    public static class NBM
    {
        public static readonly List<NBMSection> Sections = new List<NBMSection>();
        public static readonly List<NBMTarget> Targets = new List<NBMTarget>();

        static NBM()
        {
            Discover();
            foreach (var target in Targets.Where(IsOn))
                target.Apply(true);
            ClearStatCache();
        }

        private static void Discover()
        {
            var extras = new HashSet<string>();
            var known = new List<NBMKnownMod>();
            foreach (var def in DefDatabase<NBMTargetsDef>.AllDefs)
            {
                extras.UnionWith(def.defNames);
                known.AddRange(def.knownMods);
            }

            var sections = new Dictionary<string, NBMSection>();

            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                bool curated = extras.Contains(def.defName);
                bool metallic = def.stuffProps?.categories?.Contains(StuffCategoryDefOf.Metallic) == true;
                if (!curated && !metallic) continue;

                var target = new NBMTarget { def = def, viaFactor = def.stuffProps != null, curated = curated };
                target.original = target.Entry?.value;
                if (target.original == 0f) continue;

                var pack = def.modContentPack;
                string id = pack.PackageId;
                if (!sections.TryGetValue(id, out var section))
                {
                    section = new NBMSection
                    {
                        id = id,
                        label = pack.Name,
                        order = id == ModContentPack.CoreModPackageId.ToLowerInvariant() ? 0 : id.StartsWith("ludeon.") ? 1 : 2
                    };
                    sections[id] = section;
                }
                section.items.Add(target);
                Targets.Add(target);
            }

            foreach (var section in sections.Values)
                section.items.Sort((a, b) => string.Compare(a.def.label, b.def.label, StringComparison.OrdinalIgnoreCase));

            foreach (var km in known)
            {
                if (ModLister.GetActiveModWithIdentifier(km.packageId, true) != null) continue;
                string id = "inactive:" + km.packageId.ToLowerInvariant();
                sections[id] = new NBMSection { id = id, label = km.label, order = 3, active = false };
            }

            Sections.AddRange(sections.Values.OrderBy(s => s.order).ThenBy(s => s.label, StringComparer.OrdinalIgnoreCase));
        }

        public static bool IsOn(NBMTarget target) => NoBurnMetalMod.Settings.IsOn(target.Key, target.curated);

        public static void SetOn(NBMTarget target, bool on)
        {
            NoBurnMetalMod.Settings.Set(target.Key, on);
            target.Apply(on);
            ClearStatCache();
        }

        private static void ClearStatCache() => StatDefOf.Flammability.Worker.TryClearCache();
    }
}
