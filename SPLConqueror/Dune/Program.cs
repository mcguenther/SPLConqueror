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
        static String PATH = @"D:\HiWi\SPLConqueror_Dune\all1.xml";

        public static String DEBUG_PATH = @"D:\HiWi\DebugOutput\";

        public const bool INCLUDE_CLASSES_FROM_STD = false;


        public static bool USE_DUCK_TYPING = false;


        /// <summary>
        /// The main-method of the Dune-plugin. This calls the corresponding <code>XMLParser</code>-methods.
        /// </summary>
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");


            // If the path is not given as argument, use the path specified in this file.
            if (args.Length > 0)
            {
                PATH = args[0];
                if (args.Length > 1)
                {
                    DEBUG_PATH = args[1];

                    // Add an additional directory separator if it was not included by the user.
                    DEBUG_PATH = DEBUG_PATH.EndsWith(Path.DirectorySeparatorChar.ToString()) ? DEBUG_PATH : DEBUG_PATH + Path.DirectorySeparatorChar;
                }

            }
            else
            {
                System.Console.WriteLine("No path passed as argument. Aborting...");
                return;
            }

            

            try {
                var writer = new StreamWriter(DEBUG_PATH+"out.txt");
                // Redirect standard output from the console to the output file.
                Console.SetOut(writer);

                //Console.SetOut(

            }catch(IOException e) {
                TextWriter errorWriter = Console.Error;
                errorWriter.WriteLine(e.Message);
            }
            XMLParser.parse(PATH);

            // Needed for debugging purposes.
            Shell.showShell();
            System.Console.WriteLine("Press a button to close the window.");
            System.Console.ReadKey();
        }

    }
}
