using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.Buildster.Core.Utils
{
    public class VersionNumber
    {
        public VersionNumber(string numberString)
        {
            var splits = numberString.Split('.');
            Major = int.Parse(splits[0]);
            Minor = int.Parse(splits[1]);
            Revision = int.Parse(splits[2]);
            Patch = splits.Length >= 4 ? int.Parse(splits[3]) : 0;
        }

        public VersionNumber(int major, int minor, int rev, int patch)
        {
            Major = major;
            Minor = minor;
            Revision = rev;
            Patch = patch;
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Revision { get; set; }
        public int Patch { get; set; }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}.{Patch}";
        }

        internal static bool TryParse(string v, out VersionNumber currentVersion)
        {
            try
            {
                currentVersion = new VersionNumber(v);
                return true;
            }
            catch
            {
                currentVersion = new VersionNumber("0.0.0.0");
                return false;
            }
        }
    }
}
