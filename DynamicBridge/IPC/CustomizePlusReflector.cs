﻿using ECommons;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC;
public static class CustomizePlusReflector
{
    public static object GetProfileManager()
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("CustomizePlus", out var plugin, false, true) && DalamudReflector.TryGetLocalPlugin(plugin, out var lp, out var lpType))
            {
                var context = lp.GetFoP("loader").GetFoP<AssemblyLoadContext>("context");
                var profileManager = ReflectionHelper.CallGenericStatic(context.Assemblies, "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions", "GetRequiredService", ["CustomizePlus.Profiles.ProfileManager"], [plugin.GetFoP("_services")]);
                return profileManager;
            }
        }
        catch(Exception e)
        {
            e.LogVerbose();
        }
        return null;
    }

    public static List<CustomizePlusProfile> GetProfiles()
    {
        var ret = new List<CustomizePlusProfile>();
        try
        {
            var profiles = (System.Collections.IList)GetProfileManager()?.GetFoP("Profiles");
            if (profiles == null) return ret;
            foreach (var x in profiles)
            {
                if (x.GetFoP<int>("ProfileType") != 0) continue;
                ret.Add((
                    x.GetFoP("Name").GetFoP<string>("Text"),
                    x.GetFoP("CharacterName").GetFoP<string>("Text"),
                    x.GetFoP<bool>("Enabled"),
                    x.GetFoP<Guid>("UniqueId")
                    ));
            }
        }
        catch(Exception e)
        {
            e.LogVerbose();
        }
        return ret;
    }

    public static void SetEnabled(Guid id, bool enabled)
    {
        try
        {
            var mgr = GetProfileManager();
            var profiles = (System.Collections.IList)mgr?.GetFoP("Profiles");
            if (profiles == null) return;
            foreach (var x in profiles)
            {
                if(x.GetFoP<Guid>("UniqueId") == id)
                {
                    mgr.Call("SetEnabled", [x, enabled, false]);
                }
            }
        }
        catch (Exception e)
        {
            e.LogVerbose();
        }
    }
}
