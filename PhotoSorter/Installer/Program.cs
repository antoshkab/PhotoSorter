using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WixSharp;
using WixSharp.CommonTasks;

namespace Installer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Project project =
                new Project("PhotoSorter",
                    new Dir(@"%AppData%\[ProductName]",
                        new File(@"..\..\..\PhotoSorter\bin\Release\PhotoSorter.exe"),
                        new File(@"..\..\..\PhotoSorter\bin\Release\PhotoSorter.exe.config")));

            project.GUID = new Guid("{BFC29159-317A-48B3-9B15-F809E98DA4DA}");
            project.Codepage = "1251";
            project.Media.CompressionLevel = CompressionLevel.high;
            project.Language = "ru-RU";

            Compiler.BuildMsi(project);
        }
    }
}
