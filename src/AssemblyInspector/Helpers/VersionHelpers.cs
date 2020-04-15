using System;

namespace AssemblyInspector.Helpers
{
    public static class VersionHelpers
    {
        /// <summary>
        /// Will return true if <paramref name="firstVersion"/> is a closer match to <paramref name="targetVersion"/> than <paramref name="secondVersion"/>
        /// </summary>
        /// <param name="firstVersion"></param>
        /// <param name="secondVersion"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        public static bool IsBetterThan(this Version firstVersion, Version secondVersion, Version targetVersion)
        {
            /*
             We need to see if 4.0.0.2 is a better match to 4.0.0.0 than 4.0.1.0
             hint (it is).
             */

            //compute the 4_00_00_02 - 4_00_00_00 == 2
            //compute the 4_00_01_00 - 4_00_00_00 == 100
            // The lower the diff, the better the match.

            int firstNumber = firstVersion.GetVersionAsNumber() - targetVersion.GetVersionAsNumber();
            int secondNumber = secondVersion.GetVersionAsNumber() - targetVersion.GetVersionAsNumber();

            return firstNumber < secondNumber; ;
        }

        public static int GetVersionAsNumber(this Version v)
        {
            return v.Major * 1_00_00_00 + v.Minor * 1_00_00 + v.Build * 1_00 + v.Revision;
        }
    }
}
