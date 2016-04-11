﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Dune.util;

namespace Dune
{
    class Shell
    {

        static string[][] help = {
                                  new string[] {"Command", "Abreviation", "Description"},
                                  new string[] {"analyze <argument>", "a", "Analyzes the given arguments and returns the possible alternatives found by the programm in the current state."},
                                  new string[] {"help", "?", "Prints all commands and the according description."},
                                  new string[] {"quit", "q", "Quits the shell and will terminate the program."}
                                 };

        public static void showShell()
        {
            bool quit = false;
            while (!quit)
            {
                System.Console.Write("Dune>");
                string input = System.Console.ReadLine();
                int firstSpace = input.IndexOf(' ');
                string command;
                string arguments = "";
                if (firstSpace < 0)
                {
                    command = input;
                }
                else
                {
                    command = input.Substring(0, firstSpace);
                    arguments = input.Substring(firstSpace, input.Length - firstSpace).Trim();
                }

                switch (command.ToLower())
                {
                    case "getClassesWithName":
                    case "g":
                        foreach (DuneClass f in XMLParser.getClassesWithName(arguments))
                        {
                            System.Console.WriteLine(f.getFeatureName());
                        }
                        break;
                    case "analyze":
                    case "a":
                        DuneFeature df = findFeature(arguments);
                        List<string> result = XMLParser.getVariability(df);

                        if (result != null)
                        {
                            foreach (string s in result)
                            {
                                System.Console.WriteLine(s);
                            }
                            System.Console.WriteLine("Found " + result.Count + " possible alternative(s).");
                        }
                        else
                        {
                            System.Console.WriteLine("The class wasn't found.");
                        }
                        break;
                    case "fileAnalyzation":
                    case "f":
                        StreamReader inputFile = new System.IO.StreamReader(Program.DEBUG_PATH + "classesInDiffusion.txt");
                        StreamReader compFile = new System.IO.StreamReader(Program.DEBUG_PATH + "minimalSetClasses.txt");
                        StreamWriter output = new System.IO.StreamWriter(Program.DEBUG_PATH + "analyzation.txt");
                        StreamWriter positives = new System.IO.StreamWriter(Program.DEBUG_PATH + "positives.txt");

                        List<List<string>> globalResult = new List<List<string>>();

                        while (!inputFile.EndOfStream)
                        {
                            String line = inputFile.ReadLine();
                            if (!line.Trim().Equals(""))
                            {
                                List<string> analyzationResult = XMLParser.getVariability(line);
                                if (analyzationResult != null)
                                {
                                    globalResult.Add(analyzationResult);
                                }
                                else
                                {
                                    globalResult.Add(new List<string>());
                                }
                            }
                        }

                        int c = 0;
                        int foundMin = 0;
                        int notFound = 0;
                        while (!compFile.EndOfStream)
                        {
                            String l = compFile.ReadLine();

                            if (!l.Trim().Equals(""))
                            {
                                switch (containsName(l, globalResult.ElementAt(c)))
                                {
                                    case 1:
                                        foundMin++;
                                        break;
                                    case 0:
                                        foundMin++;
                                        output.WriteLine("This classes name was found: " + l);
                                        break;
                                    case -1:
                                        notFound++;
                                        output.WriteLine(l);
                                        break;
                                }
                            }
                            else
                            {
                                output.WriteLine(foundMin + "; " + notFound + "; " + globalResult.ElementAt(c).Count);
                                foundMin = 0;
                                notFound = 0;
                                c++;
                            }
                        }

                        // Write the whole set of positives in a file
                        foreach (List<string> results in globalResult)
                        {
                            foreach (string localResult in results)
                            {
                                positives.WriteLine(localResult);
                            }
                            positives.WriteLine();
                        }

                        output.Flush();
                        output.Close();
                        inputFile.Close();
                        compFile.Close();
                        positives.Flush();
                        positives.Close();
                        break;
                    case "help":
                    case "?":
                        printHelp();
                        break;
                    case "quit":
                    case "q":
                        quit = true;
                        break;
                }
            }
        }

        /// <summary>
        /// This method analyzes the given template by calling another helper-method.
        /// </summary>
        /// <param name="template">the template to be analyzed</param>
        /// <param name="refersto">the global <code>RefersToAliasing</code></param>
        /// <param name="d">the <code>DuneClass</code> the template is related to</param>
        /// <returns>the <code>TemplateTree</code> including the whole information about the template</returns>
        private TemplateTree analyzeTemplate(string template, RefersToAliasing refersto, DuneClass d)
        {
            TemplateTree tt = new TemplateTree();
            analyzeTemplate(template, refersto, d, tt);

            return tt;
        }

