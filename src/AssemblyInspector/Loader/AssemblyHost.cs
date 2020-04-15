using AssemblyInspector.Objects.Assembly;
using OutputColorizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AssemblyInspector.Loader
{
    public class AssemblyHost
    {
        // we need to have a list of probing paths to use.

        private static Action<string, AssemblyName, ResolvedAssemblyResultEnum> _AssemblyLoaded = (sourceAssembly, targetAssembly, loadResult) =>
        {
            Colorizer.WriteLine("Reference from [Magenta!{0}] -> [Cyan!{1}]", Path.GetFileNameWithoutExtension(sourceAssembly), targetAssembly);

            if (loadResult == ResolvedAssemblyResultEnum.NoMatch)
            {
                string error = $"[Red!Error: Missing dependency]: '[Magenta!{ Path.GetFileNameWithoutExtension(sourceAssembly)}]' -> [Cyan!{targetAssembly}]";
                Colorizer.WriteLine(error);
            }
            else if (loadResult == ResolvedAssemblyResultEnum.BestMatch)
            {
                Colorizer.WriteLine("  [DarkYellow!Warning]: Using best match version from [Yellow!{0}]", new Uri(targetAssembly.CodeBase).LocalPath);
            }
            else
            {
                Colorizer.WriteLine("  Using assembly with exact match from [Yellow!{0}]", new Uri(targetAssembly.CodeBase).LocalPath);
            }
        };

        private static Action<AssemblyName, AssemblyName> _AssemblyLoadedVersionMismatch = (expectedAssembly, actualAssembly) =>
        {
            // we loaded a different assembly than what the reference was saying.
            string error = $"  [DarkYellow!Warning]: Loaded a different version that expected.\n    expected: [Yellow!{expectedAssembly.FullName}], \n    found  : [Yellow!{actualAssembly.FullName}]";
            Colorizer.WriteLine(error);
        };

        private AssemblyResolver _assemblyResolver;
        private Dictionary<AssemblyName, AssemblyObject> _loadedAssemblies;

        public static AssemblyHost Create(params string[] probingPaths)
        {
            return new AssemblyHost(new AssemblyResolver(probingPaths, _AssemblyLoaded, _AssemblyLoadedVersionMismatch));
        }
        public static AssemblyHost Create(IEnumerable<string> probingPaths)
        {
            return new AssemblyHost(new AssemblyResolver(probingPaths, _AssemblyLoaded, _AssemblyLoadedVersionMismatch));
        }

        private AssemblyHost(AssemblyResolver resolver)
        {
            _loadedAssemblies = new Dictionary<AssemblyName, AssemblyObject>(AssemblyNameComparer.Singleton);
            _assemblyResolver = resolver;
        }

        public AssemblyObject LoadAssembly(string assemblyFile)
        {
            if (File.Exists(assemblyFile))
            {
                var assembly = ResolvedAssemblyObject.CreateFrom(assemblyFile, new ResolveAssemblyDelegate(LoadAssembly));
                _loadedAssemblies[AssemblyName.GetAssemblyName(assemblyFile)] = assembly;

                assembly.LoadDependencies();
                return assembly;
            }

            return null;
        }

        public AssemblyObject LoadAssembly(AssemblyName assemblyName)
        {
            if (!_loadedAssemblies.ContainsKey(assemblyName))
            {
                // load the assembly.
                if (_assemblyResolver.TryResolveAssembly(assemblyName, out AssemblyName resolvedAssembly) != ResolvedAssemblyResultEnum.NoMatch)
                {
                    var assembly = ResolvedAssemblyObject.CreateFrom(new Uri(resolvedAssembly.CodeBase).LocalPath, new ResolveAssemblyDelegate(LoadAssembly));
                    _loadedAssemblies[assemblyName] = assembly;

                    assembly.LoadDependencies();
                }
                else
                {
                    return UnresolvedAssemblyObject.Create(assemblyName);

                    //return null;
                }
            }

            return _loadedAssemblies[assemblyName];
        }
    }
}
