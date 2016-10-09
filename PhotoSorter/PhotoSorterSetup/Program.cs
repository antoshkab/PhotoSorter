using System;
using Microsoft.Win32;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

namespace WixSharpSetup
{
    class Program
    {
        static void Main()
        {
            //DON'T FORGET to execute "Install-Package WixSharp" in the Package Manager Console

            //KeyPath="yes"
            var project = new Project("PhotoSorter",
                             new Dir(new Id("INSTALLDIR"), @"%LocalAppData%\PhotoSorter",
                                 new Files(@"..\PhotoSorter\bin\Release\*.*", f => !f.EndsWith(".pdb") && !f.Contains("vshost")),
                                 new ExeFileShortcut("Uninstall PhotoSorter", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                                 {
                                     WorkingDirectory = "%Temp%"
                                 }),
                             new Dir(@"%ProgramMenu%",
                             new Dir("PhotoSorter", 
                                new ExeFileShortcut("PhotoSorter", @"[INSTALLDIR]\PhotoSorter.exe", "")
                                {
                                    WorkingDirectory = "[INSTALLDIR]"
                                },
                                new ExeFileShortcut("Uninstall PhotoSorter", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                                {
                                    WorkingDirectory = "%Temp%"
                                })),
                             new Dir(@"%Desktop%",
                                new ExeFileShortcut("PhotoSorter", @"[INSTALLDIR]\PhotoSorter.exe", "")
                                {
                                    WorkingDirectory = "[INSTALLDIR]"
                                }));
            project.GUID = new Guid("{5E1EED8B-5CEB-4EE7-BD2D-B7A4BFA836C0}");
            project.Codepage = "1251";
            project.Language = "ru-RU";
            project.SetVersionFromFile(@"..\PhotoSorter\bin\Release\PhotoSorter.exe");
            project.Media.CompressionLevel = CompressionLevel.high;
            project.Media.EmbedCab = true;
            project.UI = WUI.WixUI_Common;
            project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
            var customUi = new CustomUI();


            customUi.On(NativeDialogs.ExitDialog, Buttons.Finish, new CloseDialog() { Order = 9999 });

            customUi.On(NativeDialogs.WelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.InstallDirDlg));

            customUi.On(NativeDialogs.InstallDirDlg, Buttons.Back, new ShowDialog(NativeDialogs.WelcomeDlg));
            customUi.On(NativeDialogs.InstallDirDlg, Buttons.Next, new SetTargetPath(),
                                                             new ShowDialog(NativeDialogs.VerifyReadyDlg));

            customUi.On(NativeDialogs.InstallDirDlg, Buttons.ChangeFolder,
                                                             new SetProperty("_BrowseProperty", "[WIXUI_INSTALLDIR]"),
                                                             new ShowDialog(CommonDialogs.BrowseDlg));

            customUi.On(NativeDialogs.VerifyReadyDlg, Buttons.Back, new ShowDialog(NativeDialogs.InstallDirDlg, Condition.NOT_Installed),
                                                              new ShowDialog(NativeDialogs.MaintenanceTypeDlg, Condition.Installed));

            customUi.On(NativeDialogs.MaintenanceWelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.MaintenanceTypeDlg));

            customUi.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Back, new ShowDialog(NativeDialogs.MaintenanceWelcomeDlg));
            customUi.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Repair, new ShowDialog(NativeDialogs.VerifyReadyDlg));
            customUi.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Remove, new ShowDialog(NativeDialogs.VerifyReadyDlg));
            project.SetNetFxPrerequisite(Condition.Net45_Installed, "Please install .Net FrameWork 4.5");
            project.CustomUI = customUi;
            project.OutFileName = "PhotoSorterSetup";
            project.PreserveTempFiles = true;
            project.ResolveWildCards(true);
            project.BuildMsi();
        }
    }
}
