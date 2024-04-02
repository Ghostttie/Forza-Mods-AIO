﻿using System.Collections.ObjectModel;
using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Forza_Mods_AIO.Cheats;
using Forza_Mods_AIO.Models;
using Forza_Mods_AIO.Resources.Keybinds;
using Forza_Mods_AIO.Views.Pages;
using MahApps.Metro.Controls;
using Memory;
using static System.Diagnostics.FileVersionInfo;
using static System.IO.Path;
using static Forza_Mods_AIO.Resources.Cheats;
using static Forza_Mods_AIO.Resources.Memory;

namespace Forza_Mods_AIO.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    private bool _isInitialized;
    
    #region Constants

    private const string NotAttachedText = "Launch FH4, FH5 or FM8";
    private const double WindowCornerRadiusSize = 7.5;

    #endregion
    
    #region Misc Vars

    [ObservableProperty]
    private string _applicationTitle = string.Empty;
    
    [ObservableProperty]
    private string _attachedText = string.Empty;

    [ObservableProperty]
    private object _currentView = new();
    
    [ObservableProperty]
    private bool _attached;
    
    [ObservableProperty]
    private CornerRadius _windowCornerRadius = new(WindowCornerRadiusSize);
    
    [ObservableProperty]
    private CornerRadius _topBarCornerRadius = new(WindowCornerRadiusSize, WindowCornerRadiusSize, 0, 0);

    [ObservableProperty]
    private CornerRadius _sideBarCornerRadius = new(0, 0, 0, WindowCornerRadiusSize);
    
    #endregion

    #region Search

    [ObservableProperty]
    private Visibility _searchVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private double _searchOpacity;
    
    [ObservableProperty]
    private ObservableCollection<SearchResult> _searchResults = [];
    
    #endregion

    #region Hotkeys

    private HotKey? _hotKey = new(Key.None);

    public HotKey? HotKey
    {
        get => _hotKey;
        set
        {
            if (EqualityComparer<HotKey>.Default.Equals(_hotKey, value)) return;
            OnPropertyChanging();
            _hotKey = value;
            _wasHotkeyChanged = true;
            OnPropertyChanged();
        }
    }

    private bool _wasHotkeyChanged;

    [ObservableProperty]
    private Visibility _hotkeysVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private double _hotkeysOpacity;
    
    #endregion
    
    public MainWindowViewModel()
    {
        if (_isInitialized) return;
        InitializeViewModel();
    }

    private void InitializeViewModel()
    {
        ApplicationTitle = "Forza Mods AIO";
        AttachedText = NotAttachedText;

        CurrentView = Resources.Pages.GetPage(typeof(AioInfo));

        SetupAttach();
        _isInitialized = true;
    }
    
    [RelayCommand]
    private void HandleMaximizeMinimize(object mainWindow)
    {
        if (mainWindow is not MetroWindow metroWindow)
        {
            return;
        }
        
        switch (metroWindow.WindowState)
        {
            case WindowState.Maximized:
            {
                metroWindow.WindowState = WindowState.Normal;
                WindowCornerRadius = new CornerRadius(WindowCornerRadiusSize);
                SideBarCornerRadius = new CornerRadius(0, 0, 0, WindowCornerRadiusSize);
                TopBarCornerRadius = new CornerRadius(WindowCornerRadiusSize, WindowCornerRadiusSize, 0, 0);
                break;
            }
            case WindowState.Normal:
            {
                metroWindow.WindowState = WindowState.Maximized;
                WindowCornerRadius = new CornerRadius(0);
                SideBarCornerRadius = new CornerRadius(0);
                TopBarCornerRadius = new CornerRadius(0);
                break;
            }
            case WindowState.Minimized:
            default:
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
    
    private void SetupAttach()
    {
        string[] processNames = ["forzahorizon5.exe", "forzahorizon4.exe"];
        if (LoopProcesses(processNames))
        {
            SetupExit();
        }

        var watcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        watcher.EventArrived += (_, e) =>
        {
            if (Attached)
            {
                return;
            }
            
            var name = e.NewEvent.Properties["ProcessName"].Value.ToString()?.ToLower();
            if (name == null)
            {
                return;
            }

            if (processNames.All(processName => name != processName)) return;
            if (!LoopProcesses(processNames))
            {
                return;
            }

            SetupExit();
        };
        
        watcher.Start();
    }

    private bool LoopProcesses(IEnumerable<string> processNames)
    {
        Attached = false;
        
        foreach (var processName in processNames)
        {
            if (GetInstance().OpenProcess(processName) != Mem.OpenProcessResults.Success) continue;
            GvpMaker(processName);
            Attached = true;
            break;
        }

        return Attached;
    }

    private void SetupExit()
    {
        GetInstance().MProc.Process.EnableRaisingEvents = true;
        GetInstance().MProc.Process.Exited += (_, _) =>
        {
            Attached = false;
            AttachedText = NotAttachedText;
            CurrentView = Resources.Pages.GetPage(typeof(AioInfo));
            Resources.Pages.Clear();
            CleanClasses();
        };
    }

    private static void CleanClasses()
    {
        foreach (var cheatInstance in CachedInstances.Where(kv => typeof(ICheatsBase).IsAssignableFrom(kv.Key)))
        {
            ((ICheatsBase)cheatInstance.Value).Reset();
        }
    }
    
    private void GvpMaker(string name)
    {
        var process = GetInstance().MProc.Process;
        if (process.MainModule == null)
        {
            return;
        }
        
        string platform;
        string update;    
        var gamePath = process.MainModule.FileName;

        if (gamePath.Contains("Microsoft.624F8B84B80") || gamePath.Contains("Microsoft.SunriseBaseGame") ||
            gamePath.Contains("Microsoft.ForzaMotorsport"))
        {
            platform = "MS";
            var filePath = Combine(GetDirectoryName(gamePath) ?? string.Empty, "appxmanifest.xml");
            var xml = XElement.Load(filePath);
            var descendants = xml.Descendants().Where(e => e.Name.LocalName == "Identity");
            var version = descendants.Select(e => e.Attribute("Version")).FirstOrDefault();
            update = version == null ? "Unable to get update info" : version.Value;
        }
        else
        {
            var filePath = Combine(GetDirectoryName(gamePath) ?? string.Empty, "OnlineFix64.dll");
            platform = File.Exists(filePath) ? "OnlineFix - Steam" : "Steam";
            update = GetVersionInfo(process.MainModule.FileName).FileVersion ?? "Unable to get update info";
        }

        var type = GetTypeFromName(name);
        GameVerPlat.GetInstance().Name = name;
        GameVerPlat.GetInstance().Platform = platform;
        GameVerPlat.GetInstance().Update = update;
        GameVerPlat.GetInstance().Type = type;
        AttachedText = $"{GameVerPlat.GetInstance().Name}, {GameVerPlat.GetInstance().Platform}, {GameVerPlat.GetInstance().Update}";
    }

    private static GameVerPlat.GameType GetTypeFromName(string name)
    {
        return name switch
        {
            "Forza Horizon 4" => GameVerPlat.GameType.Fh4,
            "Forza Horizon 5" => GameVerPlat.GameType.Fh5,
            _ => GameVerPlat.GameType.None
        };
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        if (HotkeysVisibility != Visibility.Collapsed)
        {
            return;
        }
        
        switch (SearchVisibility)
        {
            case Visibility.Collapsed:
            {
                HandleCollapsed();
                break;
            }
            case Visibility.Visible:
            {
                HandleVisible();
                break;
            }
            case Visibility.Hidden:
            default:
            {
                throw new IndexOutOfRangeException();
            }
        }
    }

    [RelayCommand]
    private void CloseSearch()
    {
        if (SearchVisibility != Visibility.Visible)
        {
            return;
        }
        
        HandleVisible();
    }

    private async void HandleCollapsed()
    {
        SearchVisibility = Visibility.Visible;
        
        while (SearchOpacity < 0.5)
        {
            SearchOpacity += 0.05;
            await Task.Delay(15);
        }
    }

    private async void HandleVisible()
    {
        while (SearchOpacity > 0)
        {
            SearchOpacity -= 0.05;
            await Task.Delay(15);
        }
        
        SearchVisibility = Visibility.Collapsed;
    }
    
    public async Task<HotKey> GetHotkey(GlobalHotkey hotkey)
    {
        HotKey = new HotKey(hotkey.Key, hotkey.Modifier);   
        
        _wasHotkeyChanged = false;
        HotkeysVisibility = Visibility.Visible;
        
        while (HotkeysOpacity < 0.5)
        {
            HotkeysOpacity += 0.05;
            await Task.Delay(15);
        }
        
        var returnWithNoChange = false;
        while (!_wasHotkeyChanged)
        {
            await Task.Delay(5);
            if (!Keyboard.IsKeyDown(Key.Escape)) continue;
            returnWithNoChange = true;
            break;
        }

        while (HotkeysOpacity > 0)
        {
            HotkeysOpacity -= 0.05;
            await Task.Delay(15);
        }
        
        HotkeysVisibility = Visibility.Collapsed;

        if (returnWithNoChange)
        {
            return new HotKey(hotkey.Key, hotkey.Modifier);  
        }
        
        if (HotKey == null) return new HotKey(Key.None);
        var result = new HotKey(HotKey.Key, HotKey.ModifierKeys); 
        return result;
    }
}