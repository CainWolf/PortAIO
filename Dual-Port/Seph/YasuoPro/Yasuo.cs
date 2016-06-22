﻿using System;
using System.Linq;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
using System.Collections.Generic;
using Valvrave_Sharp.Evade;

namespace YasuoPro
{

    //Credits to Kortatu/Esk0r for his work on Evade which this assembly relies on heavily!

    internal class Yasuo : Helper
    {
        public AIHeroClient CurrentTarget;
        public bool Fleeing;

        public Yasuo()
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        void OnLoad(EventArgs args)
        {
            Yasuo = ObjectManager.Player;

            if (Yasuo.CharData.BaseSkinName != "Yasuo")
            {
                return;
            }
            Chat.Print("<font color='#1d87f2'>YasuoPro by Seph Loaded. Good Luck!</font>");
            Chat.Print("<font color='#1d87f2'>::::New E Mode - To try ---> Combo --> EMode --> Beta</font>");
            InitItems();
            InitSpells();
            YasuoMenu.Init(this);
            if (GetBool("Misc.Walljump", YasuoMenu.MiscM) && Game.MapId == GameMapId.SummonersRift)
            {
                WallJump.Initialize();
            }
            Valvrave_Sharp.Program.MainA();
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnGapClose;
            Interrupter2.OnInterruptableTarget += OnInterruptable;
            Spellbook.OnCastSpell += (sender, eventArgs) =>
            {
                if (sender.Owner.IsMe && eventArgs.Slot == SpellSlot.E)
                {
                    ETarget = (Obj_AI_Base)eventArgs.Target;
                }
            };
        }

        private static Vector3 GetPosAfterDash(Obj_AI_Base target)
        {
            return ObjectManager.Player.ServerPosition.LSExtend(target.ServerPosition, Spells[E].Range);
        }

        void OnUpdate(EventArgs args)
        {
            if (Yasuo.IsDead || Yasuo.IsRecalling())
            {
                return;
            }

            CastUlt();

            if (GetBool("Misc.AutoStackQ", YasuoMenu.MiscM) && !TornadoReady && !CurrentTarget.IsValidEnemy(Spells[Q].Range))
            {
                var closest =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(x => x.IsValidMinion(Spells[Q].Range) && MinionManager.IsMinion(x))
                        .MinOrDefault(x => x.LSDistance(Yasuo));

                var pred = Spells[Q].GetPrediction(closest);
                if (pred.Hitchance >= HitChance.Low)
                {
                    Spells[Q].Cast(closest);
                }
            }

            if (GetBool("Misc.Walljump", YasuoMenu.MiscM) && Game.MapId == GameMapId.SummonersRift)
            {
                WallJump.OnUpdate();
            }

            if (GetKeyBind("Misc.DashMode", YasuoMenu.MiscM))
            {
                MoveToMouse();
                return;
            }

            Fleeing = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee);

            if (GetBool("Killsteal.Enabled", YasuoMenu.KillstealM) && !Fleeing)
            {
                Killsteal();
            }

            if (GetKeyBind("Harass.KB", YasuoMenu.HarassM) && !Fleeing)
            {
                Harass();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Orbwalker.OrbwalkTo(Game.CursorPos);
                Orbwalker.DisableAttacking = false;
                Combo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Orbwalker.OrbwalkTo(Game.CursorPos);
                Orbwalker.DisableAttacking = false;
                Mixed();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Orbwalker.OrbwalkTo(Game.CursorPos);
                Orbwalker.DisableAttacking = false;
                LHSkills();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                Orbwalker.OrbwalkTo(Game.CursorPos);
                Orbwalker.DisableAttacking = false;
                Waveclear();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
        }

        void CastUlt()
        {
            if (!SpellSlot.R.IsReady())
            {
                return;
            }
            if (GetBool("Combo.UseR", YasuoMenu.ComboM) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                CastR(GetSliderInt("Combo.RMinHit", YasuoMenu.ComboM));
            }

            if (GetBool("Misc.AutoR", YasuoMenu.MiscM) && !Fleeing)
            {
                CastR(GetSliderInt("Misc.RMinHit", YasuoMenu.MiscM));
            }
        }

