using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OutfitSets
{
    internal sealed class VisualSlotsState
    {
        internal Farmer Player;
        internal int SetNumber;

        internal Hat Hat;
        internal Clothing Shirt;
        internal Clothing Pants;
        internal Boots Boots;

        internal Ring LeftRing;
        internal Ring RightRing;

        internal bool Save;
    }

    internal static class VisualSlots
    {
        private const BindingFlags Flags =
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        private static FieldInfo hatField;
        private static FieldInfo clothingField;
        private static FieldInfo bootsField;
        private static FieldInfo ringField;

        private static int depth;

        internal static bool Active => depth > 0;

        private static FieldInfo Find(
            Type type,
            Type valueType
        )
        {
            for (; type != null; type = type.BaseType)
            {
                FieldInfo field =
                    type.GetField("value", Flags);

                if (field != null &&
                    field.FieldType == valueType)
                {
                    return field;
                }
            }

            return null;
        }

        private static bool Prepare(Farmer player)
        {
            hatField ??= Find(
                player.hat.GetType(),
                typeof(Hat)
            );

            clothingField ??= Find(
                player.shirtItem.GetType(),
                typeof(Clothing)
            );

            bootsField ??= Find(
                player.boots.GetType(),
                typeof(Boots)
            );

            ringField ??= Find(
                player.leftRing.GetType(),
                typeof(Ring)
            );

            if (hatField != null &&
                clothingField != null &&
                bootsField != null &&
                ringField != null)
            {
                return true;
            }

            ModEntry.SMonitor.Log(
                "Cannot locate visual equipment backing fields; editing disabled.",
                LogLevel.Error
            );

            return false;
        }

        internal static VisualSlotsState Begin(bool save)
        {
            if (!ModEntry.Config.ModEnabled ||
                !ModEntry.EditingVisualSet ||
                Game1.player == null ||
                Active)
            {
                return null;
            }

            Farmer player = Game1.player;

            if (!Prepare(player))
                return null;

            int visualSet =
                ModEntry.SelectedVisualSet;

            if (visualSet < 1 ||
                visualSet > ModEntry.VisualSetCount)
            {
                return null;
            }

            var state = new VisualSlotsState
            {
                Player = player,
                SetNumber = visualSet,

                Hat = player.hat.Value,
                Shirt = player.shirtItem.Value,
                Pants = player.pantsItem.Value,
                Boots = player.boots.Value,

                LeftRing = player.leftRing.Value,
                RightRing = player.rightRing.Value,

                Save = save
            };

            depth++;

            try
            {
                
                hatField.SetValue(
                    player.hat,
                    ModEntry.ReadVisualItem<Hat>(
                        player,
                        "hat",
                        visualSet
                    )
                );

                clothingField.SetValue(
                    player.shirtItem,
                    ModEntry.ReadVisualItem<Clothing>(
                        player,
                        "shirt",
                        visualSet
                    )
                );

                clothingField.SetValue(
                    player.pantsItem,
                    ModEntry.ReadVisualItem<Clothing>(
                        player,
                        "pants",
                        visualSet
                    )
                );

                bootsField.SetValue(
                    player.boots,
                    ModEntry.ReadVisualItem<Boots>(
                        player,
                        "boots",
                        visualSet
                    )
                );

                ringField.SetValue(
                    player.leftRing,
                    ModEntry.ReadVisualItem<Ring>(
                        player,
                        "leftRing",
                        visualSet
                    )
                );

                ringField.SetValue(
                    player.rightRing,
                    ModEntry.ReadVisualItem<Ring>(
                        player,
                        "rightRing",
                        visualSet
                    )
                );

                return state;
            }
            catch
            {
                End(state, false);
                throw;
            }
        }

        internal static void End(
            VisualSlotsState state,
            bool successful
        )
        {
            if (state == null)
                return;

            try
            {
                if (successful && state.Save)
                {
                    Farmer player = state.Player;
                    int number = state.SetNumber;

                    ModEntry.SaveVisualItem(
                        player,
                        "hat",
                        number,
                        player.hat.Value
                    );

                    ModEntry.SaveVisualItem(
                        player,
                        "shirt",
                        number,
                        player.shirtItem.Value
                    );

                    ModEntry.SaveVisualItem(
                        player,
                        "pants",
                        number,
                        player.pantsItem.Value
                    );

                    ModEntry.SaveVisualItem(
                        player,
                        "boots",
                        number,
                        player.boots.Value
                    );

                    ModEntry.SaveVisualItem(
                        player,
                        "leftRing",
                        number,
                        player.leftRing.Value
                    );

                    ModEntry.SaveVisualItem(
                        player,
                        "rightRing",
                        number,
                        player.rightRing.Value
                    );
                }
            }
            finally
            {
                Farmer player = state.Player;

                try
                {
                    hatField.SetValue(
                        player.hat,
                        state.Hat
                    );

                    clothingField.SetValue(
                        player.shirtItem,
                        state.Shirt
                    );

                    clothingField.SetValue(
                        player.pantsItem,
                        state.Pants
                    );

                    bootsField.SetValue(
                        player.boots,
                        state.Boots
                    );

                    ringField.SetValue(
                        player.leftRing,
                        state.LeftRing
                    );

                    ringField.SetValue(
                        player.rightRing,
                        state.RightRing
                    );
                }
                finally
                {
                    depth--;
                }
            }
        }

        [HarmonyPatch(
            typeof(InventoryPage),
            nameof(InventoryPage.draw),
            new[] { typeof(SpriteBatch) }
        )]
        internal static class Draw
        {
            [HarmonyPrefix]
            private static void Prefix(
                ref VisualSlotsState __state
            )
            {
                __state = Begin(false);
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(
                Exception __exception,
                VisualSlotsState __state
            )
            {
                End(__state, false);
                return __exception;
            }
        }

        [HarmonyPatch(
            typeof(InventoryPage),
            nameof(InventoryPage.receiveLeftClick)
        )]
        internal static class Left
        {
            [HarmonyPriority(Priority.Last)]
            [HarmonyPrefix]
            private static void Prefix(
                InventoryPage __instance,
                int x,
                int y,
                ref VisualSlotsState __state
            )
            {
                for (
                    int i = 0;
                    i < ModEntry.Config.Sets;
                    i++
                )
                {
                    Microsoft.Xna.Framework.Point point =
                        ModEntry.GetCenterPoint(
                            __instance,
                            i
                        );

                    if (new Microsoft.Xna.Framework.Rectangle(
                        point.X - 12,
                        point.Y - 12,
                        24,
                        24
                    ).Contains(x, y))
                    {
                        return;
                    }
                }

                __state = Begin(true);
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(
                Exception __exception,
                VisualSlotsState __state
            )
            {
                End(
                    __state,
                    __exception == null
                );

                return __exception;
            }
        }

        [HarmonyPatch(
            typeof(InventoryPage),
            nameof(InventoryPage.receiveRightClick)
        )]
        internal static class Right
        {
            [HarmonyPrefix]
            private static void Prefix(
                ref VisualSlotsState __state
            )
            {
                __state = Begin(true);
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(
                Exception __exception,
                VisualSlotsState __state
            )
            {
                End(
                    __state,
                    __exception == null
                );

                return __exception;
            }
        }

        [HarmonyPatch(
            typeof(IClickableMenu),
            nameof(IClickableMenu.releaseLeftClick)
        )]
        internal static class Release
        {
            [HarmonyPrefix]
            private static void Prefix(
                IClickableMenu __instance,
                ref VisualSlotsState __state
            )
            {
                if (__instance is InventoryPage)
                    __state = Begin(true);
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(
                Exception __exception,
                VisualSlotsState __state
            )
            {
                End(
                    __state,
                    __exception == null
                );

                return __exception;
            }
        }

        
        [HarmonyPatch]
        internal static class SuppressVisualItemEffects
        {
            private static IEnumerable<MethodBase>
                TargetMethods()
            {
                Type[] types =
                {
                    typeof(Item),
                    typeof(Hat),
                    typeof(Clothing),
                    typeof(Boots),
                    typeof(Ring)
                };

                var found =
                    new HashSet<MethodBase>();

                foreach (Type type in types)
                {
                    foreach (
                        MethodInfo method
                        in AccessTools.GetDeclaredMethods(
                            type
                        )
                    )
                    {
                        if (
                            (method.Name == "onEquip" ||
                             method.Name == "onUnequip") &&
                            !method.IsAbstract &&
                            !method.ContainsGenericParameters &&
                            found.Add(method)
                        )
                        {
                            yield return method;
                        }
                    }
                }
            }

            [HarmonyPrefix]
            private static bool Prefix()
            {
                return !Active;
            }
        }
    }
}
