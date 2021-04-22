using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Windows.Forms;
using UInspect.Utilities;

namespace UInspect
{
    static class Program
    {
        public static Icon AppIcon { get; private set; }

        [STAThread]
        static void Main()
        {
            // read app icon from win32 embedded resources, this avoid duplicating the same icon resource per each form
            // this code expects a .ico with "Resource" build action and as the main icon of the project, with a name like "<this class namespace>.ico"
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream(typeof(Program).Namespace + ".g.resources"))
            {
                if (stream != null)
                {
                    using (var reader = new ResourceReader(stream))
                    {
                        foreach (DictionaryEntry resource in reader)
                        {
                            if (resource.Key.ToString().EqualsIgnoreCase(typeof(Program).Namespace + ".ico"))
                            {
                                AppIcon = new Icon((Stream)resource.Value);
                                break;
                            }
                        }
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
