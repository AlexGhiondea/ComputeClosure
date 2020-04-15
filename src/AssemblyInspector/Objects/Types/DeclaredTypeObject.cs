using AssemblyInspector.Helpers;
using AssemblyInspector.Objects.Assembly;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyInspector.Objects.Types
{
    public class DeclaredTypeObject : BaseObject
    {
        public string Name { get; set; }

        public bool IsPublic { get; set; }

        private TypeDefinition _typeDef;

        public ResolvedAssemblyObject Assembly { get; private set; }

        public static DeclaredTypeObject CreateFrom(ResolvedAssemblyObject ao, TypeDefinition typeDef)
        {
            DeclaredTypeObject to = new DeclaredTypeObject(ao._fs, ao._peReader, ao._metaReader, typeDef);
            to.Assembly = ao;
            return to;
        }

        public DeclaredTypeObject(FileStream fs, PEReader pe, MetadataReader mr, TypeDefinition typeDef) :
            base(fs, pe, mr, null)
        {
            _typeDef = typeDef;

            Name = AssemblyHelpers.GetTypeName(_metaReader, typeDef);
            IsPublic = AssemblyHelpers.IsVisible(_metaReader, typeDef);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
