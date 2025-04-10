﻿using System;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using JM.LinqFaster;
using Vector2 = System.Numerics.Vector2;
using RectangleF = ExileCore2.Shared.RectangleF;

namespace DPSMeter
{
    public class CacheLife
    {
        public long Life { get; set; }
    }

    public class DpsMeter : BaseSettingsPlugin<DPSMeterSettings>
    {
        private const double DPS_PERIOD = 0.2;
        private readonly string aoe_dps = "aoe dps";
        private readonly string aoe_hit = "aoe hit";
        private double[] AOEDamageMemory = new double[20];
        private double CurrentDmgAoe;
        private double CurrentDmgSingle;
        private double CurrentDpsAoe;
        private double CurrentDpsSingle;
        private readonly string dps = "dps";
        private readonly string hit = "hit";
        private readonly string hp = "hp";
        private DateTime lastTime;
        private readonly string max_aoe_dps = "max aoe dps";
        private readonly string max_dps = "max dps";
        private double MaxDpsAoe;
        private double MaxDpsSingle;
        private double[] SingleDamageMemory = new double[20];
        private double time;
        public double TotalLifeAroundMonster;
        private double LockedPeakHit;

        public DpsMeter()
        {
            Order = -20;
        }

        public override void OnLoad()
        {
            CanUseMultiThreading = true;
        }

        public override void AreaChange(AreaInstance area)
        {
            Clear(area);
        }

        public override bool Initialise()
        {
            GameController.LeftPanel.WantUse(() => Settings.Enable);
            return true;
        }

        private void Clear(AreaInstance area)
        {
            MaxDpsAoe = 0;
            MaxDpsSingle = 0;
            CurrentDpsAoe = 0;
            CurrentDpsSingle = 0;
            CurrentDmgAoe = 0;
            CurrentDmgSingle = 0;
            lastTime = DateTime.Now;
            SingleDamageMemory = new double[20];
            AOEDamageMemory = new double[20];
            LockedPeakHit = 0;
        }

        public override void Tick()
        {
            if (!Settings.Enable) return;

            //if (Settings.MultiThreading)
            //    return GameController.MultiThreadManager.AddJob(TickLogic, nameof(DpsMeter));

            TickLogic();
            return;
        }

        private void TickLogic()
        {
            time += GameController.DeltaTime;

            if (time >= Settings.UpdateTime)
            {
                time = 0;
                CalculateDps(out var aoe, out var single);

                //Shift array
                //{ 1, 2, 3, 4, 5 }
                //{ 0, 1, 2, 3, 4 }
                Array.Copy(AOEDamageMemory, 0, AOEDamageMemory, 1, AOEDamageMemory.Length - 1);
                Array.Copy(SingleDamageMemory, 0, SingleDamageMemory, 1, SingleDamageMemory.Length - 1);

                AOEDamageMemory[0] = aoe;
                SingleDamageMemory[0] = single;

                if (single > 0)
                {
                    CurrentDmgAoe = aoe;
                    CurrentDmgSingle = single;

                    CurrentDpsAoe = AOEDamageMemory.SumF();
                    CurrentDpsSingle = SingleDamageMemory.SumF();

                    MaxDpsAoe = Math.Max(CurrentDpsAoe, MaxDpsAoe);
                    MaxDpsSingle = Math.Max(CurrentDpsSingle, MaxDpsSingle);

                    if (Settings.ShowPeakHit.Value)
                    {
                        LockedPeakHit = Math.Max(LockedPeakHit, single);
                    }
                }
            }
        }

