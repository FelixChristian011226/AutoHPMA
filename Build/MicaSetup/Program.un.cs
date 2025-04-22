using MicaSetup.Attributes;
using MicaSetup.Core;
using MicaSetup.Design.Controls;
using MicaSetup.Extension.DependencyInjection;
using MicaSetup.Services;
using MicaSetup.Views;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("00000000-0000-0000-0000-000000000000")]
[assembly: AssemblyTitle("AutoHPMA Uninst")]
[assembly: AssemblyProduct("AutoHPMA")]
[assembly: AssemblyDescription("AutoHPMA Uninst")]
[assembly: AssemblyCompany("Lemutec")]
[assembly: AssemblyCopyright("Under MIT License. Copyright (c) AutoHPMA Contributors.")]
[assembly: AssemblyVersion("2.3.6.0")]
[assembly: AssemblyFileVersion("2.3.6.0")]
[assembly: RequestExecutionLevel("admin")]

namespace MicaSetup;

internal class Program
{
    [STAThread]
    internal static void Main()
    {
        Hosting.CreateBuilder()
            .UseAsUninst()
            .UseLogger(false)
            .UseSingleInstance("MicaSetup")
            .UseTempPathFork()
            .UseElevated()
            .UseDpiAware()
            .UseOptions(option =>
            {
                option.IsCreateDesktopShortcut = true;
                option.IsCreateUninst = true;
                option.IsUninstLower = false;
                option.IsCreateRegistryKeys = true;
                option.IsCreateStartMenu = true;
                option.IsCreateQuickLaunch = false;
                option.IsCreateAsAutoRun = false;
                option.IsUseRegistryPreferX86 = null!;
                option.IsAllowFirewall = true;
                option.IsRefreshExplorer = true;
                option.IsInstallCertificate = false;
                option.IsEnableUninstallDelayUntilReboot = true;
                option.IsEnvironmentVariable = false;
                option.AppName = "AutoHPMA";
                option.KeyName = "AutoHPMA";
                option.ExeName = "AutoHPMA.exe";
                option.DisplayName = $"{option.AppName}";
                option.DisplayIcon = $"{option.ExeName}";
                option.DisplayVersion = "0.0.0.0";
                option.Publisher = "FelixChristian";
                option.SetupName = $"{option.AppName} {"UninstallProgram".Tr()}";
                option.MessageOfPage1 = $"{option.AppName}";
                option.MessageOfPage2 = "ProgressTipsUninstalling".Tr();
                option.MessageOfPage3 = "UninstallFinishTips".Tr();
            })
            .UseServices(service =>
            {
                service.AddSingleton<ITrService, TrService>();
                service.AddScoped<IExplorerService, ExplorerService>();
            })
            .CreateApp()
            .UseLocale()
            .UseTheme(WindowsTheme.Auto)
            .UsePages(page =>
            {
                page.Add(nameof(MainPage), typeof(MainPage));
                page.Add(nameof(UninstallPage), typeof(UninstallPage));
                page.Add(nameof(FinishPage), typeof(FinishPage));
            })
            .UseDispatcherUnhandledExceptionCatched()
            .UseDomainUnhandledExceptionCatched()
            .UseUnobservedTaskExceptionCatched()
            .RunApp();
    }
}
