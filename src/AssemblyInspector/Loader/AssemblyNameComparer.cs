using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyInspector.Loader
{
    public class AssemblyNameComparer : EqualityComparer<AssemblyName>
    {
        public static AssemblyNameComparer Singleton = new AssemblyNameComparer();

        public override bool Equals(AssemblyName x, AssemblyName y)
        {
            return x.Name == y.Name &&
                x.Version == y.Version &&
                x.CultureName == y.CultureName &&
                x.GetPublicKeyToken().SequenceEqual(y.GetPublicKeyToken());
        }

        public override int GetHashCode(AssemblyName obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
