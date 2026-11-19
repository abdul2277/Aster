@echo off
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "WPF=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF"
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /win32icon:Aster.ico /resource:Aster-artwork.png,AsterArt /resource:update-public-key.xml,AsterUpdateKey /out:Aster.exe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /reference:System.Device.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Xml.Linq.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll AsterTaskManager.cs OnlineUpdates.cs SessionTools.cs Legendary.cs Updates.cs EmbeddedVideo.cs AsterAlerts.cs AlertLocation.cs Startup.cs AirDefenseLauncher.cs ToolsHub.cs Hardware.cs Recovery.cs Advisor.cs PlanRunner.cs Extras.cs Convenience.cs Workspace.cs AsterFeatures.cs Playful.cs Greeting.cs Themes.cs DailyPicture.cs ThemeStudio.cs PersonalWorkspace.cs NetworkLab.cs PremiumWorkspace.cs BackupCenter.cs Brand.cs
if errorlevel 1 exit /b 1
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /win32icon:Aster.ico /resource:Aster-artwork.png,AsterArt /out:AuraWallpaper.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll /reference:"%WPF%\WindowsFormsIntegration.dll" /reference:"%WPF%\PresentationCore.dll" /reference:"%WPF%\PresentationFramework.dll" /reference:"%WPF%\WindowsBase.dll" /reference:System.Xaml.dll AuraWallpaper.cs AuraCatalog.cs Brand.cs











