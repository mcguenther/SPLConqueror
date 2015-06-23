﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;

namespace Dune
{
    /// <summary>
    /// This class is concerned with parsing the xml-file in order to obtain a ClassModel.
    /// </summary>
    class XMLParser
    {

        static List<DuneFeature> features = new List<DuneFeature>();
        static Dictionary<DuneFeature, String> templatesToAnalyze = new Dictionary<DuneFeature, string>();

        // Is only here for debugging
        static System.IO.StreamWriter file;
        static List<String> classNames = new List<String>();

        /// <summary>
        /// Parses the xml-file containing all the other files(please use the combine.xslt-file in order to combine these if not done so).
        /// </summary>
        /// <param name="path">The path to the all.xml-file.</param>
        public static void parse(String path)
        {
            XmlDocument dat = new XmlDocument();

            dat.Load(path);
            XmlElement current = dat.DocumentElement;
            XmlNodeList childList = current.ChildNodes;
            foreach (XmlNode child in childList)
            {
                //            XmlNode child = current.ChildNodes.Item(0);
                String refId = child.Attributes["id"].Value.ToString();
                String name = child.FirstChild.InnerText.ToString();
                
                // Helper classes are skipped
                if (name.Contains("Helper") || name.Contains("helper"))
                {
                    continue;
                }

                String template = extractTemplate(name);
                name = convertName(name);

                DuneFeature df = getFeature(new DuneFeature(refId, name));

                if (template != null)
                {
                    // Add the class and the template to the list of templates to be analyzed
                    templatesToAnalyze.Add(df, template);
                }

                int i = 1;

                // This boolean indicates if there are another basecompoundref-elements
                Boolean anotherBase = true;

                // The features in the basecompoundref-elements are the children of the respective feature
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
                        if (!Program.INCLUDE_CLASSES_FROM_STD)
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
                    DuneFeature newDF = getFeature(new DuneFeature(refNew, nameNew));

                    df.addParent(newDF);
                    newDF.addChildren(df);
                    i++;
                }
            }

            file = new System.IO.StreamWriter(@"D:\HiWi\DebugOutput\templates.txt");

            dat = null;

            List<DuneFeature> toRemove = new List<DuneFeature>();
            // analyze all templates
            foreach (DuneFeature d in templatesToAnalyze.Keys)
            {
                String value;
                if (templatesToAnalyze.TryGetValue(d,out value)) {
                    analyzeTemplate(d, value);
                    toRemove.Add(d);
                }
            }

            while (toRemove.Count() > 0)
            {
                templatesToAnalyze.Remove(toRemove.ElementAt(0));
                toRemove.RemoveAt(0);
            }
            toRemove.Clear();

            printClassList();
            file.Flush();
            file.Close();
        }

        /// <summary>
        /// Returns the given DuneFeature if the feature is not already in the features-list; the feature in the features-list is returned otherwise.
        /// </summary>
        /// <param name="df">the feature to search for</param>
        /// <returns>the given DuneFeature if the feature is not already in the features-list; the feature in the features-list is returned otherwise</returns>
        private static DuneFeature getFeature(DuneFeature df)
        {
            int indx = XMLParser.features.IndexOf(df);
            if (indx >= 0)
            {
                df = XMLParser.features[indx];
            }
            else
            {
                XMLParser.features.Add(df);
            }
            return df;
        }

        /// <summary>
        /// Analyzes the given template
        /// </summary>
        /// <param name="f">the feature the template belongs to</param>
        /// <param name="template">the template to be analyzed</param>
        private static void analyzeTemplate(DuneFeature f, String template)
        {
            template = template.Trim();
            int level = 0;
            int i = 0;
            int from = 0;
            DuneFeature n = null;
            int currentBeginning = 0;
            while (i < template.Length)
            {
                switch (template.ElementAt(i)) {
                    case '<':
                        if (level == 0)
                        {
                            n = getFeature(template.Substring(currentBeginning, i - currentBeginning).Trim());

                            //Debug
                            if (n == null)
                            {
                                addToList(template.Substring(currentBeginning, i - currentBeginning).Trim());
                            }

                            from = i;
                        }
                        level++;
                        break;
                    case '>':
                        level--;
                        if (n == null)
                        {
                            f.addUnknownTemplate(template.Substring(currentBeginning, i - currentBeginning).Trim());
                        }
                        else
                        {
                            f.addTemplateClass(n);
                            
                        }
                      //  analyzeTemplate(n, template.Substring(from, i - from));
                        
                        break;
                    case ',':
                        if (level != 0)
                        {
                            break;
                        }

                        if (from <= currentBeginning)
                        {
                            DuneFeature df = getFeature(template.Substring(currentBeginning, i - currentBeginning).Trim());

                            // Debug
                            if (df == null)
                            {
                                addToList(template.Substring(currentBeginning, i - currentBeginning).Trim());
                            }

                            // The last class contains no template
                            if (df != null)
                            {
                                f.addTemplateClass(df);
                            }
                        }
                        // There has to be one more classname which may also contain a template
                        currentBeginning = i + 1;

                        break;
                    default:
                        break;
                }

                i++;
            }

        }

        private static void addToList(String className) {
            if (!classNames.Contains(className))
            {
                classNames.Add(className);
            }
        }

        private static void printClassList()
        {
            foreach (String s in classNames)
            {
                file.WriteLine(s);
            }
        }

        /// <summary>
        /// Returns the feature with the given name. Note that is does return the <ul>first</ul> feature with this name if it occurs more than once.
        /// </summary>
        /// <param name="className">the name of the feature to be searched for</param>
        /// <returns>the feature with the given name</returns>
        private static DuneFeature getFeature(String className)
        {
            foreach (DuneFeature f in features)
            {
                if (f.getClassName().Equals(className))
                {
                    return f;
                }
            }
            return null;
        }

        /// <summary>
        /// This method extracts the information of the template.
        /// </summary>
        /// <param name="toConv">the whole name of the class including the template</param>
        /// <returns>the string containing the template</returns>
        private static String extractTemplate(String toConv)
        {
            if (!toConv.Contains("<"))
            {
                return null;
            }
            return toConv.Substring(toConv.IndexOf("<") + 1, toConv.LastIndexOf(">") - toConv.IndexOf("<") - 1);
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
                toConv = toConv.Substring(0, index);
            }
            else
            {
                toConv = toConv.Substring(0);
            }

            // The index-variable is now used to iterate through the name in order to obtain the class with its path(without methods and variables name etc.)
            index = toConv.Length - 1;
            // This variable indicates where the last ":" appeared.
            int last = index;
            Boolean found = false;
            while (index >= 0 && !found)
            {
                // The position index + 1 has to exist if ":" appears on position index
                if (toConv[index].Equals(":"))
                {
                    if (Char.IsUpper(toConv[index + 1]))
                    {
                        found = true;
                        if (last != toConv.Length)
                        {
                            toConv = toConv.Substring(0, last - 1);
                        }
                    }
                    else
                    {
                        last = index;
                    }
                }

                index--;

            }
            return toConv;
        }
    }
}