        /// <summary>
        /// This method analyzes the given template and adds the information to the given <code>TemplateTree</code>.
        /// </summary>
        /// <param name="template">the template to be analyzed</param>
        /// <param name="refersto">the global <code>RefersToAliasing</code></param>
        /// <param name="d">the <code>DuneClass</code> the template is related to</param>
        /// <param name="tt">the template tree in which the information will be saved</param>
        /// <returns>the <code>TemplateTree</code> including the whole information about the template</returns>
        private TemplateTree analyzeTemplate(string template, RefersToAliasing refersto, DuneClass d, TemplateTree tt)
        {
            string[] args = splitTemplate(template);
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                // Firstly, check if the argument is a number
                double output;
                bool isNumber = Double.TryParse(arg, out output);

                if (isNumber)
                {
                    tt.addNumericValue(arg);
                    continue;
                }

                // Secondly, check if the given argument is an enum value, a class or even a method is invoked
                bool classOrEnum = false;
                string method = arg.Contains("(") ? arg.Substring(arg.LastIndexOf("::"), arg.LastIndexOf(")") + 1 - arg.LastIndexOf("::")) : "";

                // Check if enum value; Therefore, the argument has to contain '::' because of <class>::<enumvalue>
                if (arg.Contains("::"))
                {
                    string value = arg.Substring(arg.LastIndexOf("::") + 2, arg.Length - arg.LastIndexOf("::") - 3).Trim();
                    foreach (DuneEnum de in XMLParser.enums)
                    {
                        foreach (DuneEnumValue dev in de.getValueObjects())
                        {
                            if (dev.getFeatureNameWithoutTemplateAndNamespace().Equals(value))
                            {
                                // Append to the template tree
                                // TODO: Check if this works;
                                classOrEnum = true;
                                tt.addInformation(dev);

                            }
                        }
                    }
                }

                if (classOrEnum && method.Equals(""))
                {
                    continue;
                }

                // Check if class; Here, the argument has not to contain '::' because some classes could be in the same namespace as the simulation

                string className = arg;
                if (className.Contains("::"))
                {
                    className = className.Substring(className.LastIndexOf("::") + 2, className.Length - className.LastIndexOf("::") - 3);
                }

                if (className.Contains("<"))
                {
                    int index = className.IndexOf('<');
                    string temp = className.Substring(index, className.LastIndexOf('>') - index);
                    className = className.Substring(0, index);
                    DuneClass classObject = XMLParser.getFeature(className);
                    if (classObject != null)
                    {
                        analyzeTemplate(temp, refersto, classObject, tt);
                    }
                    else
                    {
                        Console.WriteLine("Class " + className + " not found...");
                    }
                }

                if (className.Contains(" "))
                {
                    Console.WriteLine("Non-obvious case");
                }

                if (XMLParser.nameWithoutPackageToDuneFeatures.ContainsKey(className))
                {
                    classOrEnum = true;
                    tt.addInformation(className);
                }


                if (classOrEnum && method.Equals(""))
                {
                    continue;
                }

                // If no other case matches then it has to be an alias; A method should also be an alias (TODO: Also analyze the class and template in which the method appears in)
                string templateArgument = d.getTemplateArgument(i);
                if (templateArgument != null)
                    refersto.add(templateArgument, arg);
                else
                    Console.WriteLine("Non-obvious case");


            }
            return tt;
        }

        /// <summary>
        /// Splits the template and returns the single arguments in an array.
        /// </summary>
        /// <param name="template">the template</param>
        /// <returns>the arguments in an array</returns>
        private string[] splitTemplate(string template)
        {
            LinkedList<string> args = new LinkedList<string>();
            // Ommit '<' and '>' by beginning with the character on position 1(instead of 0)
            string arg = "";
            int level = 0;
            for (int i = 1; i < template.Length - 1; i++)
            {
                switch (template[i])
                {
                    case ',':
                        if (level == 0)
                        {
                            args.AddLast(arg);
                        }
                        else
                        {
                            arg += template[i];
                        }

                        break;
                    case '<':
                        level++;
                        arg += template[i];
                        break;
                    case '>':
                        level--;
                        arg += template[i];
                        break;
                    default:
                        arg += template[i];
                        break;
                }
            }

            return args.ToArray();
        }

        /// <summary>
        /// This method tries to find the right class. If multiple classes are found, the user has to select one. This method returns <code>null</code> if no class has been found or the input is invalid.
        /// </summary>
        /// <param name="feature">the class to search for</param>
        /// <returns>the selected <code>DuneClass</code>. <code>null</code> is returned if no class has been found or the input is invalid</returns>
        private static DuneFeature findFeature(string feature)
        {
            List<DuneFeature> dfs = XMLParser.getAllFeaturesByName(feature);

            if (dfs.Count == 0)
            {
                return null;
            }

            // If there is only one choice, the selection is obvious
            if (dfs.Count == 1)
            {
                return dfs.ElementAt(0);
            }

            System.Console.WriteLine("Multiple classes were found. Please select one by entering the number.");

            int count = 0;
            // In this case multiple classes were found
            foreach (DuneClass df in dfs)
            {
                System.Console.WriteLine(count + ": " + df.getFeatureName());
                count++;
            }

            System.Console.Write("Which one do you want to choose? ");
            string input = System.Console.ReadLine().Trim();
            int selected = 0;
            if (Int32.TryParse(input, out selected) && selected >= 0 && selected < count)
            {
                return dfs.ElementAt(selected);
            }
            else
            {
                System.Console.WriteLine("Selection not valid...aborting.");
                return null;
            }


        }

        /// <summary>
        /// Returns if the given name is included in the array either with the template or without.
        /// </summary>
        /// <param name="name">the name of the feature to search for</param>
        /// <param name="array">the array to search in</param>
        /// <returns><code>1</code> if the name including the template is found in the array; <code>0</code> if only the name is found; <code>-1</code> otherwise</returns>
        private static int containsName(string name, List<string> array)
        {
            if (array.Contains(name))
            {
                return 1;
            }
            else
            {
                if (name.Contains('<'))
                {
                    name = name.Substring(0, name.IndexOf('<'));
                }

                foreach (string comp in array)
                {
                    if (comp == null || comp.Equals("") || !comp.Contains('<'))
                    {
                        continue;
                    }

                    string compName = comp.Substring(0, comp.IndexOf('<'));
                    if (name.Equals(compName))
                    {
                        return 0;
                    }
                }
            }
            return -1;
        }

        static void printHelp()
        {
            foreach (string[] s in help)
            {
                System.Console.Write(s[0]);
                for (int i = 1; i < s.Length; i++)
                {
                    System.Console.Write("\t \t" + s[i]);
                }
                System.Console.WriteLine();
            }
        }
    }
}
