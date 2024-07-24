using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace SamplePlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {

        private readonly Plugin plugin;
        private readonly List<Achievement> achievements;
        private readonly Dictionary<uint, bool> achievementCompletion;
        private Dictionary<string, (int completed, int total, int earnedPoints, int totalPoints)> kindProgress;
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

        private void ComputeKindProgress()
        {
            kindProgress = achievements
                .Where(a => a.AchievementCategory.Value?.AchievementKind.Value != null && a.AchievementCategory.Value.AchievementKind.Value.Name.ToString() != "")
                .GroupBy(a => a.AchievementCategory.Value.AchievementKind.Value.Name.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => (
                        completed: g.Count(a => achievementCompletion.TryGetValue(a.RowId, out var isCompleted) && isCompleted),
                        total: g.Count(),
                        earnedPoints: g.Where(a => achievementCompletion.TryGetValue(a.RowId, out var isCompleted) && isCompleted).Sum(a => a.Points),
                        totalPoints: g.Sum(a => a.Points)
                    )
                );
        }


        public override void Draw()
        {
            if (!achievementsChecked)
            {
                DisplayAchievementsJournalPrompt();
                return;
            }

            ImGui.InputText("Search", ref searchQuery, 256);

            ImGui.Spacing();

            if (ImGui.BeginTabBar("AchievementTabs"))
            {
                if (ImGui.BeginTabItem("Overview"))
                {
                    DisplayOverview();
                    ImGui.EndTabItem();
                }

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

        private void DisplayOverview()
        {
            ComputeKindProgress();

            ImGui.BeginChild("OverviewChild", new Vector2(0, 0), true);

            int totalAchievements = achievements.Count;
            int completedAchievements = achievements.Count(a => achievementCompletion.TryGetValue(a.RowId, out var isCompleted) && isCompleted);
            float totalProgress = (float)completedAchievements / totalAchievements;
            ImGui.Spacing();
            ImGui.ProgressBar(totalProgress, new Vector2(-1, 20), $"{completedAchievements} / {totalAchievements} Achievements Completed");
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            // Calculate the number of columns based on the window width
            float windowWidth = ImGui.GetWindowWidth();
            int columnCount = (int)(windowWidth / 300); // 300 is an arbitrary width for each column
            columnCount = Math.Max(1, Math.Min(3, columnCount)); // Ensure between 1 and 3 columns
            float columnWidth = Math.Min(600, (windowWidth / columnCount) - 30);


            ImGui.Columns(columnCount, "OverviewColumns", false);

            foreach (var kind in kindProgress)
            {
                var kindName = kind.Key;
                var (completed, total, earnedPoints, totalPoints) = kind.Value;
                float progress = total == 0 ? 0 : (float)completed / total;

                ImGui.Text($"{kindName} ({completed} / {total}) \t Points: {earnedPoints} / {totalPoints}"); 
                ImGui.ProgressBar(progress, new Vector2(columnWidth, 20));
                ImGui.Spacing();
                ImGui.NextColumn();
            }

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        private void DisplayAchievementsJournalPrompt()
        {
            string errorText = "Please open the Achievements Journal to load your completed achievements.";
            ImGui.SetCursorPosY((ImGui.GetWindowHeight() - ImGui.CalcTextSize(errorText).Y) / 2);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(errorText).X) / 2);
            ImGui.Text(errorText);

            string buttonText = "Open Achievements Journal";
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(buttonText).X) / 2);
            if (ImGui.Button(buttonText))
            {
                unsafe
                {
                    var agentAchievement = AgentModule.Instance()->GetAgentAchievement();
                    if (agentAchievement != null)
                    {
                        agentAchievement->Show();
                    }
                }
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
            var achievementPoints = achievement.Points.ToString() ?? "No Points";

            if (ImGui.Selectable($"{achievementName} \t{achievementKindName}/{achievementCategoryName} \t {achievementPoints} \n {achievementDescription} ##{achievement.RowId}", true, 0, new Vector2(0, 40)))
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
