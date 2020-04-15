using AssemblyInspector.Objects.Types;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyInspector.Objects.Assembly
{
    public class UnresolvedAssemblyObject : AssemblyObject
    {
        public override AssemblyName Name { get; set; }

        public override List<AssemblyObject> AssemblyReferences => new List<AssemblyObject>();

        public override List<DeclaredTypeObject> TypesDeclared => new List<DeclaredTypeObject>();

        public override List<ReferencedTypeObject> TypesReferenced { get => new List<ReferencedTypeObject>(); set { } }
        public override List<ExportedTypeObject> TypesExported { get => new List<ExportedTypeObject>(); set { } }

        public static UnresolvedAssemblyObject Create(AssemblyName an) => new UnresolvedAssemblyObject(an);

        private UnresolvedAssemblyObject(AssemblyName an)
            : base(null, null, null, null)
        {
            Name = an;
        }

        public override string ToString()
        {
            return "Unresolved: " + Name.ToString();
        }
    }
}
