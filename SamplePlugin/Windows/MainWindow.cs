using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace SamplePlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly List<Achievement> achievements;
        private readonly Dictionary<uint, bool> achievementCompletion;
        private string searchQuery = string.Empty;
        public static bool achievementsChecked { get; set; } = false;

        public MainWindow(Plugin plugin, List<Achievement> achievements, Dictionary<uint, bool> achievementCompletion)
            : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(375, 330),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            this.plugin = plugin;
            this.achievements = achievements;
            this.achievementCompletion = achievementCompletion;
        }

        public void Dispose() { }

        public override void Draw()
        {
            if (!achievementsChecked)
            {
                ImGui.Text("Please open the Achievements Journal in-game to load your achievements.");
                return;
            }

            int totalAchievements = achievements.Count;
            int completedAchievements = achievements.Count(a => achievementCompletion.TryGetValue(a.RowId, out var isCompleted) && isCompleted);

            float progress = (float)completedAchievements / totalAchievements;
            ImGui.ProgressBar(progress, new Vector2(-1, 20), $"{completedAchievements} / {totalAchievements} Achievements Completed");

            ImGui.Spacing();
            ImGui.InputText("Search", ref searchQuery, 256);

            ImGui.Spacing();

            if (ImGui.BeginTabBar("AchievementTabs"))
            {
                if (ImGui.BeginTabItem("All Achievements"))
                {
                    DisplayAchievements(achievement => FilterAchievements(achievement));
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Incomplete Achievements"))
                {
                    DisplayAchievements(achievement => !achievementCompletion.TryGetValue(achievement.RowId, out var isCompleted) || !isCompleted && FilterAchievements(achievement));
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Completed Achievements"))
                {
                    DisplayAchievements(achievement => achievementCompletion.TryGetValue(achievement.RowId, out var isCompleted) && isCompleted && FilterAchievements(achievement));
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Recommendations"))
                {
                    DisplayRecommendations();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private bool FilterAchievements(Achievement achievement)
        {
            return string.IsNullOrEmpty(searchQuery) ||
                   achievement.Name.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                   achievement.Description.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void DisplayAchievements(Func<Achievement, bool> filter)
        {
            ImGui.BeginChild("achievementsList", new Vector2(-1, -1), true);
            foreach (var achievement in achievements.Where(filter))
            {
                DisplayAchievement(achievement);
            }
            ImGui.EndChild();
        }

        private void DisplayAchievement(Achievement achievement)
        {
            var icon = Plugin.TextureProvider.GetFromGameIcon((uint)achievement.Icon);
            if (icon != null)
            {
                ImGui.Image(icon.GetWrapOrEmpty().ImGuiHandle, new Vector2(40, 40));
            }
            ImGui.SameLine();

            if (achievementCompletion.TryGetValue(achievement.RowId, out var isCompleted) && isCompleted)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 0.0f, 1.0f)); // Green color
            }

            var achievementName = achievement.Name?.ToString() ?? "Unknown";
            var achievementKindName = achievement.AchievementCategory?.Value?.AchievementKind?.Value?.Name?.ToString() ?? "Unknown Kind";
            var achievementCategoryName = achievement.AchievementCategory?.Value?.Name?.ToString() ?? "Unknown Category";
            var achievementDescription = achievement.Description?.ToString() ?? "No description";

            if (ImGui.Selectable($"{achievementName} \t {achievementKindName} \t {achievementCategoryName} \t {achievement.Points} \n {achievementDescription} ##{achievement.RowId}", true, 0, new Vector2(0, 40)))
            {
                // Handle selection
            }

            if (isCompleted)
            {
                ImGui.PopStyleColor();
            }
            ImGui.NewLine();
        }




        private void DisplayRecommendations()
        {
            // Placeholder for recommendation logic
            ImGui.Text("Recommendations will be displayed here.");
        }
    }
}
