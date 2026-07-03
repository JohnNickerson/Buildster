using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.Buildster.Core.Utils
{
    internal class AssemblyAttribute
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public AssemblyAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public static bool TryParse(string attributeString, out AssemblyAttribute result)
        {
            if (attributeString.Trim().StartsWith("[assembly:", StringComparison.CurrentCultureIgnoreCase))
            {
                var namePieces = attributeString.Split(new[] { ':', '(' });
                var valuePieces = attributeString.Split('"');
                if (namePieces.Length > 1 && valuePieces.Length > 1)
                {
                    result = new AssemblyAttribute(namePieces[1].Trim(), valuePieces[1].Trim());
                    return true;
                }
            }
            result = null;
            return false;
        }

        public static bool TryParseExact(string attributeString, string expectedName, out AssemblyAttribute result)
        {
            return TryParse(attributeString, out result) && result.Name == expectedName;
        }

        public override string ToString()
        {
            return $"[assembly: {Name}(\"{Value}\")]";
        }
    }
}
