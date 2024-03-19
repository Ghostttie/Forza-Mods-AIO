﻿using static Forza_Mods_AIO.Resources.Cheats;
using static Forza_Mods_AIO.Resources.Memory;

namespace Forza_Mods_AIO.Cheats.ForzaHorizon5;

public class PhotomodeCheats : CheatsUtilities, ICheatsBase
{
    private UIntPtr _noClipAddress;
    public UIntPtr NoClipDetourAddress;
    private UIntPtr _noHeightLimitAddress;
    public UIntPtr NoHeightLimitDetourAddress;

    public async Task CheatNoClip()
    {
        _noClipAddress = 0;
        NoClipDetourAddress = 0;

        const string sig = "48 89 ? ? 48 C7 45 B0 ? ? ? ? 48 8B ? ? ? ? ? 48 85";
        _noClipAddress = await SmartAobScan(sig);

        if (_noClipAddress > 0)
        {
            _noClipAddress -= 93;
            if (GetClass<Bypass>().CrcFuncDetourAddress == 0)
            {
                await GetClass<Bypass>().DisableCrcChecks();
            }
            
            if (GetClass<Bypass>().CrcFuncDetourAddress == 0) return;

            var asm = new byte[]
            {
                0x80, 0x3D, 0x12, 0x00, 0x00, 0x00, 0x01, 0x75, 0x06, 0x31, 0xC0, 0x89, 0x44, 0x24, 0x50, 0x0F, 0x11,
                0x44, 0x24, 0x54
            };
            NoClipDetourAddress = GetInstance().CreateDetour(_noClipAddress, asm, 5);
            return;
        }
        
        ShowError("Photo mode no clip", sig);
    }

    public async Task CheatNoHeightLimits()
    {
        _noHeightLimitAddress = 0;
        NoHeightLimitDetourAddress = 0;

        const string sig = "F2 0F ? ? ? ? ? ? 66 0F ? ? 0F 2F";
        _noHeightLimitAddress = await SmartAobScan(sig);

        if (_noHeightLimitAddress > 0)
        {
            if (GetClass<Bypass>().CrcFuncDetourAddress == 0)
            {
                await GetClass<Bypass>().DisableCrcChecks();
            }
            
            if (GetClass<Bypass>().CrcFuncDetourAddress == 0) return;

            var asm = new byte[]
            {
                0x80, 0x3D, 0x1D, 0x00, 0x00, 0x00, 0x01, 0x75, 0x0E, 0x68, 0xA5, 0xD4, 0x68, 0x53, 0xF3, 0x0F, 0x10,
                0x14, 0x24, 0x48, 0x83, 0xC4, 0x08, 0xF2, 0x0F, 0x10, 0x9E, 0xC0, 0x05, 0x00, 0x00
            };
            NoHeightLimitDetourAddress = GetInstance().CreateDetour(_noHeightLimitAddress, asm, 8);
            return;
        }
        
        ShowError("Photo mode no height limits", sig);
    }
    
    public void Cleanup()
    {
        var mem = GetInstance();

        if (_noClipAddress > 0)
        {
            mem.WriteArrayMemory(_noClipAddress, new byte[] { 0x0F, 0x11, 0x44, 0x24, 0x54 });
            Free(NoClipDetourAddress);
        }

        if (_noHeightLimitAddress > 0)
        {
            mem.WriteArrayMemory(_noHeightLimitAddress, new byte[] { 0xF2, 0x0F, 0x10, 0x9E, 0xC0, 0x05, 0x00, 0x00 });
            Free(NoHeightLimitDetourAddress);
        }
    }

    public void Reset()
    {
        var fields = typeof(PhotomodeCheats).GetFields().Where(f => f.FieldType == typeof(UIntPtr));
        foreach (var field in fields)
        {
            field.SetValue(this, UIntPtr.Zero);
        }
    }
}