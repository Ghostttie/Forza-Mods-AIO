﻿using System;
using System.Collections.Generic;
using Forza_Mods_AIO.Overlay.Options;
using static Forza_Mods_AIO.Tabs.Self_Vehicle.DropDownTabs.CameraPage;

namespace Forza_Mods_AIO.Overlay.Menus.SelfCarMenu.FovMenu;

public abstract class FovLock
{
    private static readonly FloatOption FovLockValue = new("Value", 0f, Camera.FovSlider.Minimum, Camera.FovSlider.Maximum);
    private static readonly ToggleOption FovLockToggle = new("Enable",false);
        
    public static void InitiateSubMenu()
    {
        FovLockValue.ValueChanged += FovLockValueChanged;
        FovLockToggle.Toggled += FovLockToggled;
    }
    
    private static void FovLockValueChanged(object s, EventArgs e)
    {
        if (s is not FloatOption floatOption)
        {
            return;
        }
        
        Camera.FovSlider.Value = Convert.ToSingle(Math.Round(floatOption.Value, 1));
    }
    
    private static void FovLockToggled(object s, EventArgs e)
    {
        Camera.FovSwitch.IsOn = FovLockToggle.IsOn;
    }
    
    public static readonly List<MenuOption> FovLockOptions = new()
    {
        new SubHeaderOption("Fov Lock"),
        FovLockValue,
        FovLockToggle
    };
}