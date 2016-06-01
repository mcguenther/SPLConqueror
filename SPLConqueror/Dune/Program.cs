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
                StreamWriter writer = new StreamWriter(DEBUG_PATH + "out.txt");
                writer.AutoFlush = true;
                // Redirect standard output from the console to the output file.
                Console.SetOut(writer);

                //Console.SetOut(

            }catch(IOException e) {
                TextWriter errorWriter = Console.Error;
                errorWriter.WriteLine(e.Message);
            }
            XMLParser.parse(PATH);

            List<String> alternativesFIM =  getAlternativesRecursive("Dune::PDELab::QkLocalFiniteElementMap < GV, GV::ctype , Real , degree > ");

            foreach (String t in alternativesFIM)
                Console.WriteLine(t);

            System.Environment.Exit(1);

            // Needed for debugging purposes.
            Shell.showShell();
            

            System.Console.WriteLine("Press a button to close the window.");
            System.Console.ReadKey();
        }


        public static List<String> getAlternativesRecursive(String input)
        {
            List<String> alternatives = new List<string>();

            DuneClass improtantClass = null;

            TemplateTree treeOfInterest = new TemplateTree();

            input = input.Replace(",","").Trim();
            while (input.Contains("  "))
            {
                input = input.Replace("  "," ");
            }
            String[] nameAndTemplateOfClassSplitted = input.Split(' '); 

            List<DuneClass> allOthers = new List<DuneClass>();
            foreach (DuneClass others in XMLParser.features)
            {
                if(others.getFeatureNameWithoutTemplate().Equals(nameAndTemplateOfClassSplitted[0]))
                {
                    improtantClass = others;
                    allOthers.Add(others);
                    Console.WriteLine("");
                }
            }
            if (allOthers.Count > 1 || improtantClass == null)
            {
                Console.WriteLine("Potentiel Error in getAlternativesRecursive() in the identification of the DuneClass of the given class ");
                System.Environment.Exit(1);
            }


            // mapping from the default placeholder strings of the templte in the strings of the given input template
            Dictionary<String, String> mapping = new Dictionary<string, string>();
            String[] templateInInput = improtantClass.implementingTemplate.Split(',');


            // we start with 1 because element is the name of the class
            int offset = 1;
            for (int i = 1; i < nameAndTemplateOfClassSplitted.Length; i++)
            {
                String token = nameAndTemplateOfClassSplitted[i];
                if (token.Equals(">") || token.Equals("<"))
                {
                    offset += 1;
                    continue;
                }

                mapping.Add(templateInInput[i - offset].Trim(), token);


            }



            List<String> alternativesFirstLevel = ((DuneFeature)improtantClass).getVariability(XMLParser.root);
            List<String> alternativesFirstLevelWithConcreteParameters = new List<string>();

            for (int i = 0; i < alternativesFirstLevel.Count; i++)
            {
                
                String[] splitted = alternativesFirstLevel[i].Substring(0,alternativesFirstLevel[i].Length-1).Split('<');
                if(splitted.Length > 2)
                {
                    Console.WriteLine("Potentiel Error in getAlternativesRecursive():: element in alternativesFirstLevel have a template hierarchy of more than one, see:: "+alternativesFirstLevel[i]);
                    System.Environment.Exit(1);
                }

                String newName = splitted[0]+"<";
                String[] templateElements = splitted[1].Split(',');
                for (int j = 0; j < templateElements.Length; j++)
                {
                    String token = templateElements[j].Trim();
                    if (mapping.ContainsKey(token))
                    {
                        newName += mapping[token];
                    }
                    else
                    {
                        newName += "??" + token + "??";
                    }
                    if (j < templateElements.Length - 1)
                        newName += ",";
                    else
                        newName += ">";
                }

                alternativesFirstLevelWithConcreteParameters.Add(newName);
            }


            return alternativesFirstLevelWithConcreteParameters;
        }


    }
}
