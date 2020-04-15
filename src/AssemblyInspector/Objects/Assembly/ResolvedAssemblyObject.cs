using AssemblyInspector.Objects.Types;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyInspector.Objects.Assembly
{
    public delegate AssemblyObject ResolveAssemblyDelegate(AssemblyName assembly);

    public class ResolvedAssemblyObject : AssemblyObject
    {
        private List<AssemblyObject> _references = null;

        public override AssemblyName Name { get; set; }
        public override List<AssemblyObject> AssemblyReferences => _references;
        public override List<DeclaredTypeObject> TypesDeclared { get; }
        public override List<ReferencedTypeObject> TypesReferenced { get; set; }
        public override List<ExportedTypeObject> TypesExported { get; set; }

        public static ResolvedAssemblyObject CreateFrom(string filePath)
        {
            return CreateFrom(filePath, null);
        }
        public static ResolvedAssemblyObject CreateFrom(string filePath, ResolveAssemblyDelegate resolveAssembly)
        {
            var _fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var _peReader = new PEReader(_fs);
            var _metaReader = _peReader.GetMetadataReader();

            return new ResolvedAssemblyObject(_fs, _peReader, _metaReader, resolveAssembly);
        }

        private ResolvedAssemblyObject(FileStream fs, PEReader pe, MetadataReader mr, ResolveAssemblyDelegate resolveAssembly) :
            base(fs, pe, mr, resolveAssembly)
        {
            Name = AssemblyName.GetAssemblyName(_fs.Name);

            TypesDeclared = GetDeclaredTypes();
        }

        /// <summary>
        /// Loads all the dependencies for the assembly using the resolveAssembly delegate
        /// </summary>
        public void LoadDependencies()
        {
            // traverse the list of references, and load them through the host.
            if (_references != null)
                return;

            _references = new List<AssemblyObject>();

            foreach (var item in GetAssemblyReferences())
            {
                if (_resolveAssembly != null)
                {
                    var resolvedAssembly = _resolveAssembly(item);
                    if (resolvedAssembly != null)
                    {
                        _references.Add(resolvedAssembly);
                    }
                    else
                    {
                        // at this point, we failed to resolve the assembly...
                        var unresolvedAssembly = UnresolvedAssemblyObject.Create(item);
                    }
                }
            }

            TypesReferenced = GetReferencedTypes();
            TypesExported = GetExportedTypes();
        }

        private List<DeclaredTypeObject> GetDeclaredTypes()
        {
            List<DeclaredTypeObject> types = new List<DeclaredTypeObject>();
            foreach (TypeDefinitionHandle item in _metaReader.TypeDefinitions)
            {
                types.Add(DeclaredTypeObject.CreateFrom(this, _metaReader.GetTypeDefinition(item)));
            }
            return types;
        }

        private List<ReferencedTypeObject> GetReferencedTypes()
        {
            List<ReferencedTypeObject> types = new List<ReferencedTypeObject>();
            foreach (TypeReferenceHandle item in _metaReader.TypeReferences)
            {
                

                types.Add(ReferencedTypeObject.CreateFrom(this, _metaReader.GetTypeReference(item), _resolveAssembly));
            }
            return types;
        }

        private List<ExportedTypeObject> GetExportedTypes()
        {
            List<ExportedTypeObject> types = new List<ExportedTypeObject>();
            foreach (ExportedTypeHandle item in _metaReader.ExportedTypes)
            {
                types.Add(ExportedTypeObject.CreateFrom(this, _metaReader.GetExportedType(item), _resolveAssembly));
            }

            return types;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
