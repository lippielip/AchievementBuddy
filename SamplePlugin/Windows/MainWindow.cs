using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace SamplePlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private List<Achievement> Achievements;
        private Dictionary<uint, bool> AchievementCompletion;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

        public MainWindow(Plugin plugin, List<Achievement> achievements, Dictionary<uint, bool> achievementCompletion)
            : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(375, 330),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            Plugin = plugin;
            Achievements = achievements;
            AchievementCompletion = achievementCompletion;
        }

        public void Dispose() { }

        public override void Draw()
        {

            int totalAchievements = Achievements.Count;
            int completedAchievements = 0;
            foreach (var achievement in Achievements)
            {
                if (AchievementCompletion.TryGetValue(achievement.RowId, out var isCompleted) && isCompleted)
                {
                    completedAchievements++;
                }
            }

            float progress = (float)completedAchievements / totalAchievements;
            ImGui.ProgressBar(progress, new Vector2(-1, 20), $"{completedAchievements} / {totalAchievements} Achievements Completed");

            ImGui.Spacing();
            ImGui.Text("Incomplete Achievements:");
            if (Achievements != null && Achievements.Count > 0)
            {
                ImGui.BeginChild("achievementsList", new Vector2(-1, -1), true);
                foreach (var achievement in Achievements)
                {
                    if (AchievementCompletion.TryGetValue(achievement.RowId, out var isCompleted) && !isCompleted)
                    {
                        var icon = Plugin.TextureProvider.GetFromGameIcon((uint)achievement.Icon);
                        if (icon != null)
                        {
                            ImGui.Image(icon.GetWrapOrEmpty().ImGuiHandle, new Vector2(40, 40));
                        }
                        ImGui.SameLine();
                        if (ImGui.Selectable($"{achievement.Name} \t {achievement.AchievementCategory.Value.AchievementKind.Value.Name} \t {achievement.AchievementCategory.Value.Name} \t {achievement.Points} \n {achievement.Description} ##{achievement.RowId}", true, 0, new Vector2(0, 40)))
                        {
                            // Handle selection
                        }
                        ImGui.NewLine();
                    }
                }
                ImGui.EndChild();
            }
            else
            {
                ImGui.Text("No achievements data available.");
            }
        }
    }
}
