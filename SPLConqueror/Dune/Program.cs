using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;

namespace Dune
{
    static class Program
    {

        static String PATH = @"D:\owncloud\all.xml";
        const String XML_LOCATION = @"doc\doxygen\xml\";
        public const bool INCLUDE_CLASSES_FROM_STD = false;

        /// <summary>
        /// Der Einstiegspunkt für die Anwendung, falls das Dune FeatureModell erstellt werden sollte.
        /// </summary>
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            // If the path was not specified until now, the path is taken from the arguments
            if (PATH.Equals(@""))
            {
                if (args.Length > 0)
                {
                    PATH = args[0];
                }
                else
                {
                    System.Console.WriteLine("No path passed as argument. Aborting...");
                    return;
                }
            }
            XMLParser.parse(PATH);

            // Force the gc to remove all the unneeded data in memory - May be remove if it does not bring any improvement
            System.GC.Collect();
            GC.WaitForPendingFinalizers();

            // Needed for debugging purposes.
            System.Console.ReadKey();
        }

    }
}
