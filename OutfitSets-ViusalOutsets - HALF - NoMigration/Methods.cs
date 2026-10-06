using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.IO;
using System.Xml.Serialization;

namespace OutfitSets
{
    public partial class ModEntry
    {
        private static void SwitchSet(
            int which,
            bool force = false
        )
        {
            Farmer player = Game1.player;

            if (player == null || !Config.ModEnabled)
                return;

            if (which < 1 || which > Config.Sets)
            {
                SMonitor.Log(
                    $"Set must be between 1 and {Config.Sets}.",
                    LogLevel.Warn
                );
                return;
            }

            if (which <= VisualSetCount)
            {
               
                SelectedVisualSet = which;
                EditingVisualSet = true;
                Game1.playSound("shwip");
                return;
            }

            EditingVisualSet = false;

            int oldSet = 0;
            bool hasOldSet =
                player.modData.TryGetValue(
                    keyPrefix + "currentSet",
                    out string data
                ) &&
                int.TryParse(data, out oldSet);

            if (hasOldSet &&
                oldSet == which &&
                !force)
            {
                return;
            }

          
            if (hasOldSet &&
                oldSet >= FirstRealSet &&
                oldSet <= Config.Sets)
            {
                SaveCurrentRealSet(player, oldSet);
            }

            player.leftRing.Value?.onUnequip(player);
            player.rightRing.Value?.onUnequip(player);

            player.Equip(
                ReadSavedItem<Hat>(player, "hat", which),
                player.hat
            );

            player.Equip(
                ReadSavedItem<Clothing>(
                    player,
                    "shirt",
                    which
                ),
                player.shirtItem
            );

            player.Equip(
                ReadSavedItem<Clothing>(
                    player,
                    "pants",
                    which
                ),
                player.pantsItem
            );

            player.Equip(
                ReadSavedItem<Boots>(
                    player,
                    "boots",
                    which
                ),
                player.boots
            );

            Ring left = ReadSavedItem<Ring>(
                player,
                "leftRing",
                which
            );

            Ring right = ReadSavedItem<Ring>(
                player,
                "rightRing",
                which
            );

            player.leftRing.Value = left;
            player.rightRing.Value = right;

            left?.onEquip(player);
            right?.onEquip(player);

            player.modData[
                keyPrefix + "currentSet"
            ] = which.ToString();

            Game1.playSound("shwip");
        }

      
        internal static string VisualItemKey(
            string slot,
            int number
        )
        {
            return number == 1
                ? keyPrefix + slot + "1"
                : keyPrefix + "visual/" + slot + number;
        }

        internal static T ReadVisualItem<T>(
            Farmer farmer,
            string slot,
            int number
        ) where T : class
        {
            return ReadItemAtKey<T>(
                farmer,
                VisualItemKey(slot, number)
            );
        }

        internal static void SaveVisualItem<T>(
            Farmer farmer,
            string slot,
            int number,
            T item
        ) where T : class
        {
            SaveItemAtKey(
                farmer,
                VisualItemKey(slot, number),
                item
            );
        }

        internal static T ReadSavedItem<T>(
            Farmer farmer,
            string slot,
            int number
        ) where T : class
        {
            return ReadItemAtKey<T>(
                farmer,
                keyPrefix + slot + number
            );
        }

        private static T ReadItemAtKey<T>(
            Farmer farmer,
            string key
        ) where T : class
        {
            if (!farmer.modData.TryGetValue(
                key,
                out string xml
            ) || string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                return xml.FromXml<T>();
            }
            catch (Exception ex)
            {
                SMonitor.Log(
                    $"Cannot load equipment at {key}: {ex}",
                    LogLevel.Warn
                );

                return null;
            }
        }

        internal static void SaveItem<T>(
            Farmer farmer,
            string slot,
            int number,
            T item
        ) where T : class
        {
            SaveItemAtKey(
                farmer,
                keyPrefix + slot + number,
                item
            );
        }

        private static void SaveItemAtKey<T>(
            Farmer farmer,
            string key,
            T item
        ) where T : class
        {
            if (item == null)
                farmer.modData.Remove(key);
            else
                farmer.modData[key] = item.ToXmlString();
        }

        private static void SaveCurrentRealSet(
            Farmer farmer,
            int number
        )
        {
            SaveItem(farmer, "hat", number, farmer.hat.Value);
            SaveItem(
                farmer,
                "shirt",
                number,
                farmer.shirtItem.Value
            );
            SaveItem(
                farmer,
                "pants",
                number,
                farmer.pantsItem.Value
            );
            SaveItem(
                farmer,
                "boots",
                number,
                farmer.boots.Value
            );
            SaveItem(
                farmer,
                "leftRing",
                number,
                farmer.leftRing.Value
            );
            SaveItem(
                farmer,
                "rightRing",
                number,
                farmer.rightRing.Value
            );
        }

        private static bool HasAnySavedRealEquipment(
            Farmer farmer,
            int number
        )
        {
            foreach (string slot in new[]
            {
                "hat",
                "shirt",
                "pants",
                "boots",
                "leftRing",
                "rightRing"
            })
            {
                if (farmer.modData.ContainsKey(
                    keyPrefix + slot + number
                ))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Point GetCenterPoint(
            InventoryPage page,
            int i
        )
        {
            string label = (i + 1).ToString();
            Vector2 size =
                Game1.tinyFont.MeasureString(label);

            return new Point(
                page.xPositionOnScreen
                    + Config.Xoffset
                    + (int)Math.Round(
                        (i + .5f)
                        * 270
                        / Config.Sets
                    ),

                page.yPositionOnScreen
                    + IClickableMenu.borderWidth
                    + IClickableMenu.spaceToClearTopBorder
                    + Config.Yoffset
                    - (int)size.Y / 2
            );
        }
    }

    public static class XmlTools
    {
        public static string ToXmlString<T>(this T input)
        {
            using (StringWriter writer = new StringWriter())
            {
                new XmlSerializer(typeof(T))
                    .Serialize(writer, input);
                return writer.ToString();
            }
        }

        public static T FromXml<T>(this string value)
        {
            using (StringReader reader =
                new StringReader(value))
            {
                return (T)new XmlSerializer(typeof(T))
                    .Deserialize(reader);
            }
        }
    }
}
