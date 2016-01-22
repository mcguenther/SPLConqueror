using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
                        foreach (DuneFeature f in XMLParser.getClassesWithName(arguments))
                        {
                            System.Console.WriteLine(f.getClassName());
                        }
                        break;
                    case "analyze":
                    case "a":
                        List<string> result = XMLParser.getVariability(arguments);
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
                        StreamReader inputFile = new System.IO.StreamReader(@"D:\HiWi\DebugOutput\classesInDiffusion.txt");
                        StreamReader compFile = new System.IO.StreamReader(@"D:\HiWi\DebugOutput\minimalSetClasses.txt");
                        StreamWriter output = new System.IO.StreamWriter(@"D:\HiWi\DebugOutput\analyzation.txt");

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
                                //if (!globalResult.Contains(l))
                                //{
                                //    output.WriteLine(l);
                                //}

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
                                output.WriteLine(foundMin + "; " + globalResult.ElementAt(c).Capacity + "; " + notFound);
                                foundMin = 0;
                                notFound = 0;
                                c++;
                            }
                        }
                        output.Flush();
                        output.Close();
                        inputFile.Close();
                        compFile.Close();
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
