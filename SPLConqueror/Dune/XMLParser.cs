using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;
using System.Diagnostics;

namespace Dune
{
    /// <summary>
    /// This class is concerned with parsing the xml-file in order to obtain a ClassModel.
    /// </summary>
    class XMLParser
    {

        static String[] blacklisted = { };//"Dune::YaspGrid::YGridLevel" };

        // The root of the whole feature-tree.
        static DuneFeature root = new DuneFeature("", "root");

        static List<DuneFeature> features = new List<DuneFeature>();
        static Dictionary<DuneFeature, String> templatesToAnalyze = new Dictionary<DuneFeature, string>();

        static Dictionary<DuneFeature, String> classesToAnalyze = new Dictionary<DuneFeature, string>();

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
            System.Console.WriteLine("Parsing the file...");
            foreach (XmlNode child in childList)
            {
                //            XmlNode child = current.ChildNodes.Item(0);
                String refId = child.Attributes["id"].Value.ToString();

                String name = child.FirstChild.InnerText.ToString();

                if (refId == null)
                {
                    refId = name;
                }
                
                // Helper classes are skipped
                if (name.Contains("Helper") || name.Contains("helper"))
                {
                    continue;
                }

                String templateInName = extractTemplateInName(name);
                String template = extractTemplate(child);
                name = convertName(name);


                //if (template != null || (template != null && !template.Trim().Equals(""))) //&& !template.Trim().Equals(""))
                //{
                //    name += "<" + template + ">";
                //}

                DuneFeature df;
                df = getFeature(new DuneFeature(refId, name, template, templateInName));
                

                if (df.getReference() == null && refId != null)
                {
                    df.setReference(refId);
                }


                if (template != null && !templatesToAnalyze.ContainsKey(df))
                {
                    // Add the class and the template to the list of templates to be analyzed
                    templatesToAnalyze.Add(df, template);
                }

                // This boolean indicates if the current child is an interface, an abstract class or a normal class.
                Boolean structClass = child.Attributes.GetNamedItem("kind").Value.Equals("struct");

                df.setType(structClass, child.Attributes.GetNamedItem("abstract") != null);


                // Save the enums in the feature
                saveEnums(child, df);

                // Save the methods in the feature
                saveMethods(child, df);

                // Has to start from 1, because the child at position 0 is always the compoundname-tag containing the own name of the class as well as the template
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
                        i++;
                        continue;
                    }

                    String refNew = null;
                    String nameNew = convertName(c.InnerText.ToString());

                    if (c.Attributes["refid"] == null)
                    {
                        if (nameNew.Contains("std") && !Program.INCLUDE_CLASSES_FROM_STD)
                        {
                            i++;
                            continue;
                        }
                    }
                    else
                    {
                        refNew = c.Attributes["refid"].Value.ToString();
                    }
                    
                    DuneFeature newDF = getFeature(new DuneFeature(refNew, nameNew));

