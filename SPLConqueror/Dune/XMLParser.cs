using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Dune.util;

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
        public static DuneClass root = new DuneClass("", "root");

        public static List<DuneClass> featuresWithPublicMethods = new List<DuneClass>();
        public static List<DuneClass> features = new List<DuneClass>();
        public static List<DuneEnum> enums = new List<DuneEnum>();

        static Dictionary<DuneClass, String> templatesToAnalyze = new Dictionary<DuneClass, string>();

        static Dictionary<DuneClass, String> classesToAnalyze = new Dictionary<DuneClass, string>();

        static List<Tuple<DuneClass, DuneClass>> relations = new List<Tuple<DuneClass, DuneClass>>();

        static List<DuneClass> classesWithNoNormalMethods = new List<DuneClass>();

        static Dictionary<string, string> typeMapping = new Dictionary<string, string>();

        static Dictionary<DuneFeature, List<DuneFeature>> alternativeClasses = new Dictionary<DuneFeature, List<DuneFeature>>();


        static Dictionary<String, DuneFeature> refIdToFeature = new Dictionary<string, DuneFeature>();
        public static Dictionary<String, List<DuneFeature>> nameWithoutPackageToDuneFeatures = new Dictionary<string, List<DuneFeature>>();
        public static Dictionary<String, List<DuneFeature>> nameWithPackageToDuneFeatures = new Dictionary<string, List<DuneFeature>>();



        // Is only here for debugging
        static System.IO.StreamWriter file;
        static List<String> classNames = new List<String>();
        static int typedefCounter = 0;
        static int stdCounter = 0;

        static int globalNumerator = 1;

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

            //List<XmlNode> namespaces = new List<XmlNode>();

            //foreach (XmlNode child in childList)
            //{
            //    if (child.Attributes.GetNamedItem("kind") != null && !child.Attributes.GetNamedItem("kind").Equals("namespace"))
            //        extractFeatures(child);
            //    else
            //        namespaces.Add(child);
            //}

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
            Console.WriteLine("further informaion for template elements : " + tempElementsWithFurtherInformation);
            Console.WriteLine("only info in paramlist " + onlyInParamList);
            Console.WriteLine("multi refs in one param " + multiRefsInOneParam);
            Console.WriteLine("multi refs in einem defVal " + defValRefCountGlob);
            Console.WriteLine("id not found... " + idNotFound);


            closeFoundOutput();

            //Environment.Exit(0);

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


            if (Program.USE_DUCK_TYPING)
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

        static List<String> innerClassNames = new List<string>();

        /// <summary>
        /// Extracts the information needed for a <code>DuneClass</code> and also constructs one.
        /// </summary>
        /// <param name="child">the node in the xml-file pointing on the <code>compounddef</code> tag</param>
        private static void extractFeatures(XmlNode child)
        {
            // TODO:::: Input: Dune::ALUGrid&lt; 3, 3, elType, refineType, Comm &gt;
            // Output::: name of the feature: Dune::ALUGrid
                // Liste der Template Parameter: [3,3, elType -> ALUGridElementType, refineType -> ALUGridRefinementType, Comm]


            DuneClass df = null;

            // Ignore private classes
            String prot = child.Attributes.GetNamedItem("prot") == null ? null : child.Attributes.GetNamedItem("prot").Value;
            String kind = child.Attributes.GetNamedItem("kind") == null ? null : child.Attributes.GetNamedItem("kind").Value;
            if (prot != null && prot.Equals("private") || kind != null && (kind.Equals("file") || kind.Equals("dir") || kind.Equals("example") || kind.Equals("page"))) //  || kind.Equals("group") || kind.Equals("namespace")
            {
                return;
            }

            String template = "";
            String refId = child.Attributes["id"].Value.ToString();
            String name = "";
            String templateInName = "";
            String suffix = "";
            List<Enum> enumerations = null;

            bool hasPublicMethods = false;

            foreach (XmlNode node in child.ChildNodes)
            {
                switch (node.Name)
                {
                    case "memberdef":
                        // extract inner classes
                        name = node.InnerText.ToString();
                        Console.WriteLine("innernamespace  " + name);
                        innerClassNames.Add(name);
                        break;

                    case "compoundname":
                        name = node.InnerText.ToString();
                        name = name.Replace("<", " < ");
                        name = name.Replace(">", " > ");
                        while (name.Contains("  "))
                        {
                            name = name.Replace("  ", " ");
                        }
                        templateInName = extractTemplateInName(name);
                        if (!templateInName.Equals(String.Empty) && name.LastIndexOf("::") > name.LastIndexOf(">"))
                        {
                            suffix = name.Substring(name.LastIndexOf("::") + 2);
                        }
                        name = convertName(name);

                        if (name.Contains("helper") || name.Contains("Helper"))
                        {
                            return;
                        }
                        break;
                    case "sectiondef":
                        if (node.Attributes.GetNamedItem("kind") != null)
                        {

                            switch (node.Attributes.GetNamedItem("kind").Value)
                            {
                                //case "user-defined":
                                //case "typedef":
                                 
                                //case "group":
                                case "enum":
                                case "public-type":
                                    // Save the enums in the feature
                                    enumerations = saveEnums(node, name);

                                    break;
                                // Only the public functions are crucial for saving methods
                                case "public-func":
                                    hasPublicMethods = true;
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

            df = new DuneClass(refId, name, template, templateInName, suffix);

            features.Add(df);

            if (hasPublicMethods)
            {
                featuresWithPublicMethods.Add(df);
            }
            

            string nameWithoutPackage = df.getFeatureNameWithoutTemplateAndNamespace();

            if (!refIdToFeature.ContainsKey(refId))
                refIdToFeature.Add(refId, df);

            if (!nameWithoutPackageToDuneFeatures.ContainsKey(nameWithoutPackage))
                nameWithoutPackageToDuneFeatures.Add(nameWithoutPackage, new List<DuneFeature>());
            
            nameWithoutPackageToDuneFeatures[nameWithoutPackage].Add(df);

            string nameWithPackage = df.getFeatureNameWithoutTemplate();

            if (!nameWithPackageToDuneFeatures.ContainsKey(nameWithPackage))
            {
                nameWithPackageToDuneFeatures.Add(nameWithPackage, new List<DuneFeature>());
            }
            nameWithPackageToDuneFeatures[nameWithPackage].Add(df);


            // This boolean indicates if the current child is an interface, an abstract class or a normal class.
            Boolean structClass = child.Attributes.GetNamedItem("kind").Value.Equals("struct");

            df.setType(structClass, child.Attributes.GetNamedItem("abstract") != null);

            if (enumerations != null)
            {
                // Add the enums to the refIDToFeature-mapping
                foreach (Enum enumObject in enumerations)
                {
                    DuneEnum de = new DuneEnum(name, enumObject);

                    // It may happen that an enum appears multiple times
                    if (refIdToFeature.ContainsKey(de.getReference()))
                    {
                        DuneFeature d = null;
                        refIdToFeature.TryGetValue(de.getReference(), out d);
                        if (!de.Equals(d))
                        {
                            Console.WriteLine("The reference " + de.getReference() + " occurs multiple times for different enums!");
                        }
                    }
                    else
                    {
                        refIdToFeature.Add(de.getReference(), de);

                        // Add also the values of the enums because they have also a reference ID
                        foreach (DuneEnumValue dev in enumObject.getValueObjects())
                        {
                            if (refIdToFeature.ContainsKey(dev.getReference()))
                            {
                                DuneFeature comp;
                                refIdToFeature.TryGetValue(dev.getReference(), out comp);
                                if (!comp.getFeatureName().Equals(dev.getFeatureName()))
                                {
                                    Console.WriteLine("The reference " + de.getReference() + " occurs multiple times for different enum values!");
                                }
                            }
                            else
                            {
                                refIdToFeature.Add(dev.getReference(), dev);
                            }
                        }

                        enums.Add(de);
                    }
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
            if (prot != null && prot.Equals("private") || kind != null && (kind.Equals("file") || kind.Equals("dir") || kind.Equals("example") || kind.Equals("page"))) // || kind.Equals("group") || kind.Equals("namespace")
            {
                return;
            }

            bool isNamespace = kind != null && kind.Equals("namespace");

            Dictionary<String, String> templateTypeMapping = null;
            String template = "";
            String refId = child.Attributes["id"].Value.ToString();
            String name = "";
            String templateInName = "";
            String suffix = "";
            List<DuneClass> inherits = new List<DuneClass>();
            MethodList methods = null;
            int max = -1;
            int min = -1;

            List<String> alternativeRefIds = new List<String>();

            foreach (XmlNode node in child.ChildNodes)
            {
                switch (node.Name)
                {
                    case "compoundname":
                        name = node.InnerText.ToString();
                        templateInName = extractTemplateInName(name);
                        if (!templateInName.Equals(String.Empty) && name.LastIndexOf("::") > name.LastIndexOf(">"))
                        {
                            suffix = name.Substring(name.LastIndexOf("::") + 2);
                        }
                        name = convertName(name);
                        if (name.Contains("helper") || name.Contains("Helper"))
                        {
                            return;
                        }
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
                        // Set the range of the template
                        max = node.ChildNodes.Count;
                        min = max;
                        foreach (XmlNode param in node.ChildNodes)
                        {
                            if (getChild("defval", param.ChildNodes) != null)
                            {
                                min--;
                            }

                        }
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
                                case "user-defined":
                                case "typedef":
                                case "enum":
                                    foreach (XmlNode c in node.ChildNodes)
                                    {
                                        if (c.Name.Equals("memberdef") && c.Attributes.GetNamedItem("kind") != null && c.Attributes.GetNamedItem("kind").Value.Equals("typedef"))
                                        {
                                            String id = c.Attributes["id"].InnerText;
                                            String localName = getChild("name", c.ChildNodes).InnerText;
                                            XmlNode type = getChild("type", c.ChildNodes);
                                            XmlNode definition = getChild("definition", c.ChildNodes);
                                            alternativeRefIds.Add(id);
                                            if (!refIdToFeature.ContainsKey(id))
                                                refIdToFeature.Add(id, new DuneTypeDef(id, localName));
                                            else if (!isNamespace)
                                                Console.Write("");

                                        }
                                    }
                                    break;

                            }
                        }
                        break;
                }
            }

            DuneClass df = getClass(new DuneClass(refId, name, template, templateInName, suffix));

            // Abort if the class was not loaded previously.
            if (df == null)
            {
                return;
            }

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

            // Set the range; Note that this can be variable because of the default values.
            if (min > -1 && max > -1)
            {
                df.setTemplateArgumentCount(min, max);
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

            foreach (String alternativeRefId in alternativeRefIds)
            {
                //refIdToFeature.Add(alternativeRefId, df);

                if (alternativeRefId.Equals("group__GridFunctionSpace_1ga4ac2e3483d6f96786582dfb56a3eaaca"))
                {
                    String c = df.getFeatureName();
                    Console.WriteLine(c);
                }

            }


            output.Flush();
        }

        /// <summary>
        /// Returns the XmlNode with a shortened InnerText.
        /// </summary>
        /// <param name="node">the node standing for the definition-tag of the typedef</param>
        /// <param name="type">the node standing for the type-tag of the typedef</param>
        /// <returns>the XmlNode with a shortened Innertext-element</returns>
        internal static XmlNode shortenDefinition(XmlNode node, XmlNode type)
        {
            XmlNode result = node.Clone();
            string def = node.InnerText.Replace(" ", "");
            string typeText = type.InnerText.Replace(" ", "");
            int offset = typeText.Length;
            int start = def.IndexOf(typeText);
            result.InnerText = def.Substring(def.IndexOf(typeText) + offset);
            //int spaces = 2;
            //int level = 0;
            //int i = 0;
            //while (spaces > 0 && i < def.Length)
            //{
            //    if (!(i >= 2 && def[i - 2] == ':' && def[i - 1] == ':' && def[i] == ' '))
            //    {
            //        switch (def[i])
            //        {
            //            case '<':
            //                level++;
            //                break;
            //            case '>':
            //                level--;
            //                break;
            //            case ' ':
            //                if (level == 0)
            //                    spaces--;
            //                break;
            //        }
            //    }
            //    i++;
            //}
            //result.InnerText = def.Substring(i);
            return result;
        }

        /// <summary>
        /// Returns the variability of the given <code>DuneClass</code>.
        /// </summary>
        /// <param name="df">the class to return the variability for</param>
        /// <returns>the variability of the class or enum</returns>
        public static List<DuneFeature> getVariability(DuneFeature df)
        {
            if (df == null)
            {
                return null;
            }
            List<DuneFeature> result = new List<DuneFeature>(); ;
            alternativeClasses.TryGetValue(df, out result);
            foreach (KeyValuePair<DuneFeature, List<DuneFeature>> curr in alternativeClasses)
            {
                if (curr.Key.getFeatureNameWithoutTemplate().Contains("ALUGrid"))
                {
                    var x = curr.Key.getFeatureNameWithoutTemplate();
                    Console.WriteLine("");
                }

                if (curr.Key.getFeatureNameWithoutTemplate().Equals(df.getFeatureNameWithoutTemplate()))
                {   
                    
                    Console.WriteLine();
                }
            }
            
            return result;
        }


        /// <summary>
        /// Returns all possible replacements according to the inheritance and the template analysis as a list of strings.
        /// </summary>
        /// <param name="feature">the feature to analyze</param>
        /// <returns>a list of strings in which every element is a possible replacement for the given feature</returns>
        public static Dictionary<string, DuneFeature> getVariability(string feature, RefersToAliasing refersTo)
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

            df = searchForClass(new DuneClass("", feature));

            if (df == null || template.Equals(""))
            {
                df = searchForClass(new DuneClass("", feature + "<>"));
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
                if (df.GetType() == typeof(DuneClass) && !template.Equals(String.Empty))
                {
                    analyzeTemplate(template, (DuneClass)df);
                }
                return df.getVariability();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// This method analyzes the given template by calling another helper-method.
        /// </summary>
        /// <param name="template">the template to be analyzed</param>
        /// <param name="d">the <code>DuneClass</code> the template is related to</param>
        /// <returns>the <code>TemplateTree</code> including the whole information about the template</returns>
        private static TemplateTree analyzeTemplate(string template, DuneClass d)
        {
            TemplateTree tt = new TemplateTree();
            analyzeTemplate(template, d, tt);

            return tt;
        }

        /// <summary>
        /// This method analyzes the given template and adds the information to the given <code>TemplateTree</code>.
        /// </summary>
        /// <param name="template">the template to be analyzed</param>
        /// <param name="d">the <code>DuneClass</code> the template is related to</param>
        /// <param name="tt">the template tree in which the information will be saved</param>
        /// <returns>the <code>TemplateTree</code> including the whole information about the template</returns>
        private static TemplateTree analyzeTemplate(string template, DuneClass d, TemplateTree tt)
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

                if (arg.Contains("("))
                {
                    tt.addInformation(arg.Substring(arg.LastIndexOf("::"), arg.LastIndexOf(")") + 1 - arg.LastIndexOf("::")));
                }

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
                    className = className.Substring(className.LastIndexOf("::") + 2, className.Length - className.LastIndexOf("::") - 2);
                }

                if (className.Contains("<"))
                {
                    int index = className.IndexOf('<');
                    string temp = className.Substring(index, className.LastIndexOf('>') - index + 1);
                    className = className.Substring(0, index);
                    DuneClass classObject = XMLParser.getFeature(className);
                    
                    if (classObject != null)
                    {
                        tt.addInformation(classObject);
                        tt.incHierarchy();
                        analyzeTemplate(temp, classObject, tt);
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
                }


                if (classOrEnum && method.Equals(""))
                {
                    continue;
                }

                // If no other case matches then it has to be an alias; A method should also be an alias (TODO: Also analyze the class and template in which the method appears in)
                string templateArgument = d.getTemplateArgument(i);
                if (templateArgument != null)
                {
                    tt.addAlias(templateArgument, arg);
                    tt.addInformation(arg);
                }
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
        private static string[] splitTemplate(string template)
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
                            args.AddLast(arg.Trim());
                            arg = "";
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
            // Also add the last element
            if (!arg.Equals(""))
                args.AddLast(arg.Trim());

            return args.ToArray();
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

                // Show the progress bar:
                Console.Write("\r{0}%   ", finished * 100 / total);



                foreach (DuneClass comp in featuresToCompare)
                {
                    if (comp.isIgnored())
                    {
                        continue;
                    }

                    // Every class is analyzed with every other class
                    // TODO: Add the commented part of the next line as soon as possible
                    if (df != comp && df.getNumberOfMethodHashes() >= comp.getNumberOfMethodHashes()) // && !df.hasRelationTo(comp, root))
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

                        if(!alternativeClasses.ContainsKey(df))
                            alternativeClasses.Add(df,new List<DuneFeature>()); 



                        if (isSubclassOf)
                        {
                            List<DuneFeature> values;
                            alternativeClasses.TryGetValue(df, out values);
                            values.Add(comp);

                            file.WriteLine(df.ToString() + " -> " + comp.ToString());
                        }
                    }

                }
                List<DuneFeature> altFeature;
                alternativeClasses.TryGetValue(df, out altFeature);

                // The own class is also an alternative (duck-typing is reflexive)
                altFeature.Add(df);
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
        /// Splits the arguments from the given template.
        /// </summary>
        /// <param name="toSplit">the template to split</param>
        /// <returns>the arguments of the template in a <code>List</code></returns>
        internal static List<string> splitArgs(string toSplit)
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
        /// <param name="currentNamespace">the namespace we are currently in</param>
        /// <returns>a <code>Dictionary</code> which contains the name of the enums and its elements</returns>
        private static List<Enum> saveEnums(XmlNode node, string currentNamespace)
        {
            List<Enum> result = new List<Enum>();
            // Access memberdefs and search for the value of the definition tag
            foreach (XmlNode c in node.ChildNodes)
            {
                if (c.Name.Equals("memberdef") && c.Attributes.GetNamedItem("kind") != null && c.Attributes.GetNamedItem("kind").Value.Equals("enum"))
                {
                    String reference = "";
                    XmlNode enumName = getChild("name", c.ChildNodes);
                    List<DuneEnumValue> enumNames = new List<DuneEnumValue>();

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
                            String valueReference = "";
                            foreach (XmlAttribute attribute in enumvalue.Attributes)
                            {
                                if (attribute.Name.Equals("id"))
                                {
                                    valueReference = attribute.Value;
                                    break;
                                }
                            }

                            enumNames.Add(new DuneEnumValue(valueReference, currentNamespace, getChild("name", enumvalue.ChildNodes).InnerText));

                        }
                    }

                    result.Add(new Enum(reference, enumName.InnerText, enumNames));
                }

                // add crossreferences from memberdef elements to the real classes 
                if (c.Name.Equals("memberdef") && c.Attributes.GetNamedItem("kind") != null && c.Attributes.GetNamedItem("kind").Value.Equals("typedef"))
                {

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
                        if (Program.INCLUDE_CONSTRUCTORS && !methodArgs.Equals("()"))
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
        private static DuneClass searchForClass(DuneClass df)
        {
            foreach (DuneClass d in features)
            {
                // Not only the name of the classes has to correspond... also the template argument count has to fit.
                if (d.getFeatureNameWithoutTemplate().Equals(df.getFeatureNameWithoutTemplate()) && d.getTemplateArgumentCount().isIn(df.getTemplateArgumentCount().getLowerBound()) && d.getTemplateArgumentCount().isIn(df.getTemplateArgumentCount().getUpperBound()))
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
        private static DuneClass searchForClassName(DuneClass df)
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
                return df = (DuneClass)XMLParser.features[indx];
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

            df = searchForClass(new DuneClass("", feature));

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
            }
            else
            {
                List<DuneFeature> dfs = new List<DuneFeature>();
                dfs.Add(df);
                return dfs;
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
        internal static DuneClass getFeature(String className)
        {
            foreach (DuneClass f in features)
            {
                if (f.getFeatureNameWithoutTemplateAndNamespace().Equals(className))
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
        private static String extractOnlyTemplate(XmlNode child) //, String templateFromCompoundName)
        {
          // TODO here could be an error...... inside
            
            // parse template in name
            //2, dimw, elType, refinementType, Comm
            //Dictionary<String, int> templetFromNameWithID = new Dictionary<string, int>();
            //if (templateFromCompoundName.Count(f => f == '<') > 1)
            //    Console.WriteLine();

            string template = "";
            for (int j = 0; j < child.ChildNodes.Count; j++)
            {
                // XmlNode c = cur.ChildNodes.Item(j);
                XmlNode c = child.ChildNodes.Item(j);

                if (j > 0)
                {
                    template += ",";
                }

                if (!c.FirstChild.InnerText.Equals("int") && !c.FirstChild.InnerText.Equals("string"))
                    template += c.FirstChild.InnerText;

                if ((c.FirstChild.InnerText.Equals("class") || c.FirstChild.InnerText.Equals("int") || c.FirstChild.InnerText.Equals("string") ) && c.ChildNodes.Count > 1)
                {
                    template += " " + c.ChildNodes[1].InnerText;
                }
                // If only the class-type is given, a specific character with its numeration is inserted
                else if ((c.FirstChild.InnerText.Equals("class") || c.FirstChild.InnerText.Equals("int") || c.FirstChild.InnerText.Equals("string")) && c.ChildNodes.Count == 1)
                {
                    template += "$" + globalNumerator;
                    globalNumerator++;
                }
            }

            return template;
        }

        public static int ifConds = 0;
        public static int easyToFind = 0;
        public static int notEasy = 0;
        public static int mehrdeutigkeit = 0;
        public static int furtherInformation = 0;
        public static int tempElementsWithFurtherInformation = 0;
        public static int onlyInParamList = 0;
        public static int multiRefsInOneParam = 0;
        public static int defValRefCountGlob = 0;
        public static int idNotFound = 0;

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

            // Ignore helper, private classes and so on.
            String prot = world.Attributes.GetNamedItem("prot") == null ? null : world.Attributes.GetNamedItem("prot").Value;
            String kind = world.Attributes.GetNamedItem("kind") == null ? null : world.Attributes.GetNamedItem("kind").Value;
            if (name.Contains("helper") || name.Contains("Helper") || (prot != null && prot.Equals("private")) || kind != null && (kind.Equals("file") || kind.Equals("dir") || kind.Equals("example") || kind.Equals("page")))  //|| kind.Equals("group") || kind.Equals("namespace")
            {
                return null;
            }

            String refId = world.Attributes["id"].Value.ToString();

            if (!refIdToFeature.ContainsKey(refId))
            {
                return templateTree;
            }
            DuneFeature currFeature = refIdToFeature[refId];

            Dictionary<String, TemplateTree> templateParamList = new Dictionary<String, TemplateTree>();

            // analyse the templateparamlist-elements.
            // I assume, here we have one element for each placeholder in the template
            XmlNode type = getChild("templateparamlist", world.ChildNodes);
            if (type != null)
            {
                
                TemplateTree type_tree = new TemplateTree();

                // Only classes have templateparamlists
                ((DuneClass)currFeature).setTemplateTree(type_tree);

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
                                        addTemplateTreeOf(innerNode, type_tree);
                                        break;
                                    case "defname":
                                        defname_cont = innerNode.InnerText;
                                        break;
                                    case "type":
                                        foreach (XmlNode defValRef in innerNode.ChildNodes)
                                        {
                                            switch (defValRef.Name)
                                            {
                                                case "ref":
                                                    String classNameInDefValue = defValRef.InnerText;
                                                    String defValRef_curr_id = defValRef.Attributes["refid"].InnerText;
                                                    DuneFeature df = null;
                                                    if (XMLParser.refIdToFeature.ContainsKey(defValRef_curr_id))
                                                        df = XMLParser.refIdToFeature[defValRef_curr_id];
                                                    else
                                                    {
                                                        if (!XMLParser.nameWithoutPackageToDuneFeatures.ContainsKey(defValRef.InnerText))
                                                        {
                                                            // ignore such cases:::
                                                            Console.WriteLine("id not found type " + defValRef_curr_id);
                                                            idNotFound += 1;
                                                            if (defValRef_curr_id.Contains("structDune_1_1PDELab_1_1FunctionTraits"))
                                                                Console.WriteLine("");
                                                            Console.WriteLine("NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNEEEEEEEEEEEEEEEEEEEEEIIIIIIIIIIIIIIINNNNNNNNNNNNNNNNNN");

                                                            df = new DuneClass();
                                                            df.isNotParsable = true;

                                                        }
                                                        else
                                                        {
                                                            df = XMLParser.nameWithoutPackageToDuneFeatures[defValRef.InnerText].First();
                                                        }
                                                    }

                                                    if (df != null)
                                                        type_tree.addInformation(df);

                                                    break;
                                                default:
                                                    if (defValRef.InnerText.Equals("&gt;"))
                                                    {
                                                        // TODO: there are elements such as &gt; ::v . We need to split the string
                                                        type_tree.decHierarchy();
                                                    }
                                                    else if (defValRef.InnerText.Equals("&lt;"))
                                                    {
                                                        type_tree.incHierarchy();
                                                    }
                                                    else
                                                    {
                                                        //type_tree.addInformation(defValRef.InnerText);
                                                        Console.Write(defValRef.InnerText);
                                                        Console.Write(defValRef.InnerText);
                                                    }
                                                    break;
                                            }
                                        }    
                                        break;
                                    default:
                                        Console.WriteLine(innerNode.Name + " is not considered in the template extraction");
                                        break;
                                }
                            }

                            // Create a object for the template element defined in the param tag

                            if (declmame_cont != "" || deftype_cont != "")
                            {
                                TemplateTree te = new TemplateTree();
                                te.declmame_cont = declmame_cont;
                                te.defval_cont = defval_cont;
                                te.defVal_cont_ref = defVal_cont_ref;
                                te.defVal_cont_ref_id = defVal_cont_ref_id;
                                te.defname_cont = defname_cont;
                                te.deftype_cont = deftype_cont;
                                te.defVal_tree = new TemplateTree();
                                te.type_tree = new TemplateTree();


                                String identifier = "";
                                if (declmame_cont != "")
                                    identifier = declmame_cont;
                                else
                                {
                                    identifier = deftype_cont.Replace("class", "").Trim();
                                }

                                if (currFeature.GetType() == typeof(DuneClass))
                                {
                                    ((DuneClass)currFeature).addTemplateElement(te);
                                }

                                //if(!identifier.Equals("typename"))
                                //    templateParamList.Add(declmame_cont, te);

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
                                            //String refId = att.InnerText;
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

                int elementsWithFurterInformation = templateParamList.Count();

                if (templateString.Contains("enable_if"))
                {
                    ifConds += 1;
                    Console.WriteLine("");
                    return null;
                }
                // template splitting
                // in einem template sind entweder terminale Elemente oder Klassen, die selbst wieder Elemente besitzen. 



                templateTree = new TemplateTree();
                String[] templateParts = templateString.Trim().Split(' ');
                for (int i = 0; i < templateParts.Count(); i++)
                {
                    string token = templateParts[i];
                    double val = 0;


                    if (token.EndsWith("..."))
                    {
                        templateTree.parentHasUnlimitedNumberOfParameters();
                        token = token.Replace("...", "");
                    }


                    if (templateParamList.ContainsKey(token))
                    {
                        tempElementsWithFurtherInformation += 1;
                        templateTree.addInformation(token, templateParamList[token]);
                        templateParamList.Remove(token);

                    }

                    if (Double.TryParse(token, out val))
                    {
                        easyToFind += 1;
                        templateTree.addNumericValue(token);
                    }

                    if (token.Equals("<"))
                    {
                        // increase hierarchy
                        templateTree.incHierarchy();
                    }
                    else if (token.Equals(">"))
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



            onlyInParamList += templateParamList.Count();

            if (templateTree != null)
                Console.WriteLine("parsed:: " + templateTree.toString());
            //Console.WriteLine("");

            if (currFeature.tempTree != null && templateTree == null)
            {

            }
            else
            {
                currFeature.tempTree = templateTree;
            }



            return templateTree;


        }

        /// <summary>
        /// Creates a new <code>TemplateTree</code> and adds the information which is included in the given node.
        /// </summary>
        /// <param name="node">the <code>XmlNode</code> with the needed information</param>
        /// <returns>the <code>TemplateTree</code> created out of the information from the node</returns>
        internal static TemplateTree getTemplateTreeOf(XmlNode node)
        {
            TemplateTree type_tree = new TemplateTree();
            addTemplateTreeOf(node, type_tree);
            return type_tree;
        }

        /// <summary>
        /// Adds the information given in the node to the template tree.
        /// </summary>
        /// <param name="node">the node with the information</param>
        /// <param name="type_tree">the <code>TemplateTree</code> which will be used to add the information from the given node</param>
        private static void addTemplateTreeOf(XmlNode node, TemplateTree type_tree)
        {
            string wholeDef = node.InnerText;
            foreach (XmlNode defValRef in node.ChildNodes)
            {
                switch (defValRef.Name)
                {
                    case "ref":
                        String classNameInDefValue = defValRef.InnerText;
                        String defValRef_curr_id = defValRef.Attributes["refid"].InnerText;
                        DuneFeature df = null;
                        if (XMLParser.refIdToFeature.ContainsKey(defValRef_curr_id))
                        {
                            df = XMLParser.refIdToFeature[defValRef_curr_id];

                        } else
                        {
                            if (!XMLParser.nameWithoutPackageToDuneFeatures.ContainsKey(defValRef.InnerText))
                            {
                                // ignore such cases:::
                                Console.WriteLine("id not found type " + defValRef.InnerText);
                                idNotFound += 1;
                                Console.WriteLine("NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNEEEEEEEEEEEEEEEEEEEEEIIIIIIIIIIIIIIINNNNNNNNNNNNNNNNNN");
                            }
                            else
                            {
                                df = XMLParser.nameWithoutPackageToDuneFeatures[defValRef.InnerText].First();
                            }
                        }

                        if (df != null)
                        {
                            type_tree.addInformation(df);

                            if (df.GetType() == typeof(DuneTypeDef))
                            {
                                // Add the namespace if there is one
                                DuneTypeDef dtd = (DuneTypeDef)df;
                                string typedefNamespace = classNameInDefValue.Contains("::") ? classNameInDefValue.Substring(0, classNameInDefValue.Length - classNameInDefValue.LastIndexOf("::") - 1) : "";
                                if (!typedefNamespace.Equals("") && !typedefNamespace.Equals(dtd.getNamespace()))
                                {
                                    dtd.setNamespace(typedefNamespace);
                                }
                            }
                        }

                        break;
                    case "#text":
                        string[] splitText = textElementSplitter(defValRef.InnerText, type_tree);
                        string[] wholeSplitText = textElementSplitter(wholeDef, type_tree);

                        foreach (string s in splitText)
                        {

                            // The string is either a class, an enumvalue, an internal variable or a method call.
                            if (s.Contains("::"))
                            {
                                string lastArg = s.Substring(s.LastIndexOf("::") + 2);

                                string rest = s.Substring(0, s.LastIndexOf("::"));
                                // The forelast argument is important in case of an internal variable and a method.
                                string forelastArg = "";
                                string forelastArgWithNS = "";
                                if (rest.Length > 0)
                                {
                                    if (rest.Contains("::"))
                                    {
                                        forelastArg = rest.Substring(rest.LastIndexOf("::") + 2);
                                    }
                                    else
                                    {
                                        forelastArg = rest;
                                    }
                                    forelastArgWithNS = rest;
                                } else
                                {
                                    // Get the forelast argument from the whole string
                                    forelastArg = getForelastArg(s, wholeSplitText);
                                    forelastArgWithNS = forelastArg;
                                }

                                if (lastArg.Equals(String.Empty))
                                    continue;

                                // In case of a method
                                if (s.Contains("(") && s.Contains(")"))
                                {
                                    type_tree.addInvocation(lastArg);
                                }
                                else if (!type_tree.isClass() && nameWithPackageToDuneFeatures.ContainsKey(s))
                                {
                                    // Class or enumvalue
                                    List<DuneFeature> multiple = nameWithPackageToDuneFeatures[s];
                                    if (nameWithPackageToDuneFeatures[s].Count > 1)
                                    {
                                        System.Console.WriteLine("The feature it refers to is not unique.");
                                        type_tree.addInformation(nameWithPackageToDuneFeatures[s]);
                                    }
                                    else if (nameWithPackageToDuneFeatures[s].Count > 0)
                                    {
                                        type_tree.addInformation(nameWithPackageToDuneFeatures[s].First());
                                    } 

                                } else if (!type_tree.isClass() && nameWithoutPackageToDuneFeatures.ContainsKey(lastArg))
                                {
                                    initBaseClass(type_tree, lastArg);

                                }
                                else if (forelastArg.Length > 0 && nameWithoutPackageToDuneFeatures.ContainsKey(forelastArg) || type_tree.isClass())
                                {

                                    if (!type_tree.isClass() && type_tree.getLastElement() == null)
                                    {
                                        List<DuneFeature> fs;
                                        if (nameWithPackageToDuneFeatures.ContainsKey(forelastArgWithNS))
                                        {
                                            nameWithPackageToDuneFeatures.TryGetValue(forelastArgWithNS, out fs);
                                        }
                                        else
                                        {
                                            nameWithoutPackageToDuneFeatures.TryGetValue(forelastArg, out fs);
                                        }

                                        if (fs.Count == 1)
                                        {
                                            type_tree.addInformation(fs[0]);
                                        } else
                                        {
                                            { }
                                        }
                                    }

                                    typedefCounter++;
                                    if (forelastArg.Contains("std"))
                                        stdCounter++;

                                    if (!hasFurtherElements(s, splitText) && !isFollowedByTemplate(s, splitText))
                                    {
                                        // internal variable
                                        type_tree.addInvocation(lastArg);
                                    } else
                                    {
                                        
                                        // add the whole string as information if no other case matches
                                        type_tree.addInformation(lastArg.Trim());
                                    }
                                } else
                                {
                                    // add the whole string as information if no other case matches
                                    type_tree.addInformation(lastArg.Trim());
                                }

                            }
                            else
                            {
                                switch (s)
                                {
                                    case "<":
                                        type_tree.incHierarchy();
                                        break;
                                    case ">":
                                        type_tree.decHierarchy();
                                        break;
                                    case ",":
                                        break;
                                    default:
                                        if (!s.Trim().Equals(String.Empty))
                                            type_tree.addInformation(s.Trim());
                                        break;
                                }
                            }
                        }
                        break;
                    default:
                        Console.WriteLine("foo");
                        // TODO: What about "Dune::PDELab::DOFIndex&lt; std::size_t, <ref refid="structDune_1_1TypeTree_1_1TreeInfo" kindref="compound">TypeTree::TreeInfo</ref>&lt; GFS &gt;::depth, 2 &gt;"?
                        if (defValRef.InnerText.Equals("&gt;"))
                        {
                            // TODO: there are elements such as &gt; ::v . We need to split the string
                            type_tree.decHierarchy();
                        }
                        else if (defValRef.InnerText.Equals("&lt;"))
                        {
                            type_tree.incHierarchy();
                        }
                        else
                        {
                            type_tree.addInformation(defValRef.InnerText);
                            Console.Write(defValRef.InnerText);
                            Console.Write(defValRef.InnerText);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Initializes the base class
        /// </summary>
        /// <param name="type_tree">the template tree</param>
        /// <param name="lastArg">the last argument</param>
        private static void initBaseClass(TemplateTree type_tree, string lastArg)
        {
            // Class or enumvalue
            List<DuneFeature> nonGroupObjects = new List<DuneFeature>();
            foreach (DuneFeature dfeature in nameWithoutPackageToDuneFeatures[lastArg])
            {
                if (!dfeature.getReference().Contains("group"))
                {
                    nonGroupObjects.Add(dfeature);
                }
            }
            if (nonGroupObjects.Count > 1)
            {
                System.Console.WriteLine("The feature it refers to is not unique.");
            }
            else if (nonGroupObjects.Count > 0)
            {
                type_tree.addInformation(nonGroupObjects.First());
            }
            else
            {
                type_tree.addInformation(nameWithoutPackageToDuneFeatures[lastArg].First());
            }
        }

        private static string getForelastArg(string current, string[] wholeDef)
        {
            string result = "";

            int currentLevel = -1;
            int level = 0;
            // Firstly, find the level the current string lies in
            foreach (string s in wholeDef)
            {
                switch (s)
                {
                    case "<":
                        level++;
                        break;
                    case ">":
                        level--;
                        break;
                }
                if (s.Equals(current))
                {
                    currentLevel = level;
                }
            }

            foreach (string s in wholeDef)
            {
                switch (s)
                {
                    case "<":
                        level++;
                        break;
                    case ">":
                        level--;
                        break;
                }
                if (s.Equals(current))
                {
                    return result;
                } else if (level == currentLevel && !s.Equals(">"))
                {
                    result += s;
                }
            }

            return result;
        }


        /// <summary>
        /// Returns <code>true</code> iff the given argument is followed by a template.
        /// </summary>
        /// <param name="current">the current element</param>
        /// <param name="wholeDef">the whole definition as a splitted string array</param>
        /// <returns><code>true</code> iff the given argument is followed by a template</returns>
        private static bool isFollowedByTemplate(string current, string[] wholeDef)
        {
            int currentLevel = -1;
            int level = 0;
            foreach (string s in wholeDef)
            {
                switch (s)
                {
                    case "<":
                        if (level == currentLevel)
                            return true;
                        level++;
                        break;
                    case ">":
                        level--;
                        break;
                }
                if (s.Equals(current))
                {
                    currentLevel = level;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <code>true</code> if the given argument has another elements that use it (as methods and variables do with classes and classes do with namespaces).
        /// </summary>
        /// <param name="current">the current element</param>
        /// <param name="wholeDef">the whole definition as a splitted string array</param>
        /// <returns><code>true</code> iff the given argument has another elements that use it</returns>
        private static bool hasFurtherElements(string current, string[] wholeDef)
        {
            int currentLevel = -1;
            int level = 0;
            foreach(string s in wholeDef)
            {
                switch(s)
                {
                    case "<":
                        level++;
                        break;
                    case ">":
                        level--;
                        break;
                }

                if (s.Equals(current))
                {
                    currentLevel = level;
                } else if (currentLevel >= 0 && currentLevel == level && s.Contains("::"))
                {
                    return true;
                }

            }
            return false;
        }

        /// <summary>
        /// The symbols to split the string for.
        /// </summary>
        private static char[] splitSymbols = { ',', '<', '>' };
        /// <summary>
        /// The outer split symbols.
        /// </summary>
        private static string[] outerSplitSymbols = {  };

        /// <summary>
        /// Splits the text element according to <code>splitSymbols</code> and <code>outerSplitSymbols</code>.
        /// </summary>
        /// <param name="textElement">the string with the needed information</param>
        /// <param name="tt">the according <code>TemplateTree</code></param>
        /// <returns>the string array split by the given symbols. Note that the split symbols are also included</returns>
        private static string[] textElementSplitter(string textElement, TemplateTree tt)
        {
            List<string> result = new List<string>();
            string temp = "";
            bool skip = false;

            foreach (char c in textElement)
            {
                if (splitSymbols.Contains(c))
                {
                    if (temp.Trim().Length > 0)
                    {
                        result.Add(temp);
                    }
                    result.Add(c.ToString());
                    temp = "";
                    skip = true;
                } else if (tt.isRoot())  {
                    string[] splittedByOuter = getSplittedStringByOuterSymbol(temp, c);
                    if (splittedByOuter != null)
                    {
                        skip = true;
                        if (splittedByOuter[0].Trim().Length > 0)
                        {
                            result.Add(splittedByOuter[0]);
                        }
                        temp = splittedByOuter[1];

                    }
                }

                if (!tt.isRoot() || tt.isRoot() && !skip)
                {
                    temp += c;
                }

                skip = false;
            }

            if (temp.Trim().Length > 0)
            {
                result.Add(temp);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns an array of <code>string</code> containing two elements iff an <code>outerSplitSymbol</code> is found; <code>null</code> otherwise.
        /// </summary>
        /// <param name="temp">the temporary string</param>
        /// <param name="c">the current character</param>
        /// <returns>an array of <code>string</code> containing two elements iff an <code>outerSplitSymbol</code> is found</returns>
        private static string[] getSplittedStringByOuterSymbol (string temp, char c)
        {
            foreach (string o in outerSplitSymbols)
            {
                if (c.Equals(o[o.Length - 1]) && temp.EndsWith(o.Substring(0, o.Length - 1)))
                {
                    string[] result = new string[2];
                    result[0] = temp.Substring(0, temp.LastIndexOf(o.Substring(0, o.Length - 1)));
                    result[1] = o;
                    return result;
                }
            }
            return null;
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
                return "";
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