        private void CalculateDps(out long aoeDamage, out long singleDamage)
        {
            TotalLifeAroundMonster = 0;
            aoeDamage = 0;
            singleDamage = 0;

            foreach (var monster in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
            {
                var cacheLife = monster.GetHudComponent<CacheLife>();
                if (cacheLife == null) continue;
                var life = monster.GetComponent<Life>();
                if (life == null) continue;

                if (!monster.IsAlive && Settings.HasCullingStrike.Value)
                    continue;

                var hp = monster.IsAlive ? life.CurHP + life.CurES : 0;

                if (hp > -1 && hp < 30000000)
                {
                    TotalLifeAroundMonster += hp;

                    if (cacheLife.Life != hp)
                    {
                        var dmg = cacheLife.Life - hp;

                        if (dmg > life.MaxHP + life.MaxES)
                            dmg = life.MaxHP + life.MaxES;

                        aoeDamage += dmg;
                        singleDamage = Math.Max(singleDamage, dmg);
                    }

                    cacheLife.Life = hp;
                }
            }
        }

        public override void EntityAdded(Entity Entity)
        {
            if (!Entity.HasComponent<Monster>() || !Entity.IsHostile || !Entity.IsAlive) return;

            var life = Entity.GetComponent<Life>();

            if (life != null)
                Entity.SetHudComponent(new CacheLife {Life = life.CurHP > 0 ? life.CurHP + life.CurES : 0});
        }

        private string SimplifyNumber(double number)
        {
            if (number >= 1000000 && Settings.SimplifyDpsNumbers)
                return (number / 1000000).ToString("0.##") + "M";
            if (number >= 1000 && Settings.SimplifyDpsNumbers)
                return (number / 1000).ToString("0.##") + "k";
            return number.ToString("N0");
        }

        public override void Render()
        {
            if (GameController.Area.CurrentArea == null || !Settings.ShowInTown && GameController.Area.CurrentArea.IsTown ||
                !Settings.ShowInTown && GameController.Area.CurrentArea.IsHideout)
                return;

            // Store the original value of StartDrawPoint
            var originalStartDrawPoint = GameController.LeftPanel.StartDrawPoint;

            var position = Settings.DisplayPosition.Value;
            var startY = position.Y;
            var measury = Graphics.MeasureText($"12345678 {max_aoe_dps}");
            var positionLeft = position.Translate(-measury.X, 0);
            Vector2 drawText;

            if (Settings.ShowCurrentHitDamage.Value)
            {
                if (Settings.ShowAOE.Value)
                {
                    drawText = Graphics.DrawText(SimplifyNumber(CurrentDmgAoe), positionLeft, Settings.DpsFontColor, FontAlign.Left);
                    drawText = Graphics.DrawText(aoe_hit, position, Settings.DpsFontColor, FontAlign.Right);
                    position.Y += drawText.Y;
                    positionLeft.Y += drawText.Y;
                }

                drawText = Graphics.DrawText(SimplifyNumber(CurrentDmgSingle), positionLeft, Settings.DpsFontColor, FontAlign.Left);
                drawText = Graphics.DrawText(hit, position, Settings.DpsFontColor, FontAlign.Right);
                position.Y += drawText.Y;
                positionLeft.Y += drawText.Y;
            }

            if (Settings.ShowPeakHit.Value)
            {
                drawText = Graphics.DrawText(SimplifyNumber(LockedPeakHit), positionLeft, Settings.PeakFontColor, FontAlign.Left);
                drawText = Graphics.DrawText("peak hit", position, Settings.PeakFontColor, FontAlign.Right);
                position.Y += drawText.Y;
                positionLeft.Y += drawText.Y;
            }

            if (Settings.ShowAOE.Value)
            {
                drawText = Graphics.DrawText(SimplifyNumber(CurrentDpsAoe), positionLeft, Settings.PeakFontColor, FontAlign.Left);
                drawText = Graphics.DrawText(aoe_dps, position, Settings.PeakFontColor, FontAlign.Right);
                position.Y += drawText.Y;
                positionLeft.Y += drawText.Y;
            }

            drawText = Graphics.DrawText(SimplifyNumber(CurrentDpsSingle), positionLeft, Settings.PeakFontColor, FontAlign.Left);
            drawText = Graphics.DrawText(dps, position, Settings.PeakFontColor, FontAlign.Right);
            position.Y += drawText.Y;
            positionLeft.Y += drawText.Y;

            if (Settings.ShowAOE.Value)
            {
                drawText = Graphics.DrawText(SimplifyNumber(MaxDpsAoe), positionLeft, Settings.PeakFontColor, FontAlign.Left);
                drawText = Graphics.DrawText(max_aoe_dps, position, Settings.PeakFontColor, FontAlign.Right);
                position.Y += drawText.Y;
                positionLeft.Y += drawText.Y;
            }

            drawText = Graphics.DrawText(SimplifyNumber(MaxDpsSingle), positionLeft, Settings.PeakFontColor, FontAlign.Left);
            drawText = Graphics.DrawText(max_dps, position, Settings.PeakFontColor, FontAlign.Right);
            position.Y += drawText.Y;
            positionLeft.Y += drawText.Y;

            drawText = Graphics.DrawText(SimplifyNumber(TotalLifeAroundMonster), positionLeft, Settings.PeakFontColor, FontAlign.Left);
            drawText = Graphics.DrawText(hp, position, Settings.PeakFontColor, FontAlign.Right);
            position.Y += drawText.Y;
            positionLeft.Y += drawText.Y;
            var bounds = new RectangleF(positionLeft.X - 50, startY + 3, measury.X + 50, position.Y - startY);

            // Restore the original value of StartDrawPoint
            GameController.LeftPanel.StartDrawPoint = originalStartDrawPoint;
        }
    }
}