        void OnDraw(EventArgs args)
        {

            if (Debug)
            {
                Drawing.DrawCircle(DashPosition.To3D(), Yasuo.BoundingRadius, System.Drawing
                    .Color.Chartreuse);
            }


            if (Yasuo.IsDead || GetBool("Drawing.Disable", YasuoMenu.DrawingsM))
            {
                return;
            }

            if (GetBool("Misc.Walljump", YasuoMenu.MiscM) && Game.MapId == GameMapId.SummonersRift)
            {
                WallJump.OnDraw();
            }

            var pos = Yasuo.Position.WTS();

            Drawing.DrawText(pos.X - 25, pos.Y - 25, isHealthy ? System.Drawing.Color.Green : System.Drawing.Color.Red, "Healthy: " + isHealthy);

            var drawq = GetCircle("Drawing.DrawQ", YasuoMenu.DrawingsM);
            var drawe = GetCircle("Drawing.DrawE", YasuoMenu.DrawingsM);
            var drawr = GetCircle("Drawing.DrawR", YasuoMenu.DrawingsM);

            if (drawq)
            {
                Render.Circle.DrawCircle(Yasuo.Position, Qrange, System.Drawing.Color.Red);
            }
            if (drawe)
            {
                Render.Circle.DrawCircle(Yasuo.Position, Spells[E].Range, System.Drawing.Color.CornflowerBlue);
            }
            if (drawr)
            {
                Render.Circle.DrawCircle(Yasuo.Position, Spells[R].Range, System.Drawing.Color.DarkOrange);
            }
        }



        void Combo()
        {
            float range = 0;
            if (SpellSlot.R.IsReady())
            {
                range = Spells[R].Range;
            }

            else if (Spells[Q2].IsReady())
            {
                range = Spells[Q2].Range;
            }

            else if (Spells[E].IsReady())
            {
                range = Spells[E].Range;
            }

            CurrentTarget = TargetSelector.GetTarget(range, DamageType.Physical);


            if (GetBool("Combo.UseEQ", YasuoMenu.ComboM))
            {
                PerformEQ();
            }

            CastQ(CurrentTarget);

            if (GetBool("Combo.UseE", YasuoMenu.ComboM))
            {
                var emode = GetEMode();
                if (emode == EMode.New)
                {
                    CastENew();
                }
                else
                {
                    CastEOld(CurrentTarget);
                }
            }

            if (GetBool("Items.Enabled", YasuoMenu.ComboM))
            {
                if (GetBool("Items.UseTIA", YasuoMenu.ComboM))
                {
                    Tiamat.Cast(null);
                }
                if (GetBool("Items.UseHDR", YasuoMenu.ComboM))
                {
                    Hydra.Cast(null);
                }
                if (GetBool("Items.UseTitanic", YasuoMenu.ComboM))
                {
                    Titanic.Cast(null);
                }
                if (GetBool("Items.UseBRK", YasuoMenu.ComboM) && CurrentTarget != null)
                {
                    Blade.Cast(CurrentTarget);
                }
                if (GetBool("Items.UseBLG", YasuoMenu.ComboM) && CurrentTarget != null)
                {
                    Bilgewater.Cast(CurrentTarget);
                }
                if (GetBool("Items.UseYMU", YasuoMenu.ComboM))
                {
                    Youmu.Cast(null);
                }
            }
        }

        internal void CastQ(AIHeroClient target)
        {
            if (target != null && !target.IsInRange(Qrange))
            {
                target = TargetSelector.GetTarget(Qrange, DamageType.Physical);
            }

            if (target != null)
            {
                if (Spells[Q].IsReady() && target.IsValidEnemy(Qrange))
                {
                    UseQ(target, GetHitChance("Hitchance.Q"), GetBool("Combo.UseQ", YasuoMenu.ComboM), GetBool("Combo.UseQ2", YasuoMenu.ComboM));
                    return;
                }

                if (GetBool("Combo.StackQ", YasuoMenu.ComboM) && !target.IsValidEnemy(Qrange) && !TornadoReady)
                {
                    var bestmin =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(x => x.IsValidMinion(Qrange) && MinionManager.IsMinion(x, false))
                            .MinOrDefault(x => x.LSDistance(Yasuo));
                    if (bestmin != null)
                    {
                        var pred = Spells[Q].GetPrediction(bestmin);

                        if (pred.Hitchance >= HitChance.Medium)
                        {
                            Spells[Q].Cast(bestmin);
                        }
                    }
                }
            }
        }

        class PathResult
        {
            public List<Obj_AI_Minion> minionPath = new List<Obj_AI_Minion>();
            public bool foundPath;
            public int numberOfHops;
        }

