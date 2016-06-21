﻿namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.Utils;
    using EloBuddy.SDK.Menu;
    using SharpDX;
    using EloBuddy.SDK.Menu.Values;
    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;

    #endregion
    using EloBuddy;
    using LeagueSharp.SDK.Modes;
    using EloBuddy.SDK;
    using LeagueSharp.Data.Enumerations;
    using EloBuddy.SDK.Enumerations;
    using LeagueSharp.SDK.Enumerations;
    internal class LeeSin : Program
    {
        #region Constants

        private const int RKickRange = 725;

        #endregion

        #region Static Fields

        private static readonly List<string> SpecialPet = new List<string>
                                                              { "jarvanivstandard", "teemomushroom", "illaoiminion" };

        private static int cPassive;

        private static bool isDashing;

        private static int lastW, lastW2, lastE2, lastR;

        private static Obj_AI_Base objQ;

        private static Vector3 posBubbaKush;

        private static EloBuddy.SDK.Menu.Menu config = Program._MainMenu;

        #endregion

        public static Menu miscMenu, drawMenu, ksMenu, lhMenu, lcMenu, comboMenu, kuMenu, insecMenu;

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        public static int getBoxItem(Menu m, string item)
        {
            return m[item].Cast<ComboBox>().CurrentValue;
        }

        #region Constructors and Destructors

        public static LeagueSharp.Common.Spell QS;

        public LeeSin()
        {
            Q = new LeagueSharp.SDK.Spell(SpellSlot.Q, 1095).SetSkillshot(0.25f, 60, 1800, true, SkillshotType.SkillshotLine);
            QS = new LeagueSharp.Common.Spell(SpellSlot.Q, 1100);
            QS.SetSkillshot(0.275f, 60f, 1850f, true, LeagueSharp.Common.SkillshotType.SkillshotLine);
            Q2 = new LeagueSharp.SDK.Spell(Q.Slot, 1300);
            W = new LeagueSharp.SDK.Spell(SpellSlot.W, 700);
            E = new LeagueSharp.SDK.Spell(SpellSlot.E, 425).SetTargetted(0.25f, float.MaxValue);
            E2 = new LeagueSharp.SDK.Spell(E.Slot, 570);
            R = new LeagueSharp.SDK.Spell(SpellSlot.R, 375);
            R2 = new LeagueSharp.SDK.Spell(R.Slot, RKickRange).SetSkillshot(0.325f, 0, 950, false, SkillshotType.SkillshotLine);
            R3 = new LeagueSharp.SDK.Spell(R.Slot, R.Range).SetSkillshot(R2.Delay, 0, R2.Speed, false, R2.Type);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = LeagueSharp.SDK.Enumerations.HitChance.VeryHigh;

            WardManager.Init();
            Insec.Init();

            kuMenu = config.AddSubMenu("KnockUp", "Knock Up");
            kuMenu.AddGroupLabel("Auto Knock Up Settings");
            kuMenu.Add("RAuto", new KeyBind("Use R", false, KeyBind.BindTypes.PressToggle, 'L'));
            kuMenu.Add("RAutoKill", new CheckBox("If Kill Enemy Behind"));
            kuMenu.Add("RAutoCountA", new Slider("Or Hit Enemy Behind >=", 1, 1, 4));

            comboMenu = config.AddSubMenu("Combo", "Combo");
            comboMenu.Add("Ignite", new CheckBox("Use Ignite"));
            comboMenu.Add("Item", new CheckBox("Use Item"));
            comboMenu.AddGroupLabel("Q Settings");
            comboMenu.Add("Q", new CheckBox("Use Q"));
            comboMenu.Add("Q2", new CheckBox("Also Q2"));
            comboMenu.Add("Q2Obj", new CheckBox("Q2 Even Miss", false));
            comboMenu.Add("QCol", new CheckBox("Smite Collision"));
            comboMenu.AddGroupLabel("W Settings");
            comboMenu.Add("W", new CheckBox("Use W", false));
            comboMenu.Add("W2", new CheckBox("Also W2", false));
            comboMenu.AddGroupLabel("E Settings");
            comboMenu.Add("E", new CheckBox("Use E"));
            comboMenu.Add("E2", new CheckBox("Also E2"));
            comboMenu.AddGroupLabel("Star Combo Settings");
            comboMenu.Add("Star", new KeyBind("Star Combo", false, KeyBind.BindTypes.HoldActive, 'X'));
            comboMenu.Add("StarKill", new CheckBox("Auto Star Combo If Killable", false));
            comboMenu.Add("StarKillWJ", new CheckBox("-> Ward Jump In Auto Star Combo", false));

            lcMenu = config.AddSubMenu("LaneClear", "Lane Clear");
            lcMenu.Add("W", new CheckBox("Use W", false));
            lcMenu.Add("E", new CheckBox("Use E"));
            lcMenu.AddGroupLabel("Q Settings");
            lcMenu.Add("Q", new CheckBox("Use Q"));
            lcMenu.Add("QBig", new CheckBox("Only Q Big Mob In Jungle"));

            lhMenu = config.AddSubMenu("LastHit", "Last Hit");
            lhMenu.Add("Q", new CheckBox("Use Q1"));

            ksMenu = config.AddSubMenu("KillSteal", "Kill Steal");
            ksMenu.Add("E", new CheckBox("Use E"));
            ksMenu.Add("R", new CheckBox("Use R"));
            ksMenu.AddGroupLabel("Q Settings");
            ksMenu.Add("Q", new CheckBox("Use Q"));
            ksMenu.Add("Q2", new CheckBox("Also Q2"));
            ksMenu.AddGroupLabel("Extra R Settings");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(o => o.IsEnemy))
            {
                ksMenu.Add("RCast" + enemy.NetworkId, new CheckBox("Cast On " + enemy.ChampionName, false));
            }

            drawMenu = config.AddSubMenu("Draw", "Draw");
            drawMenu.Add("Q", new CheckBox("Q Range", false));
            drawMenu.Add("W", new CheckBox("W Range", false));
            drawMenu.Add("E", new CheckBox("E Range", false));
            drawMenu.Add("R", new CheckBox("R Range", false));
            drawMenu.Add("KnockUp", new CheckBox("Auto Knock Up Status"));

            miscMenu = config.AddSubMenu("Misc", "Misc");
            miscMenu.Add("FleeW", new KeyBind("Use W To Flee", false, KeyBind.BindTypes.HoldActive, 'C'));
            miscMenu.Add("RFlash", new KeyBind("R-Flash To Mouse", false, KeyBind.BindTypes.HoldActive, 'Z'));
            miscMenu.Add("RAdv", new KeyBind("Bubba Kush (R-Flash)", false, KeyBind.BindTypes.HoldActive, 'Y'));

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;

            Obj_AI_Base.OnBuffGain += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        switch (args.Buff.DisplayName)
                        {
                            case "BlindMonkFlurry":
                                cPassive = 2;
                                break;
                            case "BlindMonkQTwoDash":
                                isDashing = true;
                                break;
                        }
                    }
                    else if (sender.IsEnemy)
                    {
                        if (args.Buff.DisplayName == "BlindMonkSonicWave")
                        {
                            objQ = sender;
                        }
                        else if (args.Buff.Name == "blindmonkrroot" && Flash.IsReady())
                        {
                            CastRFlash(sender);
                        }
                    }
                };
            Obj_AI_Base.OnBuffLose += (sender, args) =>
            {
                if (sender.IsMe)
                {
                    switch (args.Buff.DisplayName)
                    {
                        case "BlindMonkFlurry":
                            cPassive = 0;
                            break;
                        case "BlindMonkQTwoDash":
                            isDashing = false;
                            break;
                    }
                }
                else if (sender.IsEnemy && args.Buff.DisplayName == "BlindMonkSonicWave")
                {
                    objQ = null;
                }
            };
            Obj_AI_Base.OnBuffUpdate += (sender, args) =>
            {
                if (!sender.IsMe || args.Buff.DisplayName != "BlindMonkFlurry")
                {
                    return;
                }
                cPassive = args.Buff.Count;
            };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.Slot != SpellSlot.R)
                    {
                        return;
                    }
                    lastR = Variables.TickCount;
                };
        }

        #endregion

        #region Properties

        private static bool IsDashing => (lastW > 0 && Variables.TickCount - lastW <= 100) || Player.IsDashing();

        private static bool IsEOne => E.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsQOne => Q.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsRecentR => Variables.TickCount - lastR < 2500;

        private static bool IsWOne => W.Instance.SData.Name.ToLower().Contains("one");

        #endregion

        #region Methods

        private static void AutoKnockUp()
        {
            if (!R.IsReady() || !getKeyBindItem(kuMenu, "RAuto"))
            {
                return;
            }
            var multiR = GetMultiR();
            if (multiR.Item1 != null && (multiR.Item2 == -1 || multiR.Item2 >= getSliderItem(kuMenu, "RAutoCountA") + 1))
            {
                R.CastOnUnit(multiR.Item1);
            }
        }

        private static bool CanE2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time < 0.25 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanQ2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < 0.25 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanR(AIHeroClient target)
        {
            var buff = target.GetBuff("BlindMonkDragonsRage");
            return buff != null && buff.EndTime - Game.Time <= 0.75 * (buff.EndTime - buff.StartTime);
        }

        private static void CastE(List<Obj_AI_Minion> minions = null)
        {
            if (!E.IsReady() || isDashing || Variables.TickCount - lastW <= 150 || Variables.TickCount - lastW2 <= 100)
            {
                return;
            }
            if (minions == null)
            {
                CastECombo();
            }
            else
            {
                CastELaneClear(minions);
            }
        }

        private static void CastECombo()
        {
            if (IsEOne)
            {
                var target = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && Player.IsInRange(x, E.Range + 20) && x.LSIsValidTarget()).Where(x => E.CanHitCircle(x)).ToList();
                if (target.Count == 0)
                {
                    return;
                }
                if ((cPassive == 0 && Player.Mana >= 70) || target.Count > 2
                    || (Orbwalker.LastTarget == null
                            ? target.Any(i => i.DistanceToPlayer() > Player.GetRealAutoAttackRange() + 100)
                            : cPassive < 2))
                {
                    E.Cast();
                }
            }
            else if (getCheckBoxItem(comboMenu, "E2"))
            {
                var target = GameObjects.EnemyHeroes.Where(i => i.LSIsValidTarget(E2.Range) && HaveE(i)).ToList();
                if (target.Count == 0)
                {
                    return;
                }
                if ((cPassive == 0 || target.Count > 2
                     || target.Any(i => CanE2(i) || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50))
                    && E2.Cast())
                {
                    lastE2 = Variables.TickCount;
                }
            }
        }

        private static void CastELaneClear(List<Obj_AI_Minion> minions)
        {
            if (IsEOne)
            {
                if (cPassive > 0)
                {
                    return;
                }
                var count = minions.Count(i => i.LSIsValidTarget(E.Range));
                if (count > 0 && (Player.Mana >= 70 || count > 2))
                {
                    E.Cast();
                }
            }
            else
            {
                var minion = minions.Where(i => i.LSIsValidTarget(E2.Range) && HaveE(i)).ToList();
                if (minion.Count > 0 && (cPassive == 0 || minion.Any(CanE2)) && E2.Cast())
                {
                    lastE2 = Variables.TickCount;
                }
            }
        }

        private static void CastQSmite(AIHeroClient target)
        {
            var pred = Q.GetPrediction(target, false, -1, CollisionableObjects.YasuoWall);
            var predS = QS.GetPrediction(target);
            if (pred.Hitchance < Q.MinHitChance)
            {
                return;
            }
            var col = Q.GetCollision(target, new List<Vector3> { pred.UnitPosition, target.Position });
            if (col.Count == 0 || (getCheckBoxItem(comboMenu, "QCol") && Common.CastSmiteKillCollision(col)))
            {
                Q.Cast(predS.CastPosition);
            }
        }

        private static void CastRFlash(Obj_AI_Base target)
        {
            var targetSelect = TargetSelector.SelectedTarget;
            if (!targetSelect.LSIsValidTarget() || !targetSelect.Compare(target)
                || target.Health + target.AttackShield <= R.GetDamage(target))
            {
                return;
            }
            var pos = new Vector3();
            if (getKeyBindItem(miscMenu, "RFlash"))
            {
                pos = Game.CursorPos;
            }
            else if (getKeyBindItem(miscMenu, "RAdv"))
            {
                pos = posBubbaKush;
            }
            else if (getKeyBindItem(insecMenu, "Insec")
                     && Variables.TickCount - Insec.LastRFlashTime < 5000)
            {
                pos = Insec.GetPositionKickTo((AIHeroClient)target);
            }
            if (!pos.IsValid())
            {
                return;
            }
            Player.Spellbook.CastSpell(Flash, target.ServerPosition.LSExtend(pos, -(150 + target.BoundingRadius / 2)));
        }

        private static void CastW(List<Obj_AI_Minion> minions = null)
        {
            if (!W.IsReady() || Variables.TickCount - lastW <= 300 || isDashing || Variables.TickCount - lastE2 <= 100)
            {
                return;
            }
            var hero = Orbwalker.LastTarget as AIHeroClient;
            Obj_AI_Minion minion = null;
            if (minions != null && minions.Count > 0)
            {
                minion = minions.FirstOrDefault(i => i.InAutoAttackRange());
            }
            if (hero == null && minion == null)
            {
                return;
            }
            if (hero != null && !IsWOne && !getCheckBoxItem(comboMenu, "W2"))
            {
                return;
            }
            if (hero != null && Player.HealthPercent < hero.HealthPercent && Player.HealthPercent < 30)
            {
                if (IsWOne)
                {
                    if (W.Cast())
                    {
                        lastW = Variables.TickCount;
                        return;
                    }
                }
                else if (W.Cast())
                {
                    lastW2 = Variables.TickCount;
                    return;
                }
            }
            if (Player.HealthPercent < (minions == null ? 8 : 5) || (!IsWOne && Variables.TickCount - lastW > 2600)
                || cPassive == 0
                || (minion != null && minion.Team == GameObjectTeam.Neutral
                    && minion.GetJungleType() != JungleType.Small && Player.HealthPercent < 40 && IsWOne))
            {
                if (IsWOne)
                {
                    if (W.Cast())
                    {
                        lastW = Variables.TickCount;
                    }
                }
                else if (W.Cast())
                {
                    lastW2 = Variables.TickCount;
                }
            }
        }

        private static void Combo()
        {
            if (getCheckBoxItem(comboMenu, "StarKill"))
            {
                if (R.IsReady() && Q.IsReady() && !IsQOne && getCheckBoxItem(comboMenu, "Q") && getCheckBoxItem(comboMenu, "Q2"))
                {
                    var target = EntityManager.Heroes.Enemies.Where(x => Q2.IsInRange(x) && HaveQ(x)).FirstOrDefault();
                    if (target != null && target.Health + target.AttackShield > Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target) && target.Health + target.AttackShield <= GetQ2Dmg(target, R.GetDamage(target)) + Player.GetAutoAttackDamage(target))
                    {
                        if (R.CastOnUnit(target))
                        {
                            return;
                        }
                        if (getCheckBoxItem(comboMenu, "StarKillWJ") && !R.IsInRange(target) && target.DistanceToPlayer() < WardManager.WardRange + R.Range - 50 && Player.Mana >= 80 && !isDashing)
                        {
                            Flee(target.ServerPosition, true);
                        }
                    }
                }
            }

            if (getCheckBoxItem(comboMenu, "Q") && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget(Q.Width / 2);
                    if (target != null)
                    {
                        CastQSmite(target);
                    }
                }
                else if (getCheckBoxItem(comboMenu, "Q2") && !IsDashing && objQ.LSIsValidTarget(Q2.Range))
                {
                    var target = objQ as AIHeroClient;
                    if (target != null)
                    {
                        if (CanQ2(target) || (!R.IsReady() && IsRecentR && CanR(target)) || (target.Health + target.AttackShield <= Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)) || ((R.IsReady() || (!target.HasBuff("BlindMonkDragonsRage") && Variables.TickCount - lastR > 1000)) && target.DistanceToPlayer() > target.GetRealAutoAttackRange() + 100) || cPassive == 0)
                        {
                            Q2.Cast();
                            isDashing = true;
                            return;
                        }
                    }
                    else if (getCheckBoxItem(comboMenu, "Q2Obj"))
                    {
                        var targetQ2 = Q2.GetTarget(200);
                        if (targetQ2 != null && objQ.Distance(targetQ2) < targetQ2.DistanceToPlayer() && !targetQ2.InAutoAttackRange())
                        {
                            Q2.Cast();
                            isDashing = true;
                            return;
                        }
                    }
                }
            }
            if (getCheckBoxItem(comboMenu, "E"))
            {
                CastE();
            }
            if (getCheckBoxItem(comboMenu, "W"))
            {
                CastW();
            }
            var subTarget = W.GetTarget();
            if (getCheckBoxItem(comboMenu, "Item"))
            {
                UseItem(subTarget);
            }
            if (subTarget != null && getCheckBoxItem(comboMenu, "Ignite") && Ignite.IsReady() && subTarget.HealthPercent < 30 && subTarget.DistanceToPlayer() <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Flee(Vector3 pos, bool isStar = false)
        {
            if (!W.IsReady() || !IsWOne || Variables.TickCount - lastW <= 500)
            {
                return;
            }
            var posPlayer = Player.ServerPosition;
            var posJump = pos.Distance(posPlayer) < W.Range ? pos : posPlayer.LSExtend(pos, W.Range);
            var objJumps = new List<Obj_AI_Base>();
            objJumps.AddRange(GameObjects.AllyHeroes.Where(i => !i.IsMe));
            objJumps.AddRange(GameObjects.AllyWards.Where(i => i.IsWard()));
            objJumps.AddRange(
                GameObjects.AllyMinions.Where(
                    i => i.IsMinion() || i.IsPet() || SpecialPet.Contains(i.CharData.BaseSkinName.ToLower())));
            var objJump =
                objJumps.Where(
                    i => i.LSIsValidTarget(W.Range, false) && i.Distance(posJump) < (isStar ? R.Range - 50 : 200))
                    .MinOrDefault(i => i.Distance(posJump));
            if (objJump != null)
            {
                if (W.CastOnUnit(objJump))
                {
                    lastW = Variables.TickCount;
                }
            }
            else
            {
                WardManager.Place(posJump);
            }
        }

        private static Tuple<AIHeroClient, Vector3, Vector3> GetBubbaKush()
        {
            var bestHit = 0;
            AIHeroClient bestTarget = null;
            Vector3 bestPos = new Vector3(), startPos = new Vector3();
            var targetKicks =
                GameObjects.EnemyHeroes.Where(
                    i =>
                    i.LSIsValidTarget(R.Range) && i.Health + i.AttackShield > R.GetDamage(i)
                    && !i.HasBuffOfType(BuffType.SpellShield) && !i.HasBuffOfType(BuffType.SpellImmunity))
                    .OrderByDescending(i => i.AllShield)
                    .ToList();
            foreach (var targetKick in targetKicks)
            {
                var posTarget = targetKick.ServerPosition;
                R3.Width = targetKick.BoundingRadius;
                R3.Range = RKickRange + R3.Width / 2;
                R3.UpdateSourcePosition(posTarget, posTarget);
                var targetHits =
                    GameObjects.EnemyHeroes.Where(
                        i => i.LSIsValidTarget(R3.Range + R3.Width / 2, true, R3.From) && !i.Compare(targetKick)).ToList();
                if (targetHits.Count == 0)
                {
                    continue;
                }
                var cHit = 1;
                var pos = new Vector3();
                foreach (var targetHit in targetHits)
                {
                    var pred = R3.GetPrediction(targetHit);
                    if (pred.Hitchance < LeagueSharp.SDK.Enumerations.HitChance.High)
                    {
                        continue;
                    }
                    cHit++;
                    pos = pred.CastPosition;
                    var dmgR = GetRColDmg(targetKick, targetHit);
                    if (targetHit.Health + targetHit.AttackShield <= dmgR
                        && !Invulnerable.Check(targetHit, R.DamageType, true, dmgR))
                    {
                        return new Tuple<AIHeroClient, Vector3, Vector3>(targetKick, pos, posTarget);
                    }
                }
                if (bestHit == 0 || bestHit < cHit)
                {
                    bestHit = cHit;
                    bestTarget = targetKick;
                    bestPos = pos;
                    startPos = posTarget;
                }
            }
            return new Tuple<AIHeroClient, Vector3, Vector3>(bestTarget, bestPos, startPos);
        }

        private static Tuple<AIHeroClient, int> GetMultiR()
        {
            var bestHit = 0;
            AIHeroClient bestTarget = null;
            var targetKicks =
                GameObjects.EnemyHeroes.Where(
                    i =>
                    i.LSIsValidTarget(R.Range) && i.Health + i.AttackShield > R.GetDamage(i)
                    && !i.HasBuffOfType(BuffType.SpellShield) && !i.HasBuffOfType(BuffType.SpellImmunity))
                    .OrderByDescending(i => i.AllShield)
                    .ToList();
            foreach (var targetKick in targetKicks)
            {
                var posTarget = targetKick.ServerPosition;
                R2.Width = targetKick.BoundingRadius;
                R2.Range = RKickRange + R2.Width / 2;
                R2.UpdateSourcePosition(posTarget, posTarget);
                var targetHits =
                    GameObjects.EnemyHeroes.Where(
                        i => i.LSIsValidTarget(R2.Range + R2.Width / 2, true, R2.From) && !i.Compare(targetKick)).ToList();
                if (targetHits.Count == 0)
                {
                    continue;
                }
                var cHit = 1;
                foreach (var targetHit in targetHits)
                {
                    var pred = R2.GetPrediction(targetHit);
                    if (pred.Hitchance < LeagueSharp.SDK.Enumerations.HitChance.High)
                    {
                        continue;
                    }
                    cHit++;
                    if (getCheckBoxItem(kuMenu, "RAutoKill"))
                    {
                        var dmgR = GetRColDmg(targetKick, targetHit);
                        if (targetHit.Health + targetHit.AttackShield <= dmgR
                            && !Invulnerable.Check(targetHit, R.DamageType, true, dmgR))
                        {
                            return new Tuple<AIHeroClient, int>(targetKick, -1);
                        }
                    }
                }
                if (bestHit == 0 || bestHit < cHit)
                {
                    bestHit = cHit;
                    bestTarget = targetKick;
                }
            }
            return new Tuple<AIHeroClient, int>(bestTarget, bestHit);
        }

        private static double GetQ2Dmg(Obj_AI_Base target, double subHp)
        {
            var dmg = new[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod
                      + 0.08 * (target.MaxHealth - (target.Health - subHp));
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                target is Obj_AI_Minion ? Math.Min(dmg, 400) : dmg) + subHp;
        }

        private static float GetRColDmg(AIHeroClient kickTarget, AIHeroClient hitTarget)
        {
            return R.GetDamage(hitTarget)
                   + (float)
                     Player.CalculateDamage(
                         hitTarget,
                         DamageType.Physical,
                         new[] { 0.12, 0.15, 0.18 }[R.Level - 1] * kickTarget.AllShield);
        }

        private static bool HaveE(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkTempest");
        }

        private static bool HaveQ(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkSonicWave");
        }

        private static void KillSteal()
        {
            var ksQ1 = getCheckBoxItem(ksMenu, "Q");
            var ksQ2 = getCheckBoxItem(ksMenu, "Q2");
            if (ksQ1 && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget(Q.Width / 2);
                    var predS = QS.GetPrediction(target);
                    if (target != null
                        && (target.Health + target.AttackShield <= Q.GetDamage(target)
                            || (ksQ2
                            && target.Health + target.AttackShield
                                <= GetQ2Dmg(target, Q.GetDamage(target)) + Player.GetAutoAttackDamage(target)
                                && Player.Mana - Q.Instance.SData.Mana >= 30))
                        && Q.Cast(predS.CastPosition))
                    {
                        return;
                    }
                }
                else if (ksQ2 && !IsDashing)
                {
                    var target = objQ as AIHeroClient;
                    if (target != null
                        && target.Health + target.AttackShield
                        <= Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target) && Q2.Cast())
                    {
                        isDashing = true;
                        return;
                    }
                }
            }
            if (getCheckBoxItem(ksMenu, "E") && E.IsReady() && IsEOne && EntityManager.Heroes.Enemies.Where(x => !x.IsDead && E.IsInRange(x) && E.CanHitCircle(x) && x.Health + x.MagicShield <= E.GetDamage(x) && x.LSIsValidTarget()).Any() && E.Cast())
            {
                return;
            }
            if (getCheckBoxItem(ksMenu, "R") && R.IsReady())
            {
                var targetList = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && R.IsInRange(x) && getCheckBoxItem(ksMenu, "RCast" + x.NetworkId) && x.LSIsValidTarget()).ToList();
                if (targetList.Count > 0)
                {
                    var targetR = targetList.FirstOrDefault(i => i.Health + i.AttackShield <= R.GetDamage(i));
                    if (targetR != null)
                    {
                        R.CastOnUnit(targetR);
                    }
                    else if (ksQ1 && ksQ2 && Q.IsReady() && !IsQOne)
                    {
                        var targetQ2R =
                            targetList.FirstOrDefault(
                                i =>
                                HaveQ(i)
                                && i.Health + i.AttackShield
                                <= GetQ2Dmg(i, R.GetDamage(i)) + Player.GetAutoAttackDamage(i));
                        if (targetQ2R != null)
                        {
                            R.CastOnUnit(targetQ2R);
                        }
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var minions =
                Common.ListMinions().Where(i => i.LSIsValidTarget(Q2.Range)).OrderByDescending(i => i.MaxHealth).ToList();
            if (minions.Count == 0)
            {
                return;
            }
            if (getCheckBoxItem(lcMenu, "E"))
            {
                CastE(minions);
            }
            if (getCheckBoxItem(lcMenu, "W"))
            {
                CastW(minions);
            }
            if (getCheckBoxItem(lcMenu, "Q") && Q.IsReady())
            {
                if (IsQOne)
                {
                    if (cPassive < 2)
                    {
                        var minionQ = minions.Where(i => i.DistanceToPlayer() < Q.Range - 10).ToList();
                        if (minionQ.Count > 0)
                        {
                            var minionJungle =
                                minionQ.Where(i => i.Team == GameObjectTeam.Neutral)
                                    .OrderByDescending(i => i.MaxHealth)
                                    .ThenBy(i => i.DistanceToPlayer())
                                    .ToList();
                            if (getCheckBoxItem(lcMenu, "QBig") && minionJungle.Count > 0 && Player.Health > 100)
                            {
                                minionJungle =
                                    minionJungle.Where(
                                        i =>
                                        i.GetJungleType() == JungleType.Legendary
                                        || i.GetJungleType() == JungleType.Large || i.Name.Contains("Crab")).ToList();
                            }
                            if (minionJungle.Count > 0)
                            {
                                minionJungle.ForEach(i => Q.Casting(i));
                            }
                            else
                            {
                                var minionLane =
                                    minionQ.Where(i => i.Team != GameObjectTeam.Neutral)
                                        .OrderByDescending(i => i.GetMinionType().HasFlag(MinionTypes.Siege))
                                        .ThenBy(i => i.GetMinionType().HasFlag(MinionTypes.Super))
                                        .ThenBy(i => i.Health)
                                        .ThenByDescending(i => i.MaxHealth)
                                        .ToList();
                                if (minionLane.Count == 0)
                                {
                                    return;
                                }
                                foreach (var minion in minionLane)
                                {
                                    if (minion.InAutoAttackRange())
                                    {
                                        if (Q.GetHealthPrediction(minion) > Q.GetDamage(minion)
                                            && Q.Casting(minion).IsCasted())
                                        {
                                            return;
                                        }
                                    }
                                    else if ((Orbwalker.LastTarget != null
                                                  ? Q.CanLastHit(minion, Q.GetDamage(minion))
                                                  : Q.GetHealthPrediction(minion) > Q.GetDamage(minion))
                                             && Q.Casting(minion).IsCasted())
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (!IsDashing)
                {
                    var q2Minion = objQ;
                    if (q2Minion.LSIsValidTarget(Q2.Range)
                        && (CanQ2(q2Minion) || q2Minion.Health <= Q.GetDamage(q2Minion, DamageStage.SecondCast)
                            || q2Minion.DistanceToPlayer() > q2Minion.GetRealAutoAttackRange() + 100 || cPassive == 0)
                        && Q2.Cast())
                    {
                        isDashing = true;
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (!getCheckBoxItem(lhMenu, "Q") || !Q.IsReady() || !IsQOne || Player.Spellbook.IsAutoAttacking)
            {
                return;
            }
            var minions =
                GameObjects.EnemyMinions.Where(
                    i => (i.IsMinion() || i.IsPet(false)) && i.LSIsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i)))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0)
            {
                return;
            }
            minions.ForEach(
                i =>
                Q.Casting(
                    i,
                    false,
                    CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall));
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (Q == null || W == null || E == null || R == null)
            {
                return;
            }
            if (getCheckBoxItem(drawMenu, "Q") && Q != null)
            {
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, (IsQOne ? Q : Q2).Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (getCheckBoxItem(drawMenu, "W") && W.Level > 0 && IsWOne)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (getCheckBoxItem(drawMenu, "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position,
                    (IsEOne ? E : E2).Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (R.Level > 0)
            {
                if (getCheckBoxItem(drawMenu, "R"))
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
                }
                if (getCheckBoxItem(drawMenu, "KnockUp"))
                {
                    var menu = getKeyBindItem(kuMenu, "RAuto");
                    var text = $"Auto KnockUp: {(menu ? "On" : "Off")} [{menu}]";
                    var pos = Drawing.WorldToScreen(Player.Position);
                    Drawing.DrawText(pos.X - (float)90 / 2, pos.Y + 20, menu ? Color.White : Color.Gray, text);
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.LSIsRecalling() || Shop.IsOpen)
            {
                return;
            }
            KillSteal();

            Orbwalker.DisableAttacking = getKeyBindItem(insecMenu, "Insec");

            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
            {
                Orbwalker.ForcedTarget = null;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
            {
                if (getKeyBindItem(miscMenu, "FleeW"))
                {
                    Orbwalker.OrbwalkTo(Game.CursorPos);
                    Flee(Game.CursorPos);
                }

                else if (getKeyBindItem(miscMenu, "RFlash"))
                {
                    Orbwalker.OrbwalkTo(Game.CursorPos);
                    if (R.IsReady() && Flash.IsReady())
                    {
                        var target = EntityManager.Heroes.Enemies.Where(i => i.Health + i.AttackShield > R.GetDamage(i) && R.IsInRange(i)).FirstOrDefault();
                        if (target != null && R.CastOnUnit(target))
                        {
                            Orbwalker.ForcedTarget = target;
                        }
                    }
                }
                else if (getKeyBindItem(miscMenu, "RAdv"))
                {
                    Orbwalker.MoveTo(Game.CursorPos);
                    if (R.IsReady() && Flash.IsReady())
                    {
                        var bubbaKush = GetBubbaKush();
                        if (bubbaKush.Item1 != null && bubbaKush.Item2.IsValid() && R.CastOnUnit(bubbaKush.Item1))
                        {
                            posBubbaKush = bubbaKush.Item2;
                            Orbwalker.ForcedTarget = bubbaKush.Item1;
                        }
                    }
                }
                else if (getKeyBindItem(comboMenu, "Star"))
                {
                    StarCombo();
                }
                else if (getKeyBindItem(insecMenu, "Insec"))
                {
                    Insec.Start(Insec.GetTarget);
                }
            }

            if (!getKeyBindItem(insecMenu, "Insec"))
            {
                AutoKnockUp();
            }
        }

        private static void StarCombo()
        {
            var target = Q.GetTarget(Q.Width / 2);
            if (!IsQOne)
            {
                target = objQ as AIHeroClient;
            }
            if (!Q.IsReady())
            {
                target = W.GetTarget();
            }
            Orbwalker.OrbwalkTo(Game.CursorPos);
            if (target == null)
            {
                return;
            }
            if (Q.IsReady())
            {
                if (IsQOne)
                {
                    CastQSmite(target);
                }
                else if (!IsDashing && HaveQ(target)
                         && (target.Health + target.AttackShield
                             <= Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                             || (!R.IsReady() && IsRecentR && CanR(target))) && Q2.Cast())
                {
                    isDashing = true;
                    return;
                }
            }
            if (E.IsReady() && IsEOne && E.CanHitCircle(target) && (!HaveQ(target) || Player.Mana >= 70) && E.Cast())
            {
                return;
            }
            if (!R.IsReady() || !Q.IsReady() || IsQOne || !HaveQ(target))
            {
                return;
            }
            if (R.IsInRange(target))
            {
                R.CastOnUnit(target);
            }
            else if (target.DistanceToPlayer() < WardManager.WardRange + R.Range - 50 && Player.Mana >= 70 && !isDashing)
            {
                Flee(target.ServerPosition, true);
            }
        }

        private static void UseItem(AIHeroClient target)
        {
            if (target != null && (target.HealthPercent < 40 || Player.HealthPercent < 50))
            {
                if (Bilgewater.IsReady())
                {
                    Bilgewater.Cast(target);
                }
                if (BotRuinedKing.IsReady())
                {
                    BotRuinedKing.Cast(target);
                }
            }
            if (Youmuu.IsReady() && Player.CountEnemyHeroesInRange(W.Range + E.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady() && Player.CountEnemyHeroesInRange(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady() && Player.CountEnemyHeroesInRange(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (Titanic.IsReady() && !Player.Spellbook.IsAutoAttacking && Orbwalker.LastTarget != null)
            {
                Titanic.Cast();
            }
        }

        #endregion

        private static class Insec
        {
            #region Constants

            private const int DistWard = 230, DistFlash = 130;

            #endregion

            #region Static Fields

            internal static bool IsWardFlash;

            internal static int LastRFlashTime;

            private static Vector3 lastEndPos, lastFlashPos;

            private static int lastInsecTime, lastMoveTime, lastFlashRTime;

            private static Obj_AI_Base lastObjQ;

            #endregion

            #region Properties

            internal static AIHeroClient GetTarget
            {
                get
                {
                    AIHeroClient target = null;
                    if (getCheckBoxItem(insecMenu, "TargetSelect"))
                    {
                        var sub = TargetSelector.SelectedTarget;
                        if (sub.LSIsValidTarget())
                        {
                            target = sub;
                        }
                    }
                    else
                    {
                        target = Q.GetTarget(-100);
                        if ((getCheckBoxItem(insecMenu, "Q") && Q.IsReady()) || objQ.LSIsValidTarget(Q2.Range))
                        {
                            target = Q2.GetTarget(FlashRange);
                        }
                    }
                    return target;
                }
            }

            private static bool CanInsec
                =>
                    (WardManager.CanWardJump || (getCheckBoxItem(insecMenu, "Flash") && Flash.IsReady()) || IsRecent)
                    && R.IsReady();

            private static bool CanWardFlash
                =>
                    getCheckBoxItem(insecMenu, "Flash") && getCheckBoxItem(insecMenu, "FlashJump") && WardManager.CanWardJump
                    && Flash.IsReady();

            private static bool IsRecent
                => IsRecentWardJump || (getCheckBoxItem(insecMenu, "Flash") && Variables.TickCount - lastFlashRTime < 5000);

            private static bool IsRecentWardJump
                =>
                    Variables.TickCount - WardManager.LastInsecWardTime < 5000
                    || Variables.TickCount - WardManager.LastInsecJumpTme < 5000;

            private static float RangeNormal
                =>
                    WardManager.CanWardJump || IsRecentWardJump
                        ? WardManager.WardRange - DistWard
                        : FlashRange - DistFlash;

            private static float RangeWardFlash => WardManager.WardRange + R.Range - 100;

            #endregion

            #region Methods

            internal static Vector3 GetPositionKickTo(AIHeroClient target)
            {
                if (lastEndPos.IsValid() && target.Distance(lastEndPos) <= RKickRange + 700)
                {
                    return lastEndPos;
                }
                var pos = Player.ServerPosition;
                switch (getBoxItem(insecMenu, "Mode"))
                {
                    case 0:
                        var turret =
                            GameObjects.AllyTurrets.Where(
                                i =>
                                !i.IsDead && target.Distance(i) <= RKickRange + 500
                                && i.Distance(target) - RKickRange <= 950 && i.Distance(target) > 400)
                                .MinOrDefault(i => i.DistanceToPlayer());
                        if (turret != null)
                        {
                            pos = turret.ServerPosition;
                        }
                        else
                        {
                            var hero =
                                GameObjects.AllyHeroes.Where(
                                    i =>
                                    i.LSIsValidTarget(RKickRange + 700, false, target.ServerPosition) && !i.IsMe
                                    && i.HealthPercent > 10 && i.Distance(target) > 350)
                                    .MaxOrDefault(i => new Priority().GetDefaultPriority(i));
                            if (hero != null)
                            {
                                pos = hero.ServerPosition;
                            }
                        }
                        break;
                    case 1:
                        pos = Game.CursorPos;
                        break;
                }
                return pos;
            }

            internal static void Init()
            {
                insecMenu = config.AddSubMenu("Insec", "Insec");
                insecMenu.Add("TargetSelect", new CheckBox("Only Insec Target Selected", false));
                insecMenu.Add("Mode", new ComboBox("Mode", 0, "Tower/Hero/Current", "Mouse Position", "Current Position"));

                insecMenu.AddGroupLabel("Draw Settings");
                insecMenu.Add("DLine", new CheckBox("Line"));
                insecMenu.Add("DWardFlash", new CheckBox("WardJump Flash Range"));

                insecMenu.AddGroupLabel("Flash Settings");
                insecMenu.Add("Flash", new CheckBox("Use Flash"));
                insecMenu.Add("FlashMode", new ComboBox("Flash Mode", 0, "R-Flash", "Flash-R", "Both"));
                insecMenu.Add("FlashJump", new CheckBox("Use WardJump To Gap For Flash"));

                insecMenu.AddGroupLabel("Q Settings");
                insecMenu.Add("Q", new CheckBox("Use Q"));
                insecMenu.Add("QCol", new CheckBox("Smite Collision"));
                insecMenu.Add("QObj", new CheckBox("Use Q On Near Object"));

                insecMenu.AddGroupLabel("Keybinds");
                insecMenu.Add("Insec", new KeyBind("Insec", false, KeyBind.BindTypes.HoldActive, 'T'));

                Game.OnUpdate += args =>
                    {
                        if (lastInsecTime > 0 && Variables.TickCount - lastInsecTime > 5000)
                        {
                            CleanData();
                        }
                        if (lastMoveTime > 0 && Variables.TickCount - lastMoveTime > 1000 && !R.IsReady())
                        {
                            lastMoveTime = 0;
                        }
                    };
                Drawing.OnDraw += args =>
                    {
                        if (Player.IsDead || R.Level == 0 || !CanInsec)
                        {
                            return;
                        }
                        if (getCheckBoxItem(insecMenu, "DLine"))
                        {
                            var target = GetTarget;
                            if (target != null)
                            {
                                Render.Circle.DrawCircle(
                                    target.Position,
                                    target.BoundingRadius * 1.35f,
                                    Color.BlueViolet);
                                Render.Circle.DrawCircle(
                                    GetPositionBehind(target),
                                    target.BoundingRadius * 1.35f,
                                    Color.BlueViolet);
                                Drawing.DrawLine(
                                    Drawing.WorldToScreen(target.Position),
                                    Drawing.WorldToScreen(GetPositionKickTo(target)),
                                    1,
                                    Color.BlueViolet);
                            }
                        }
                        if (getCheckBoxItem(insecMenu, "DWardFlash") && CanWardFlash)
                        {
                            Render.Circle.DrawCircle(Player.Position, RangeWardFlash, Color.Orange);
                        }
                    };
                Obj_AI_Base.OnBuffGain += (sender, args) =>
                    {
                        if (!sender.IsEnemy || args.Buff.DisplayName != "BlindMonkSonicWave")
                        {
                            return;
                        }
                        lastObjQ = sender;
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe || !getKeyBindItem(insecMenu, "Insec")
                            || !lastFlashPos.IsValid() || args.SData.Name != "SummonerFlash"
                            || !getCheckBoxItem(insecMenu, "Flash") || Variables.TickCount - lastFlashRTime > 1250
                            || args.End.Distance(lastFlashPos) > 150)
                        {
                            return;
                        }
                        lastFlashRTime = Variables.TickCount;
                        var target = TargetSelector.SelectedTarget;
                        if (target.LSIsValidTarget())
                        {
                            DelayAction.Add(5, () => R.CastOnUnit(target));
                        }
                    };
                Obj_AI_Base.OnSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe || args.Slot != SpellSlot.R)
                        {
                            return;
                        }
                        CleanData();
                    };
            }

            internal static void Start(AIHeroClient target)
            {
                if (Orbwalker.CanMove && Variables.TickCount - lastMoveTime > 250)
                {
                    if (target != null && lastMoveTime > 0 && CanInsec)
                    {
                        var posEnd = GetPositionKickTo(target);
                        Orbwalker.MoveTo(posEnd.DistanceToPlayer() > target.Distance(posEnd) ? GetPositionBehind(target) : Game.CursorPos);
                    }
                    else
                    {
                        Orbwalker.MoveTo(Game.CursorPos);
                    }
                }
                if (target == null || !CanInsec)
                {
                    return;
                }
                if (R.IsInRange(target))
                {
                    var posEnd = GetPositionKickTo(target);
                    var posTarget = target.Position;
                    var posPlayer = Player.Position;
                    if (posPlayer.Distance(posEnd) > posTarget.Distance(posEnd))
                    {
                        var project = posTarget.LSExtend(posPlayer, -RKickRange)
                            .ProjectOn(posTarget, posEnd.LSExtend(posTarget, -(RKickRange * 0.5f)));
                        if (project.IsOnSegment && project.SegmentPoint.Distance(posEnd) <= RKickRange * 0.5f
                            && R.CastOnUnit(target))
                        {
                            return;
                        }
                    }
                }
                if (!IsRecent)
                {
                    if (!IsWardFlash)
                    {
                        var checkJump = GapCheck(target);
                        if (checkJump.Item2)
                        {
                            GapByWardJump(target, checkJump.Item1);
                        }
                        else
                        {
                            var checkFlash = GapCheck(target, true);
                            if (checkFlash.Item2)
                            {
                                GapByFlash(target, checkFlash.Item1);
                            }
                            else if (CanWardFlash)
                            {
                                var posTarget = target.ServerPosition;
                                if (posTarget.DistanceToPlayer() < RangeWardFlash
                                    && (!isDashing
                                        || (!lastObjQ.Compare(target) && lastObjQ.Distance(posTarget) > RangeNormal)))
                                {
                                    IsWardFlash = true;
                                    return;
                                }
                            }
                        }
                    }
                    else if (WardManager.Place(target.ServerPosition))
                    {
                        Orbwalker.ForcedTarget = target;
                        return;
                    }
                }
                if (!IsDashing && (!CanWardFlash || !IsWardFlash))
                {
                    GapByQ(target);
                }
            }

            private static void CleanData()
            {
                lastEndPos = lastFlashPos = new Vector3();
                lastInsecTime = 0;
                IsWardFlash = false;
                Orbwalker.ForcedTarget = null;
            }

            private static void GapByFlash(AIHeroClient target, Vector3 posBehind)
            {
                switch (getBoxItem(insecMenu, "FlashMode"))
                {
                    case 0:
                        GapByRFlash(target);
                        break;
                    case 1:
                        GapByFlashR(target, posBehind);
                        break;
                    case 2:
                        if (!posBehind.IsValid())
                        {
                            GapByRFlash(target);
                        }
                        else
                        {
                            GapByFlashR(target, posBehind);
                        }
                        break;
                }
            }

            private static void GapByFlashR(AIHeroClient target, Vector3 posBehind)
            {
                if (!Player.Spellbook.CastSpell(Flash, posBehind))
                {
                    return;
                }
                if (Player.CanMove)
                {
                    lastMoveTime = Variables.TickCount;
                    Orbwalker.MoveTo(posBehind.LSExtend(GetPositionKickTo(target), -(DistFlash + Player.BoundingRadius / 2))); // - might bug
                }
                lastFlashPos = posBehind;
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = lastFlashRTime = Variables.TickCount;
                Orbwalker.ForcedTarget = target;
            }

            private static void GapByQ(AIHeroClient target)
            {
                if (!getCheckBoxItem(insecMenu, "Q") || !Q.IsReady())
                {
                    return;
                }
                if (CanWardFlash && IsQOne && Player.Mana < 50 + 80)
                {
                    return;
                }
                var minDist = CanWardFlash ? RangeWardFlash : RangeNormal;
                if (IsQOne)
                {
                    var pred = Q.GetPrediction(target, false, -1, CollisionableObjects.YasuoWall);
                    var predS = QS.GetPrediction(target);
                    if (pred.Hitchance >= Q.MinHitChance)
                    {
                        var col = Q.GetCollision(target, new List<Vector3> { pred.UnitPosition, target.Position });
                        if ((col.Count == 0 || (getCheckBoxItem(insecMenu, "QCol") && Common.CastSmiteKillCollision(col)))
                            && Q.Cast(predS.CastPosition))
                        {
                            return;
                        }
                    }
                    if (!getCheckBoxItem(insecMenu, "QObj"))
                    {
                        return;
                    }
                    var nearObj =
                        Common.ListEnemies(true)
                            .Where(
                                i =>
                                !i.Compare(target) && i.LSIsValidTarget(Q.Range)
                                && Q.GetHealthPrediction(i) > Q.GetDamage(i) && i.Distance(target) < minDist - 100)
                            .OrderBy(i => i.Distance(target))
                            .ThenByDescending(i => i.Health)
                            .ToList();
                    if (nearObj.Count == 0)
                    {
                        return;
                    }
                    nearObj.ForEach(i => Q.Casting(i));
                }
                else if (target.DistanceToPlayer() > minDist
                         && (HaveQ(target) || (objQ.LSIsValidTarget(Q2.Range) && target.Distance(objQ) < minDist - 100))
                         && ((WardManager.CanWardJump && Player.Mana >= 80)
                             || (getCheckBoxItem(insecMenu, "Flash") && Flash.IsReady())) && Q2.Cast())
                {
                    isDashing = true;
                    Orbwalker.ForcedTarget = target;
                }
            }

            private static void GapByRFlash(AIHeroClient target)
            {
                if (!R.CastOnUnit(target))
                {
                    return;
                }
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = LastRFlashTime = Variables.TickCount;
                Orbwalker.ForcedTarget = target;
            }

            private static void GapByWardJump(AIHeroClient target, Vector3 posBehind)
            {
                if (!WardManager.Place(posBehind, 1))
                {
                    return;
                }
                if (Orbwalker.CanMove)
                {
                    lastMoveTime = Variables.TickCount;
                    Orbwalker.MoveTo(posBehind.LSExtend(GetPositionKickTo(target), -(DistWard + Player.BoundingRadius / 2)));
                }
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = WardManager.LastInsecWardTime = WardManager.LastInsecJumpTme = Variables.TickCount;
                Orbwalker.ForcedTarget = target;
            }

            private static Tuple<Vector3, bool> GapCheck(AIHeroClient target, bool useFlash = false)
            {
                if (!useFlash ? !WardManager.CanWardJump : !getCheckBoxItem(insecMenu, "Flash") || !Flash.IsReady())
                {
                    return new Tuple<Vector3, bool>(new Vector3(), false);
                }
                var posEnd = GetPositionKickTo(target);
                var posTarget = target.ServerPosition;
                var posPlayer = Player.ServerPosition;
                if (!useFlash)
                {
                    var posBehind = posTarget.LSExtend(posEnd, -DistWard);
                    if (posPlayer.Distance(posBehind) < WardManager.WardRange
                        && posTarget.Distance(posBehind) < posEnd.Distance(posBehind))
                    {
                        return new Tuple<Vector3, bool>(posBehind, true);
                    }
                }
                else
                {
                    var flashMode = getBoxItem(insecMenu, "FlashMode");
                    if (flashMode != 1 && posPlayer.Distance(posTarget) < R.Range)
                    {
                        return new Tuple<Vector3, bool>(new Vector3(), true);
                    }
                    if (flashMode > 0)
                    {
                        var posBehind = posTarget.LSExtend(posEnd, -DistFlash);
                        if (posPlayer.Distance(posBehind) < FlashRange
                            && posTarget.Distance(posBehind) < posEnd.Distance(posBehind))
                        {
                            return new Tuple<Vector3, bool>(posBehind, true);
                        }
                    }
                }
                return new Tuple<Vector3, bool>(new Vector3(), false);
            }

            private static Vector3 GetPositionAfterKick(AIHeroClient target)
            {
                return target.ServerPosition.LSExtend(GetPositionKickTo(target), RKickRange);
            }

            private static Vector3 GetPositionBehind(AIHeroClient target)
            {
                return target.ServerPosition.LSExtend(
                    GetPositionKickTo(target),
                    -(WardManager.CanWardJump ? DistWard : DistFlash));
            }

            #endregion
        }

        private static class WardManager
        {
            #region Constants

            internal const int WardRange = 600;

            #endregion

            #region Static Fields

            internal static int LastInsecWardTime, LastInsecJumpTme;

            private static Vector3 lastPlacePos;

            private static int lastPlaceTime;

            #endregion

            #region Properties

            internal static bool CanWardJump => CanCastWard && W.IsReady() && IsWOne;

            private static bool CanCastWard => Variables.TickCount - lastPlaceTime > 1250 && Items.GetWardSlot() != null
                ;

            private static bool IsTryingToJump => lastPlacePos.IsValid() && Variables.TickCount - lastPlaceTime < 1250;

            #endregion

            #region Methods

            internal static void Init()
            {
                Game.OnUpdate += args =>
                    {
                        if (lastPlacePos.IsValid() && Variables.TickCount - lastPlaceTime > 1500)
                        {
                            lastPlacePos = new Vector3();
                        }
                        if (Player.IsDead)
                        {
                            return;
                        }
                        if (IsTryingToJump)
                        {
                            Jump(lastPlacePos);
                        }
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!lastPlacePos.IsValid() || !sender.IsMe || args.Slot != SpellSlot.W
                            || !args.SData.Name.ToLower().Contains("one"))
                        {
                            return;
                        }
                        var ward = args.Target as Obj_AI_Minion;
                        if (ward == null || !ward.IsValid() || ward.Distance(lastPlacePos) > 150)
                        {
                            return;
                        }
                        var tick = Variables.TickCount;
                        if (tick - LastInsecJumpTme < 1250)
                        {
                            LastInsecJumpTme = tick;
                        }
                        Insec.IsWardFlash = false;
                        lastPlacePos = new Vector3();
                    };
                GameObjectNotifier<Obj_AI_Minion>.OnCreate += (sender, minion) =>
                {
                    if (!lastPlacePos.IsValid() || minion.Distance(lastPlacePos) > 150 || !minion.IsAlly
                        || !minion.IsWard() || !W.IsInRange(minion))
                    {
                        return;
                    }
                    var tick = Variables.TickCount;
                    if (tick - LastInsecWardTime < 1250)
                    {
                        LastInsecWardTime = tick;
                    }
                    if (tick - lastPlaceTime < 1250 && W.IsReady() && IsWOne && W.CastOnUnit(minion))
                    {
                        lastW = tick;
                    }
                };
            }

            internal static bool Place(Vector3 pos, int type = 0)
            {
                if (!CanWardJump)
                {
                    return false;
                }
                var ward = Items.GetWardSlot();
                if (ward == null)
                {
                    return false;
                }
                var posPlayer = Player.ServerPosition;
                var posPlace = pos.Distance(posPlayer) < WardRange ? pos : posPlayer.LSExtend(pos, WardRange);
                if (!Player.Spellbook.CastSpell(ward.SpellSlot, posPlace))
                {
                    return false;
                }
                if (type == 0)
                {
                    lastPlaceTime = Variables.TickCount + 1100;
                }
                else if (type == 1)
                {
                    lastPlaceTime = LastInsecWardTime = LastInsecJumpTme = Variables.TickCount;
                }
                lastPlacePos = posPlace;
                return true;
            }

            private static void Jump(Vector3 pos)
            {
                if (!W.IsReady() || !IsWOne || Variables.TickCount - lastW <= 500)
                {
                    return;
                }
                var wardObj =
                    GameObjects.AllyWards.Where(
                        i => i.LSIsValidTarget(W.Range, false) && i.IsWard() && i.Distance(pos) < 200)
                        .MinOrDefault(i => i.Distance(pos));
                if (wardObj != null && W.CastOnUnit(wardObj))
                {
                    lastW = Variables.TickCount;
                }
            }

            #endregion
        }
    }
}