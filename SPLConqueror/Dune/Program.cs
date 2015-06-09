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

        static String PATH = @"D:\owncloud\all.xml";
        const String XML_LOCATION = @"doc\doxygen\xml\";
        static ArrayList features = new ArrayList();
        const bool INCLUDE_CLASSES_FROM_STD = false;

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
            parse(PATH);

            // Force the gc to remove all the unneeded data in memory
            System.GC.Collect();
            GC.WaitForPendingFinalizers();

            // Needed for debugging purposes.
            System.Console.ReadKey();
        }

        /// <summary>
        /// Parses the xml-file containing all the other files(please use the combine.xslt-file in order to combine these if not done so).
        /// </summary>
        /// <param name="path">The path to the all.xml-file.</param>
        static void parse(String path)
        {
            XmlDocument dat = new XmlDocument();

            dat.Load(PATH);
            XmlElement current = dat.DocumentElement;
            XmlNodeList childList = current.ChildNodes;
            foreach (XmlNode child in childList)
            {
                //            XmlNode child = current.ChildNodes.Item(0);
                String refId = child.Attributes["id"].Value.ToString();
                String name = child.FirstChild.InnerText.ToString();

                // Ignore the classes which are not by Dune
                if (!name.Contains("Dune::"))
                {
                    continue;
                }

                name = convertName(name);

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
            }
            dat = null;

        }

        /// <summary>
        /// Extracts the name of the class without the content in "<>" 
        /// </summary>
        /// <param name="toConv">the name to convert</param>
        /// <returns>the name of the class</returns>
        private static String convertName(String toConv)
        {
            // TODO add template-recognition
            int index = toConv.IndexOf("<");
            if (index > 0 && toConv.Contains("::"))
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
