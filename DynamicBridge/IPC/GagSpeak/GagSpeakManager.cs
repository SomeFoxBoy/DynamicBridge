using ECommons.EzIpcManager;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.GagSpeak;

public class GagSpeakManager
{
    private List<string> cachedGagTypes;
    [EzIPC("GetGagTypes")] private readonly Func<List<string>> _GetGagTypes;
    [EzIPC] public readonly Func<List<string>> GetWornGags;
    [EzIPC] public readonly Func<Guid?> GetWornRestraint;
    private List<(string, Guid)> cachedRestraintSets;
    [EzIPC("GetRestraintSets")] private readonly Func<List<(string, Guid)>> _GetRestraintSets;
    public GagSpeakManager()
    {
        EzIPC.Init(this, "GagSpeak");
    }
    public List<string> GetGagTypes()
    {
        cachedGagTypes ??= _GetGagTypes();
        return cachedGagTypes;
    }
    public List<(string, Guid)> GetRestraintSets()
    {
        if (cachedRestraintSets is null || EzThrottler.Throttle("UpdateGagspeakRestraint", 5000))
        {
            cachedRestraintSets = _GetRestraintSets();
        }
        return cachedRestraintSets;
    }
}