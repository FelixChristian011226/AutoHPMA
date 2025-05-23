﻿using System.ComponentModel;

namespace MicaSetup;

/// <summary>
/// Option Context
/// </summary>
public partial class Option
{
    public static Option Current { get; } = new();

    /// <summary>
    /// Indicates whether enable logger
    /// </summary>
    [Category("GlobalSetting")]
    public bool Logging { get; internal set; } = false;

    /// <summary>
    /// Indicates whether App installing
    /// </summary>
    [Category("GlobalVariable")]
    public bool Installing { get; set; } = false;

    /// <summary>
    /// Indicates whether App uninstalling
    /// </summary>
    [Category("GlobalVariable")]
    public bool Uninstalling { get; set; } = false;

    /// <summary>
    /// Indicates whether this assembly as uninst
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUninst { get; set; } = false;

    /// <summary>
    /// Indicates whether create uninst after app installed
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCreateUninst { get; set; } = true;

    /// <summary>
    /// Indicates whether create uninst program file named as lower case
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUninstLower { get; set; } = false;

    /// <summary>
    /// Indicates whether to generate Desktop Shortcut
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCreateDesktopShortcut { get; set; } = true;

    /// <summary>
    /// Indicates whether to generate Registry Keys
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCreateRegistryKeys { get; set; } = true;

    /// <summary>
    /// Indicates whether to generate StartMenu Shortcut
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCreateStartMenu { get; set; } = true;

    /// <summary>
    /// Indicates whether to generate StartMenu Shortcut
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCreateQuickLaunch { get; set; } = false;

    /// <summary>
    /// Indicates whether to generate StartMenu Shortcut
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsPinToStartMenu { get; set; } = false;

    /// <summary>
    /// Indicates whether to generate AutoRun
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCreateAsAutoRun { get; set; } = false;

    /// <summary>
    /// Indicates whether to show customize option of AutoRun
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsCustomizeVisiableAutoRun { get; set; } = false;

    /// <summary>
    /// Indicates AutoRun CLI
    /// </summary>
    [Category("GlobalSetting")]
    public string AutoRunLaunchCommand { get; set; } = string.Empty;

    /// <summary>
    /// Prefer to provide classic type folder selector
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUseFolderPickerPreferClassic { get; set; } = false;

    /// <summary>
    /// Prefer to provide x86 type install path (Admin requested)
    /// Such as `C:\Program Files (x86)\{APP_NAME}`
    /// <seealso cref="System.Environment.SpecialFolder.ProgramFiles"/>
    /// <seealso cref="System.Environment.SpecialFolder.ProgramFilesX86"/>
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUseInstallPathPreferX86 { get; set; } = false;

    /// <summary>
    /// Prefer to provide x86 type install path
    /// Such as `C:\Users\{USER_NAME}\AppData\Local\Programs\{APP_NAME}`
    /// <seealso cref="System.Environment.SpecialFolder.LocalApplicationData"/>
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUseInstallPathPreferAppDataLocalPrograms { get; set; } = false;

    /// <summary>
    /// Prefer to provide app data type install path
    /// Such as `C:\Users\{USER_NAME}\AppData\Roaming\{APP_NAME}`
    /// <seealso cref="System.Environment.SpecialFolder.ApplicationData"/>
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUseInstallPathPreferAppDataRoaming { get; set; } = false;

    /// <summary>
    /// Prefer to provide x86 type Registry Key
    /// </summary>
    [Category("GlobalSetting")]
    public bool? IsUseRegistryPreferX86 { get; set; } = null!;

    /// <summary>
    /// Indicates whether to Allow Full Security intall path
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsAllowFullFolderSecurity { get; set; } = true;

    /// <summary>
    /// Indicates whether to Allow Network Firewall
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsAllowFirewall { get; set; } = true;

    /// <summary>
    /// Indicates whether to Refresh Explorer
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsRefreshExplorer { get; set; } = false;

