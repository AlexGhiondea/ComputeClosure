using AssemblyInspector.Helpers;
using AssemblyInspector.Objects.Types;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace AssemblyInspector.Objects.Assembly
{
    public abstract class AssemblyObject : BaseObject
    {
        protected AssemblyObject(FileStream fs, PEReader pe, MetadataReader mr, ResolveAssemblyDelegate resolveAssembly) :
            base(fs, pe, mr, resolveAssembly)
        {
        }

        public AssemblyName[] GetAssemblyReferences()
        {
            var count = _metaReader.GetTableRowCount(TableIndex.AssemblyRef);
            var references = new AssemblyName[count];

            for (int i = 0; i < count; i++)
            {
                var assemblyRef = _metaReader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(i + 1));
                var assemblyRefName = _metaReader.GetString(assemblyRef.Name);
                var cultureName = assemblyRef.Culture.IsNil ? "neutral" : _metaReader.GetString(assemblyRef.Culture);
                var pubKeyTok = AssemblyHelpers.FormatPublicKeyToken(_metaReader, assemblyRef.PublicKeyOrToken);

                references[i] = new AssemblyName(string.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}", assemblyRefName, assemblyRef.Version, cultureName, pubKeyTok));
            }

            return references;
        }

        public string[] GetModuleReferences()
        {
            var count = _metaReader.GetTableRowCount(TableIndex.ModuleRef);
            var moduleReferences = new string[count];

            for (int i = 0; i < count; i++)
            {
                var moduleRef = _metaReader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i + 1));
                var moduleName = _metaReader.GetString(moduleRef.Name);

                moduleReferences[i] = moduleName;
            }

            return moduleReferences;
        }

        public bool TryGetCustomAttribute(string ns, string name, out CustomAttribute ca)
        {
            ca = default(CustomAttribute);
            foreach (var handle in _metaReader.GetAssemblyDefinition().GetCustomAttributes())
            {
                ca = _metaReader.GetCustomAttribute(handle);

                if (AssemblyHelpers.IsAttributeOfType(_metaReader, ca, ns, name))
                {
                    return true;
                }
            }

            return false;
        }

        public abstract AssemblyName Name { get; set; }
        public abstract List<AssemblyObject> AssemblyReferences { get; }
        public abstract List<DeclaredTypeObject> TypesDeclared { get; }
        public abstract List<ReferencedTypeObject> TypesReferenced { get; set; }
        public abstract List<ExportedTypeObject> TypesExported { get; set; }
    }
}
