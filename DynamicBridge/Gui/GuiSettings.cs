﻿using DynamicBridge.Configuration;
using DynamicBridge.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiSettings
{
    public static Dictionary<GlamourerNoRuleBehavior, string> GlamourerNoRuleBehaviorNames = new()
    {
        [GlamourerNoRuleBehavior.RevertToNormal] = "Revert to game state",
        [GlamourerNoRuleBehavior.RevertToAutomation] = "Revert to Glamourer's automation",
        [GlamourerNoRuleBehavior.StoreRestore] = "[Beta] Restore appearance as it was before applying rule",
    };

    public static Dictionary<ImGuiComboFlags, string> ComboFlagNames = new()
    {
        [ImGuiComboFlags.HeightSmall] = "Small",
        [ImGuiComboFlags.HeightRegular] = "Standard",
        [ImGuiComboFlags.HeightLarge] = "Large",
        [ImGuiComboFlags.HeightLargest] = "Maximum possible",
    };

    public static void Draw()
    {
        if (ImGuiGroup.BeginGroupBox("General"))
        {
            ImGui.Checkbox($"Allow applying negative conditions", ref C.AllowNegativeConditions);
            ImGuiEx.HelpMarker("If you will enable this option, you will be able to mark any condition with cross marker. If any condition marked with cross within the rule is matching, that entire rule is ignored.");
            ImGui.Checkbox("Display full path in profile editor, where available", ref C.GlamourerFullPath);
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGuiEx.EnumCombo("Dropdown menu size", ref C.ComboSize, ComboFlagNames.ContainsKey, ComboFlagNames);
            if(ImGui.Checkbox($"Force update appearance on job and gearset changes", ref C.UpdateJobGSChange))
            {
                if (C.UpdateJobGSChange)
                {
                    P.Memory.EquipGearsetHook.Enable();
                }
                else
                {
                    P.Memory.EquipGearsetHook.Disable();
                }
            }
            /*ImGui.Checkbox($"Force update appearance on manual gear changes", ref C.UpdateGearChange);
            ImGuiEx.HelpMarker("This option impacts performance", EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());*/
            ImGuiGroup.EndGroupBox();
        }

        if(ImGuiGroup.BeginGroupBox("Configure rule conditions"))
        {
            ImGuiEx.TextWrapped($"Enable extra conditions or disable unused for convenience and performance boost.");
            ImGui.Checkbox($"State", ref C.Cond_State);
            ImGui.Checkbox($"Biome", ref C.Cond_Biome);
            ImGui.Checkbox($"Weather", ref C.Cond_Weather);
            ImGui.Checkbox($"Time", ref C.Cond_Time);
            ImGui.Checkbox($"Zone group", ref C.Cond_ZoneGroup);
            ImGui.Checkbox($"Zone", ref C.Cond_Zone);
            ImGui.Checkbox($"House", ref C.Cond_House);
            ImGui.Checkbox($"Emote", ref C.Cond_Emote);
            ImGui.Checkbox($"Job", ref C.Cond_Job);
            ImGui.Checkbox($"World", ref C.Cond_World);
            ImGui.Checkbox($"Gearset", ref C.Cond_Gearset);
            ImGuiGroup.EndGroupBox();
        }

        if (ImGuiGroup.BeginGroupBox("Integrations"))
        {
            ImGuiEx.Text($"Here you can individually enable/disable plugin integrations and configure appropriate related settings.");
            //glam

            ImGui.Checkbox("Glamourer", ref C.EnableGlamourer);
            DrawPluginCheck("Glamourer", "1.2.0.2");
            ImGuiEx.TextV($"DynamicBridge behavior when no Glamourer rule is found:");
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(200f);
            ImGuiEx.EnumCombo("##dbglamdef", ref C.GlamNoRuleBehaviour, GlamourerNoRuleBehaviorNames);
            if (C.ManageGlamourerAutomation)
            {
                if (C.GlamNoRuleBehaviour != GlamourerNoRuleBehavior.RevertToAutomation)
                {
                    ImGuiEx.HelpMarker("Revert to Automation is recommended if you are using Glamourer automation.", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            var c = new CancellationTokenSource();
            ImGui.Checkbox("Allow DynamicBridge to manage Glamourer's automation setting", ref C.ManageGlamourerAutomation);
            ImGuiEx.HelpMarker("If this setting is enabled, Glamourer's global automation setting will be automatically disabled upon applying any rule and will be automatically enabled when no rules are found.");
            if (GlamourerReflector.GetAutomationGlobalState() && GlamourerReflector.GetAutomationStatusForChara())
            {
                if (!C.ManageGlamourerAutomation)
                {
                    ImGuiEx.HelpMarker("You MUST enable this setting or disable Glamourer's automation, otherwise either Glamourer's or DynamicBridge's automation will not work correctly.", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGui.Checkbox("[Beta] Revert character before restoring automation", ref C.RevertBeforeAutomationRestore);
            ImGui.Checkbox("[Beta] Revert character before applying rule", ref C.RevertGlamourerBeforeApply);


            ImGui.Separator();

            //customize

            ImGui.Checkbox("Customize+", ref C.EnableCustomize);
            DrawPluginCheck("CustomizePlus", "2.0.1.0");

            //honorific

            ImGui.Checkbox("Honorific", ref C.EnableHonorific);
            DrawPluginCheck("Honorific", "1.4.2.0");

            ImGuiGroup.EndGroupBox();
        }

        if (ImGuiGroup.BeginGroupBox("About"))
        {
            GuiAbout.Draw();
            ImGuiGroup.EndGroupBox();
        }
    }

    static void DrawPluginCheck(string name, string minVersion = "0.0.0.0")
    {
        ImGui.SameLine();
        var plugin = Svc.PluginInterface.InstalledPlugins.FirstOrDefault(x => x.InternalName == name && x.IsLoaded);
        if(plugin == null)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
            ImGui.SameLine();
            ImGuiEx.Text($"not installed");
        }
        else
        {
            if(plugin.Version < Version.Parse(minVersion))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.RedBright, "\uf00d");
                ImGui.PopFont();
                ImGui.SameLine();
                ImGuiEx.Text($"unsupported version");
            }
            else
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
                ImGui.PopFont();
            }
        }
    }
}
