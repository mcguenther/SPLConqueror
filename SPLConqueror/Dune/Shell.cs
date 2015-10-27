using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
