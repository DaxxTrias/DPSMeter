﻿using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Numerics;

namespace DPSMeter
{
    public class DPSMeterSettings : ISettings
    {
        public DPSMeterSettings()
        {
            ShowInTown = new ToggleNode(false);
            ClearNode = new ButtonNode();
            ShowPeakHit = new ToggleNode(false);
            SimplifyDpsNumbers = new ToggleNode(false);
        }

        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        [Menu("Show in Town and Hideout")]
        public ToggleNode ShowInTown { get; set; }

        // public RangeNode<int> TextSize { get; set; } = new RangeNode<int>(16, 10, 20);
        public ColorNode DpsFontColor { get; set; } = System.Drawing.Color.LightSkyBlue;
        public ColorNode PeakFontColor { get; set; } = System.Drawing.Color.OrangeRed;
        public ColorNode BackgroundColor { get; set; } = System.Drawing.Color.Transparent;
        public ButtonNode ClearNode { get; set; }
        [Menu("Show AOE")]
        public ToggleNode ShowAOE { get; set; } = new ToggleNode(true);
        public ToggleNode ShowCurrentHitDamage { get; set; } = new ToggleNode(true);
        [Menu("Show Peak Hit Damage", "Track highest single hit damage")]
        public ToggleNode ShowPeakHit { get; set; }
        [Menu("Simplify DPS Numbers", "Display DPS numbers in a simplified format (e.g., 100k)")]
        public ToggleNode SimplifyDpsNumbers { get; set; }
        [Menu("Has Culling Strike", "You have a source of culling strike")]
        public ToggleNode HasCullingStrike { get; set; } = new ToggleNode(false);
        [Menu("UpdateTime","milliseconds to wait between dps updates")]
        public RangeNode<int> UpdateTime { get; set; } = new RangeNode<int>(100, 50, 2000);
        [Menu("Display Position", "X and Y coordinates to draw on screen")]
        public RangeNode<Vector2> DisplayPosition { get; set; } = new(new Vector2(160, 160), new Vector2(-3000, -3000), Vector2.One * 4000);

    }
}