        //minimum amount of minions
        PathResult GetClosestPath(AIHeroClient target, int minioncount)
        {
            var result = new PathResult();
            var dist = Yasuo.Distance(target);
            var pos = Yasuo.ServerPosition.To2D();
            var minionsinbw =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.Distance(target) <= dist && x.Distance(Yasuo) < dist)
                    .OrderBy(x => x.Distance(Yasuo));
            result.minionPath = new List<Obj_AI_Minion>();
            foreach (var minion in minionsinbw)
            {
                if (result.numberOfHops > minioncount)
                {
                    result.foundPath = false;
                    break;
                }
                if (pos.Distance(target) <= Spells[E].Range)
                {
                    result.foundPath = true;
                    break;
                }

                if (minion.IsDashable(40000) && minion.Distance(pos) <= Spells[E].Range)
                {
                    result.numberOfHops++;
                    result.minionPath.Add(minion);
                    pos = GetDashPos(minion);
                }
                else
                {
                    result.foundPath = false;
                }
            }
            return result;
        }

        PathResult IsPossibleToDash(AIHeroClient target, int jumpcount)
        {
            var result = new PathResult();
            var dist = Yasuo.Distance(target);
            var pos = Yasuo.ServerPosition.To2D();
            var minionsinbw =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.Distance(target) <= dist && x.Distance(Yasuo) < dist)
                    .OrderBy(x => x.Distance(Yasuo));
            result.foundPath = false;
            foreach (var minion in minionsinbw)
            {
                if (result.numberOfHops > jumpcount)
                {
                    result.foundPath = false;
                    return result;
                }
                if (pos.Distance(target) <= Yasuo.AttackRange || (target.IsDashable() && pos.Distance(target) <= Spells[E].Range))
                {
                    result.foundPath = true;
                    return result;
                }

                if (pos.Distance(minion) > Spells[E].Range)
                {
                    continue;
                }

                if (minion.IsDashable(40000))
                {
                    result.numberOfHops++;
                    result.minionPath.Add(minion);
                    pos = GetDashPos(minion);
                }

                else
                {
                    continue;
                }
            }

