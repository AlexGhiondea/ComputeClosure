using AssemblyInspector.Helpers;
using AssemblyInspector.Objects.Assembly;
using OutputColorizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssemblyInspector.Loader
{
    public class AssemblyResolver
    {
        private List<string> _probingPaths;
        //TODO: support customizing this
        private string _assemblyExtension = ".dll";

        private Action<AssemblyName, AssemblyName> _assemblyVersionMismatchDelegate;

        private Action<string, AssemblyName, ResolvedAssemblyResultEnum> _assemblyLoadedDelegate;

        public AssemblyResolver(IEnumerable<string> probingPaths)
            : this(probingPaths, null, null)
        {
        }

        public AssemblyResolver(IEnumerable<string> probingPaths, Action<string, AssemblyName, ResolvedAssemblyResultEnum> assemblyLoadedDelegate, Action<AssemblyName, AssemblyName> assemblyVersionMismatch)
        {
            _probingPaths = new List<string>(probingPaths);
            _assemblyLoadedDelegate = assemblyLoadedDelegate;
            _assemblyVersionMismatchDelegate = assemblyVersionMismatch;
        }

        public ResolvedAssemblyResultEnum TryResolveAssembly(AssemblyName requestedAssembly, out AssemblyName resolvedAssembly)
        {
            resolvedAssembly = null;

            foreach (var libFolder in _probingPaths)
            {
                // we have to scan through all of them and find the best one
                string referenceWithPath = Path.Combine(libFolder, requestedAssembly.Name + _assemblyExtension);
                if (!File.Exists(referenceWithPath))
                {
                    //try loading exe.
                    referenceWithPath = Path.Combine(libFolder, requestedAssembly.Name + ".exe");
                }

                if (File.Exists(referenceWithPath))
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(referenceWithPath);

                    // stash the location where the assembly was loaded in the CodeBase property
                    an.CodeBase = Path.GetFullPath(referenceWithPath);

                    // if we found the explicit match, use that.
                    if (AssemblyNameComparer.Singleton.Equals(requestedAssembly, an))
                    {
                        resolvedAssembly = an;
                        return ResolvedAssemblyResultEnum.ExactMatch;
                    }

                    if (resolvedAssembly == null || an.Version.IsBetterThan(resolvedAssembly.Version, requestedAssembly.Version))
                    {
                        resolvedAssembly = an;
                    }
                }
            }

            return resolvedAssembly == null ? ResolvedAssemblyResultEnum.NoMatch : ResolvedAssemblyResultEnum.BestMatch;
        }

        public List<AssemblyName> ComputeClosure(string assemblyFile)
        {
            Stack<AssemblyName> newAssemblies = new Stack<AssemblyName>();
            HashSet<AssemblyName> visitedAssembliesWithVersion = new HashSet<AssemblyName>();

            GetReferencesFromFile(newAssemblies, visitedAssembliesWithVersion, assemblyFile);

            while (newAssemblies.Count > 0)
            {
                var curAssembly = newAssemblies.Pop();
                // AssemblyName an = AssemblyName.GetAssemblyName(currentFile);
                if (visitedAssembliesWithVersion.Contains(curAssembly, AssemblyNameComparer.Singleton))
                {
                    continue;
                }

                visitedAssembliesWithVersion.Add(curAssembly);

                GetReferencesFromFile(newAssemblies, visitedAssembliesWithVersion, curAssembly.CodeBase);
            }

            return visitedAssembliesWithVersion.ToList();
        }

        private void GetReferencesFromFile(Stack<AssemblyName> newAssemblies, HashSet<AssemblyName> visitedAssemblies, string currentFile)
        {
            var assemblyObj = ResolvedAssemblyObject.CreateFrom(currentFile);
            AssemblyName[] refs = assemblyObj.GetAssemblyReferences();

            foreach (var newFile in refs)
            {
                ResolvedAssemblyResultEnum resolveResult = TryResolveAssembly(newFile, out AssemblyName an);

                _assemblyLoadedDelegate?.Invoke(currentFile,
                    resolveResult == ResolvedAssemblyResultEnum.NoMatch ? newFile : an,
                    resolveResult);

                // If there is no match, move on to the next reference.
                if (resolveResult == ResolvedAssemblyResultEnum.NoMatch)
                {
                    continue;
                }

                if (!new AssemblyNameComparer().Equals(newFile, an))
                {
                    // we loaded a different assembly than what the reference was saying.
                    _assemblyVersionMismatchDelegate?.Invoke(newFile, an);

                    // we overwrite the one that was loaded because the closure is based on what is referenced, not what we end up loading.
                    newFile.CodeBase = an.CodeBase;
                    an = newFile;
                }

                // if we haven't seen this already
                if (!visitedAssemblies.Contains(an))
                {
                    newAssemblies.Push(an);
                }
            }
        }
    }
}