    /// <summary>
    /// Indicates whether to Install Certification file (*.cer)
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsInstallCertificate { get; set; } = false;

    /// <summary>
    /// Indicates whether to enable `DeleteDelayUntilReboot`
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsEnableUninstallDelayUntilReboot { get; set; } = false;

    /// <summary>
    /// Indicates whether to use license.txt instead of others
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsUseLicenseFile { get; internal set; }

    /// <summary>
    /// The file ext filter to remove when overlay install
    /// Using just like "exe,dll,pdb"
    /// </summary>
    [Category("GlobalSetting")]
    public string OverlayInstallRemoveExt { get; set; } = string.Empty;

    /// <summary>
    /// A handler to remove when overlay install
    /// Handler will be run after <option refs="OverlayInstallRemoveExt"/>
    /// </summary>
    [Category("GlobalSetting")]
    public AbstractOverlayInstallRemoveHandler OverlayInstallRemoveHandler { get; set; } = null!;

    /// <summary>
    /// The archive file unpacking password
    /// </summary>
    [Category("GlobalSetting")]
    public string UnpackingPassword { get; set; } = null!;

    /// <summary>
    /// Indicates whether to add environment variable
    /// </summary>
    [Category("GlobalSetting")]
    public bool IsEnvironmentVariable { get; set; } = false;

    /// <summary>
    /// Your Product Exe file name
    /// </summary>
    [Category("GlobalSetting")]
    public string ExeName { get; set; } = null!;

    /// <summary>
    /// Indicates whether to uninst and keep my data
    /// For security reason, it's only can be used in admin
    /// </summary>
    [Category("GlobalVariable")]
    public bool KeepMyData { get; set; } = true;

    /// <summary>
    /// Registry Uninstall key name
    /// </summary>
    [Category("GlobalSetting")]
    public string KeyName { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `DisplayName` key value
    /// </summary>
    [Category("GlobalSetting")]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `DisplayIcon` key value
    /// </summary>
    [Category("GlobalSetting")]
    public string DisplayIcon { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `DisplayVersion` key value
    /// </summary>
    [Category("GlobalSetting")]
    public string DisplayVersion { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `InstallLocation` key value
    /// </summary>
    [Category("GlobalVariable")]
    public string InstallLocation { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `Publisher` key value
    /// </summary>
    [Category("GlobalSetting")]
    public string Publisher { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `UninstallString` key value
    /// </summary>
    [Category("GlobalVariable")]
    public string UninstallString { get; set; } = null!;

    /// <summary>
    /// Registry Uninstall `SystemComponent` key value
    /// true(1) => Hidden
    /// false(0) or _ => Shown (defalut)
    /// </summary>
    [Category("GlobalSetting")]
    public bool SystemComponent { get; set; } = false;

    /// <summary>
    /// Provide {AppName}.exe to auto run
    /// </summary>
    [Category("GlobalSetting")]
    public string AppName { get; set; } = null!;

    /// <summary>
    /// Provide SetupName
    /// </summary>
    [Category("GlobalSetting")]
    public string SetupName { get; set; } = null!;

    /// <summary>
    /// Provide Package Name for Get-AppxPackage
    /// You can get it from AppxManifest.xml in your MSIX/APPX type file
    /// </summary>
    [Category("GlobalSetting")]
    public string AppxPackageName { get; set; } = null!;

    /// <summary>
    /// Message of <see cref="Views.MainPage"/>
    /// </summary>
    [Category("GlobalSetting")]
    public string MessageOfPage1 { get; set; } = null!;

    /// <summary>
#if SETUP
    /// Message of <see cref="Views.InstallPage"/>
#elif UNINST
    /// Message of <see cref="Views.UninstallPage"/>
#endif

    /// </summary>
    [Category("GlobalSetting")]
    public string MessageOfPage2 { get; set; } = null!;

    /// <summary>
    /// Message of <see cref="Views.FinishPage"/>
    /// </summary>
    [Category("GlobalSetting")]
    public string MessageOfPage3 { get; set; } = null!;
}
