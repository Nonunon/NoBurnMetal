using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoBurnMetal
{
    public class NoBurnMetalMod : Mod
    {
        public static NBMSettings Settings;

        private const float RowHeight = 28f;
        private const float HeaderHeight = 30f;
        private const float Gap = 6f;

        private static readonly Color HeaderColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color Dim = new Color(1f, 1f, 1f, 0.5f);

        private Vector2 scroll;
        private string search = "";

        private struct Row
        {
            public NBMSection section;
            public NBMTarget target;
            public float y, height;
        }

        public NoBurnMetalMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NBMSettings>();
        }

        public override string SettingsCategory() => "NBM_ModName".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float y = inRect.y;
            DrawToolbar(inRect, ref y);

            var rows = BuildRows(out float totalHeight);
            var outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            var viewRect = new Rect(0f, 0f, outRect.width - 18f, totalHeight);

            Widgets.BeginScrollView(outRect, ref scroll, viewRect);
            if (rows.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Dim;
                Widgets.Label(new Rect(0f, 20f, viewRect.width, 30f), "NBM_NoResults".Translate());
                GUI.color = Color.white;
            }

            foreach (var row in rows)
            {
                if (row.y + row.height < scroll.y || row.y > scroll.y + outRect.height) continue;
                var rect = new Rect(0f, row.y, viewRect.width, row.height);
                if (row.target == null) DrawHeader(rect, row.section);
                else DrawItem(rect, row.target);
            }
            Widgets.EndScrollView();

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawToolbar(Rect inRect, ref float y)
        {
            const float bw = 120f;
            var searchRect = new Rect(inRect.x, y, 260f, 28f);
            search = Widgets.TextField(searchRect, search);
            if (search.NullOrEmpty())
            {
                GUI.color = Dim;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(searchRect.x + 6f, y, searchRect.width - 12f, 28f), "NBM_Search".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            float x = inRect.xMax - bw;
            if (Widgets.ButtonText(new Rect(x, y, bw, 28f), "NBM_DisableAll".Translate())) SetAll(false);
            x -= bw + 4f;
            if (Widgets.ButtonText(new Rect(x, y, bw, 28f), "NBM_EnableAll".Translate())) SetAll(true);
            x -= bw + 4f;
            if (Widgets.ButtonText(new Rect(x, y, bw, 28f), "NBM_CollapseAll".Translate()))
                Settings.collapsed.UnionWith(NBM.Sections.Where(s => s.active).Select(s => s.id));
            x -= bw + 4f;
            if (Widgets.ButtonText(new Rect(x, y, bw, 28f), "NBM_ExpandAll".Translate())) Settings.collapsed.Clear();
            y += 32f;

            var hideRect = new Rect(inRect.x, y, 360f, 24f);
            Widgets.CheckboxLabeled(hideRect, "NBM_HideInactive".Translate(), ref Settings.hideInactiveMods);
            TooltipHandler.TipRegion(hideRect, "NBM_HideInactiveTip".Translate());

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = Dim;
            Widgets.Label(new Rect(inRect.xMax - 360f, y, 360f, 24f),
                "NBM_Summary".Translate(NBM.Targets.Count(NBM.IsOn), NBM.Targets.Count));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            y += 30f;
        }

        private static bool Has(string text, string needle) =>
            text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private List<Row> BuildRows(out float totalHeight)
        {
            var rows = new List<Row>();
            float y = 0f;
            bool searching = !search.NullOrEmpty();

            foreach (var section in NBM.Sections)
            {
                if (!section.active)
                {
                    if (Settings.hideInactiveMods || searching && !Has(section.label, search)) continue;
                    rows.Add(new Row { section = section, y = y, height = HeaderHeight });
                    y += HeaderHeight + Gap;
                    continue;
                }

                var items = section.items
                    .Where(t => !searching || Has(section.label, search) || Has(t.def.label, search) || Has(t.def.defName, search))
                    .ToList();
                if (items.Count == 0) continue;

                rows.Add(new Row { section = section, y = y, height = HeaderHeight });
                y += HeaderHeight;

                if (searching || !Settings.collapsed.Contains(section.id))
                {
                    foreach (var item in items)
                    {
                        rows.Add(new Row { section = section, target = item, y = y, height = RowHeight });
                        y += RowHeight;
                    }
                }
                y += Gap;
            }

            totalHeight = y;
            return rows;
        }

        private void DrawHeader(Rect rect, NBMSection section)
        {
            Widgets.DrawBoxSolid(rect, HeaderColor);
            Text.Anchor = TextAnchor.MiddleLeft;

            if (!section.active)
            {
                GUI.color = Dim;
                Widgets.Label(new Rect(rect.x + 32f, rect.y, rect.width - 40f, rect.height),
                    section.label + "  (" + "NBM_NotActive".Translate() + ")");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            bool collapsed = Settings.collapsed.Contains(section.id);
            const float bw = 80f;
            var toggleArea = new Rect(rect.x, rect.y, rect.width - bw * 2f - 130f, rect.height);

            Widgets.DrawHighlightIfMouseover(toggleArea);
            if (Widgets.ButtonInvisible(toggleArea))
            {
                if (collapsed) Settings.collapsed.Remove(section.id);
                else Settings.collapsed.Add(section.id);
            }
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 6f, 18f, 18f), collapsed ? TexButton.Plus : TexButton.Minus);
            Widgets.Label(new Rect(rect.x + 32f, rect.y, toggleArea.width - 32f, rect.height), section.label);

            GUI.color = Dim;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.xMax - bw * 2f - 130f, rect.y, 80f, rect.height),
                section.items.Count(NBM.IsOn) + " / " + section.items.Count);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            var offRect = new Rect(rect.xMax - bw - 4f, rect.y + 2f, bw, rect.height - 4f);
            var onRect = new Rect(offRect.x - bw - 4f, offRect.y, bw, offRect.height);
            if (Widgets.ButtonText(onRect, "NBM_AllOn".Translate()))
                foreach (var t in section.items) NBM.SetOn(t, true);
            if (Widgets.ButtonText(offRect, "NBM_AllOff".Translate()))
                foreach (var t in section.items) NBM.SetOn(t, false);
        }

        private void DrawItem(Rect rect, NBMTarget target)
        {
            bool on = NBM.IsOn(target);
            var def = target.def;

            Widgets.DrawHighlightIfMouseover(rect);
            if (Widgets.ButtonInvisible(new Rect(rect.x, rect.y, rect.width - 40f, rect.height)))
            {
                on = !on;
                NBM.SetOn(target, on);
            }

            Widgets.DefIcon(new Rect(rect.x + 30f, rect.y + 2f, 24f, 24f), def);

            Text.Anchor = TextAnchor.MiddleLeft;
            string label = def.LabelCap;
            float labelWidth = Text.CalcSize(label).x;
            Widgets.Label(new Rect(rect.x + 62f, rect.y, labelWidth + 4f, rect.height), label);

            GUI.color = Dim;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 66f + labelWidth, rect.y, 300f, rect.height), def.defName);
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.xMax - 220f, rect.y, 170f, rect.height),
                on ? "NBM_StateOn".Translate() : "NBM_StateOff".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            bool check = on;
            Widgets.Checkbox(new Vector2(rect.xMax - 38f, rect.y + 2f), ref check, 24f);
            if (check != on) NBM.SetOn(target, check);

            string original = target.original?.ToString("0.##") ?? "NBM_Unset".Translate().ToString();
            string stat = (target.viaFactor ? "NBM_Factor" : "NBM_Base").Translate();
            TooltipHandler.TipRegion(rect, "NBM_ItemTip".Translate(def.defName, def.modContentPack.Name, stat, original));
        }

        private static void SetAll(bool on)
        {
            foreach (var t in NBM.Targets) NBM.SetOn(t, on);
        }
    }
}
