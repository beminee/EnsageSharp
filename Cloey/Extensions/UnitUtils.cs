﻿using Ensage;
using Ensage.Common.Menu;
using Ensage.Common.Enums;
using Ensage.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using Ensage.Common.Objects;
using SharpDX;

namespace Cloey.Extensions
{
    internal static class UnitUtils
    {
        #region Tidy: Aura Mechanics

        internal static bool IsImmobile(this Unit unit)
        {
            var strs = new[] {"Stunned", "Frozen", "Rooted", "Flying"};
            return strs.Any(unitstate => unit.UnitState.ToString().Contains(unitstate));
        }

        #endregion

        #region Tidy: Clustered Units

        internal static IEnumerable<Unit> GetRadiusCluster(this Unit unit, IEnumerable<Unit> otherUnits, float radius)
        {
            if (unit != null)
            {
                var targetLoc = unit.Position;
                return otherUnits.Where(u => u.Position.Dist(targetLoc, true) <= radius * radius);
            }
            return null;
        }

        internal static Unit GetBestUnitForCluster(IEnumerable<Unit> units, float clusterRange)
        {
            IEnumerable<Unit> wUnits = units as Unit[] ?? units.ToArray();

            if (wUnits.Any())
            {
                var firstOrDefault = (wUnits.Select(u => new {Count = GetRadiusClusterCount(u, wUnits, clusterRange), Unit = u})).OrderByDescending(a => a.Count).FirstOrDefault();
                if (firstOrDefault != null)
                    return firstOrDefault.Unit;
            }

            return null;
        }

        internal static int GetRadiusClusterCount(this Unit target, IEnumerable<Unit> otherUnits, float radius)
        {
            var rdx = radius * radius;
            var targetLoc = target.NetworkPosition;
            return otherUnits.Count(u => u.Position.Dist(targetLoc, true) <= rdx);
        }

        #endregion

        #region Tidy: ValidUnit

        internal static bool IsValidUnit(this Unit u, float range = float.MaxValue, Vector3 from = default(Vector3), bool checkTeam = true)
        {
            return u != null && u.IsVisible && u.IsValid && u.IsAlive && u.IsSelectable && !u.IsIllusion &&
                   u.Position.Dist(from != default(Vector3) ? from : ObjectManager.LocalHero.NetworkPosition, true) <=
                   range * range && (u.Team != ObjectManager.LocalHero.Team || !checkTeam);
        }

        internal static bool IsControlledByMe(this Unit unit)
        {
            var source = ObjectManager.LocalPlayer;
            return source != null && source.Selection.Where(x => (x as Unit).IsValidUnit()).Contains(unit);
        }

        internal static bool IsControlledByPlayer(this Unit unit, Player player)
        {
            var source = player;
            return source != null && source.Selection.Where(x => (x as Unit).IsValidUnit()).Contains(unit);
        }

        #endregion

        #region Tidy: Misc

        internal static Hero GetTarget(this Hero me, float range, Menu menu)
        {
            // todo: finish/new target selector currently using a common edit
            var num1 = me.MinimumDamage + me.BonusDamage;
            var num2 = 0.0f;

            Hero target = null;

            if (menu.Item("targeting").GetValue<StringList>().SelectedIndex == 0)
                return Heroes.All.Where(x=> x.IsValidUnit(range)).OrderBy(x => x.Dist(me.Position)).FirstOrDefault();

            foreach (Hero hero2 in Heroes.All.Where(x => x.IsValidUnit(range)))
            {
                var num3 = hero2.DamageTaken(num1, DamageType.Physical, me);
                var num4 = hero2.Health / num3;

                if (target == null || num2 > num4)
                {
                    target = hero2;
                    num2 = num4;
                }
            }

            return target;
        }

        public static string GetHeroName(this string n)
        {
            return n.Substring(n.LastIndexOf("o_", StringComparison.Ordinal) + 2);
        }

        public static float GetSpellAmp(this Hero hero)
        {

            var spellAmp = (100.0f + hero.TotalIntelligence / 16.0f) / 100.0f;

            var aether = hero.GetItemById(ItemId.item_aether_lens);
            if (aether != null)
            {
                spellAmp += aether.AbilitySpecialData.First(x => x.Name == "spell_amp").Value / 100.0f;
            }

            var talent = hero.Spellbook.Spells.FirstOrDefault(x => x.Level > 0 && x.Name.StartsWith("special_bonus_spell_amplify_"));
            if (talent != null)
            {
                spellAmp += talent.AbilitySpecialData.First(x => x.Name == "value").Value / 100.0f;
            }

            return spellAmp;
        }

        #endregion
    }
}