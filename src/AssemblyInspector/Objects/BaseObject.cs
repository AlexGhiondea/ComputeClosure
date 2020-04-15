using AssemblyInspector.Objects.Assembly;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyInspector.Objects
{
    public abstract class BaseObject : IDisposable
    {
        protected internal FileStream _fs;
        protected internal PEReader _peReader;
        protected internal MetadataReader _metaReader;
        protected internal ResolveAssemblyDelegate _resolveAssembly;

        public BaseObject(FileStream fs, PEReader pe, MetadataReader mr, ResolveAssemblyDelegate resolveAssembly)
        {
            _fs = fs;
            _peReader = pe;
            _metaReader = mr;
            _resolveAssembly = resolveAssembly;
        }

        public void Dispose()
        {
            _metaReader = null;
            _peReader.Dispose();
            _fs.Dispose();
        }

        public AssemblyName ToAssemblyName(AssemblyReference ar)
        {
            AssemblyName result = new AssemblyName();
            result.CultureInfo = string.IsNullOrEmpty(_metaReader.GetString(ar.Culture)) ? null : new System.Globalization.CultureInfo(_metaReader.GetString(ar.Culture));
            result.Name = _metaReader.GetString(ar.Name);
            result.Version = ar.Version;
            result.SetPublicKeyToken(_metaReader.GetBlobBytes(ar.PublicKeyOrToken));

            return result;
        }

    }
}
