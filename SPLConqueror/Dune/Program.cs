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

        public static Logger infoLogger = new DuneAnalyzationLogger(DEBUG_PATH + "analyzation.log");


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
            bool inputHasTemplate = false;
            

            List<String> alternatives = new List<string>();
            DuneFeature importantClass = null;
            TemplateTree treeOfInterest = new TemplateTree();

            // split input in name and template parameters
            String name = "";
            String[] templateDefinedByUser = new String[0];

            if (input.Contains('<'))
            {
                inputHasTemplate = true;

                name = input.Substring(0, input.IndexOf('<')).Trim();
                templateDefinedByUser = getTemplateParts(input);
            }
            else
            {
                name = input; 
            }


            // Search for internal representations of the given class...
            List<DuneClass> allOthers = new List<DuneClass>();
            foreach (DuneClass others in XMLParser.featuresWithPublicMethods)
            {
                if (others.getFeatureNameWithoutTemplate().Equals(name))
                {
                    importantClass = others;
                    allOthers.Add(others);
                }
            }

            if (allOthers.Count > 1)
            {
                infoLogger.log("Potential error in getAlternativesRecursive() in the identification of the DuneClass of the given class for " + input+".  ");
                infoLogger.logLine("More than one internal class could match the given one.");
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

                    if (templateOfClass[i].declmame_cont.Trim().Length == 0)
                    {
                        if (mapping.ContainsKey(templateOfClass[i].deftype_cont))
                        {
                            mapping.Add(templateOfClass[i].deftype_cont + "_" + i, templateDefinedByUser[i]);
                        }
                        else
                        {
                            mapping.Add(templateOfClass[i].deftype_cont, templateDefinedByUser[i]);
                        }
                    }
                    else
                    {
                        if (mapping.ContainsKey(templateOfClass[i].declmame_cont))
                        {
                            mapping.Add(templateOfClass[i].declmame_cont + "_" + i, templateDefinedByUser[i]);
                        }
                        else
                        {
                            mapping.Add(templateOfClass[i].declmame_cont, templateDefinedByUser[i]);
                        }
                    }
                }

                String s = cont;

            }

            if (importantClass == null)
            {
                infoLogger.log("Potential error in getAlternativesRecursive() in the identification of the DuneClass of the given class for " + input + ".  ");
                infoLogger.logLine("No internal representation for the given class could be found.");
                return new List<String>();
                //System.Environment.Exit(1);
            }

            Dictionary<String, DuneFeature> alternativesFirstLevel = ((DuneFeature)importantClass).getVariability();
            List<String> alternativesFirstLevelWithConcreteParameters = new List<string>();

            if (inputHasTemplate)
            {
                foreach(KeyValuePair<String,DuneFeature> element in alternativesFirstLevel)
                {

                    if(((DuneClass)element.Value).templateElements.Count > 0)
                    {
                        DuneClass alternative = (DuneClass)element.Value;
                        String alternativStringWithUserInput = element.Value.getFeatureNameWithoutTemplate()+"<";
                        for (int i = 0; i < alternative.templateElements.Count; i++)
                        {

                            String nameTemplateParameter = alternative.templateElements[i].declmame_cont;

                            if (nameTemplateParameter.Trim().Length == 0)
                            {
                                if (mapping.ContainsKey(alternative.templateElements[i].deftype_cont))
                                {
                                    alternativStringWithUserInput += mapping[alternative.templateElements[i].deftype_cont];
                                }
                                else
                                {
                                    if (alternative.templateElements[i].deftype_cont.Length > 0)
                                    {
                                        if (alternative.templateElements[i].defval_cont.Length > 0)
                                            if (mapping.ContainsKey(alternative.templateElements[i].defval_cont))
                                                alternativStringWithUserInput += mapping[alternative.templateElements[i].defval_cont];
                                            else
                                                alternativStringWithUserInput += alternative.templateElements[i].defval_cont;
                                        else
                                            alternativStringWithUserInput += alternative.templateElements[i].deftype_cont;
                                    }
                                    else
                                    {
                                        String deftype_cont = alternative.templateElements[i].deftype_cont;
                                        Double res;
                                        if (Double.TryParse(deftype_cont, out res))
                                        {
                                            alternativStringWithUserInput += deftype_cont;
                                        }
                                        else
                                            alternativStringWithUserInput += "??" + nameTemplateParameter + "??";
                                    }
                                }
                            }
                            else
                            {
                                if (mapping.ContainsKey(nameTemplateParameter))
                                {
                                    alternativStringWithUserInput += mapping[nameTemplateParameter];
                                }
                                else
                                {
                                    if (alternative.templateElements[i].defval_cont.Length > 0)
                                        alternativStringWithUserInput += alternative.templateElements[i].defval_cont;
                                    else
                                        alternativStringWithUserInput += "??" + nameTemplateParameter + "??";
                                }
                            }

                            if (i < alternative.templateElements.Count - 1)
                                alternativStringWithUserInput += ",";
                            else
                                alternativStringWithUserInput += ">";
                        }
                        alternativesFirstLevelWithConcreteParameters.Add(alternativStringWithUserInput);
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

                    if (element.Value.GetType() == typeof(DuneEnum))
                    {

                        alternativesFirstLevelWithConcreteParameters.Add(element.Key);
                    }
                    else
                    {

                        DuneClass alternative = (DuneClass)element.Value;

                        String alternativStringWithUserInput = alternative.getFeatureNameWithoutTemplate();

                        if (alternative.templateElements.Count > 0)
                        {
                            alternativStringWithUserInput += " < ";

                            for (int i = 0; i < alternative.templateElements.Count; i++)
                            {

                                String nameTemplateParameter = alternative.templateElements[i].declmame_cont;

                                if (mapping.ContainsKey(nameTemplateParameter))
                                {
                                    alternativStringWithUserInput += mapping[nameTemplateParameter];
                                }
                                else
                                {
                                    if (alternative.templateElements[i].defval_cont.Length > 0)
                                        alternativStringWithUserInput += alternative.templateElements[i].defval_cont;
                                    else
                                        alternativStringWithUserInput += "??" + nameTemplateParameter + "??";
                                }


                                if (i < alternative.templateElements.Count - 1)
                                    alternativStringWithUserInput += ",";
                                else
                                    alternativStringWithUserInput += ">";
                            }

                        }

                        alternativesFirstLevelWithConcreteParameters.Add(alternativStringWithUserInput);
                    }
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
                        infoLogger.logLine("Multiple classes found that could match with the input");
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
