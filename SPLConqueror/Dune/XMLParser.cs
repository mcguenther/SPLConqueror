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

        static char TemplateStart = '<';

        // The root of the whole feature-tree.
        static DuneClass root = new DuneClass("", "root");

        static List<DuneClass> features = new List<DuneClass>();
        static List<DuneEnum> enums = new List<DuneEnum>();

        static Dictionary<DuneClass, String> templatesToAnalyze = new Dictionary<DuneClass, string>();

        static Dictionary<DuneClass, String> classesToAnalyze = new Dictionary<DuneClass, string>();

        static List<Tuple<DuneClass, DuneClass>> relations = new List<Tuple<DuneClass, DuneClass>>();

        static List<DuneClass> classesWithNoNormalMethods = new List<DuneClass>();

        static Dictionary<string, string> typeMapping = new Dictionary<string, string>();


        static Dictionary<String, DuneFeature> refIdToFeature = new Dictionary<string, DuneFeature>();
        public static Dictionary<String, List<DuneFeature>> nameWithoutPackageToDuneFeatures = new Dictionary<string, List<DuneFeature>>(); 

        

        // Is only here for debugging
        static System.IO.StreamWriter file;
        static List<String> classNames = new List<String>();

        static List<DuneClass> featuresNotFound = new List<DuneClass>();

        static StreamWriter output;
        static int notFound = 0;

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
                extractFeatures(child);
            }

            initFoundOutput();
            foreach (XmlNode child in childList)
            {
                buildRelations(child);
            }

            foreach (XmlNode child in childList)
            {
                extractTemplate(child);
            }
            foreach (DuneFeature feature in XMLParser.features)
            {
                templatePreProcessing_aliasReplacement(feature);
            }


            Console.WriteLine("number of if conditions in tremplates: " + ifConds);
            Console.WriteLine("easy to find : " + easyToFind);
            Console.WriteLine("not easy to find : " + notEasy);
            Console.WriteLine("mehrdeutigkeiten : " + mehrdeutigkeit);
            Console.WriteLine("further informaion : " + furtherInformation);

            closeFoundOutput();



            // Every class with no parent gets a connection to the root-node
            foreach (DuneFeature dfeature in features)
            {

                if (dfeature.GetType() != typeof(DuneClass))
                {
                    continue;
                }

                DuneClass df = (DuneClass)dfeature;

                if (!df.hasParents(root))
                {
                    // Add the root as a parent, so every node has a common node as parent in the transitive closure
                    df.addParent(root);
                    root.addChildren(df);
                }
            }

            System.Console.WriteLine("Done!");

            
            if(Program.USE_DUCK_TYPING)


            {
                System.Console.WriteLine("Now finding potential parents(duck-typing)");
                Stopwatch stopwatch = Stopwatch.StartNew();
                findPotentialParents();
                stopwatch.Stop();
                System.Console.WriteLine("\rFinished duck-typing. Time needed for duck-typing: " + stopwatch.Elapsed);
            }
            else
            {
                System.Console.WriteLine("Duck-typing is disabled.");
            }

            System.Console.Write("Writing the classes with no normal methods in a file...");
            printClassesWithNoNormalMethods();
            System.Console.WriteLine("Finished!");

        }

        /// <summary>
        /// In a template of a class, another class might be references (not only with a placeholder, here we have a real reference to another class). 
        /// Here, it might happen, that the referenced class also have template parameters. 
        /// Exampel:::
        /// Dune::Zero &lt;MultiIndex&lt;F,dim&gt;&gt;
        /// Dune::MultiIndex&lt;Field,dim&gt;
        /// 
        /// In this method, we ceatre an alias map for the template parameters of the inner class.
        /// 
        /// TODO: Find a better example. (One, where the F is also used in the outer class). 
        /// 
        /// The basic idea behin this is that the parameters of the outer most template are specified by the user, while the inner template-parameters might not be specified because we found the inner class in the alternative checking. 
        /// 
        /// 
        /// </summary>
        /// <param name="feature">the current feature</param>
        private static void templatePreProcessing_aliasReplacement(DuneFeature feature)
        {
            if (feature.GetType().Equals(typeof(DuneClass)))
            {
                LinkedList<TemplateTree> elements = ((DuneClass)feature).templat;
                

            }
        }

        /// <summary>
        /// Initializes the output to the file including the classes which were not found.
        /// </summary>
        private static void initFoundOutput()
        {
            output = new System.IO.StreamWriter(Program.DEBUG_PATH + "notFound.txt");
        }

        /// <summary>
        /// Closes the output stream to the file including the classes which were not found.
        /// </summary>
        private static void closeFoundOutput()
        {
            output.Flush();
            output.Close();
        }

        /// <summary>
        /// This method prints the classes in the list <code>classesWithNoNormalMethods</code>.
        /// </summary>
        private static void printClassesWithNoNormalMethods()
        {
            StreamWriter output = new System.IO.StreamWriter(Program.DEBUG_PATH + "classesWithNoNormalMethods.txt");
            foreach (DuneClass df in classesWithNoNormalMethods)
            {
                output.WriteLine(df);
            }
            output.Flush();
            output.Close();
        }

        /// <summary>
        /// Extracts the information needed for a <code>DuneClass</code> and also constructs one.
        /// </summary>
        /// <param name="child">the node in the xml-file pointing on the <code>compounddef</code> tag</param>
        private static void extractFeatures(XmlNode child)
        {
            DuneClass df = null;

            // Ignore private classes
            String prot = child.Attributes.GetNamedItem("prot") == null ? null : child.Attributes.GetNamedItem("prot").Value;
            String kind = child.Attributes.GetNamedItem("kind") == null ? null : child.Attributes.GetNamedItem("kind").Value;
            if (prot != null && prot.Equals("private") || kind != null && (kind.Equals("file") || kind.Equals("dir") || kind.Equals("example") || kind.Equals("group") || kind.Equals("namespace") || kind.Equals("page")))
            {
                return;
            }


            String template = "";
            String refId = child.Attributes["id"].Value.ToString();
            String name = "";
            String templateInName = "";
            List<Enum> enumerations = null;

            foreach (XmlNode node in child.ChildNodes)
            {
                switch (node.Name)
                {
                    case "compoundname":
                        name = node.InnerText.ToString();
                        templateInName = extractTemplateInName(name);
                        name = convertName(name);
                        break;
                    case "sectiondef":
                        if (node.Attributes.GetNamedItem("kind") != null)
                        {
                            switch (node.Attributes.GetNamedItem("kind").Value)
                            {
                                // Only the public types are crucial for saving enums
                                case "public-type":
                                    // Save the enums in the feature
                                    enumerations = saveEnums(node);
                                    break;
                                // Only the public functions are crucial for saving methods
                                case "public-func":
                                    saveTemplateMapping(node);
                                    break;
                            }
                        }
                        break;
                    case "templateparamlist":
                        template = extractOnlyTemplate(node);
                        break;
                }
            }

            df = new DuneClass(refId, name, template, templateInName);
            features.Add(df);

            string nameWithoutPackage = name.Substring(name.LastIndexOf(':')+1);

            if(!refIdToFeature.ContainsKey(refId))
                refIdToFeature.Add(refId, df);

            if(!nameWithoutPackageToDuneFeatures.ContainsKey(nameWithoutPackage))
                nameWithoutPackageToDuneFeatures.Add(nameWithoutPackage,new List<DuneFeature>());
            nameWithoutPackageToDuneFeatures[nameWithoutPackage].Add(df);
            

            // This boolean indicates if the current child is an interface, an abstract class or a normal class.
            Boolean structClass = child.Attributes.GetNamedItem("kind").Value.Equals("struct");

            df.setType(structClass, child.Attributes.GetNamedItem("abstract") != null);

            if (enumerations != null)
            {
                foreach (Enum enumObject in enumerations)
                {
                    DuneEnum de = new DuneEnum(name, enumObject);
                    refIdToFeature.Add(de.getReference(), de);
                    enums.Add(de);
                }
            }


            return;
        }

        /// <summary>
        /// Builds the relations to the other classes.
        /// </summary>
        /// <param name="child">the node containing the class whose relations should be added</param>
        private static void buildRelations(XmlNode child)
        {
            // Ignore private classes
            String prot = child.Attributes.GetNamedItem("prot") == null ? null : child.Attributes.GetNamedItem("prot").Value;
            String kind = child.Attributes.GetNamedItem("kind") == null ? null : child.Attributes.GetNamedItem("kind").Value;
            if (prot != null && prot.Equals("private") || kind != null && (kind.Equals("file") || kind.Equals("dir") || kind.Equals("example") || kind.Equals("group") || kind.Equals("namespace") || kind.Equals("page")))
            {
                return;
            }

            Dictionary<String, String> templateTypeMapping = null;
            String template = "";
            String refId = child.Attributes["id"].Value.ToString();
            String name = "";
            String templateInName = "";
            List<DuneClass> inherits = new List<DuneClass>();
            MethodList methods = null;

            foreach (XmlNode node in child.ChildNodes)
            {
                switch (node.Name)
                {
                    case "compoundname":
                        name = node.InnerText.ToString();
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

                        DuneClass newDF = new DuneClass(refNew, nameNew);

                        inherits.Add(newDF);
                        break;
                    case "templateparamlist":
                        //Tuple<String, Dictionary<String, String>> templateParams = 
                        //extractTemplate(node);
                        //template = templateParams.Item1;
                        //templateTypeMapping = templateParams.Item2;
                        break;
                    case "sectiondef":
                        if (node.Attributes.GetNamedItem("kind") != null)
                        {
                            switch (node.Attributes.GetNamedItem("kind").Value)
                            {
                                // Only the public functions are crucial for saving methods
                                case "public-func":
                                    methods = saveMethods(node, name, templateTypeMapping);
                                    break;
                            }
                        }
                        break;
                }
            }

            DuneClass df = getClass(new DuneClass(refId, name, template, templateInName));

            if (methods != null)
            {
                df.setMethods(methods.getMethodHashes());
                df.setMethodNameHashes(methods.getMethodNameHashes());
                df.setMethodArgumentCount(methods.getArgumentCount());
                df.setMethodArguments(methods.getMethodArguments());
                df.setReplaceableMethodArguments(methods.getReplaceableArguments());
                df.ignoreAtDuckTyping(methods.classHasNormalMethods());
                if (!methods.classHasNormalMethods())
                {
                    classesWithNoNormalMethods.Add(df);
                }
            }
            else
            {
                classesWithNoNormalMethods.Add(df);
            }

            // Now add all relations
            foreach (DuneClass inherit in inherits)
            {
                DuneClass newDF = getClass(inherit);
                if (newDF != null)
                {

                    df.addParent(newDF);
                    newDF.addChildren(df);
                }
                else
                {
                    // If the class is still not found, it will be matched by name.
                    if (newDF == null)
                    {
                        newDF = getFeatureByName(inherit);
                    }

                    if (newDF != null)
                    {
                        newDF.addChildren(df);
                        df.addParent(newDF);
                    }
                    else
                    {
                        if (!featuresNotFound.Contains(inherit))
                        {
                            featuresNotFound.Add(inherit);
                            notFound++;
                            output.WriteLine(inherit);
                        }
                    }
                }
            }
            output.Flush();
        }

        /// <summary>
        /// Returns the variability of the given <code>DuneClass</code>.
        /// </summary>
        /// <param name="df">the class to return the variability for</param>
        /// <returns>the variability of the class or enum</returns>
        public static List<String> getVariability(DuneFeature df)
        { 
            if (df == null)
            {
                return null;
            }

            return df.getVariability(root);
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

            DuneFeature df;

            df = searchForFeature(new DuneClass("", feature));

            if (df == null || template.Equals(""))
            {
                df = searchForFeature(new DuneClass("", feature + "<>"));
            }

            if (df == null)
            {
                int colonIndex = feature.LastIndexOf(':');
                String enumNamespace = feature.Substring(0, colonIndex - 1);
                String enumValue = feature.Substring(colonIndex + 1, feature.Length - colonIndex - 1);
                df = searchForEnum(enumNamespace, enumValue);
            }

            if (df != null)
            {
                return df.getVariability(root);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Searches for the enum containing the given value.
        /// </summary>
        /// <param name="enumNamespace">the namespace of the enum, typically the surrounding class</param>
        /// <param name="value">the value of the enum to search for</param>
        /// <returns>the <code>enum</code> if a enum is found containing the given value; <code>null</code> otherwise</returns>
        private static DuneEnum searchForEnum(String enumNamespace, String value)
        {
            foreach (DuneEnum de in enums)
            {
                if (de.getNamespace().Equals(enumNamespace) && de.getValues().Contains(value))
                {
                    return de;
                }
            }
            return null;
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
            file = new System.IO.StreamWriter(Program.DEBUG_PATH + "inherits.txt");

            List<DuneClass> featuresToCompare = new List<DuneClass>();

            // Compare only classes with at least one public method
            foreach (DuneClass df in features)
            {
                if (df.getNumberOfMethodHashes() > 0)
                {
                    featuresToCompare.Add(df);
                }
            }

            List<Tuple<DuneClass, DuneClass>> toInsert = new List<Tuple<DuneClass, DuneClass>>();

            int total = featuresToCompare.Count;
            int finished = 0;
            // The newer version with optimizations
            foreach (DuneClass df in featuresToCompare)
            {
                finished++;
                if (df.isIgnored())
                {
                    continue;
                }
                //System.Console.WriteLine(df.ToString());

                // Show the progress bar:
                Console.Write("\r{0}%   ", finished * 100 / total);

                foreach (DuneClass comp in featuresToCompare)
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
                            toInsert.Add(new Tuple<DuneClass, DuneClass>(comp, df));
                            file.WriteLine(df.ToString() + " -> " + comp.ToString());
                        }
                    }

                }
            }

            // Only now the relations are added.
            foreach (Tuple<DuneClass, DuneClass> t in toInsert)
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
        private static bool variableSubmethod(DuneClass df, DuneClass comp, int index)
        {
            List<Tuple<string, List<int>>> potentialMethods = df.getMethodArgumentsWithNameAndCount(comp.getMethodNameHash(index), comp.getMethodArgumentCount(index));
            foreach (Tuple<string, List<int>> t in potentialMethods)
            {
                if (isSubmethod(t, comp.getMethodArguments(index)))
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
        /// This method saves the enums of the respective class in the corresponding Dictionary-element from the DuneClass-class.
        /// </summary>
        /// <param name="node">the object containing all information about the class/interface</param>
        /// <returns>a <code>Dictionary</code> which contains the name of the enums and its elements</returns>
        private static List<Enum> saveEnums(XmlNode node)
        {
            List<Enum> result = new List<Enum>();
            // Access memberdefs and search for the value of the definition tag
            foreach (XmlNode c in node.ChildNodes)
            {
                if (c.Name.Equals("memberdef") && c.Attributes.GetNamedItem("kind") != null && c.Attributes.GetNamedItem("kind").Value.Equals("enum"))
                {
                    String reference = "";
                    XmlNode enumName = getChild("name", c.ChildNodes);
                    List<String> enumNames = new List<String>();

                    foreach (XmlAttribute attribute in c.Attributes)
                    {
                        if (attribute.Name.Equals("id"))
                        {
                            reference = attribute.Value;
                            break;
                        }
                    }

                    // Extract the enum-options
                    foreach (XmlNode enumvalue in c.ChildNodes)
                    {
                        if (enumvalue.Name.Equals("enumvalue"))
                        {
                            enumNames.Add(getChild("name", enumvalue.ChildNodes).InnerText);

                        }
                    }

                    result.Add(new Enum(reference, enumName.InnerText, enumNames));
                }
            }

            return result;

        }

        /// <summary>
        /// Saves the methods of the class/interface
        /// </summary>
        /// <param name="node">the object containing all information about the class/interface</param>
        /// <param name="classname">The name of the class</param>
        /// <param name="templateTypeMapping">the mapping from the template name of the type to its type</param>
        /// <returns>a tuple with a list containing the method hashes, a list containing the hash of the method names and a list containing the count of the arguments (in this order)</returns>
        private static MethodList saveMethods(XmlNode node, String classname, Dictionary<String, String> templateTypeMapping)
        {
            // The pure class name (e.g. 'x' in 'Dune::y::x') is needed in order to identify the constructor
            int indx = classname.LastIndexOf(':');
            String pureClassName = null;
            if (indx >= 0)
            {
                pureClassName = classname.Substring(indx + 1, classname.Length - indx - 1);
            }

            if (pureClassName != null && (pureClassName.Equals("GridDefaultImplementation") || pureClassName.Equals("UGGrid")))
            {
                System.Console.Write("");
            }

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
                    XmlNode template = getChild("templateparamlist", c.ChildNodes);
                    List<XmlNode> parameters = getChildren("param", c.ChildNodes);
                    XmlNode type = getChild("type", c.ChildNodes);
                    XmlNode args = getChild("argsstring", c.ChildNodes);
                    XmlNode name = getChild("name", c.ChildNodes);

                    String methodName = name.InnerText;

                    //df.addMethod(type.InnerText + " " + name.InnerText + convertMethodArgs(args.InnerText));

                    List<int> replaceableArguments = new List<int>();

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

                    String methodArgs = convertMethodArgs(args.InnerText, true).Trim();

                    // In case that the method is a constructor...
                    if (pureClassName != null && name.InnerText.EndsWith(pureClassName))
                    {
                        // add only the constructor WITH arguments. 
                        if (!methodArgs.Equals("()"))
                        {
                            // In case of a constructor, the name remains empty
                            methodNameHashes.Add("".GetHashCode());
                            methodHashes.Add(methodArgs.GetHashCode());
                            methodArguments.Add(convertMethodArgs(args.InnerText, false));
                            // Retrieve the number of arguments and the name of the method 
                            argumentCount.Add(getCountOfArgs(args.InnerText));
                            replaceableArgs.Add(replaceableArguments);
                        }
                    }
                    else
                    {
                        hasNormalMethods = true;
                        methodNameHashes.Add(methodName.GetHashCode());
                        //methodHashes.Add((type.InnerText + " " + name.InnerText + methodArgs).GetHashCode());
                        string typename = retrieveType(type, template, templateTypeMapping);
                        methodHashes.Add((typename + " " + name.InnerText + methodArgs).GetHashCode());
                        methodArguments.Add(convertMethodArgs(args.InnerText, false));
                        // Retrieve the number of arguments and the name of the method 
                        argumentCount.Add(getCountOfArgs(args.InnerText));
                        replaceableArgs.Add(replaceableArguments);
                    }
                }
            }

            return new MethodList(methodHashes, methodNameHashes, argumentCount, methodArguments, replaceableArgs, !hasNormalMethods);

        }

        /// <summary>
        /// Saves the mapping given in the template
        /// </summary>
        /// <param name="node">the node including the template</param>
        private static void saveTemplateMapping(XmlNode node)
        {
            // Access memberdefs and search for the value of the definition tag
            foreach (XmlNode c in node.ChildNodes)
            {
                if (c.Name.Equals("memberdef"))
                {
                    XmlNode template = getChild("templateparamlist", c.ChildNodes);

                    if (template == null)
                    {
                        continue;
                    }

                    // Retrieve the mapping from the template parameter
                    foreach (XmlNode templateParam in template.ChildNodes)
                    {
                        XmlNode declname = getChild("declname", templateParam.ChildNodes);
                        XmlNode typeNode = getChild("type", templateParam.ChildNodes);

                        if (declname != null && typeNode != null)
                        {
                            string key = declname.InnerText;
                            string val = typeNode.InnerText;
                            if (!typeMapping.ContainsKey(key))
                            {
                                typeMapping.Add(key, val);
                            }
                            //else
                            //{
                            //    string currentValue ="";
                            //    typeMapping.TryGetValue(key, out currentValue);
                            //    if (!currentValue.Equals(val))
                            //    {
                            //        System.Console.WriteLine("Key: " + key + "; Current value: " + currentValue + "; Other value: " + val);
                            //    }
                            //}
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the text in between the type-tag. In this method, the template parameter are replaced by their types for better results in duck typing.
        /// </summary>
        /// <param name="type">the node with the type tag of the xml file</param>
        /// <param name="template">the node with the templateparamlist</param>
        /// <returns>the string in between the type tag where the name of the template parameter are replaced by their types</returns>
        private static string retrieveType(XmlNode type, XmlNode template, Dictionary<String, String> templateTypeMapping)
        {
            string text = type.InnerText;



            if (template != null)
            {

                // Firstly, apply the mapping from the template parameter
                foreach (XmlNode templateParam in template.ChildNodes)
                {
                    XmlNode declname = getChild("declname", templateParam.ChildNodes);
                    XmlNode typeNode = getChild("type", templateParam.ChildNodes);

                    if (declname != null && typeNode != null && !typeMapping.ContainsKey(declname.InnerText))
                    {
                        string key = declname.InnerText;
                        string val = typeNode.InnerText;
                        text = text.Replace(" " + key + " ", " " + val + " ");
                        text = text.Replace(" " + key + ",", " " + val + ",");
                    }
                }
            }

            if (templateTypeMapping != null)
            {
                // Apply the mapping from the template parameter list of the class itself
                foreach (String templateTypeName in templateTypeMapping.Keys)
                {
                    String templateType;
                    templateTypeMapping.TryGetValue(templateTypeName, out templateType);

                    text = text.Replace(" " + templateTypeName + " ", " " + templateType + " ");
                    text = text.Replace(" " + templateTypeName + ",", " " + templateType + ",");

                }
            }

            // DEBUG
            if (text.Contains(" k ") || text.Contains(" k,") || text.Contains(" dorder ") || text.Contains(" dorder,") || text.Contains(" size ") || text.Contains(" size,"))
            {
                System.Console.WriteLine("Found a class that uses k, dorder or size from the global dictionary...");
            }

            // Apply the mapping on the inner text of the type node using the global mapping
            foreach (string key in typeMapping.Keys)
            {
                string val;
                typeMapping.TryGetValue(key, out val);
                text = text.Replace(" " + key + " ", " " + val + " ");
                text = text.Replace(" " + key + ",", " " + val + ",");
            }
            return text;
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
                    }
                    else if (c.Equals(' '))
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

        private static XmlAttribute getAttribute(String name, XmlNode parent)
        {
            foreach (XmlAttribute attribute in parent.Attributes)
            {
                if (attribute.Name.Equals(name))
                {
                    return attribute;
                }
            }
            return null;
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
        private static DuneClass searchForFeature(DuneClass df)
        {
            foreach (DuneClass d in features)
            {
                if (d.getFeatureName().Equals(df.getFeatureName()))
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
        private static DuneClass searchForFeatureName(DuneClass df)
        {
            foreach (DuneClass d in features)
            {
                if (d.getFeatureNameWithoutTemplate().Equals(df.getFeatureNameWithoutTemplate()))
                {
                    return d;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the found features in a list. Note that this list is empty if there are no matches.
        /// </summary>
        /// <param name="df">the feature to search for</param>
        /// <returns>the feature if it was found; <code>null</code> otherwise</returns>
        private static List<DuneFeature> searchForAllClassNames(DuneClass df)
        {
            List<DuneFeature> dfs = new List<DuneFeature>();
            foreach (DuneClass d in features)
            {
                if (d.getFeatureNameWithoutTemplate().Equals(df.getFeatureNameWithoutTemplate()))
                {
                    dfs.Add(d);
                }
            }
            return dfs;
        }

        /// <summary>
        /// Returns the given DuneClass if the feature is not already in the features-list; the feature in the features-list is returned otherwise.
        /// </summary>
        /// <param name="df">the feature to search for</param>
        /// <returns>the given DuneClass if the feature is not already in the features-list; the feature in the features-list is returned otherwise</returns>
        private static DuneClass getClass(DuneClass df)
        {
            int indx = XMLParser.features.IndexOf(df);
            if (indx >= 0 && XMLParser.features[indx].GetType() == typeof(DuneClass))
            {
                return df =(DuneClass) XMLParser.features[indx];
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
        private static DuneClass getFeatureByName(DuneClass df)
        {
            String name = df.getFeatureName();

            foreach (DuneFeature dfeature in features)
            {
                if (dfeature.GetType() != typeof(DuneClass))
                {
                    continue;
                }

                DuneClass d = (DuneClass)dfeature;

                if (d.getFeatureNameWithoutTemplate().Equals(df.getFeatureNameWithoutTemplate()) && d.getTemplateArgumentCount() == df.getTemplateArgumentCount())
                {
                    return d;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a list containing all features that match by the name.
        /// </summary>
        /// <param name="feature">the feature to search for</param>
        /// <returns>a list containing all features that match by the given name</returns>
        public static List<DuneFeature> getAllFeaturesByName(String feature)
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

            List<DuneFeature> alternatives = new List<DuneFeature>();

            DuneFeature df;

            df = searchForFeature(new DuneClass("", feature));

            int colonIndex = feature.LastIndexOf(':');
            String enumNamespace = feature.Substring(0, colonIndex - 1);

            // If not found search for the enum
            if (df == null)
            {
                String enumValue = feature.Substring(colonIndex + 1, feature.Length - colonIndex - 1);

                df = searchForEnum(enumNamespace, enumValue);
            }

            // If still not found, the class with the given prefix is searched
            if (df == null)
            {
                return searchForAllClassNames(new DuneClass("", enumNamespace));
            } else
            {
                List<DuneFeature> dfs = new List<DuneFeature>();
                dfs.Add(df);
                return dfs;
            }
        }

        /// <summary>
        /// Analyzes the given template
        /// </summary>
        /// <param name="f">the feature the template belongs to</param>
        /// <param name="template">the template to be analyzed</param>
        private static void analyzeTemplate(DuneClass f, String template, Tree root, ref Dictionary<String, Tree> nonIdentifiedNodes, ref List<Tuple<Tree, String>> todo)
        {
            template = template.Trim();
            int level = 0;
            int i = 0;
            int from = 0;
            DuneClass n = null;
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
                            DuneClass df = getFeature(template.Substring(currentBeginning, i - currentBeginning).Trim());

                            // TODO Debug
                            if (df == null)
                            {
                                addToList(template.Substring(currentBeginning, i - currentBeginning).Trim());

                                // Debug
                                //if (template.Substring(currentBeginning, i - currentBeginning).Trim().Equals("1"))
                                //{
                                //    System.Console.WriteLine("1: " + f.getFeatureName());
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
        private static DuneClass getFeature(String className)
        {
            foreach (DuneClass f in features)
            {
                if (f.getFeatureName().Equals(className))
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
        public static List<DuneClass> getClassesWithName(String name)
        {
            List<DuneClass> result = new List<DuneClass>();
            foreach (DuneClass f in features)
            {
                if (f.getFeatureNameWithoutTemplate().Equals(name))
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
        private static String extractOnlyTemplate(XmlNode child)
        {

            string template = "";

            //bool found = false;
            //bool tooFar = false;
            for (int j = 0; j < child.ChildNodes.Count; j++)
            {
                // XmlNode c = cur.ChildNodes.Item(j);
                XmlNode c = child.ChildNodes.Item(j);

                if (j > 0)
                {
                    template += ",";
                }

                template += c.FirstChild.InnerText;
            }

            return template;
        }

        public static int ifConds = 0;
        public static int easyToFind = 0;
        public static int notEasy = 0;
        public static int mehrdeutigkeit = 0;
        public static int furtherInformation = 0; 
        /// <summary>
        /// This method extracts the information of the template.
        /// </summary>
        /// <param name="child">the xml-element containing the feature where the template should be extracted from</param>
        private static TemplateTree extractTemplate(XmlNode world)
        {
            TemplateTree templateTree = null;

            bool hasTemplate = false;
            string className = world.FirstChild.InnerText;
            string name = className;
            if (name.Contains(TemplateStart))
            {
                name = name.Substring(0, name.IndexOf(TemplateStart));
                hasTemplate = true;
            }

            // analyse the templateparamlist-elements.
            // I assume, here we have one element for each placeholder in the template
            XmlNode type = getChild("templateparamlist", world.ChildNodes);
            if (type != null)
            {
                foreach (XmlNode node in type.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "param":

                            String declmame_cont = "";
                            String defval_cont = "";
                            String defVal_cont_ref = "";
                            String defVal_cont_ref_id = "";
                            String defname_cont = "";
                            String deftype_cont = "";

                            foreach (XmlNode innerNode in node.ChildNodes)
                            {
                                switch (innerNode.Name)
                                {
                                    case "declname":
                                        declmame_cont = innerNode.InnerText;
                                        break;
                                    case "defval":
                                        defval_cont = innerNode.InnerText;
                                        if (getChild("ref", innerNode.ChildNodes) != null)
                                        {
                                            defVal_cont_ref = getChild("ref", innerNode.ChildNodes).InnerText;
                                            XmlAttribute att = getAttribute("refid", getChild("ref", innerNode.ChildNodes));
                                            if (att != null)
                                            {
                                                defVal_cont_ref_id = att.InnerText;
                                            }
                                            string defValueTemplate = innerNode.InnerText;
                                            if(defValueTemplate.Length >0){
                                                Console.WriteLine("asd");
                                            }
                                        }
                                        break;
                                    case "defname":
                                        defname_cont = innerNode.InnerText;
                                        break;
                                    case "type":
                                        deftype_cont = innerNode.InnerText;
                                        break;
                                    default:
                                        Console.WriteLine(innerNode.Name+" is not considered in the template extraction");
                                        break;
                                }
                            }

                            if (getChild("declname", node.ChildNodes) != null)
                            {

                                string placeHolderName = getChild("declname", node.ChildNodes).InnerText;
                                XmlNode defval = getChild("defval", node.ChildNodes);
                                //String typ = getChild("defval", node.ChildNodes).InnerText;
                                if (defval != null)
                                {
                                    if (getChild("ref", defval.ChildNodes) != null)
                                    {
                                        String defValue = getChild("ref", defval.ChildNodes).InnerText;
                                        XmlAttribute att = getAttribute("refid", getChild("ref", defval.ChildNodes));
                                        if (att != null)
                                        {
                                            String refId = att.InnerText;
                                        }
                                    }
                                    string defValueTemplate = defval.InnerText;
                                    //Console.WriteLine(defValueTemplate);
                                }
                            }
                            break;
                    }
                }
            }



            if (hasTemplate)
            {
                string templateString = className.Substring(name.Count());
                templateString = templateString.Replace("&", "");
                templateString = templateString.Replace("*", "");
                templateString = templateString.Replace(",", "");
                templateString = templateString.Replace(">", " > ");
                templateString = templateString.Replace("<", " < ");

                while (templateString.Contains("  "))
                    templateString = templateString.Replace("  ", " ");
                templateString = templateString.Trim();

                // remove first < and last > because we know implicitly that they are there 
                templateString = templateString.Substring(1, templateString.Count() - 2);
                templateString = templateString.Trim();


                if (templateString.Contains("enable_if"))
                {
                    ifConds += 1;
                    Console.WriteLine("");
                    return null; 
                }
                // template splitting
                // in einem template sind entweder terminale Elemente oder Klassen, die selbst wieder Elemente besitzen. 

                templateTree = new TemplateTree();
                String[] templateParts = templateString.Split(' ');
                for (int i = 0; i < templateParts.Count(); i++)
                {
                    string token = templateParts[i];
                    double val = 0;
                    if(Double.TryParse(token,out val))
                    {
                        easyToFind += 1;
                    }

                    if(token.Equals("<"))
                    {
                        // increase hierarchy
                        templateTree.incHierarchy();
                    }else if (token.Equals(">"))
                    {
                        // decrease hierarchy
                        templateTree.decHierarchy();
                    }
                    else if (token.StartsWith("::"))
                    {
                        // here, futher information for the last terminal elemenent on the same level can be derived
                        furtherInformation += 1;
                        templateTree.addFurtherInformation(token);
                    }
                    else
                    {
                        templateTree.addInformation(token);
                    }
                    if (!token.Equals("<") && !token.Equals(">"))
                    {
                        notEasy += 1;
                    }

                }
                Console.WriteLine("OrgString " + className);
            }
            if(templateTree != null)
                Console.WriteLine("parsed:: " + templateTree.toString());
            //Console.WriteLine("");

            //refIdToFeature[refIdToFeature]

            return templateTree;

  
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
