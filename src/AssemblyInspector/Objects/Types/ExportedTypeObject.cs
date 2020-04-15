using AssemblyInspector.Helpers;
using AssemblyInspector.Objects.Assembly;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyInspector.Objects.Types
{
    public class ExportedTypeObject : BaseObject
    {
        public string Name => AssemblyHelpers.GetTypeName(_metaReader, _exportedType);
        public bool IsForwarder => _exportedType.IsForwarder;

        public AssemblyObject AssemblyForwardedTo { get; private set; }

        private ExportedType _exportedType;

        public static ExportedTypeObject CreateFrom(ResolvedAssemblyObject ao, ExportedType exportedType)
        {
            return CreateFrom(ao, exportedType, null);
        }

        public static ExportedTypeObject CreateFrom(ResolvedAssemblyObject ao, ExportedType exportedType, ResolveAssemblyDelegate resolveAssembly)
        {
            ExportedTypeObject to = new ExportedTypeObject(ao._fs, ao._peReader, ao._metaReader, exportedType, resolveAssembly);
            return to;
        }

        public ExportedTypeObject(FileStream fs, PEReader pe, MetadataReader mr, ExportedType exportedType, ResolveAssemblyDelegate resolveAssembly) :
            base(fs, pe, mr, resolveAssembly)
        {
            _exportedType = exportedType;

            AssemblyForwardedTo = _resolveAssembly(ToAssemblyName(AssemblyHelpers.GetAssemblyReferenceForExportedType(_metaReader, exportedType)));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
