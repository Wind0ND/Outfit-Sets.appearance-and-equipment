using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OutfitSets
{
    internal sealed class VisualSetDrawState
    {
        internal Farmer Player;

        internal Hat Hat;
        internal Clothing Shirt;
        internal Clothing Pants;

        internal string ShoeColor;
        internal bool ShoeChanged;
    }

    [HarmonyPatch]
    internal static class VisualSetDrawPatch
    {
        private const BindingFlags Flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic;

        private sealed class Cached
        {
            internal string Xml;
            internal Item Item;
        }

        private static readonly
            Dictionary<string, Cached> Cache =
                new Dictionary<string, Cached>();

        private static Farmer cacheOwner;

        private static FieldInfo hatField;
        private static FieldInfo clothingField;
        private static FieldInfo shoeField;

        private static FieldInfo spriteDirty;
        private static FieldInfo shirtDirty;
        private static FieldInfo pantsDirty;
        private static FieldInfo shoesDirty;

        private static int depth;
        private static bool warned;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools
                .GetDeclaredMethods(
                    typeof(FarmerRenderer)
                )
                .Where(method =>
                    method.Name == "draw"
                );
        }

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

        private static T GetVisual<T>(
            Farmer farmer,
            string slot,
            int number
        ) where T : Item
        {
            if (!ReferenceEquals(cacheOwner, farmer))
            {
                Cache.Clear();
                cacheOwner = farmer;
            }

            string cacheKey = slot + number;
            string dataKey =
                ModEntry.VisualItemKey(
                    slot,
                    number
                );

            if (!farmer.modData.TryGetValue(
                dataKey,
                out string xml
            ) || string.IsNullOrWhiteSpace(xml))
            {
                Cache.Remove(cacheKey);
                return null;
            }

            if (Cache.TryGetValue(
                cacheKey,
                out Cached cached
            ) && cached.Xml == xml)
            {
                return cached.Item as T;
            }

            T item = null;

            try
            {
                item = xml.FromXml<T>();
            }
            catch (Exception ex)
            {
                ModEntry.SMonitor.Log(
                    $"Visual {slot} in set "
                    + $"{number} could not be read: {ex}",
                    LogLevel.Warn
                );
            }

            Cache[cacheKey] = new Cached
            {
                Xml = xml,
                Item = item
            };

            return item;
        }

        private static Farmer FindFarmer(
            object[] args
        )
        {
            foreach (object arg in args)
            {
                if (arg is Farmer farmer)
                    return farmer;
            }

            return null;
        }

        private static void MarkDirty(
            FarmerRenderer renderer
        )
        {
            Type type = typeof(FarmerRenderer);

            spriteDirty ??= type.GetField(
                "_spriteDirty",
                Flags
            );

            shirtDirty ??= type.GetField(
                "_shirtDirty",
                Flags
            );

            pantsDirty ??= type.GetField(
                "_pantsDirty",
                Flags
            );

            shoesDirty ??= type.GetField(
                "_shoesDirty",
                Flags
            );

            spriteDirty?.SetValue(renderer, true);
            shirtDirty?.SetValue(renderer, true);
            pantsDirty?.SetValue(renderer, true);
            shoesDirty?.SetValue(renderer, true);
        }

        [HarmonyPrefix]
        private static void Prefix(
            object[] __args,
            ref VisualSetDrawState __state
        )
        {
            __state = null;

            if (!ModEntry.Config.ModEnabled ||
                !Context.IsWorldReady ||
                Game1.player == null ||
                depth > 0)
            {
                return;
            }

            Farmer farmer = FindFarmer(__args);

            if (!ReferenceEquals(
                farmer,
                Game1.player
            ))
            {
                return;
            }

            hatField ??= Find(
                farmer.hat.GetType(),
                typeof(Hat)
            );

            clothingField ??= Find(
                farmer.shirtItem.GetType(),
                typeof(Clothing)
            );

            shoeField ??= Find(
                farmer.FarmerRenderer
                    .shoes.GetType(),
                typeof(string)
            );

            if (hatField == null ||
                clothingField == null ||
                shoeField == null)
            {
                if (!warned)
                {
                    warned = true;

                    ModEntry.SMonitor.Log(
                        "Visual renderer backing fields not found; skipping override.",
                        LogLevel.Warn
                    );
                }

                return;
            }

            var state = new VisualSetDrawState
            {
                Player = farmer,

                Hat = farmer.hat.Value,
                Shirt = farmer.shirtItem.Value,
                Pants = farmer.pantsItem.Value,

                ShoeColor =
                    shoeField.GetValue(
                        farmer.FarmerRenderer.shoes
                    ) as string
            };

            __state = state;
            depth++;

            try
            {
                int number =
                    ModEntry.SelectedVisualSet;

                Hat hat = GetVisual<Hat>(
                    farmer,
                    "hat",
                    number
                );

                Clothing shirt =
                    GetVisual<Clothing>(
                        farmer,
                        "shirt",
                        number
                    );

                Clothing pants =
                    GetVisual<Clothing>(
                        farmer,
                        "pants",
                        number
                    );

                Boots boots =
                    GetVisual<Boots>(
                        farmer,
                        "boots",
                        number
                    );

                
                hatField.SetValue(
                    farmer.hat,
                    hat
                );

                clothingField.SetValue(
                    farmer.shirtItem,
                    shirt
                );

                clothingField.SetValue(
                    farmer.pantsItem,
                    pants
                );

                shoeField.SetValue(
                    farmer.FarmerRenderer.shoes,
                    boots?.GetBootsColorString()
                        ?? "0"
                );

                state.ShoeChanged = true;

                MarkDirty(
                    farmer.FarmerRenderer
                );
            }
            catch
            {
                Restore(state);
                __state = null;
                throw;
            }
        }

        private static void Restore(
            VisualSetDrawState state
        )
        {
            if (state == null)
                return;

            try
            {
                Farmer farmer = state.Player;

                hatField?.SetValue(
                    farmer.hat,
                    state.Hat
                );

                clothingField?.SetValue(
                    farmer.shirtItem,
                    state.Shirt
                );

                clothingField?.SetValue(
                    farmer.pantsItem,
                    state.Pants
                );

                if (state.ShoeChanged)
                {
                    shoeField?.SetValue(
                        farmer.FarmerRenderer.shoes,
                        state.ShoeColor ?? ""
                    );
                }

                MarkDirty(
                    farmer.FarmerRenderer
                );
            }
            finally
            {
                depth--;
            }
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(
            Exception __exception,
            VisualSetDrawState __state
        )
        {
            Restore(__state);
            return __exception;
        }
    }
}