            return result;
        }

        public class TargetSelectorA
        {
            public delegate bool TargetSelectionConditionDelegate(AIHeroClient target);
        }


        void CastENew()
        {
            if (Yasuo.LSIsDashing())
            {
                return;
            }

            if (isHealthy)
            {
                AIHeroClient best = null;
                TargetSelectorA.TargetSelectionConditionDelegate condition = delegate (AIHeroClient target)
                {
                    return IsPossibleToDash(target, 3).foundPath;
                };

                best = TargetSelector.GetTarget(Spells[E].Range * 4, DamageType.Physical);

                if (best != null)
                {
                    if (best.Distance(Yasuo) < 0.30 * Orbwalking.GetRealAutoAttackRange(best))
                    {
                        return;
                    }


                    if (best.IsValidTarget(Spells[E].Range))
                    {
                        Spells[E].Cast(best);
                    }

                    else
                    {
                        var getPath = GetClosestPath(best, 3);
                        if (getPath.foundPath)
                        {
                            var min = getPath.minionPath.FirstOrDefault();
                            if (min != null && min.IsValidTarget() && ((GetBool("Combo.ETower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM)) || !GetDashPos(min).PointUnderEnemyTurret()))
                            {
                                Spells[E].CastOnUnit(min);
                            }
                        }
                    }
                }
            }

            else if (!Yasuo.LSIsDashing() && GetBool("Combo.EToSafety", YasuoMenu.ComboM))
            {
                var bestmin =
                    ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsDashable() && GetDashPos(x).Distance(shop.Position) < Yasuo.Distance(shop.Position)).MinOrDefault(x => GetDashPos(x).Distance(shop.Position));
                if (bestmin != null)
                {
                    Spells[E].Cast(bestmin);
                }
            }
        }

        internal void CastEOld(AIHeroClient target, bool force = false)
        {
            var minionsinrange = ObjectManager.Get<Obj_AI_Minion>().Any(x => x.IsDashable());
            if (target == null || !target.IsInRange(minionsinrange ? Spells[E].Range * 2 : Spells[E].Range))
            {
                target = TargetSelector.GetTarget(minionsinrange ? Spells[E].Range * 2 : Spells[E].Range, DamageType.Physical);
            }

            if (target != null)
            {
                if (SpellSlot.E.IsReady() && isHealthy && target.LSDistance(Yasuo) >= 0.30 * Yasuo.AttackRange)
                {
                    if (TornadoReady && ((GetBool("Combo.ETower", YasuoMenu.ComboM) && GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM)) || !GetDashPos(target).PointUnderEnemyTurret()))
                    {
                        Spells[E].CastOnUnit(target);
                        return;
                    }

                    if (DashCount >= 1 && GetDashPos(target).IsCloser(target) && target.IsDashable() &&
                        (GetBool("Combo.ETower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !GetDashPos(target).PointUnderEnemyTurret()))
                    {
                        ETarget = target;
                        Spells[E].CastOnUnit(target);
                        return;
                    }

                    if (DashCount == 0)
                    {
                        var dist = Yasuo.LSDistance(target);

                        var bestminion =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(
                                    x =>
                                         x.IsDashable()
                                         && GetDashPos(x).IsCloser(target) &&
                                        (GetBool("Combo.ETower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !GetDashPos(x).PointUnderEnemyTurret()))
                                .OrderBy(x => Vector2.Distance(GetDashPos(x), target.ServerPosition.LSTo2D()))
                                .FirstOrDefault();
                        if (bestminion != null)
                        {
                            ETarget = bestminion;
                            Spells[E].CastOnUnit(bestminion);
                        }

                        else if (target.IsDashable() && GetDashPos(target).IsCloser(target) && (GetBool("Combo.ETower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !GetDashPos(target).PointUnderEnemyTurret()))
                        {
                            ETarget = target;
                            Spells[E].CastOnUnit(target);
                        }
                    }


                    else
                    {
                        var minion =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(x => x.IsDashable() && GetDashPos(x).IsCloser(target) && (GetBool("Combo.ETower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !GetDashPos(x).PointUnderEnemyTurret()))
                                .OrderBy(x => GetDashPos(x).LSDistance(target.ServerPosition)).FirstOrDefault();

                        if (minion != null && GetDashPos(minion).IsCloser(target))
                        {
                            ETarget = minion;
                            Spells[E].CastOnUnit(minion);
                        }
                    }
                }
            }
        }

        internal void PerformEQ()
        {
            if (ETarget != null && ETarget.IsValid && !ETarget.ECanKill() && Yasuo.LSIsDashing() && Spells[Q].IsReady())
            {
                if (!TornadoReady)
                {
                    Spells[Q].Cast(ETarget);
                }
                else
                {
                    if (ETarget is AIHeroClient)
                    {
                        Spells[Q].Cast(ETarget);
                    }
                }
            }
        }

        void CastR(float minhit = 1)
        {
            UltMode ultmode = GetUltMode();

            IOrderedEnumerable<AIHeroClient> ordered = null;


            if (ultmode == UltMode.Health)
            {
                ordered = KnockedUp.OrderBy(x => x.Health).ThenByDescending(x => TargetSelector.GetPriority(x)).ThenByDescending(x => x.CountEnemiesInRange(350));
            }

            if (ultmode == UltMode.Priority)
            {
                ordered = KnockedUp.OrderByDescending(x => TargetSelector.GetPriority(x)).ThenBy(x => x.Health).ThenByDescending(x => x.CountEnemiesInRange(350));
            }

            if (ultmode == UltMode.EnemiesHit)
            {
                ordered = KnockedUp.OrderByDescending(x => x.CountEnemiesInRange(350)).ThenByDescending(x => TargetSelector.GetPriority(x)).ThenBy(x => x.Health);
            }

            if (GetBool("Combo.UltOnlyKillable", YasuoMenu.ComboM))
            {
                var killable = ordered.FirstOrDefault(x => !x.isBlackListed() && x.Health <= Yasuo.GetSpellDamage(x, SpellSlot.R) && x.HealthPercent >= GetSliderInt("Combo.MinHealthUlt", YasuoMenu.ComboM) && (GetBool("Combo.UltTower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !x.Position.LSTo2D().PointUnderEnemyTurret()));
                if (killable != null && (!killable.IsInRange(Spells[Q].Range) || !isHealthy))
                {
                    Spells[R].CastOnUnit(killable);
                    return;
                }
                return;
            }

            if ((GetBool("Combo.OnlyifMin", YasuoMenu.ComboM) && ordered.Count() < minhit) || (ordered.Count() == 1 && ordered.FirstOrDefault().HealthPercent < GetSliderInt("Combo.MinHealthUlt", YasuoMenu.ComboM)))
            {
                return;
            }

            if (GetBool("Combo.RPriority", YasuoMenu.ComboM))
            {
                var best = ordered.Find(x => !x.isBlackListed() && TargetSelector.GetPriority(x).Equals(2.5f) && (GetBool("Combo.UltTower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !x.Position.LSTo2D().PointUnderEnemyTurret()));
                if (best != null && Yasuo.HealthPercent / best.HealthPercent <= 1)
                {
                    Spells[R].CastOnUnit(best);
                    return;
                }
            }

            if (ordered.Count() >= minhit)
            {
                var best2 = ordered.FirstOrDefault(x => !x.isBlackListed() && (GetBool("Combo.UltTower", YasuoMenu.ComboM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !x.Position.LSTo2D().PointUnderEnemyTurret()));
                if (best2 != null)
                {
                    Spells[R].CastOnUnit(best2);
                }
                return;
            }
        }

        void Flee()
        {
            Orbwalker.DisableAttacking = true; // BERB
            if (GetBool("Flee.UseQ2", YasuoMenu.MiscM) && !Yasuo.LSIsDashing() && SpellSlot.Q.IsReady() && TornadoReady)
            {
                var qtarg = TargetSelector.GetTarget(Spells[Q2].Range, DamageType.Physical);
                if (qtarg != null)
                {
                    Spells[Q2].Cast(qtarg.ServerPosition);
                }
            }

            if (FleeMode == FleeType.ToCursor)
            {
                Orbwalker.OrbwalkTo(Game.CursorPos);

                var smart = GetBool("Flee.Smart", YasuoMenu.MiscM);

                if (Spells[E].IsReady())
                {
                    if (smart)
                    {
                        Obj_AI_Base dashTarg;

                        if (Yasuo.ServerPosition.PointUnderEnemyTurret())
                        {
                            var closestturret =
                                ObjectManager.Get<Obj_AI_Turret>()
                                    .Where(x => x.IsEnemy)
                                    .MinOrDefault(y => y.LSDistance(Yasuo));

                            var potential =
                                ObjectManager.Get<Obj_AI_Base>()
                                    .Where(x => x.IsDashable())
                                    .MaxOrDefault(x => GetDashPos(x).LSDistance(closestturret));

                            var gdpos = GetDashPos(potential);
                            if (potential != null && gdpos.LSDistance(Game.CursorPos) < Yasuo.LSDistance(Game.CursorPos) && gdpos.LSDistance(closestturret.Position) - closestturret.BoundingRadius > Yasuo.LSDistance(closestturret.Position) - Yasuo.BoundingRadius)
                            {
                                Spells[E].Cast(potential);
                            }
                        }

                        dashTarg = ObjectManager.Get<Obj_AI_Base>()
                           .Where(x => x.IsDashable())
                           .MinOrDefault(x => GetDashPos(x).LSDistance(Game.CursorPos));

                        if (dashTarg != null)
                        {
                            var posafdash = GetDashPos(dashTarg);

                            if (posafdash.LSDistance(Game.CursorPos) < Yasuo.LSDistance(Game.CursorPos) &&
                                !posafdash.PointUnderEnemyTurret())
                            {
                                Spells[E].CastOnUnit(dashTarg);
                            }
                        }
                    }

                    else
                    {
                        var dashtarg =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(x => x.IsDashable())
                                .MinOrDefault(x => GetDashPos(x).LSDistance(Game.CursorPos));

                        if (dashtarg != null)
                        {
                            Spells[E].CastOnUnit(dashtarg);
                        }
                    }
                }

                if (GetBool("Flee.StackQ", YasuoMenu.MiscM) && SpellSlot.Q.IsReady() && !TornadoReady && !Yasuo.LSIsDashing())
                {
                    Obj_AI_Minion qtarg = null;
                    if (!Spells[E].IsReady())
                    {
                        qtarg =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Find(x => x.LSIsValidTarget(Spells[Q].Range) && MinionManager.IsMinion(x));

                    }
                    else
                    {
                        var etargs =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    x => x.LSIsValidTarget(Spells[E].Range) && MinionManager.IsMinion(x) && x.IsDashable());
                        if (!etargs.Any())
                        {
                            qtarg =
                           ObjectManager.Get<Obj_AI_Minion>()
                               .Find(x => x.LSIsValidTarget(Spells[Q].Range) && MinionManager.IsMinion(x));
                        }
                    }

                    if (qtarg != null)
                    {
                        Spells[Q].Cast(qtarg);
                    }
                }
            }

            if (FleeMode == FleeType.ToNexus)
            {
                var nexus = shop;
                if (nexus != null)
                {
                    Orbwalker.OrbwalkTo(nexus.Position);
                    var bestminion = ObjectManager.Get<Obj_AI_Base>().Where(x => x.IsDashable()).MinOrDefault(x => GetDashPos(x).LSDistance(nexus.Position));
                    if (bestminion != null && (!GetBool("Flee.Smart", YasuoMenu.MiscM) || GetDashPos(bestminion).Distance(nexus.Position) < Yasuo.Distance(nexus.Position)))
                    {
                        Spells[E].CastOnUnit(bestminion);
                        if (GetBool("Flee.StackQ", YasuoMenu.MiscM) && SpellSlot.Q.IsReady() && !TornadoReady)
                        {
                            Spells[Q].Cast(bestminion);
                        }
                    }
                }
            }

            if (FleeMode == FleeType.ToAllies)
            {
                Obj_AI_Base bestally = HeroManager.Allies.Where(x => !x.IsMe && x.CountEnemiesInRange(300) == 0).MinOrDefault(x => x.LSDistance(Yasuo));
                if (bestally == null)
                {
                    bestally =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(x => x.IsValidAlly(3000))
                            .MinOrDefault(x => x.LSDistance(Yasuo));
                }

                if (bestally != null)
                {
                    Orbwalker.OrbwalkTo(bestally.ServerPosition);
                    if (Spells[E].IsReady())
                    {
                        var besttarget =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(x => x.IsDashable())
                                .MinOrDefault(x => GetDashPos(x).LSDistance(bestally.ServerPosition));
                        if (besttarget != null)
                        {
                            Spells[E].CastOnUnit(besttarget);
                            if (GetBool("Flee.StackQ", YasuoMenu.MiscM) && SpellSlot.Q.IsReady() && !TornadoReady)
                            {
                                Spells[Q].Cast(besttarget);
                            }
                        }
                    }
                }

                else
                {
                    var nexus = shop;
                    if (nexus != null)
                    {
                        Orbwalker.OrbwalkTo(nexus.Position);
                        var bestminion = ObjectManager.Get<Obj_AI_Base>().Where(x => x.IsDashable()).MinOrDefault(x => GetDashPos(x).LSDistance(nexus.Position));
                        if (bestminion != null && GetDashPos(bestminion).LSDistance(nexus.Position) < Yasuo.LSDistance(nexus.Position))
                        {
                            Spells[E].CastOnUnit(bestminion);
                        }
                    }
                }
            }
        }

        void MoveToMouse()
        {
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (Spells[E].IsReady())
            {
                var bestminion =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(x => x.IsDashable())
                        .MinOrDefault(x => GetDashPos(x).Distance(Game.CursorPos));
                if (bestminion != null)
                {
                    Spells[E].CastOnUnit(bestminion);
                }
            }
        }


        void Waveclear()
        {
            if (SpellSlot.Q.IsReady() && !Yasuo.LSIsDashing())
            {
                if (!TornadoReady && GetBool("Waveclear.UseQ", YasuoMenu.WaveclearM) && Orbwalker.IsAutoAttacking)
                {
                    var minion =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(x => x.IsValidMinion(Spells[Q].Range) && ((x.IsDashable() && (x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= GetProperEDamage(x))) || (x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= 0.15 * x.MaxHealth || x.QCanKill()))).MaxOrDefault(x => x.MaxHealth);
                    if (minion != null)
                    {
                        Spells[Q].Cast(minion);
                        LastTornadoClearTick = Helper.TickCount;
                    }
                }

                else if (TornadoReady && GetBool("Waveclear.UseQ2", YasuoMenu.WaveclearM))
                {
                    var minions = ObjectManager.Get<Obj_AI_Minion>().Where(x => x.Distance(Yasuo) > Yasuo.AttackRange && x.IsValidMinion(Spells[Q2].Range) && ((x.IsDashable() && x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= 0.85 * GetProperEDamage(x)) || (x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= 0.10 * x.MaxHealth) || x.CanKill(SpellSlot.Q)));
                    var pred =
                        MinionManager.GetBestLineFarmLocation(minions.Select(m => m.ServerPosition.LSTo2D()).ToList(),
                            Spells[Q2].Width, Spells[Q2].Range);
                    if (pred.MinionsHit >= GetSliderInt("Waveclear.Qcount", YasuoMenu.WaveclearM))
                    {
                        Spells[Q2].Cast(pred.Position);
                    }
                }
            }

            if (Helper.TickCount - LastTornadoClearTick < 500)
            {
                return;
            }

            if (SpellSlot.E.IsReady() && GetBool("Waveclear.UseE", YasuoMenu.WaveclearM) && (!GetBool("Waveclear.Smart", YasuoMenu.WaveclearM) || isHealthy) && (Helper.TickCount - WCLastE) >= GetSliderInt("Waveclear.Edelay", YasuoMenu.WaveclearM))
            {
                var minions = ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsDashable() && ((GetBool("Waveclear.UseENK", YasuoMenu.WaveclearM) && (!GetBool("Waveclear.Smart", YasuoMenu.WaveclearM) || x.Health - GetProperEDamage(x) > GetProperEDamage(x) * 3)) || x.ECanKill()) && (GetBool("Waveclear.ETower", YasuoMenu.WaveclearM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !GetDashPos(x).PointUnderEnemyTurret()));
                Obj_AI_Minion minion = null;
                minion = minions.MaxOrDefault(x => GetDashPos(x).MinionsInRange(200));
                if (minion != null)
                {
                    Spells[E].Cast(minion);
                    WCLastE = Helper.TickCount;
                }
            }

            if (GetBool("Waveclear.UseItems", YasuoMenu.WaveclearM))
            {
                if (GetBool("Waveclear.UseTIA", YasuoMenu.WaveclearM))
                {
                    Tiamat.minioncount = GetSliderInt("Waveclear.MinCountHDR", YasuoMenu.WaveclearM);
                    Tiamat.Cast(null, true);
                }
                if (GetBool("Waveclear.UseTitanic", YasuoMenu.WaveclearM))
                {
                    Titanic.minioncount = GetSliderInt("Waveclear.MinCountHDR", YasuoMenu.WaveclearM);
                    Titanic.Cast(null, true);
                }
                if (GetBool("Waveclear.UseHDR", YasuoMenu.WaveclearM))
                {
                    Hydra.minioncount = GetSliderInt("Waveclear.MinCountHDR", YasuoMenu.WaveclearM);
                    Hydra.Cast(null, true);
                }
                if (GetBool("Waveclear.UseYMU", YasuoMenu.WaveclearM))
                {
                    Youmu.minioncount = GetSliderInt("Waveclear.MinCountYOU", YasuoMenu.WaveclearM);
                    Youmu.Cast(null, true);
                }
            }
        }


        void Killsteal()
        {
            if (SpellSlot.Q.IsReady() && GetBool("Killsteal.UseQ", YasuoMenu.KillstealM))
            {
                var targ = HeroManager.Enemies.Find(x => x.CanKill(SpellSlot.Q) && x.IsInRange(Qrange));
                if (targ != null)
                {
                    UseQ(targ, GetHitChance("Hitchance.Q"));
                    return;
                }
            }

            if (SpellSlot.E.IsReady() && GetBool("Killsteal.UseE", YasuoMenu.KillstealM))
            {
                var targ = HeroManager.Enemies.Find(x => x.CanKill(SpellSlot.E) && x.IsInRange(Spells[E].Range));
                if (targ != null)
                {
                    Spells[E].Cast(targ);
                    return;
                }
            }

            if (SpellSlot.R.IsReady() && GetBool("Killsteal.UseR", YasuoMenu.KillstealM))
            {
                var targ = KnockedUp.Find(x => x.CanKill(SpellSlot.R) && x.IsValidEnemy(Spells[R].Range) && !x.isBlackListed());
                if (targ != null)
                {
                    Spells[R].Cast(targ);
                    return;
                }
            }

            if (GetBool("Killsteal.UseItems", YasuoMenu.KillstealM))
            {
                if (Tiamat.item.IsReady())
                {
                    var targ =
                        HeroManager.Enemies.Find(
                            x =>
                                x.IsValidEnemy(Tiamat.item.Range) &&
                                x.Health <= Yasuo.GetItemDamage(x, LeagueSharp.Common.Damage.DamageItems.Tiamat));
                    if (targ != null)
                    {
                        Tiamat.Cast(null);
                    }
                }

                if (Titanic.item.IsReady())
                {
                    var targ =
                        HeroManager.Enemies.Find(
                            x =>
                                x.IsValidEnemy(Titanic.item.Range) &&
                                x.Health <= Yasuo.GetItemDamage(x, LeagueSharp.Common.Damage.DamageItems.Tiamat));
                    if (targ != null)
                    {
                        Titanic.Cast(null);
                    }
                }

                if (Hydra.item.IsReady())
                {
                    var targ =
                      HeroManager.Enemies.Find(
                      x =>
                          x.IsValidEnemy(Hydra.item.Range) &&
                          x.Health <= Yasuo.GetItemDamage(x, LeagueSharp.Common.Damage.DamageItems.Tiamat));
                    if (targ != null)
                    {
                        Hydra.Cast(null);
                    }
                }
                if (Blade.item.IsReady())
                {
                    var targ = HeroManager.Enemies.Find(
                     x =>
                         x.IsValidEnemy(Blade.item.Range) &&
                         x.Health <= Yasuo.GetItemDamage(x, LeagueSharp.Common.Damage.DamageItems.Botrk));
                    if (targ != null)
                    {
                        Blade.Cast(targ);
                    }
                }
                if (Bilgewater.item.IsReady())
                {
                    var targ = HeroManager.Enemies.Find(
                                   x =>
                                       x.IsValidEnemy(Bilgewater.item.Range) &&
                                       x.Health <= Yasuo.GetItemDamage(x, LeagueSharp.Common.Damage.DamageItems.Bilgewater));
                    if (targ != null)
                    {
                        Bilgewater.Cast(targ);
                    }
                }
            }
        }

        void Harass()
        {
            //No harass under enemy turret to avoid aggro
            if (Yasuo.ServerPosition.PointUnderEnemyTurret())
            {
                return;
            }

            var target = TargetSelector.GetTarget(Qrange, DamageType.Physical);
            if (SpellSlot.Q.IsReady() && target != null && target.IsInRange(Qrange))
            {
                UseQ(target, GetHitChance("Hitchance.Q"), GetBool("Harass.UseQ", YasuoMenu.HarassM), GetBool("Harass.UseQ2", YasuoMenu.HarassM));
            }

            if (target != null && isHealthy && GetBool("Harass.UseE", YasuoMenu.HarassM) && Spells[E].IsReady() && target.IsInRange(Spells[E].Range * 3) && !target.Position.To2D().PointUnderEnemyTurret())
            {
                if (target.IsInRange(Spells[E].Range))
                {
                    ETarget = target;
                    Spells[E].CastOnUnit(target);
                    return;
                }

                var minion =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(x => x.IsDashable() && !x.ServerPosition.LSTo2D().PointUnderEnemyTurret())
                        .OrderBy(x => GetDashPos(x).LSDistance(target.ServerPosition))
                        .FirstOrDefault();

                if (minion != null && GetBool("Harass.UseEMinion", YasuoMenu.HarassM) && GetDashPos(minion).IsCloser(target))
                {
                    ETarget = minion;
                    Spells[E].Cast(minion);
                }
            }
        }

        void Mixed()
        {
            if (GetBool("Harass.InMixed", YasuoMenu.HarassM))
            {
                Harass();
            }
            LHSkills();
        }


        void LHSkills()
        {
            if (SpellSlot.Q.IsReady() && !Yasuo.LSIsDashing())
            {
                if (!TornadoReady && GetBool("Farm.UseQ", YasuoMenu.FarmingM))
                {
                    var minion =
                         ObjectManager.Get<Obj_AI_Minion>()
                             .FirstOrDefault(x => x.IsValidMinion(Spells[Q].Range) && x.QCanKill());
                    if (minion != null)
                    {
                        Spells[Q].Cast(minion);
                    }
                }

                else if (TornadoReady && GetBool("Farm.UseQ2", YasuoMenu.FarmingM))
                {
                    var minions = ObjectManager.Get<Obj_AI_Minion>().Where(x => x.LSDistance(Yasuo) > Yasuo.AttackRange && x.IsValidMinion(Spells[Q2].Range) && (x.QCanKill()));
                    var pred =
                        MinionManager.GetBestLineFarmLocation(minions.Select(m => m.ServerPosition.LSTo2D()).ToList(),
                            Spells[Q2].Width, Spells[Q2].Range);
                    if (pred.MinionsHit >= GetSliderInt("Farm.Qcount", YasuoMenu.FarmingM))
                    {
                        Spells[Q2].Cast(pred.Position);
                    }
                }
            }

            if (Spells[E].IsReady() && GetBool("Farm.UseE", YasuoMenu.FarmingM))
            {
                var minion = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.IsDashable() && x.ECanKill() && (GetBool("Waveclear.ETower", YasuoMenu.WaveclearM) || GetKeyBind("Misc.TowerDive", YasuoMenu.MiscM) || !GetDashPos(x).PointUnderEnemyTurret()));
                if (minion != null)
                {
                    Spells[E].Cast(minion);
                }
            }
        }



        void OnGapClose(ActiveGapcloser args)
        {
            if (Yasuo.ServerPosition.PointUnderEnemyTurret())
            {
                return;
            }
            if (GetBool("Misc.AG", YasuoMenu.MiscM) && TornadoReady && Yasuo.LSDistance(args.End) <= 500)
            {
                var pred = Spells[Q2].GetPrediction(args.Sender);
                if (pred.Hitchance >= GetHitChance("Hitchance.Q"))
                {
                    Spells[Q2].Cast(pred.CastPosition);
                }
            }
        }

        void OnInterruptable(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Yasuo.ServerPosition.PointUnderEnemyTurret())
            {
                return;
            }
            if (GetBool("Misc.Interrupter", YasuoMenu.MiscM) && TornadoReady && Yasuo.LSDistance(sender.ServerPosition) <= 500)
            {
                if (args.EndTime >= Spells[Q2].Delay)
                {
                    Spells[Q2].Cast(sender.ServerPosition);
                }
            }
        }
    }
}
