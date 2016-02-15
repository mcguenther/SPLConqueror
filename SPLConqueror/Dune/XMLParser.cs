using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.IO;

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

        static List<Tuple<DuneFeature, DuneFeature>> relations = new List<Tuple<DuneFeature, DuneFeature>>();

        static List<DuneFeature> classesWithNoNormalMethods = new List<DuneFeature>();

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
                DuneFeature df = extractFeature(child);
            }

            StreamWriter output = new System.IO.StreamWriter(Program.DEBUG_PATH + "notFound.txt");
            List<DuneFeature> featuresNotFound = new List<DuneFeature>();
            int notFound = 0;
            foreach (Tuple<DuneFeature, DuneFeature> t in relations)
            {

                DuneFeature newDF = getFeature(t.Item1);

                // If the class is still not found, it will be matched by name.
                if (newDF == null)
                {
                    newDF = getFeatureByName(t.Item1);
                }

                if (newDF != null)
                {
                    newDF.addChildren(t.Item2);
                    t.Item2.addParent(newDF);
                }
                else
                {
                    if (!featuresNotFound.Contains(t.Item1))
                    {
                        featuresNotFound.Add(t.Item1);
                        notFound++;
                        output.WriteLine(t.Item1);
                    }
                }
            }
            output.Flush();

            // Every class with no parent gets a connection to the root-node
            foreach (DuneFeature df in features)
            {
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
            //findPotentialParents();
            stopwatch.Stop();
            System.Console.WriteLine("Finished duck-typing. Time needed for duck-typing: " + stopwatch.Elapsed);

            System.Console.Write("Writing the classes with no normal methods in a file...");
            printClassesWithNoNormalMethods();
            System.Console.WriteLine("Finished!");

        }

        /// <summary>
        /// This method prints the classes in the list <code>classesWithNoNormalMethods</code>.
        /// </summary>
        private static void printClassesWithNoNormalMethods()
        {
            StreamWriter output = new System.IO.StreamWriter(Program.DEBUG_PATH + "classesWithNoNormalMethods.txt");
            foreach (DuneFeature df in classesWithNoNormalMethods)
            {
                output.WriteLine(df);
            }
            output.Flush();
            output.Close();
        }

        /// <summary>
        /// Extracts the information needed for a <code>DuneFeature</code> and also constructs one.
        /// </summary>
        /// <param name="child">the node in the xml-file pointing on the <code>compounddef</code> tag</param>
        /// <returns>the constructed <code>DuneFeature</code> with all its properties</returns>
        private static DuneFeature extractFeature(XmlNode child)
        {
            DuneFeature df = null;

            // Ignore private classes
            String prot = child.Attributes.GetNamedItem("prot") == null ? null : child.Attributes.GetNamedItem("prot").Value;
            String kind = child.Attributes.GetNamedItem("kind") == null ? null : child.Attributes.GetNamedItem("kind").Value;
            if (prot != null && prot.Equals("private") || kind != null && (kind.Equals("file") || kind.Equals("dir") || kind.Equals("example") || kind.Equals("group") || kind.Equals("namespace") || kind.Equals("page")))
            {
                return df;
            }


            String template = "";
            String refId = child.Attributes["id"].Value.ToString();
            String name = "";
            String templateInName = "";
            Dictionary<String, List<String>> enums = null;
            Tuple<List<int>, List<int>, List<int>, List<string>, List<List<int>>, bool> methods = null;
            List<DuneFeature> inherits = new List<DuneFeature>();

            foreach (XmlNode node in child.ChildNodes)
            {
                switch (node.Name)
                {
                    case "compoundname":
                        name = node.InnerText.ToString();

                        //if (name.Contains("Dune::GridDefaultImplementation"))
                        //{
                        //    Console.Write("");
                        //}
                        //if (name.Contains("Dune::AlbertaGridLeafIntersection"))
                        //{
                        //    Console.Write("");
                        //}
   

                        templateInName = extractTemplateInName(name);
                        name = convertName(name);
                        break;
                    case "basecompoundref":
                        String refNew = null;
                        String nameNew = node.InnerText.ToString().Replace(" ", "");

                        if (node.Attributes["refid"] == null)
                        {
                            if (nameNew.Contains("std") && !Program.INCLUDE_CLASSES_FROM_STD)
                            {
                                break;
                            }
                        }
                        else
                        {
                            refNew = node.Attributes["refid"].Value.ToString();
                        }

                        DuneFeature newDF = new DuneFeature(refNew, nameNew);

                        inherits.Add(newDF);
                        break;
                    case "sectiondef":
                        if (node.Attributes.GetNamedItem("kind") != null)
                        {
                            switch (node.Attributes.GetNamedItem("kind").Value)
                            {
                                // Only the public types are crucial for saving enums
                                case "public-type":
                                    // Save the enums in the feature
                                    enums = saveEnums(node);
                                    break;
                                // Only the public functions are crucial for saving methods
                                case "public-func":
                                    methods = saveMethods(node, name);
                                    break;
                            }
                        }
                        break;
                    case "templateparamlist":
                        template = extractTemplate(node);
                        break;
                }
            }

            df = new DuneFeature(refId, name, template, templateInName);
            features.Add(df);

            // This boolean indicates if the current child is an interface, an abstract class or a normal class.
            Boolean structClass = child.Attributes.GetNamedItem("kind").Value.Equals("struct");

            df.setType(structClass, child.Attributes.GetNamedItem("abstract") != null);

            if (enums != null)
            {
                df.setEnum(enums);
            }

            if (methods != null)
            {
                df.setMethods(methods.Item1);
                df.setMethodNameHashes(methods.Item2);
                df.setMethodArgumentCount(methods.Item3);
                df.setMethodArguments(methods.Item4);
                df.setReplaceableMethodArguments(methods.Item5);
                df.ignoreAtDuckTyping(methods.Item6);
                if (methods.Item6)
                {
                    classesWithNoNormalMethods.Add(df);
                }
            }
            else
            {
                classesWithNoNormalMethods.Add(df);
            }

            // Now add all relations
            foreach (DuneFeature inherit in inherits)
            {
                DuneFeature newDF = getFeature(inherit);
                if (newDF != null)
                {

                    df.addParent(newDF);
                    newDF.addChildren(df);
                }
                else
                {
                    relations.Add(new Tuple<DuneFeature, DuneFeature>(inherit, df));
                }
            }

            return df;
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
            }
            else
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
            System.IO.StreamWriter file = new System.IO.StreamWriter(Program.DEBUG_PATH + "inherits.txt");

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
                if (df.isIgnored())
                {
                    continue;
                }
                System.Console.WriteLine(df.ToString());

                foreach (DuneFeature comp in featuresToCompare)
                {
                    if (df.isIgnored())
                    {
                        continue;
                    }

                    // If there is no transitive relation between the classes, the classes are analyzed
                    if (df != comp && df.getNumberOfMethodHashes() >= comp.getNumberOfMethodHashes() && !df.hasRelationTo(comp, root))
                    {
                        Boolean isSubclassOf = true;
                        for (int i = 0; i < comp.getMethodHashes().Count; i++)
                        {
                            int methodHash = comp.getMethodHashes()[i];
                            if (!df.containsMethodHash(methodHash))// && !variableSubmethod(df, comp, i))
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
        /// This method makes an improved check if the method with the given number of <code>df</code> may be inherited from the <code>comp</code> feature.
        /// </summary>
        /// <param name="df"></param>
        /// <param name="comp"></param>
        /// <param name="index"></param>
        /// <returns><code>true</code> iff one or more of the method's arguments differ only in the concrete classes; <code>false</code> otherwise</returns>
        private static bool variableSubmethod(DuneFeature df, DuneFeature comp, int index)
        {
            List<Tuple<string, List<int>>> potentialMethods = df.getMethodArgumentsWithNameAndCount(comp.getMethodNameHash(index), comp.getMethodArgumentCount(index));
            foreach (Tuple<string, List<int>> t in potentialMethods)
            {
                if (isSubmethod(t, comp.getMethodArguments(index)) )
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="potentialMethod"></param>
        /// <param name="compMethod"></param>
        /// <returns></returns>
        private static bool isSubmethod(Tuple<string, List<int>> potentialMethod, string compMethod)
        {
            string dfMethod = potentialMethod.Item1;
            List<int> rechangeable = potentialMethod.Item2;

            string localDfMethod = dfMethod.Substring(1, dfMethod.IndexOf(')') - 1);
            string localCompMethod = compMethod.Substring(1, compMethod.IndexOf(')') - 1);

            List<string> dfArgs = splitArgs(localDfMethod);
            List<string> compArgs = splitArgs(localCompMethod);

            for (int i = 0; i < dfArgs.Count; i++)
            {
                // If the current argument is not marked as rechangeable
                if (!rechangeable.Contains(i) && !dfArgs[i].Equals(compArgs[i]))
                {
                    return false;
                }
            }
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toSplit"></param>
        /// <returns></returns>
        private static List<string> splitArgs(string toSplit)
        {
            List<string> args = new List<string>();
            int level = 0;
            int startPos = 0;
            for (int i = 0; i < toSplit.Length; i++)
            {
                switch (toSplit[i])
                {
                    case '<':
                        level++;
                        break;
                    case '>':
                        level--;
                        break;
                    case ',':
                        if (level == 0)
                        {
                            args.Add(toSplit.Substring(startPos, i - startPos));
                            startPos = i + 1;
                        }
                        break;
                }
            }

            // Add also the last argument
            args.Add(toSplit.Substring(startPos, toSplit.Length - startPos));


            return args;
        }

        /// <summary>
        /// This method saves the enums of the respective class in the corresponding Dictionary-element from the DuneFeature-class.
        /// </summary>
        /// <param name="node">the object containing all information about the class/interface</param>
        /// <returns>a <code>Dictionary</code> which contains the enums and its elements</returns>
        private static Dictionary<String, List<String>> saveEnums(XmlNode node)
        {
            Dictionary<String, List<String>> result = new Dictionary<String, List<String>>();
            // Access memberdefs and search for the value of the definition tag
            foreach (XmlNode c in node.ChildNodes)
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

                    result.Add(name.InnerText, enumNames);
                }
            }

            return result;

        }

        /// <summary>
        /// Saves the methods of the class/interface
        /// </summary>
        /// <param name="node">the object containing all information about the class/interface</param>
        /// <returns>a tuple with a list containing the method hashes, a list containing the hash of the method names and a list containing the count of the arguments (in this order)</returns>
        private static Tuple<List<int>, List<int>, List<int>, List<string>, List<List<int>>, bool> saveMethods(XmlNode node, String classname)
        {
            // The pure class name (e.g. 'x' in 'Dune::y::x') is needed in order to identify the constructor
            int indx = classname.LastIndexOf(':');
            String pureClassName = null;
            if (indx >= 0) {
                pureClassName = classname.Substring(indx + 1, classname.Length - indx - 1);
            }

            Tuple<List<int>,List<int>,List<int>, List<string>, List<List<int>>> result;
            List<int> methodHashes = new List<int>();
            List<int> methodNameHashes = new List<int>();
            List<int> argumentCount = new List<int>();
            List<string> methodArguments = new List<string>();
            List<List<int>> replaceableArgs = new List<List<int>>();

            bool hasNormalMethods = false;

            // Access memberdefs and search for the value of the definition tag
            foreach (XmlNode c in node.ChildNodes)
            {
                if (c.Name.Equals("memberdef"))
                {
                    List<XmlNode> parameters = getChildren("param", c.ChildNodes);
                    XmlNode type = getChild("type", c.ChildNodes);
                    XmlNode args = getChild("argsstring", c.ChildNodes);
                    XmlNode name = getChild("name", c.ChildNodes);

                    //// Retrieve the number of arguments and the name of the method 
                    //int methodCount = getCountOfArgs(args.InnerText);
                    argumentCount.Add(getCountOfArgs(args.InnerText));

                    String methodName = name.InnerText;

                    //df.addMethod(type.InnerText + " " + name.InnerText + convertMethodArgs(args.InnerText));

                    List<int> replaceableArguments = new List<int>();
                    replaceableArgs.Add(replaceableArguments);

                    if (parameters.Count > 0)
                    {
                        // Check if one of the method's arguments is a concrete class
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            XmlNode param = parameters[i];
                            // The parameter contains a reference to another concrete Dune class if the ref-tag is within the type-tag
                            if (getChild("ref", getChild("type", param.ChildNodes).ChildNodes) != null)
                            {
                                replaceableArguments.Add(i);
                            }
                        }
                    }

                    String methodArgs = convertMethodArgs(args.InnerText, true);
                    methodArguments.Add(convertMethodArgs(args.InnerText, false));

                    // In case that the method is a constructor...
                   if (pureClassName != null && name.InnerText.EndsWith(pureClassName))
                   {   
                     // add only the constructor WITH arguments. 
                     if (!methodArgs.Equals("()")) {
                        // In case of a constructor, the name remains empty
                        methodNameHashes.Add("".GetHashCode());
                        methodHashes.Add(methodArgs.GetHashCode());
                    }
                  }
                  else
                  {
                      hasNormalMethods = true;
                      methodNameHashes.Add(methodName.GetHashCode());
                      methodHashes.Add((type.InnerText + " " + name.InnerText + methodArgs).GetHashCode());
                  }
                }
            }

            return new Tuple<List<int>,List<int>,List<int>, List<string>, List<List<int>>, bool>(methodHashes, methodNameHashes, argumentCount, methodArguments, replaceableArgs, !hasNormalMethods);

        }

        /// <summary>
        /// Returns the number of arguments in the given string. Arguments are separated by comma.
        /// </summary>
        /// <param name="args">a <code>string</code> which contains the arguments (with preceeding brackets or not)</param>
        /// <returns>the number of arguments in the given string</returns>
        public static int getCountOfArgs(string args)
        {
            if (args.IndexOf(">") == args.IndexOf("<") + 1 || ((args.IndexOf(")") >= 0) && args.IndexOf(")") == args.IndexOf("(") + 1) || args.Trim().Equals(""))
            {
                return 0;
            }

            int count = 1;

            int level = 0;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case '<':
                        level++;
                        break;
                    case '>':
                        level--;
                        break;
                    case ',':
                        if (level == 0)
                        {
                            count++;
                        }
                        break;
                }
            }
            return count;
        }

        private static string convertMethodArgs(string args, bool withSufix)
        {
            if (args.Equals("") || args.Equals("()"))
            {
                return args;
            }

            string result = "";
            string sufix = "";
            bool paranthesis = false;
            if (args.IndexOf('(') >= 0)
            {
                paranthesis = true;
                sufix = args.Substring(args.LastIndexOf(')') + 1, args.Length - args.LastIndexOf(')') - 1);
                args = args.Substring(args.IndexOf('(') + 1, args.LastIndexOf(')') - args.IndexOf('(') - 1);
            }

            List<string> splitted = splitArgs(args);

            for (int i = 0; i < splitted.Count; i++)
            {
                string s = splitted[i];

                if (i > 0)
                {
                    result += ",";
                }

                string trimmed = s.Trim();

                bool name = true;

                for (int j = trimmed.Length - 1; j >= 0; j--)
                {
                    char c = trimmed[j];
                    if (!name)
                    {
                        result += trimmed.Substring(0, j + 1);
                        break;
                    } else if (c.Equals(' '))
                    {
                        name = false;
                    }
                }

            }
            if (paranthesis)
            {
                result = "(" + result + ")";
            }
            if (withSufix)
            {
                result += sufix;
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
        /// Returns a list of all nodes with the given name.
        /// </summary>
        /// <param name="name">the name of the tag to be searched for</param>
        /// <param name="list">the list which contains all children nodes</param>
        /// <returns>the list containing the nodes with the given name</returns>
        private static List<XmlNode> getChildren(String name, XmlNodeList list)
        {
            List<XmlNode> result = new List<XmlNode>();
            foreach (XmlNode child in list)
            {
                if (child.Name.Equals(name))
                {
                    result.Add(child);
                }
            }
            return result;
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
                return df = XMLParser.features[indx];
            }
            //else
            //{
            //    XMLParser.features.Add(df);
            //}
            return null;
        }

        /// <summary>
        /// Searches the list of all features for the feature with the given name.
        /// </summary>
        /// <param name="df">the feature containing the name to search for</param>
        /// <returns>the feature with the given name; <code>null</code> if no feature is found</returns>
        private static DuneFeature getFeatureByName(DuneFeature df)
        {
            String name = df.getClassName();
            
            foreach (DuneFeature d in features)
            {
                if (d.getClassNameWithoutTemplate().Equals(df.getClassNameWithoutTemplate()) && d.getTemplateArgumentCount() == df.getTemplateArgumentCount())
                {
                    return d;
                }
            }
            return null;
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
        private static void addToList(String className)
        {
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
        /// Returns a list containing all classes which are known with this name.
        /// </summary>
        /// <param name="name">the name of the class to search for</param>
        /// <returns>a list containing all classes which are known with the given name</returns>
        public static List<DuneFeature> getClassesWithName(String name)
        {
            List<DuneFeature> result = new List<DuneFeature>();
            foreach (DuneFeature f in features)
            {
                if (f.getClassNameWithoutTemplate().Equals(name))
                {
                    result.Add(f);
                }
            }
            return result;
        }

        /// <summary>
        /// This method extracts the information of the template.
        /// </summary>
        /// <param name="child">the xml-element containing the feature where the template should be extracted from</param>
        /// <returns>the string containing the template</returns>
        private static String extractTemplate(XmlNode child)
        {

            //bool found = false;
            //bool tooFar = false;

            //// The searched tag cannot be at index 0.
            //int i = 1;

            //while (!found && !tooFar)
            //{
            //     XmlNode c = child.ChildNodes.Item(i);
            //     if (c.Name.Equals("templateparamlist"))
            //     {
            //         found = true;
            //     }
            //     else if (!c.Name.Equals("includes") && !c.Name.Equals("derivedcompoundref") && !c.Name.Equals("basecompoundref") && !c.Name.Equals("innerclass"))
            //     {
            //         tooFar = true;
            //     }
            //     else
            //     {
            //         i++;
            //     }
            //}
            string result = "";

            //if (found)
            //{
            //XmlNode cur = child.ChildNodes.Item(i);
            //for (int j = 0; j < cur.ChildNodes.Count; j++ )
            for (int j = 0; j < child.ChildNodes.Count; j++)
            {
                // XmlNode c = cur.ChildNodes.Item(j);
                XmlNode c = child.ChildNodes.Item(j);

                if (j > 0)
                {
                    result += ",";
                }

                result += c.FirstChild.InnerText;
            }
            //}

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
            // needed in order to work on a copy of the string
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
