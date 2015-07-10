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
        // The path of the xml-file to read the dependencies from
        // Please adjust it, as I have not found a solution not to do so...
        static String PATH = @"all.xml";

        // Should be no longer needed
        const String XML_LOCATION = @"doc\doxygen\xml\";

        public const bool INCLUDE_CLASSES_FROM_STD = false;

        /// <summary>
        /// The main-method of the Dune-plugin. This calls the corresponding <code>XMLParser</code>-methods.
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
            // Until now (10.07) it wasn't helpfull - it will be removed in a future commit
            System.GC.Collect();
            GC.WaitForPendingFinalizers();

            
            // Needed for debugging purposes.
            System.Console.WriteLine("Press a button to close the window.");
            System.Console.ReadKey();
        }

    }
}
