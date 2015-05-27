using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Globalization;

namespace Dune
{
    static class Program
    {

        static String[] DUNE_MODULES; // = new String[1] {"dune-common"};//, "dune-geometry", "dune-grid", "dune-istl", "dune-localfunctions", "dune-typetree", "dune-pdelab"};
        const String XML_LOCATION = @"doc\doxygen\xml\";
        static ArrayList features = new ArrayList();
        const bool INCLUDE_CLASSES_FROM_STD = false;

        /// <summary>
        /// Der Einstiegspunkt für die Anwendung, falls das Dune FeatureModell erstellt werden sollte.
        /// </summary>
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            DUNE_MODULES = new String[] {"dune-common"};
            parse(@"F:\Dune");
            System.Console.ReadKey();
        }

        /// <summary>
        /// Parst die xml-Dateien von doxygen, sofern vorhanden.
        /// </summary>
        /// <param name="path">Der Pfad zur Dune-Bibliothek. Im Verzeichnis befinden sich dabei in etwa Ordner wie "dune-common", "dune-grid", etc.</param>
        static void parse(String path)
        {
            foreach(String module in DUNE_MODULES) {
                try
                {
                    String d = path + "\\" + module + "\\" + XML_LOCATION;
                    DirectoryInfo dir = new DirectoryInfo(d);
                    XmlDocument dat = new XmlDocument();
                    var files = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo f in files)
                    {
                        try
                        {
                            if (!f.ToString().StartsWith("a") || !f.ToString().EndsWith(".xml")) // Skip every file which is not in our pattern "a*.xml"
                            {
                                continue;
                            }

                            dat.Load(d + "\\" + f.ToString());
                            XmlElement current = dat.DocumentElement;
                            XmlNode child = current.ChildNodes.Item(0);
                            String refId = child.Attributes["id"].Value.ToString();
                            String name = convertName(child.FirstChild.InnerText.ToString());

                            DuneFeature df = new DuneFeature(refId, name);
                            int indx = Program.features.IndexOf(df);

                            if (indx >= 0)
                            {
                                df = Program.features[Program.features.IndexOf(df)] as DuneFeature;
                            }
                            else
                            {
                                Program.features.Add(df);
                            }

                            int i = 1;
                            Boolean anotherBase = true;
                            while (anotherBase)
                            {
                                XmlNode c = child.ChildNodes.Item(i);
                                if (!c.Name.Equals("basecompoundref"))
                                {
                                    anotherBase = false;
                                    continue;
                                }
                                if (i == 2)
                                {
                                    System.Console.WriteLine("The class " + f + " has more than one basecompound.");
                                }

                                String refNew;

                                if (c.Attributes["refid"] == null)
                                {
                                    if (!INCLUDE_CLASSES_FROM_STD)
                                    {
                                        i++;
                                        continue;
                                    }
                                    refNew = "s0000";
                                }
                                else
                                {
                                    refNew = c.Attributes["refid"].Value.ToString();
                                }
                                String nameNew = convertName(c.InnerText.ToString());
                                DuneFeature newDF = new DuneFeature(refNew, nameNew);
                                indx = Program.features.IndexOf(newDF);

                                if (indx >= 0)
                                {
                                    newDF = Program.features[Program.features.IndexOf(newDF)] as DuneFeature;
                                }
                                else
                                {
                                    Program.features.Add(newDF);
                                }

                                df.addParent(newDF);
                                newDF.addChildren(df);
                                i++;
                            }
                            System.Console.WriteLine(f.ToString());
                        }
                        catch
                        {
                            System.Console.WriteLine("Maybe something wrong in " + f + "? Skipped this one.");
                        }
                    }
                }
                catch
                {
                    System.Console.WriteLine(System.Environment.StackTrace);
                }
            }
            
        }

        /// <summary>
        /// Extracts the name of the class without the content in "<>" 
        /// </summary>
        /// <param name="toConv">the name to convert</param>
        /// <returns>the name of the class</returns>
        private static String convertName(String toConv)
        {
            int index = toConv.IndexOf("<");
            if (index > 0)
            {
                toConv = toConv.Substring(6, index - 6);
            }
            else
            {
                toConv = toConv.Substring(6);
            }
            return toConv;
        }
    }
}
