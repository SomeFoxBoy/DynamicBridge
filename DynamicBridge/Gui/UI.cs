﻿using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using ECommons;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;
using Action = System.Action;

namespace DynamicBridge.Gui;

public unsafe static class UI
{

    public static Profile SelectedProfile = null;
    public static Profile Profile => SelectedProfile ?? Utils.GetProfileByCID(Player.CID);
    public const string RandomNotice = "Will be randomly selected between:\n";
    public const string AnyNotice = "Meeting any of the following conditions will result in rule being triggered:\n";
    static string PSelFilter = "";
    public static string RequestTab = null;

    public static void DrawMain()
    {
        var resolution = "";
        if (Player.CID == 0) resolution = "Not logged in";
        else if (C.Blacklist.Contains(Player.CID)) resolution = "Character blacklisted";
        else if (Utils.GetProfileByCID(Player.CID) == null) resolution = "No associated profile";
        else resolution = $"Profile {Utils.GetProfileByCID(Player.CID).CensoredName}";
        if (!C.Enable && Environment.TickCount64 % 2000 > 1000) resolution = "PLUGIN DISABLED BY SETTINGS";
        EzConfigGui.Window.WindowName = $"{DalamudReflector.GetPluginName()} v{P.GetType().Assembly.GetName().Version} [{resolution}]###{DalamudReflector.GetPluginName()}";
        if (ImGui.IsWindowAppearing())
        {
            Utils.ResetCaches();
            foreach (var x in Svc.Data.GetExcelSheet<Weather>()) ThreadLoadImageHandler.TryGetIconTextureWrap((uint)x.Icon, false, out _);
            foreach (var x in Svc.Data.GetExcelSheet<Emote>()) ThreadLoadImageHandler.TryGetIconTextureWrap(x.Icon, false, out _);
        }
        KoFiButton.DrawRight();
        ImGuiEx.EzTabBar("TabsNR2", true, RequestTab, [
            //("Settings", Settings, null, true),
            (C.ShowTutorial?"Tutorial":null, GuiTutorial.Draw, null, true),
            ("Dynamic Rules", GuiRules.Draw, Colors.TabGreen, true),
            ("Presets", GuiPresets.DrawUser, Colors.TabGreen, true),
            ("Global Presets", GuiPresets.DrawGlobal, Colors.TabYellow, true),
            ("Layered Designs", ComplexGlamourer.Draw, Colors.TabPurple, true),
            ("House Registration", HouseReg.Draw, Colors.TabPurple, true),
            ("Profiles", GuiProfiles.Draw, Colors.TabBlue, true),
            ("Characters", GuiCharacters.Draw, Colors.TabBlue, true),
            ("Settings", GuiSettings.Draw, null, true),
            InternalLog.ImGuiTab(),
            (C.Debug?"Debug":null, Debug.Draw, ImGuiColors.DalamudGrey3, true),
            ]);
        RequestTab = null;
    }

    public static void ProfileSelectorCommon(Action before = null, Action after = null)
    {
        if (SelectedProfile != null && !C.ProfilesL.Contains(SelectedProfile)) SelectedProfile = null;
        var currentCharaProfile = Utils.GetProfileByCID(Player.CID);

        before?.Invoke();

        if (SelectedProfile == null)
        {
            if (currentCharaProfile == null)
            {
                if (C.Blacklist.Contains(Player.CID))
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("blisted", $"\"{Censor.Character(Player.NameWithWorld)}\" is blacklisted. Select another profile to edit it.", ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowCircleUp))
                        {
                            C.Blacklist.Remove(Player.CID);
                        }
                        ImGuiEx.Tooltip("Unblacklist this character.");
                    });
                }
                else if (Player.CID != 0)
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("noprofile", $"\"{Censor.Character(Player.NameWithWorld)}\" has no associated profile. Select other profile to edit or associate profile in Characters tab.", ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.PlusCircle))
                        {
                            var profile = new Profile();
                            C.ProfilesL.Add(profile);
                            profile.Characters = [Player.CID];
                            profile.Name = $"Autogenerated Profile for {Player.Name}";
                        }
                        ImGuiEx.Tooltip($"Create new empty profile and assign it to current character");
                    });
                }
                else
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("nlg", $"You are not logged in. Please select profile to edit.", ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        ImGui.Dummy(Vector2.Zero);
                    });
                }
            }
            else
            {
                UsedByCurrent();
            }
        }
        else
        {
            if (currentCharaProfile == SelectedProfile)
            {
                UsedByCurrent();
            }
            else
            {
                ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("EditNotify", $"You are editing profile \"{SelectedProfile.CensoredName}\". " + (Player.Available?$"It is not used by \"{Censor.Character(Player.NameWithWorld)}\".":""), ProfileSelectable, EColor.YellowDark), () =>
                {
                    after?.Invoke();
                    if (!C.Blacklist.Contains(Player.CID))
                    {
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Link))
                        {
                            new TickScheduler(() => SelectedProfile.SetCharacter(Player.CID));
                        }
                        ImGuiEx.Tooltip($"Assign profile {SelectedProfile?.CensoredName} to {Censor.Character(Player.NameWithWorld)}");
                    }
                    else
                    {
                        ImGuiEx.HelpMarker("Your current character is blacklisted", null, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    }
                });
            }
        }

        void UsedByCurrent()
        {
            ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("EditNotify", $"You are editing profile \"{currentCharaProfile.CensoredName}\" which is used by \"{Censor.Character(Player.NameWithWorld)}\".", ProfileSelectable, EColor.GreenDark), () =>
            {
                after?.Invoke();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Unlink, enabled:ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => currentCharaProfile.Characters.Remove(Player.CID));
                }
                ImGuiEx.Tooltip($"Hold CTRL key and click to unassign profile {currentCharaProfile?.CensoredName} from {Censor.Character(Player.NameWithWorld)}.");
            });
        }

        void ProfileSelectable()
        {
            if(ImGui.Selectable("- Current character -", SelectedProfile == null))
            {
                SelectedProfile = null;
            }
            ImGui.Separator();
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGui.InputTextWithHint($"##SearchCombo", "Filter...", ref PSelFilter, 50, Utils.CensorFlags);
            foreach(var x in C.ProfilesL)
            {
                if (PSelFilter.Length > 0 && !x.Name.Contains(PSelFilter, StringComparison.OrdinalIgnoreCase)) continue;
                if (SelectedProfile == x && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                if(ImGui.Selectable($"{x.CensoredName}##{x.GUID}", SelectedProfile == x))
                {
                    new TickScheduler(() => SelectedProfile = x);
                }
            }
        }
    }

    public static void ForceUpdateButton()
    {
        if (ImGuiEx.IconButton(FontAwesomeIcon.Tshirt))
        {
            P.ForceUpdate = true;
        }
        ImGuiEx.Tooltip("Force update your character, reapplying all rules and resets");
    }
}
