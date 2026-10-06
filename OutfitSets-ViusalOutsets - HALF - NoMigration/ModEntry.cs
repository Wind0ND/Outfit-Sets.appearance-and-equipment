using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace OutfitSets
{
    public partial class ModEntry : Mod
    {
        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;
        public static ModEntry context;

        public static string keyPrefix = "aedenthorn.OutfitSets/";
        public static int IDOffset = -3939300;

        internal static bool EditingVisualSet;
        internal static int SelectedVisualSet = 1;

        internal static int VisualSetCount => Config.Sets / 2;
        internal static int FirstRealSet => VisualSetCount + 1;

        private static int NormalizeSetCount(int value)
        {
            value = Math.Clamp(value, 2, 12);
            return value % 2 == 0 ? value : value - 1;
        }

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            Config.Sets = NormalizeSetCount(Config.Sets);

            if (!Config.ModEnabled)
                return;

            context = this;
            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched +=
                GameLoop_GameLaunched;

            helper.Events.GameLoop.SaveLoaded +=
                GameLoop_SaveLoaded;

            helper.ConsoleCommands.Add(
                "outfit",
                "Select a set: outfit <1..N>",
                SetOutfit
            );

            new Harmony(ModManifest.UniqueID).PatchAll();
        }

        private void SetOutfit(
            string command,
            string[] args
        )
        {
            if (args.Length == 1 &&
                int.TryParse(args[0], out int number))
            {
                SwitchSet(number);
            }
        }

        private void GameLoop_SaveLoaded(
            object sender,
            SaveLoadedEventArgs e
        )
        {
            EditingVisualSet = false;
            SelectedVisualSet = 1;
            Config.Sets = NormalizeSetCount(Config.Sets);

            Farmer farmer = Game1.player;
            if (farmer == null)
                return;

            string currentSetKey =
                keyPrefix + "currentSet";

            if (farmer.modData.TryGetValue(
                    currentSetKey,
                    out string data
                ) &&
                int.TryParse(data, out int currentSet) &&
                currentSet >= FirstRealSet &&
                currentSet <= Config.Sets)
            {
               
                return;
            }

          
            for (
                int number = FirstRealSet;
                number <= Config.Sets;
                number++
            )
            {
                if (HasAnySavedRealEquipment(
                    farmer,
                    number
                ))
                {
                    continue;
                }

                SaveCurrentRealSet(farmer, number);
                farmer.modData[currentSetKey] =
                    number.ToString();

                return;
            }

           
            Monitor.Log(
                "No empty real set is available. "
                + "Existing set data and current equipment "
                + "were left unchanged. Move or unequip "
                + "your current gear in-game before "
                + "switching real sets.",
                LogLevel.Warn
            );
        }

        private void GameLoop_GameLaunched(
            object sender,
            GameLaunchedEventArgs e
        )
        {
            var menu = Helper.ModRegistry.GetApi<
                IGenericModConfigMenuApi
            >("spacechase0.GenericModConfigMenu");

            if (menu == null)
                return;

            menu.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () =>
                {
                    Config.Sets =
                        NormalizeSetCount(Config.Sets);

                    Helper.WriteConfig(Config);
                }
            );

            menu.AddBoolOption(
                ModManifest,
                () => Config.ModEnabled,
                value => Config.ModEnabled = value,
                () => "Mod Enabled"
            );

            menu.AddNumberOption(
                ModManifest,
                () => Config.Sets,
                value => Config.Sets =
                    NormalizeSetCount(value),
                () => "Total Sets (first half visual)",
                min: 2,
                max: 12,
                interval: 2
            );

            menu.AddNumberOption(
                ModManifest,
                () => Config.Xoffset,
                value => Config.Xoffset = value,
                () => "X offset"
            );

            menu.AddNumberOption(
                ModManifest,
                () => Config.Yoffset,
                value => Config.Yoffset = value,
                () => "Y offset"
            );
        }
    }
}
