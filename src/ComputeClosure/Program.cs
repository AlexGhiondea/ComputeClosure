using AssemblyInspector.Loader;
using CommandLine;
using ComputeClosure;
using OutputColorizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace ComputeClosure
{
    class Program
    {
        static List<string> errors = new List<string>();
        static CommandLineOptions options;
        private static AssemblyResolver _assemblyResolver;

        private static Action<string, AssemblyName, ResolvedAssemblyResultEnum> _AssemblyLoaded = (sourceAssembly, targetAssembly, loadResult) =>
        {
            Colorizer.WriteLine("Reference from [Magenta!{0}] -> [Cyan!{1}]", Path.GetFileNameWithoutExtension(sourceAssembly), targetAssembly);

            if (loadResult == ResolvedAssemblyResultEnum.NoMatch)
            {
                string error = $"[Red!Error: Missing dependency]: '[Magenta!{ Path.GetFileNameWithoutExtension(sourceAssembly)}]' -> [Cyan!{targetAssembly}]";
                errors.Add(error);
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
            errors.Add(error);
            Colorizer.WriteLine(error);
        };


        static void Main(string[] args)
        {
            if (!Parser.TryParse(args, out options))
            {
                return;
            }

            _assemblyResolver = new AssemblyResolver(options.LibFolder, _AssemblyLoaded,  _AssemblyLoadedVersionMismatch);

            Stack<AssemblyName> newAssemblies = new Stack<AssemblyName>();
            HashSet<AssemblyName> visitedAssembliesWithVersion = new HashSet<AssemblyName>();

            var assemblies = _assemblyResolver.ComputeClosure(options.RootFile);

            Console.WriteLine("Closure:");
            Console.WriteLine("=====");
            foreach (var item in assemblies.OrderBy(s => s.Name))
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("=====");

            foreach (var item in errors)
            {
                Colorizer.WriteLine(item);
            }
        }
    }
}
