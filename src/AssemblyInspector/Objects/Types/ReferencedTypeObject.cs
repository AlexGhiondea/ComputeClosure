using AssemblyInspector.Helpers;
using AssemblyInspector.Objects.Assembly;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyInspector.Objects.Types
{
    public class ReferencedTypeObject : BaseObject
    {
        public string Name { get; set; }

        private TypeReference _typeRef;
        public AssemblyObject DeclaredAssembly { get; private set; }

        public static ReferencedTypeObject CreateFrom(ResolvedAssemblyObject ao, TypeReference typeRef)
        {
            return CreateFrom(ao, typeRef, null);
        }
        public static ReferencedTypeObject CreateFrom(ResolvedAssemblyObject ao, TypeReference typeRef, ResolveAssemblyDelegate resolveAssembly)
        {
            ReferencedTypeObject to = new ReferencedTypeObject(ao._fs, ao._peReader, ao._metaReader, typeRef, resolveAssembly);
            return to;
        }

        public ReferencedTypeObject(FileStream fs, PEReader pe, MetadataReader mr, TypeReference typeRef, ResolveAssemblyDelegate resolveAssembly) :
            base(fs, pe, mr, resolveAssembly)
        {
            _typeRef = typeRef;

            Name = AssemblyHelpers.GetTypeName(_metaReader, _typeRef);
            DeclaredAssembly = null;
            
            // We are not able to resolve ModuleReferences yet. 
            if (resolveAssembly != null && _typeRef.ResolutionScope.Kind != HandleKind.ModuleReference)
            {
                var assemblyRef = ToAssemblyName(AssemblyHelpers.GetAssemblyReferenceForReferencedType(_metaReader, _typeRef));
                DeclaredAssembly = resolveAssembly(assemblyRef);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
