﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using SPLConqueror_Core;
using Dune.util;

namespace Dune
{
    static class Program
    {

        // The path of the xml-file to read the dependencies from
        // Please adjust it, as I have not found a solution not to do so...
        static String PATH = @"D:\HiWi\SPLConqueror_Dune\all1.xml";

        public static String DEBUG_PATH = @"D:\HiWi\DebugOutput\";

        public const bool INCLUDE_CLASSES_FROM_STD = false;

        public static char SPLIT_SYMBOL = '=';

        public static bool USE_DUCK_TYPING = true;

        public static bool INCLUDE_CONSTRUCTORS = false;


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
                //Console.SetOut(writer);

                
            }catch(IOException e) {
                TextWriter errorWriter = Console.Error;
                errorWriter.WriteLine(e.Message);
            }
            XMLParser.parse(PATH);

            Shell.showShell();
            

            System.Console.WriteLine("Press a button to close the window.");
            System.Console.ReadKey();
        }


        public static String[] getTemplateParts(String input)
        {
            List<char> reverseName = input.Reverse().ToList();
            int closingIndex = input.Count();
            for (int i = 0; i < reverseName.Count; i++)
            {
                if (reverseName[i].Equals('>'))
                {
                    closingIndex = i;
                    break;
                }

            }
            int templateLength = (input.Count()) + (closingIndex) - input.IndexOf('<') - 2;

            String[] templateDefinedByUser = input.Substring(input.IndexOf('<') + 1, templateLength).Split(',');

            return templateDefinedByUser;
        }

        public static List<String> getAlternativesRecursive(String input)
        {
            input = input.Trim();

            if (input.Contains("Dune::PDELab::QkLocalFiniteElementMap"))
            {
            }

           

            List<String> alternatives = new List<string>();

            DuneFeature importantClass = null;

            TemplateTree treeOfInterest = new TemplateTree();

            String name = "";
            String[] templateDefinedByUser = new String[0];

            if (input.Contains('<'))
            {
                name = input.Substring(0, input.IndexOf('<')).Trim();
                templateDefinedByUser = getTemplateParts(input);
            }
            else
            {
                name = input; 
            }



           


            List<DuneClass> allOthers = new List<DuneClass>();
            foreach (DuneClass others in XMLParser.features)
            {
                if (others.getFeatureNameWithoutTemplate().Contains("Dune::ALUGrid"))
                {
                    var x = others.getFeatureNameWithoutTemplate();

                }


                if (others.getFeatureNameWithoutTemplate().Equals(name))
                {
                    importantClass = others;
                    allOthers.Add(others);
                    Console.WriteLine("");
                }
            }

            if (allOthers.Count > 1)
            {
                Console.Write("Potential error in getAlternativesRecursive() in the identification of the DuneClass of the given class for " + input+".  ");
                Console.WriteLine("more than one internal class could macht the given one");
                //System.Environment.Exit(1);

                importantClass = getDuneClassByNumberOfTemplateParameters(allOthers, templateDefinedByUser.Count());

            }


            // mapping from the default placeholder strings of the template in the strings of the given input template
            Dictionary<String, String> mapping = new Dictionary<string, string>();

            if (importantClass == null)
            {
                // input is the value of an enum
                foreach(DuneEnum currEnum in XMLParser.enums){
                    bool found = false;
                    foreach (String s in currEnum.getValues())
                    {
                        if (s.Equals(input))
                        {
                            importantClass = currEnum;
                        }
                    }
                }
            }
            else
            {

                List<TemplateTree> templateOfClass = ((DuneClass)importantClass).templateElements;

                String cont = "";
                for (int i = 0; i < templateOfClass.Count; i++)
                {
                    cont += templateOfClass[i].declmame_cont + " | ";

                    if (mapping.ContainsKey(templateOfClass[i].declmame_cont))
                    {
                        mapping.Add(templateOfClass[i].declmame_cont+"_"+i, templateDefinedByUser[i]);
                    }
                    else
                    {
                        mapping.Add(templateOfClass[i].declmame_cont, templateDefinedByUser[i]);
                    }
                }

                String s = cont;

                // we start with 1 because element is the name of the class
                int offset = 0;
                for (int i = 1; i < templateDefinedByUser.Length; i++)
                {
                    String token = templateDefinedByUser[i];
                    if (token.Equals(">") || token.Equals("<"))
                    {
                        offset += 1;
                        continue;
                    }

                    


                }

            }

            if (importantClass == null)
            {
                Console.Write("Potential error in getAlternativesRecursive() in the identification of the DuneClass of the given class for " + input + ".  ");
                Console.WriteLine("no internal representation for the given class could be found");
                //System.Environment.Exit(1);
            }

            Dictionary<String, DuneFeature> alternativesFirstLevel = ((DuneFeature)importantClass).getVariability();
            List<String> alternativesFirstLevelWithConcreteParameters = new List<string>();

            if (input.Contains('<'))
            {
                foreach(KeyValuePair<String,DuneFeature> element in alternativesFirstLevel)
                {

                    if (element.Key.Contains('<'))
                    {

                        String nameAlternative = element.Key.Substring(0, input.IndexOf('<')).Trim();

                        String[] templateOfAlternative = getTemplateParts(element.Key);


                        String alternativeWithUserInput = nameAlternative;

                        for (int j = 0; j < templateOfAlternative.Length; j++)
                        {
                            String token = templateOfAlternative[j].Trim();
                            if (mapping.ContainsKey(token))
                            {
                                alternativeWithUserInput += mapping[token];
                            }
                            else
                            {
                                if (((DuneClass)element.Value).templateElements.Count > j)
                                {
                                    TemplateTree tree = element.Value.tempTree.getElement(j);
                                    TemplateTree te = ((DuneClass)element.Value).templateElements[j];

                                    if (te.isNotParsable)
                                    {

                                    }
                                    else
                                    {

                                        if (te.defval_cont != "")
                                        {
                                            alternativeWithUserInput += te.defval_cont;
                                        }
                                        else
                                        {
                                            double d;
                                            if (Double.TryParse(token, out d))
                                            {
                                                alternativeWithUserInput += token;
                                            }
                                            alternativeWithUserInput += "??" + token + "??";
                                        }
                                    }
                                }
                                else
                                {
                                    alternativeWithUserInput += "??__??";
                                }


                            }
                            if (j < templateOfAlternative.Length - 1)
                                alternativeWithUserInput += ",";
                            else
                                alternativeWithUserInput += ">";

                        }


                        alternativesFirstLevelWithConcreteParameters.Add(alternativeWithUserInput);
                    }
                    else
                    {
                        alternativesFirstLevelWithConcreteParameters.Add(element.Key);
                    }
                }
            }
            else
            {
                foreach(KeyValuePair<String,DuneFeature> element in alternativesFirstLevel)
                {
                    alternativesFirstLevelWithConcreteParameters.Add(element.Key);
                }
            }


            return alternativesFirstLevelWithConcreteParameters;
        }

        private static DuneClass getDuneClassByNumberOfTemplateParameters(List<DuneClass> allOthers, int p)
        {
            DuneClass f = null;
            
            for(int i = 0; i < allOthers.Count; i++)
            {
                if (allOthers[i].getTemplateArgumentCount().getLowerBound() <= p && allOthers[i].getTemplateArgumentCount().getUpperBound() >= p)
                    if (f == null)
                        f = allOthers[i];
                    else
                        Console.WriteLine("Multiple classes found that could match with the input");
            }

            return f;
        }



        internal static void generateVariabilityModel(Dictionary<string, List<string>> resultsByVariabilityPoints)
        {
            VariabilityModel varModel = new VariabilityModel("DuneCaseStudy");
            

            foreach (KeyValuePair<String, List<String>> resultForOne in resultsByVariabilityPoints)
            {
                BinaryOption alternativeParent = new BinaryOption(varModel, "group" + resultForOne.Key);
                alternativeParent.Optional = false;
                alternativeParent.Parent = varModel.Root;
                alternativeParent.OutputString = "NoOutput";
                varModel.addConfigurationOption(alternativeParent);

                List<BinaryOption> elementsOfGroup = new List<BinaryOption>();
                foreach (String alternative in resultForOne.Value)
                {
                    BinaryOption oneAlternative = new BinaryOption(varModel, alternative);
                    oneAlternative.Optional = false;
                    oneAlternative.OutputString = alternative;
                    oneAlternative.Parent = alternativeParent;
                    varModel.addConfigurationOption(oneAlternative);
                    elementsOfGroup.Add(oneAlternative);
                }

                foreach (BinaryOption alternative in elementsOfGroup)
                {
                    foreach (BinaryOption other in elementsOfGroup)
                    {
                        if (alternative.Equals(other))
                            continue;

                        alternative.Excluded_Options.Add(new List<ConfigurationOption>() { other });
                    }
                }
            }

            varModel.saveXML(DEBUG_PATH + varModel.Name+".xml");
        }
    }
}
