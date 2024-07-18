using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using System;
using SamplePlugin.Data;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; set; } = null!;
        [PluginService] internal static IFramework Framework { get; set; } = null!;
        [PluginService] internal static IGameGui GameGui { get; set; } = null!;

        private const string CommandName = "/achievementbuddy";
        private const string AltCommandName = "/acb";
        public Configuration Configuration { get; init; }

        public readonly WindowSystem WindowSystem = new("SamplePlugin");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private readonly AchievementData achievementData;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            achievementData = new AchievementData();
            achievementData.InitializeLumina();
            achievementData.LoadAchievements();

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, achievementData.GetLoadedAchievements(), achievementData.GetAchievementCompletion());

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

            // Subscribe to the framework update event to check for the achievement journal opening
            Framework.Update += OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            // Check if the achievement journal is open
            if (GameGui.GetAddonByName("Achievement", 1) != IntPtr.Zero)
            {
                // Unsubscribe from the event to avoid repeated checks
                Framework.Update -= OnFrameworkUpdate;

                // start checking for completed Achievements
                achievementData.CheckAchievementCompletion();
            }
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            Framework.Update -= OnFrameworkUpdate;

            ConfigWindow.Dispose();
            MainWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(AltCommandName);
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
