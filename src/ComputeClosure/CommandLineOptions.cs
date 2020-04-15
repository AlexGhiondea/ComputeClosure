
using CommandLine.Attributes;
using CommandLine.Attributes.Advanced;
using System.Collections.Generic;

namespace ComputeClosure
{
    public class CommandLineOptions
    {
        [RequiredArgument(0, "file", "The root file to scan")]
        public string RootFile { get; set; }


        [RequiredArgument(1, "libPath", "The folder to use when resolving references", true)]
        public List<string> LibFolder { get; set; }
    }
}
