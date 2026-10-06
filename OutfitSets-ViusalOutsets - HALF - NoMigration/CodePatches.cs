using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace OutfitSets {
    public partial class ModEntry {
        [HarmonyPatch(
                typeof(InventoryPage),
                new Type[]{typeof(int), typeof(int), typeof(int), typeof(int)}
            )]
        [HarmonyPatch(MethodType.Constructor)]
        public static class InventoryPage_Patch {
            public static void Postfix() {}
        }[HarmonyPatch(
                typeof(IClickableMenu),
                nameof(IClickableMenu.moveCursorInDirection)
            )]
        public static class IClickableMenu_moveCursorInDirection_Patch {
            public static bool Prefix(IClickableMenu __instance, int direction) {
                if (!Config.ModEnabled || __instance.currentlySnappedComponent == null || __instance is not InventoryPage page) {
                    return true;
                }

                ClickableComponent old = __instance.currentlySnappedComponent;

                int index = old.myID - IDOffset;
                int next = -1;

                if (direction == 2 && old.myID == 104) {
                    next = 0;
                } else if (direction == 2 && old.myID == 109) {
                    next = Config.Sets - 1;
                } else if (direction == 1 && index >= 0 && index < Config.Sets - 1) {
                    next = index + 1;
                } else if (direction == 3 && index > 0 && index < Config.Sets) {
                    next = index - 1;
                } else if (direction == 0 && index >= 0 && index < Config.Sets) {
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(
                        index < Config.Sets / 2
                            ? 104
                            : 109
                    );

                    __instance.snapCursorToCurrentSnappedComponent();
                    Game1.playSound("shiny4");

                    return false;
                }

                if (next < 0) 
                    return true;
                
                Point center = GetCenterPoint(page, next);

                __instance.currentlySnappedComponent = new ClickableComponent(
                    new Rectangle(center.X - 12, center.Y - 12, 24, 24),
                    ""
                ) {
                    myID = IDOffset + next,
                    downNeighborID = -99998,
                    leftNeighborID = -99998,
                    rightNeighborID = -99998
                };

                __instance.snapCursorToCurrentSnappedComponent();
                Game1.playSound("shiny4");

                return false;
            }
        }[HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.receiveLeftClick))]
        public static class InventoryPage_receiveLeftClick_Patch {
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(InventoryPage __instance, int x, int y) {
                if (!Config.ModEnabled || Game1.player == null) 
                    return true;
                
                for (int i = 0; i < Config.Sets; i++) {
                    Point center = GetCenterPoint(__instance, i);

                    if (!new Rectangle(center.X - 12, center.Y - 12, 24, 24).Contains(x, y)) {
                        continue;
                    }

                    SwitchSet(i + 1);
                    return false;
                }

                return true;
            }
        }[HarmonyPatch(
                typeof(InventoryPage),
                nameof(InventoryPage.draw),
                new Type[]{typeof(SpriteBatch)}
            )]
        public static class InventoryPage_draw_Patch {
            private static readonly Color VisualNormal = new Color(150, 119, 174); 

            private static readonly Color VisualSelected = new Color(91, 49, 139); 

            private static readonly Color RealNormal = new Color(172, 131, 93); 

            private static readonly Color RealSelected = new Color(106, 62, 36); 

            private static readonly Color SelectedText = new Color(255, 246, 226); 

            private const int MarkerTopTrim = 6;
            private const int MarkerBottomTrim = 4;

            public static void Postfix(InventoryPage __instance, SpriteBatch b) {
                if (!Config.ModEnabled || Game1.player == null) 
                    return;
                
                int selectedRealSet = FirstRealSet;

                if (Game1.player.modData.TryGetValue(keyPrefix + "currentSet", out string data) && int.TryParse(data, out int saved) && saved >= FirstRealSet && saved <= Config.Sets) {
                    selectedRealSet = saved;
                }

                int selectedVisualSet = SelectedVisualSet >= 1 && SelectedVisualSet <= VisualSetCount
                    ? SelectedVisualSet
                    : 1;

                for (int i = 0; i < Config.Sets; i++) {
                    int number = i + 1;
                    string text = number.ToString();

                    Vector2 center = GetCenterPoint(__instance, i).ToVector2();

                    Vector2 size = Game1
                        .tinyFont
                        .MeasureString(text);

                    Vector2 position = center - size / 2f;

                    bool isVisual = number <= VisualSetCount;

                    bool selected = isVisual
                        ? number == selectedVisualSet
                        : number == selectedRealSet;

                    if (selected) {
                        Color background = isVisual
                            ? VisualSelected
                            : RealSelected;

                        int markerHeight = Math.Max(
                            1,
                            (int)Math.Ceiling(size.Y) - MarkerTopTrim - MarkerBottomTrim
                        );

                        Rectangle marker = new Rectangle(
                            (int)Math.Floor(position.X) - 2,
                            
                                    (int)Math.Floor(position.Y) + MarkerTopTrim + 1,
                          
                                    (int)Math.Ceiling(size.X) + 2,
                            markerHeight
                        );

                        b.Draw(Game1.staminaRect, marker, background);
                    }

                    Color textColor = selected
                        ? SelectedText
                        : isVisual
                            ? VisualNormal
                            : RealNormal;

                    b.DrawString(Game1.tinyFont, text, position, textColor);
                }
            }
        }
    }
}
