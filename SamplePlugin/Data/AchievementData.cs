using System.Collections.Generic;
using Lumina;
using ExcelAchievement = Lumina.Excel.GeneratedSheets.Achievement;
using GameUIAchievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using SamplePlugin.Windows;

namespace SamplePlugin.Data
{
    public class AchievementData
    {
        private readonly List<ExcelAchievement> loadedAchievements = new();
        private Dictionary<uint, bool> achievementCompletion = new Dictionary<uint, bool>();
        private GameData? gameData;

        public void InitializeLumina()
        {
            gameData = Plugin.DataManager.GameData;
        }

        public void LoadAchievements()
        {
            if (gameData == null) return;

            var achievementsSheet = gameData.GetExcelSheet<ExcelAchievement>();
            if (achievementsSheet != null)
            {
                foreach (var achievement in achievementsSheet)
                {
                    if (achievement.Points == 0) continue;
                    var category = achievement.AchievementCategory?.Value;
                    var kind = category?.AchievementKind?.Value;

                    if (kind?.Name?.ToString() == "Legacy") continue;
                    if (category?.Name?.ToString() == "Seasonal Events") continue;

                    loadedAchievements.Add(achievement);
                }
            }
        }

        public unsafe void CheckAchievementCompletion()
        {
            var achievementInstance = GameUIAchievement.Instance();
            if (achievementInstance == null) return;

            foreach (var achievement in loadedAchievements)
            {
                bool isComplete = achievementInstance->IsComplete((int)achievement.RowId);
                achievementCompletion[achievement.RowId] = isComplete;
            }

            MainWindow.achievementsChecked = true;
            Plugin.PluginLog.Info("Completed Achievements Retrieved.");
        }

        public Dictionary<uint, bool> GetAchievementCompletion() => achievementCompletion;

        public List<ExcelAchievement> GetLoadedAchievements() => loadedAchievements;
    }
}
