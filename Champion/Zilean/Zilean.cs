﻿namespace ElZilean
{
    using System;
    using System.Linq;
    using System.Net;

    using EloBuddy;
    using LeagueSharp.Common;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK;
    using Color = System.Drawing.Color;
    using Spell = LeagueSharp.Common.Spell;

    internal class Zilean
    {

        #region Public Properties

        /// <summary>
        ///     Gets or sets the slot.
        /// </summary>
        /// <value>
        ///     The Smitespell
        /// </value>
        private static Spell IgniteSpell { get; set; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the E spell
        /// </summary>
        /// <value>
        ///     The E spell
        /// </value>
        private static Spell E { get; set; }

        /// <summary>
        ///     Gets or sets the menu
        /// </summary>
        /// <value>
        ///     The menu
        /// </value>
        private static Menu Menu { get; set; }
        public static Menu comboMenu, harassMenu, fleeMenu, ultMenu, laneMenu, drawingsMenu;

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private static AIHeroClient Player => ObjectManager.Player;

        /// <summary>
        ///     Check if Zilean has speed passive
        /// </summary>
        private static bool HasSpeedBuff => Player.Buffs.Any(x => x.Name.ToLower().Contains("timewarp"));

        /// <summary>
        ///     Gets or sets the Q spell
        /// </summary>
        /// <value>
        ///     The Q spell
        /// </value>
        private static Spell Q { get; set; }

        /// <summary>
        ///     Gets or sets the R spell.
        /// </summary>
        /// <value>
        ///     The R spell
        /// </value>
        private static Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the W spell
        /// </summary>
        /// <value>
        ///     The W spell
        /// </value>
        private static Spell W { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Fired when the game loads.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        public static void OnGameLoad()
        {
            try
            {
                if (Player.ChampionName != "Zilean")
                {
                    return;
                }

                var igniteSlot = Player.GetSpellSlot("summonerdot");
                if (igniteSlot != SpellSlot.Unknown)
                {
                    IgniteSpell = new Spell(igniteSlot, 600f);
                }

                foreach (var ally in HeroManager.Allies)
                {
                    IncomingDamageManager.AddChampion(ally);
                    Console.WriteLine(@"[ELZILEAN] loaded champions: {0}", ally.ChampionName);
                }

                IncomingDamageManager.Skillshots = true;


                Q = new Spell(SpellSlot.Q, 900f);
                W = new Spell(SpellSlot.W, Player.GetAutoAttackRange(Player));
                E = new Spell(SpellSlot.E, 700f);
                R = new Spell(SpellSlot.R, 900f);

                Q.SetSkillshot(0.7f, 140f, int.MaxValue, false, SkillshotType.SkillshotCircle);

                GenerateMenu();

                Game.OnUpdate += OnUpdate;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates the menu
        /// </summary>
        /// <value>
        ///     Creates the menu
        /// </value>
        private static void GenerateMenu()
        {
            try
            {
                Menu = MainMenu.AddMenu("ElZilean", "ElZilean");


                comboMenu = Menu.AddSubMenu("Combo", "Combo");
                {
                    comboMenu.Add("ElZilean.Combo.Q", new CheckBox("Use Q", true));
                    comboMenu.Add("ElZilean.Combo.W", new CheckBox("Use W", true));
                    comboMenu.Add("ElZilean.Combo.E", new CheckBox("Use E", true));
                    comboMenu.Add("ElZilean.Ignite", new CheckBox("Use Ignite", true));
                    comboMenu.Add("ElZilean.Combo.W2", new CheckBox("Always reset Q", false));
                }


                harassMenu = Menu.AddSubMenu("Harass", "Harass");
                {
                    harassMenu.Add("ElZilean.Harass.Q", new CheckBox("Use Q", true));
                    harassMenu.Add("ElZilean.Harass.W", new CheckBox("Use W", true));
                }


                ultMenu = Menu.AddSubMenu("Ultimate", "Ultimate");
                {
                    ultMenu.Add("min-health", new Slider("Health percentage", 20, 0, 100));
                    ultMenu.Add("min-damage", new Slider("Heal on % incoming damage", 20, 0, 100));
                    ultMenu.Add("ElZilean.Ultimate.R", new CheckBox("Use R", true));
                    ultMenu.AddLabel("Ultimate Wihtelist");
                    foreach (var x in HeroManager.Allies)
                    {
                        ultMenu.Add($"R{x.ChampionName}", new CheckBox("Use R on " + x.ChampionName));
                    }
                }


                laneMenu = Menu.AddSubMenu("Laneclear", "Laneclear");
                {
                    laneMenu.Add("ElZilean.laneclear.Q", new CheckBox("Use Q", true));
                    laneMenu.Add("ElZilean.laneclear.W", new CheckBox("Use W", true));
                    laneMenu.Add("ElZilean.laneclear.Mana", new Slider("Minimum mana", 20, 0, 100));
                    laneMenu.Add("ElZilean.laneclear.QMouse", new CheckBox("Cast Q to mouse", false));
                }

                fleeMenu = Menu.AddSubMenu("Flee", "Flee");
                {
                    fleeMenu.Add("ElZilean.Flee.Key", new KeyBind("Flee key", false, KeyBind.BindTypes.HoldActive, 'Z'));
                    fleeMenu.Add("ElZilean.Flee.Mana", new Slider("Minimum mana", 20, 0, 100));
                }


                drawingsMenu = Menu.AddSubMenu("Drawings", "Drawings");
                {
                    drawingsMenu.Add("ElZilean.Draw.Off", new CheckBox("Disable drawings", false));
                    drawingsMenu.Add("ElZilean.Draw.Q", new CheckBox("Draw Q", true));
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


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

        /// <summary>
        ///     Called when the game draws itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (getCheckBoxItem(drawingsMenu, "ElZilean.Draw.Off"))
                {
                    return;
                }

                if (getCheckBoxItem(drawingsMenu, "ElZilean.Draw.Q"))
                {
                    if (Q.Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.DodgerBlue);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        /// <summary>
        ///     The ignite killsteal logic
        /// </summary>
        private static void HandleIgnite()
        {
            try
            {
                if (Player.GetSpellSlot("summonerdot") == SpellSlot.Unknown)
                {
                    return;
                }

                var kSableEnemy =
                    HeroManager.Enemies.FirstOrDefault(
                        hero =>
                        hero.LSIsValidTarget(550f) && !hero.HasBuff("summonerdot") && !hero.IsZombie
                        && Player.GetSummonerSpellDamage(hero, LeagueSharp.Common.Damage.SummonerSpell.Ignite) >= hero.Health);

                if (kSableEnemy != null)
                {
                    Player.Spellbook.CastSpell(IgniteSpell.Slot, kSableEnemy);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


        /// <summary>
        ///     Combo logic
        /// </summary>
        private static void OnCombo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (getCheckBoxItem(comboMenu, "ElZilean.Combo.E") && E.IsReady())
            {
                if (Player.GetEnemiesInRange(E.Range).Any())
                {
                    var closestEnemy =
                        Player.GetEnemiesInRange(E.Range)
                            .OrderByDescending(h => (h.PhysicalDamageDealtPlayer + h.MagicDamageDealtPlayer))
                            .FirstOrDefault();

                    if (closestEnemy == null)
                    {
                        return;
                    }

                    if (closestEnemy.HasBuffOfType(BuffType.Stun))
                    {
                        return;
                    }

                    E.Cast(closestEnemy);
                }
                if (Player.GetAlliesInRange(E.Range).Any() && Player.GetEnemiesInRange(800f).Count >= 1)
                {
                    var closestToTarget = Player.GetAlliesInRange(E.Range)
                      .OrderByDescending(h => (h.PhysicalDamageDealtPlayer + h.MagicDamageDealtPlayer))
                      .FirstOrDefault();

                    E.Cast(closestToTarget);
                }
            }

            if (getCheckBoxItem(comboMenu, "ElZilean.Combo.Q") && Q.IsReady() && target.LSIsValidTarget(Q.Range) && !target.IsZombie)
            {
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance >= HitChance.VeryHigh)
                {
                    Q.Cast(pred.CastPosition);
                }
            }

            if (getCheckBoxItem(comboMenu, "ElZilean.Combo.W") && getCheckBoxItem(comboMenu, "ElZilean.Combo.W2") && W.IsReady() && !Q.IsReady())
            {
                if (Q.Instance.CooldownExpires - Game.Time < 3)
                {
                    return;
                }

                W.Cast();
                Console.WriteLine("ALWAYS RESET Q");
            }

            // Check if target has a bomb
            var isBombed =
            HeroManager.Enemies
                .FirstOrDefault(x => x.HasBuff("ZileanQEnemyBomb") && x.LSIsValidTarget(Q.Range));
            if (!isBombed.LSIsValidTarget())
            {
                return;
            }

            if (isBombed != null && isBombed.LSIsValidTarget(Q.Range))
            {
                if (Q.Instance.CooldownExpires - Game.Time < 3)
                {
                    Console.WriteLine("3");
                    return;
                }

                if (getCheckBoxItem(comboMenu, "ElZilean.Combo.W"))
                {
                    W.Cast();
                }
            }

            if (getCheckBoxItem(comboMenu, "ElZilean.Ignite") && isBombed != null)
            {
                if (Player.GetSpellSlot("summonerdot") == SpellSlot.Unknown)
                {
                    return;
                }

                if (Q.GetDamage(isBombed) + IgniteSpell.GetDamage(isBombed) > isBombed.Health)
                {
                    if (isBombed.LSIsValidTarget(Q.Range))
                    {
                        Player.Spellbook.CastSpell(IgniteSpell.Slot, isBombed);
                    }
                }
            }
        }

        /// <summary>
        ///     E Flee to mouse
        /// </summary>
        private static void OnFlee()
        {
            try
            {
                if (E.IsReady() && Player.Mana > getSliderItem(fleeMenu, "ElZilean.Flee.Mana"))
                {
                    E.Cast();
                }

                if (HasSpeedBuff)
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }

                if (!E.IsReady() && W.IsReady())
                {
                    if (HasSpeedBuff)
                    {
                        return;
                    }

                    W.Cast();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     Harass logic by Chewymoon (pls no kill)
        /// </summary>
        private static void OnHarass()
        {
            try
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (target == null)
                {
                    return;
                }

                if (getCheckBoxItem(harassMenu, "ElZilean.Harass.Q") && Q.IsReady() && target.LSIsValidTarget(Q.Range))
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.VeryHigh)
                    {
                        Q.Cast(pred.UnitPosition);
                    }
                }

                if (getCheckBoxItem(harassMenu, "ElZilean.Harass.W") && W.IsReady() && !Q.IsReady())
                {
                    W.Cast();
                    Console.WriteLine("Resetted W");
                }

                // Check if target has a bomb
                var isBombed =
                HeroManager.Enemies
                    .Find(x => x.HasBuff("ZileanQEnemyBomb") && x.LSIsValidTarget(Q.Range));

                if (isBombed.LSIsValidTarget())
                {
                    if (getCheckBoxItem(harassMenu, "ElZilean.Harass.Q"))
                    {
                        W.Cast();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     The laneclear "logic"
        /// </summary>
        private static void OnLaneclear()
        {
            try
            {
                var minion = MinionManager.GetMinions(Player.Position, Q.Range + Q.Width);
                if (minion == null)
                {
                    return;
                }

                if (Player.ManaPercent < getSliderItem(laneMenu, "ElZilean.laneclear.Mana"))
                {
                    return;
                }

                var farmLocation =
                   MinionManager.GetBestCircularFarmLocation(
                       MinionManager.GetMinions(Q.Range).Select(x => x.ServerPosition.LSTo2D()).ToList(),
                       Q.Width,
                       Q.Range);

                if (farmLocation.MinionsHit == 0)
                {
                    return;
                }

                if (getCheckBoxItem(laneMenu, "ElZilean.laneclear.Q") && getCheckBoxItem(laneMenu, "ElZilean.laneclear.QMouse") && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }

                if (getCheckBoxItem(laneMenu, "ElZilean.laneclear.Q") && Q.IsReady()
                    && !getCheckBoxItem(laneMenu, "ElZilean.laneclear.QMouse") && farmLocation.MinionsHit >= 3)
                {
                    Q.Cast(farmLocation.Position.To3D());
                }

                if (getCheckBoxItem(laneMenu, "ElZilean.laneclear.W") && W.IsReady())
                {
                    W.Cast();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     Called when the game updates
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnUpdate(EventArgs args)
        {
            try
            {
                if (Player.IsDead)
                {
                    return;
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    OnCombo();
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    OnHarass();
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    OnLaneclear();
                }

                if (getCheckBoxItem(comboMenu, "ElZilean.Ignite"))
                {
                    HandleIgnite();
                }

                if (getKeyBindItem(fleeMenu, "ElZilean.Flee.Key"))
                {
                    OnFlee();
                }

                foreach (var ally in HeroManager.Allies)
                {
                    if (!getCheckBoxItem(ultMenu, $"R{ally.ChampionName}") || ally.LSIsRecalling()
                        || ally.IsInvulnerable)
                    {
                        return;
                    }

                    var enemies = ally.LSCountEnemiesInRange(750f);
                    var totalDamage = IncomingDamageManager.GetDamage(ally) * 1.1f;
                    if (ally.HealthPercent <= getSliderItem(ultMenu, "min-health") && !ally.IsDead && enemies >= 1)
                    {
                        if ((int)(totalDamage / ally.Health) > getSliderItem(ultMenu, "min-damage")
                            || ally.HealthPercent < getSliderItem(ultMenu, "min-health"))
                        {
                            R.Cast(ally);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion
    }
}