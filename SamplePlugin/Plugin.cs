using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using Lumina;
using Lumina.Data;
using ExcelAchievement = Lumina.Excel.GeneratedSheets.Achievement;
using GameUIAchievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using System;
using Dalamud.Interface.Animation.EasingFunctions;
using Lumina.Data.Parsing;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        private const string CommandName = "/achievementbuddy";
        private const string AltCommandName = "/acb";
        public Configuration Configuration { get; init; }

        public readonly WindowSystem WindowSystem = new("SamplePlugin");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private GameData? gameData;
        private List<ExcelAchievement> loadedAchievements = new List<ExcelAchievement>();
        private Dictionary<uint, bool> achievementCompletion = new Dictionary<uint, bool>();

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            InitializeLumina();
            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, loadedAchievements, achievementCompletion);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            CommandManager.AddHandler(AltCommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;

            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            CheckAchievements();
        }

        private void InitializeLumina()
        {
            gameData = DataManager.GameData;
            LoadAchievements();
        }

        private void LoadAchievements()
        {
            if (gameData == null) return;

            var achievementsSheet = gameData.GetExcelSheet<ExcelAchievement>();
            if (achievementsSheet != null)
            {
                foreach (var achievement in achievementsSheet)
                {
                if (achievement.Points == 0) continue;
                    if (achievement.AchievementCategory.Value.AchievementKind.Value.Name == "Legacy") continue;
                    if (achievement.AchievementCategory.Value.Name == "Seasonal Events") continue;
                    loadedAchievements.Add(achievement);
                }
            }
        }

        private unsafe void CheckAchievements()
        {
            var achievementInstance = GameUIAchievement.Instance();
            if (achievementInstance == null) return;

            foreach (var achievement in loadedAchievements)
            {
                if (achievementInstance->IsComplete((int)achievement.RowId))
                {
                    PluginLog.Info(achievement.Name);
                };
                bool isComplete = achievementInstance->IsComplete((int)achievement.RowId);
                achievementCompletion[achievement.RowId] = isComplete;
            }
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            ToggleMainUI();
        }

        private void DrawUI() => WindowSystem.Draw();

        public void ToggleConfigUI() => ConfigWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
    }
}
