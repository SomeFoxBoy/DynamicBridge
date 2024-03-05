﻿using ECommons.EzHookManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge;
public class Memory : IDisposable
{
    public delegate nint RaptureGearsetModule_EquipGearsetInternal(nint a1, uint a2, byte a3);
    [EzHook("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 63 F2", false)]
    public EzHook<RaptureGearsetModule_EquipGearsetInternal> EquipGearsetHook;

    public Memory()
    {
        EzSignatureHelper.Initialize(this);
        if(C.UpdateJobGSChange)
        {
            EquipGearsetHook.Enable();
        }
    }

    nint EquipGearsetDetour(nint a1, uint a2, byte a3)
    {
        try
        {
            P.ForceUpdate = true;
            InternalLog.Information($"Gearset equip: {a2}/{a3}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return EquipGearsetHook.Original(a1, a2, a3);
    }

    public void Dispose()
    {
        //...
    }
}
