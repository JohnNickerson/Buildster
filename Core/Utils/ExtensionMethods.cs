using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.Buildster.Core.Utils
{
    public static class ExtensionMethods
    {
        public static string PathExpandCombine(params string[] folders)
        {
            for (int x = 0; x < folders.Length; x++)
            {
                folders[x] = System.Environment.ExpandEnvironmentVariables(folders[x]);
            }

            return Path.Combine(folders);
        }
    }
}
