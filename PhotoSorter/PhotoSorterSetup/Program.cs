using System;
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


            var project = new Project("PhotoSorter",
                             new Dir(@"%LocalAppDataFolder%\PhotoSorter",
                                 new Files(@"..\PhotoSorter\bin\Release\*.*", f => !f.EndsWith(".pdb") && !f.Contains("vshost")),
                                 new ExeFileShortcut("Uninstall PhotoSorter", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                                 {
                                     WorkingDirectory = "%Temp%"
                                 }),
                             new Dir(@"%Desktop%",
                                new ExeFileShortcut("PhotoSorter", "[INSTALL_DIR]PhotoSorter.exe", "")),
                             new Dir(@"%ProgramMenu%\PhotoSorter",
                                new ExeFileShortcut("PhotoSorter", "[INSTALL_DIR]PhotoSorter.exe", ""),
                                new ExeFileShortcut("Uninstall PhotoSorter", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                                {
                                    WorkingDirectory = "%Temp%"
                                }));
            project.GUID = new Guid("{5E1EED8B-5CEB-4EE7-BD2D-B7A4BFA836C0}");
            project.Codepage = "1251";
            project.Language = "ru-RU";
            project.SetVersionFromFile(@"..\PhotoSorter\bin\Release\PhotoSorter.exe");
            project.Media.CompressionLevel = CompressionLevel.high;
            project.Media.EmbedCab = true;
            project.UI = WUI.WixUI_Common;
            project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
            var customUI = new CustomUI();


            customUI.On(NativeDialogs.ExitDialog, Buttons.Finish, new CloseDialog() { Order = 9999 });

            customUI.On(NativeDialogs.WelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.InstallDirDlg));

            customUI.On(NativeDialogs.InstallDirDlg, Buttons.Back, new ShowDialog(NativeDialogs.WelcomeDlg));
            customUI.On(NativeDialogs.InstallDirDlg, Buttons.Next, new SetTargetPath(),
                                                             new ShowDialog(NativeDialogs.VerifyReadyDlg));

            customUI.On(NativeDialogs.InstallDirDlg, Buttons.ChangeFolder,
                                                             new SetProperty("_BrowseProperty", "[WIXUI_INSTALLDIR]"),
                                                             new ShowDialog(CommonDialogs.BrowseDlg));

            customUI.On(NativeDialogs.VerifyReadyDlg, Buttons.Back, new ShowDialog(NativeDialogs.InstallDirDlg, Condition.NOT_Installed),
                                                              new ShowDialog(NativeDialogs.MaintenanceTypeDlg, Condition.Installed));

            customUI.On(NativeDialogs.MaintenanceWelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.MaintenanceTypeDlg));

            customUI.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Back, new ShowDialog(NativeDialogs.MaintenanceWelcomeDlg));
            customUI.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Repair, new ShowDialog(NativeDialogs.VerifyReadyDlg));
            customUI.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Remove, new ShowDialog(NativeDialogs.VerifyReadyDlg));
            project.CustomUI = customUI;
            project.OutFileName = "PhotoSorterSetup";
            project.PreserveTempFiles = true;
            ValidateAssemblyCompatibility();
            project.ResolveWildCards(true);
            project.BuildMsi();
        }

        static void ValidateAssemblyCompatibility()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            if (!assembly.ImageRuntimeVersion.StartsWith("v2."))
            {
                Console.WriteLine("Warning: assembly '{0}' is compiled for {1} runtime, which may not be compatible with the CLR version hosted by MSI. " +
                                  "The incompatibility is particularly possible for the EmbeddedUI scenarios. " +
                                   "The safest way to solve the problem is to compile the assembly for v3.5 Target Framework.",
                                   assembly.GetName().Name, assembly.ImageRuntimeVersion);
            }
        }
    }
}