                    df.addParent(newDF);
                    newDF.addChildren(df);
                    i++;
                }

            }

            // Every class with no parent gets a connection to the root-node
            foreach (DuneFeature df in features) {
                if (!df.hasParents(root))
                {
                    // Add the root as a parent, so every node has a common node as parent in the transitive closure
                    df.addParent(root);
                    root.addChildren(df);
                }
            }

            System.Console.WriteLine("Done!");

            System.Console.WriteLine("Now finding potential parents(duck-typing)");
            Stopwatch stopwatch = Stopwatch.StartNew();
            findPotentialParents();
            stopwatch.Stop();
            System.Console.WriteLine("Finished duck-typing. Time needed for duck-typing: " + stopwatch.Elapsed);

        }


        /// <summary>
        /// Returns all possible replacements according to the inheritance and the template analysis as a list of strings.
        /// </summary>
        /// <param name="feature">the feature to analyze</param>
        /// <returns>a list of strings in which every element is a possible replacement for the given feature</returns>
        public static List<String> getVariability(string feature)
        {

            // Extract the name and the template
            string name;
            string template = "";
            int index = feature.IndexOf('<');
            if (index > 0)
            {
                name = feature.Substring(0, index);
                template = feature.Substring(index, feature.Length - index);
            }
            else
            {
                name = feature;
            }
            // If the last name begins with a lower character then it is part of an enum
            Boolean isEnum = char.IsLower(name[name.LastIndexOf(':') + 1]);

            DuneFeature df;

            if (isEnum)
            {
                string featureName = feature.Substring(0, feature.LastIndexOf(':') - 1);

                df = searchForFeature(new DuneFeature("", featureName));

                // If not found search only for the name
                if (df == null)
                {
                    df = searchForFeatureName(new DuneFeature("", featureName));
                }
            } else
            {
                df = searchForFeature(new DuneFeature("", feature));

                if (df == null || template.Equals(""))
                {
                    df = searchForFeature(new DuneFeature("", feature + "<>"));
                }
            }

            if (df != null && !isEnum)
            {
                return df.getVariability(root);
            }
            else if (df != null)
            {
                return df.getAlternativeEnums(feature.Substring(feature.LastIndexOf(':') + 1));
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Returns <code>true</code> if the given name appears in the blacklist; <code>false</code> otherwise.
        /// </summary>
        /// <param name="name">the name of the class</param>
        /// <returns><code>true</code> if the given name appears in the blacklist; <code>false</code> otherwise</returns>
        private static Boolean isBlacklisted(String name)
        {
            for (int i = 0; i < blacklisted.Length; i++)
            {
                if (name.Equals(blacklisted[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Analyzes for classes from which the classes of the <code>classes</code>-list could inherit from.
        /// </summary>
        private static void findPotentialParents()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\HiWi\DebugOutput\inherits.txt");

            List<DuneFeature> featuresToCompare = new List<DuneFeature>();

            // Compare only classes with at least one public method
            foreach (DuneFeature df in features)
            {
                if (df.getNumberOfMethodHashes() > 0)
                {
                    featuresToCompare.Add(df);
                }
            }

            List<Tuple<DuneFeature, DuneFeature>> toInsert = new List<Tuple<DuneFeature, DuneFeature>>();


            // The newer version with optimizations
            foreach (DuneFeature df in featuresToCompare)
            {
                System.Console.WriteLine(df.ToString());

                foreach (DuneFeature comp in featuresToCompare)
                {

                    // If there is no transitive relation between the classes, the classes are analyzed
                    if (df != comp && df.getNumberOfMethodHashes() >= comp.getNumberOfMethodHashes() && !df.hasRelationTo(comp, root))
                    {
                        Boolean isSubclassOf = true;
                        foreach (int methodHash in comp.getMethodHashes())
                        {
                            if (!df.containsMethodHash(methodHash))
                            {
                                isSubclassOf = false;
                                break;
                            }
                        }

                        if (isSubclassOf)
                        {
                            toInsert.Add(new Tuple<DuneFeature, DuneFeature>(comp, df));
                            file.WriteLine(df.ToString() + " -> " + comp.ToString());
                        }
                    }

                }
            }

            // Only now the relations are added.
            foreach (Tuple<DuneFeature, DuneFeature> t in toInsert)
            {
                t.Item1.addParent(t.Item2);
                t.Item2.addChildren(t.Item1);
            }
            file.Flush();
            file.Close();
        }

        /// <summary>
        /// This method saves the enums of the respective class in the corresponding Dictionary-element from the DuneFeature-class.
        /// </summary>
        /// <param name="node">the object containing all information about the class/interface</param>
        /// <param name="df">the feature-object the enums should be added to</param>
        private static void saveEnums(XmlNode node, DuneFeature df)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                // Only the public types are crucial
                if (child.Name.Equals("sectiondef") && child.Attributes.GetNamedItem("kind") != null && child.Attributes.GetNamedItem("kind").Value.Equals("public-type"))
                {
                    // Access memberdefs and search for the value of the definition tag
                    foreach (XmlNode c in child.ChildNodes)
                    {
                        if (c.Name.Equals("memberdef") && c.Attributes.GetNamedItem("kind") != null && c.Attributes.GetNamedItem("kind").Value.Equals("enum"))
                        {
                            XmlNode name = getChild("name", c.ChildNodes);
                            List<String> enumNames = new List<String>();

                            // Extract the enum-options
                            foreach (XmlNode enumvalue in c.ChildNodes)
                            {
                                if (enumvalue.Name.Equals("enumvalue"))
                                {
                                    enumNames.Add(getChild("name", enumvalue.ChildNodes).InnerText);

                                }
                            }

                            df.addEnum(name.InnerText, enumNames);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Saves the methods of the class/interface
        /// </summary>
        /// <param name="node">the object containing all information about the class/interface</param>
        /// <param name="df">the feature-object the methods should be added to</param>
        private static void saveMethods(XmlNode node, DuneFeature df)
        {

            foreach (XmlNode child in node.ChildNodes)
            {
                // Only the public functions are crucial
                if (child.Name.Equals("sectiondef") && child.Attributes.GetNamedItem("kind") != null && child.Attributes.GetNamedItem("kind").Value.Equals("public-func")) {
                    
                    // Access memberdefs and search for the value of the definition tag
                    foreach (XmlNode c in child.ChildNodes)
                    {
                        if (c.Name.Equals("memberdef")) {
                            XmlNode type = getChild("type", c.ChildNodes);
                            XmlNode args = getChild("argsstring", c.ChildNodes);
                            XmlNode name = getChild("name", c.ChildNodes);
                            df.addMethod(type.InnerText + " " + name.InnerText + '(' + convertArgs(args.InnerText) + ')');
                        }
                    }
                    break;
                }

            }
        }

        /// <summary>
        /// Returns the number of arguments in the given string. Arguments are separated by comma.
        /// </summary>
        /// <param name="args">a <code>string</code> which contains the arguments (with preceeding brackets or not)</param>
        /// <returns>the number of arguments in the given string</returns>
        public static int getCountOfArgs(string args)
        {
            int count = args.Count(f => f == ',') + 1;
            if (args.IndexOf(">") == args.IndexOf("<") + 1 || ((args.IndexOf(")") >= 0) && args.IndexOf(")") == args.IndexOf("(") + 1) || args.Trim().Equals(""))
            {
                count = 0;
            }
            return count;
        }

        private static string convertArgs(string args)
        {
            string result = "";
            if (args.IndexOf('(') >= 0)
            {
                args = args.Substring(args.IndexOf('(') + 1, args.IndexOf(')') - args.IndexOf('(') - 1);
            }
            else if (args.IndexOf('<') >= 0)
            {
                args = args.Substring(args.IndexOf('<') + 1, args.IndexOf('>') - args.IndexOf('<') - 1);
            }

            string[] splitted = args.Split(',');

            foreach (string s in splitted)
            {
                string trimmed = s.Trim();

                bool type = true;

                foreach (char c in trimmed)
                {
                    switch (c)
                    {
                        case ' ':
                            type = false;
                            break;
                        default:
                            if (type)
                            {
                                result += c;
                            }
                            break;
                    }
                }

            }
            return result;

        }

        /// <summary>
        /// Returns the node of the nodelist with the given name.
        /// </summary>
        /// <param name="name">the name to be searched for</param>
        /// <param name="list">the list which contains all children</param>
        /// <returns>the node of the nodelist with the given name</returns>
        private static XmlNode getChild(String name, XmlNodeList list)
        {
            foreach (XmlNode child in list)
            {
                if (child.Name.Equals(name))
                {
                    return child;
                }
            }
            return null;
        }


        /// <summary>
        /// Returns the feature if it was found; <code>null</code> otherwise
        /// </summary>
        /// <param name="df">the feature to search for</param>
        /// <returns>the feature if it was found; <code>null</code> otherwise</returns>
        private static DuneFeature searchForFeature(DuneFeature df) 
        {
            foreach (DuneFeature d in features)
            {
                if (d.getClassName().Equals(df.getClassName()))
                {
                    return d;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the feature if it was found; <code>null</code> otherwise
        /// </summary>
        /// <param name="df">the feature to search for</param>
        /// <returns>the feature if it was found; <code>null</code> otherwise</returns>
        private static DuneFeature searchForFeatureName(DuneFeature df)
        {
            foreach (DuneFeature d in features)
            {
                if (d.getClassNameWithoutTemplate().Equals(df.getClassNameWithoutTemplate()))
                {
                    return d;
                }
            }
            return null;
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
        private static void analyzeTemplate(DuneFeature f, String template, Tree root, ref Dictionary<String, Tree> nonIdentifiedNodes, ref List<Tuple<Tree, String>> todo)
        {
            template = template.Trim();
            int level = 0;
            int i = 0;
            int from = 0;
            DuneFeature n = null;
            int currentBeginning = 0;
            while (i < template.Length)
            {
                // The whole template is parsed and only the arguments in the upper layer(not containing another template) are important.
                // The arguments in another template are analyzed recursively.
                switch (template.ElementAt(i))
                {
                    case '<':
                        if (level == 0)
                        {
                            n = getFeature(template.Substring(currentBeginning, i - currentBeginning).Trim());

                            // TODO Debug
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
                        //if (n == null)
                        //{
                        //    f.addUnknownTemplate(template.Substring(currentBeginning, i - currentBeginning).Trim());
                        //}
                        //else
                        //{
                        //    f.addTemplateClass(n);

                        //}
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

                            // TODO Debug
                            if (df == null)
                            {
                                addToList(template.Substring(currentBeginning, i - currentBeginning).Trim());

                                // Debug
                                //if (template.Substring(currentBeginning, i - currentBeginning).Trim().Equals("1"))
                                //{
                                //    System.Console.WriteLine("1: " + f.getClassName());
                                //}
                            }

                            // The last class contains no template
                            if (df != null)
                            {
                                //f.addTemplateClass(df);
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

        /// <summary>
        /// Adds the given className to the list of class names.
        /// </summary>
        /// <param name="className">the class name to add</param>
        private static void addToList(String className) {
            if (!classNames.Contains(className))
            {
                classNames.Add(className);
            }
        }

        /// <summary>
        /// Prints out the class names which are currently in the classNames list.
        /// </summary>
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
        /// <param name="child">the xml-element containing the feature where the template should be extracted from</param>
        /// <returns>the string containing the template</returns>
        private static String extractTemplate(XmlNode child)
        {

            bool found = false;
            bool tooFar = false;

            // The searched tag cannot be at index 0.
            int i = 1;

            while (!found && !tooFar)
            {
                 XmlNode c = child.ChildNodes.Item(i);
                 if (c.Name.Equals("templateparamlist"))
                 {
                     found = true;
                 }
                 else if (!c.Name.Equals("includes") && !c.Name.Equals("derivedcompoundref") && !c.Name.Equals("basecompoundref") && !c.Name.Equals("innerclass"))
                 {
                     tooFar = true;
                 }
                 else
                 {
                     i++;
                 }
            }
            string result = "";

            if (found)
            {
                XmlNode cur = child.ChildNodes.Item(i);
                for (int j = 0; j < cur.ChildNodes.Count; j++ )
                {
                    XmlNode c = cur.ChildNodes.Item(j);

                    if (j > 0)
                    {
                        result += ",";
                    }

                    result += c.FirstChild.InnerText;
                }
            }

            return result;
        }

        /// <summary>
        /// Used for debug-purposes and returns the template itself.
        /// </summary>
        /// <param name="toConv">the name which also contains the template</param>
        /// <returns>the template itself</returns>
        private static string extractTemplateInName(string toConv)
        {

            if (!toConv.Contains("<"))
            {
                return null;
            }
            return toConv.Substring(toConv.IndexOf("<") + 1, toConv.LastIndexOf(">") - toConv.IndexOf("<") - 1);
        }

        /// <summary>
        /// This method extracts the information of the template.
        /// </summary>
        /// <param name="child">the xml-element containing the feature where the template should be extracted from</param>
        /// <returns>the number of template arguments</returns>
        private static int getCountOfTemplateArgs(XmlNode child)
        {
            //if (!toConv.Contains("<"))
            //{
            //    return null;
            //}
            //return toConv.Substring(toConv.IndexOf("<") + 1, toConv.LastIndexOf(">") - toConv.IndexOf("<") - 1);

            bool found = false;
            bool tooFar = false;

            // The searched tag cannot be at index 0.
            int i = 1;

            while (!found && !tooFar)
            {
                XmlNode c = child.ChildNodes.Item(i);
                if (c.Name.Equals("templateparamlist"))
                {
                    found = true;
                }
                else if (!c.Name.Equals("includes") && !c.Name.Equals("basecompoundred"))
                {
                    tooFar = true;
                }
                else
                {
                    i++;
                }
            }

            if (found)
            {
                return child.ChildNodes.Item(i).ChildNodes.Count;
            } 

            return 0;
        }

        /// <summary>
        /// Extracts the name of the class without the content within the template
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
