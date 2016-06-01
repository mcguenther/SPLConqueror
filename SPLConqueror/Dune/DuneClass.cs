using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Dune.util;

namespace Dune
{
    class DuneClass : DuneFeature
    {
        private String reference;
        private String featureNamespace;
        private String fullClassName;
        private String className;
        private Range templateArgumentCount;
        private List<TemplateElement> templateElements;
        private String templateForCode;
        public String implementingTemplate;
        private Boolean isStruct = false;
        private Boolean isAbstract = false;
        private List<DuneClass> parents;
        private List<DuneClass> children;
        private Tree template;

        private List<string> methodArguments;
        private List<List<int>> replaceableArguments;

        private List<int> methodHashes;
        private List<int> methodNameHashes;
        private List<int> methodArgumentCount;

        private bool ignoreDuckTyping = false;

        private List<string> alternatives = null;

        public LinkedList<TemplateTree> templat = new LinkedList<TemplateTree>();


        /// <summary>
        /// Constructs a new DuneClass with the given reference and the given className
        /// </summary>
        /// <param name="reference">the reference of the class</param>
        /// <param name="className">the name of the class</param>
        public DuneClass(String reference, String className)
        {
            // Separate the classname from the template
            int index = className.IndexOf('<');
            this.templateForCode = "";
            this.implementingTemplate = "";
            this.templateElements = new List<TemplateElement>();
            if (index > 0)
            {
                this.className = className.Substring(0, index);
                this.templateForCode = className.Substring(index + 1, className.Length - index - 2);
                this.implementingTemplate = this.templateForCode;
                int minmax = XMLParser.getCountOfArgs(this.templateForCode);
                this.templateArgumentCount = new Range(minmax, minmax);
                this.fullClassName = this.className;
                if (minmax > 0)
                {
                    this.fullClassName += "<" + this.templateForCode + ">";
                }
            }
            else
            {
                this.className = className;
                this.templateArgumentCount = new Range(0,0);
                this.fullClassName = this.className;
            }

            // Post-processing the name of the class
            if (this.className.Contains("::"))
            {
                int lastColons = this.className.LastIndexOf("::");
                // Retrieve the namespace
                this.featureNamespace = this.className.Substring(0, lastColons);
                this.className = this.className.Substring(lastColons + 2, this.className.Length - lastColons - 2);
            } else
            {
                this.featureNamespace = "";
            }

            
            this.reference = reference;
            this.parents = new List<DuneClass>();
            this.children = new List<DuneClass>();
            this.methodHashes = new List<int>();
        }

        /// <summary>
        /// Constructs a new DuneClass with the given reference and the given className
        /// </summary>
        /// <param name="reference">the reference of the class</param>
        /// <param name="className">the name of the class</param>
        /// <param name="template">the template of the class</param>
        /// <param name="templateInName">the template in the name of the class (may be null)</param>
        public DuneClass(String reference, String className, String template, String templateInName)
        {
            this.templateForCode = "";
            this.implementingTemplate = "";
            this.templateElements = new List<TemplateElement>();

            template = template.Replace("typename", "");
            template = template.Replace("class", "");

            if (template != null && !template.Equals(""))
            {
                this.className = className;
                this.implementingTemplate = template;
                int minmax = XMLParser.getCountOfArgs(this.implementingTemplate);
                this.templateArgumentCount = new Range(minmax, minmax);
                if (templateInName == null)
                {
                    this.fullClassName = this.className + "<" + template + ">";
                    this.templateForCode = this.implementingTemplate;
                }
                else
                {
                    this.fullClassName = this.className + "<" + templateInName + ">";
                    this.templateForCode = templateInName;
                }
            }
            else
            {
                this.className = className;
                this.templateArgumentCount = new Range(0,0);
                this.fullClassName = this.className;
            }

            // Post-processing the name of the class
            if (this.className.Contains("::"))
            {
                int lastColons = this.className.LastIndexOf("::");
                // Retrieve the namespace
                this.featureNamespace = this.className.Substring(0, lastColons);
                this.className = this.className.Substring(lastColons + 2, this.className.Length - lastColons - 2);
            }
            else
            {
                this.featureNamespace = "";
            }


            this.reference = reference;
            this.parents = new List<DuneClass>();
            this.children = new List<DuneClass>();
            this.methodHashes = new List<int>();
        }

        /// <summary>
        /// Returns a list containing all method arguments which match to the given method name hash and the argument count.
        /// </summary>
        /// <param name="name">the name of the method as a hash</param>
        /// <param name="methodArgCount">the number of arguments the method has</param>
        /// <returns>a list containing tuples. These tuples have the method argument names as well as their count. Note that the count may be variable</returns>
        public List<Tuple<string, List<int>>> getMethodArgumentsWithNameAndCount(int methodNameHash, int methodArgCount)
        {
            List<Tuple<string, List<int>>> result = new List<Tuple<string, List<int>>>();
            for (int i = 0; i < methodNameHashes.Count; i++)
            {
                if (methodNameHashes[i].Equals(methodNameHash) && methodArgumentCount[i].Equals(methodArgCount))
                {
                    result.Add(new Tuple<string,List<int>> (methodArguments[i], replaceableArguments[i]));
                }
            }
            return result;
        }


        /// <summary>
        /// Fills the given template with the default values.
        /// </summary>
        /// <param name="templateToFill">the template to fill with default values</param>
        /// <returns>the filled template</returns>
        public string fillTemplate(string templateToFill)
        {
            int argumentCount = XMLParser.getCountOfArgs(templateToFill) ;
            if (!templateArgumentCount.isUpperBound(argumentCount) && templateArgumentCount.isIn(argumentCount))
            {
                int index = templateToFill.LastIndexOf('>');
                string prefix = templateToFill.Substring(0, index);
                string sufix = templateToFill.Substring(index, templateToFill.Length - index);
                string extension = "";
                for (int i = argumentCount; i <= templateArgumentCount.getUpperBound(); i++)
                {
                    extension += ", " + templateElements.ElementAt(i - templateArgumentCount.getLowerBound());
                }

                return prefix + extension + sufix;
            } else
            {
                return templateToFill;
            }
        }

        /// <summary>
        /// Returns the hashed name of the method.
        /// </summary>
        /// <param name="index">the index in the local list</param>
        /// <returns>the hashed name of the method</returns>
        public int getMethodNameHash(int index)
        {
            return methodNameHashes[index];
        }

        /// <summary>
        /// Returns the method arguments of the method in the local list on the given index.
        /// </summary>
        /// <param name="index">the index in the local list</param>
        /// <returns>the method arguments of the method in the local list on the given index</returns>
        public string getMethodArguments(int index)
        {
            return methodArguments[index];
        }

        /// <summary>
        /// Returns the template argument on the specified position.
        /// </summary>
        /// <param name="index">the number of the template argument to return. Note that this begins with 0</param>
        /// <returns>the template argument on the specified position</returns>
        public string getTemplateArgument(int index)
        {
            if (index >= 0 && index < this.templateArgumentCount.getUpperBound())
            {
                if (this.templateElements[index].deftype_cont.Equals("class") || this.templateElements[index].deftype_cont.Equals("typename"))
                {
                    return this.templateElements[index].defval_cont.Equals("") ? this.templateElements[index].defname_cont : this.templateElements[index].defval_cont;
                }
                return this.templateElements[index].deftype_cont;
            } else if (index > this.templateArgumentCount.getUpperBound() && this.tempTree.hasUnlimitedNumberOfParameters)
            {
                return getTemplateArgument(this.templateArgumentCount.getUpperBound());
            }
            return null;
        }

        /// <summary>
        /// Returns the argument count of the method on the given index.
        /// </summary>
        /// <param name="index">the index in the local list</param>
        /// <returns>the argument count of the method on the given index</returns>
        public int getMethodArgumentCount(int index)
        {
            return methodArgumentCount[index];
        }

        /// <summary>
        /// Sets the type of the feature. A feature may be an interface, an abstract class or a concrete class.
        /// </summary>
        /// <param name="isSTruct"><code>true</code>if the class is an struct; <code>false</code> otherwise</param>
        /// <param name="isAbstract"><code>true</code> if the class is abstract; <code>false</code> otherwise</param>
        public void setType(Boolean isStruct, Boolean isAbstract)
        {
            this.isStruct = isStruct;
            this.isAbstract = isAbstract;
        }

        /// <summary>
        /// Sets the boolean variable <code>ignoreDuckTyping</code> to the given value.
        /// </summary>
        /// <param name="ignore">the boolean value the variable <code>ignoreDuckTyping</code> should be set to</param>
        public void ignoreAtDuckTyping(bool ignore)
        {
            this.ignoreDuckTyping = ignore;
        }

        /// <summary>
        /// Returns <code>true</code> if this class should be ignored.
        /// </summary>
        /// <returns><code>true</code> if this class should be ignored at Duck Typing; <code>false</code> otherwise</returns>
        public bool isIgnored()
        {
            return ignoreDuckTyping;
        }

        /// <summary>
        /// Adds a template element to the list of template elements.
        /// </summary>
        /// <param name="te">the template element to add</param>
        public void addTemplateElement(TemplateElement te)
        {
            this.templateElements.Add(te);
        }

        /// <summary>
        /// Returns whether this feature has parents or not.
        /// </summary>
        /// <param name="root">the root feature</param>
        /// <returns><code>true</code> if this feature has parents; <code>false</code> otherwise</returns>
        public Boolean hasParents(DuneClass root)
        {
            return parents.Contains(root) ? false : parents.Any();
        }

        /// <summary>
        /// Returns whether this feature has children or not.
        /// </summary>
        /// <returns><code>true</code> if this feature has children; <code>false</code> otherwise</returns>
        public Boolean hasChildren()
        {
            return children.Any();
        }

        /// <summary>
        /// Returns the methodHashes of the specific class.
        /// </summary>
        /// <returns>the methodHashes of the specific class</returns>
        public List<int> getMethodHashes()
        {
            return this.methodHashes;
        }

        /// <summary>
        /// Returns the number of method hashes which belong to the feature-object.
        /// </summary>
        /// <returns>the number of method hashes which belong to the feature-object</returns>
        public int getNumberOfMethodHashes()
        {
            return this.methodHashes.Count;
        }

        /// <summary>
        /// Returns <code>true</code> iff the hash is included in the <code>methodHashes</code>-list.
        /// </summary>
        /// <param name="hash">the hash it is searched for</param>
        /// <returns><code>true</code> iff the hash is included in the <code>methodHases</code>-list</returns>
        public Boolean containsMethodHash(int hash)
        {
            return methodHashes.Contains(hash);
        }

        /// <summary>
        /// Adds the given method signature to the list of the feature
        /// </summary>
        /// <param name="methodSig">the method signature to add</param>
        public void addMethod(String methodSig)
        {
            this.methodHashes.Add(methodSig.GetHashCode());
        }

        /// <summary>
        /// Sets the method hash list to the given argument.
        /// </summary>
        /// <param name="methods">the list containing the method hashes of the class</param>
        public void setMethods(List<int> methods)
        {
            this.methodHashes = methods;
        }

        /// <summary>
        /// Sets the hash list of the method names to the given argument.
        /// </summary>
        /// <param name="methodNames">the list containing the method name hashes of the class</param>
        public void setMethodNameHashes(List<int> methodNames)
        {
            this.methodNameHashes = methodNames;
        }

        /// <summary>
        /// Sets the list of the method argument count to the given argument.
        /// </summary>
        /// <param name="methodArgumentCount">the list containing the number of the method arguments</param>
        public void setMethodArgumentCount(List<int> methodArgumentCount)
        {
            this.methodArgumentCount = methodArgumentCount;
        }

        /// <summary>
        /// Returns <code>true</code> if the specific class has a direct relation to the given feature; <code>false</code> otherwise.
        /// </summary>
        /// <param name="df">the feature a relation is searched to</param>
        /// <returns><code>true</code> if the specific class has a direct relation to the given feature; <code>false</code> otherwise</returns>
        public Boolean hasDirectRelationTo(DuneClass df)
        {
            return this.parents.Contains(df) || this.children.Contains(df);
        }


        

        /// <summary>
        /// Sets the list containing the method's arguments in order to improve duck-typing.
        /// </summary>
        /// <param name="methodArgs">the list containing the method's arguments.</param>
        public void setMethodArguments(List<string> methodArgs)
        {
            this.methodArguments = methodArgs;
        }

        /// <summary>
        /// Sets the list containing the number of arguments which are replaceable.
        /// </summary>
        /// <param name="replaceable">the list containing the number of method arguments which are replaceable</param>
        public void setReplaceableMethodArguments(List<List<int>> replaceable)
        {
            this.replaceableArguments = replaceable;
        }

        /// <summary>
        /// This method returns <code>true</code> if the respective feature has the given feature as a child.
        /// </summary>
        /// <param name="df">the feature to search for</param>
        /// <returns><code>true</code> if the respective feature has the given feature as a child; <code>false</code> otherwise</returns>
        public Boolean hasDirectChildRelationTo(DuneClass df)
        {
            return this.children.Contains(df);
        }

        /// <summary>
        /// Returns if the class has a relation(also considering the transitive hull) to the given class.
        /// </summary>
        /// <param name="df">the class, a relation should be searched to</param>
        /// <returns><code>true</code> if the class has a relation(also indirect) to the given class; <code>false</code> otherwise</returns>
        public Boolean hasRelationTo(DuneClass df, DuneClass root)
        {
            return hasRelationTo(df, root, new List<DuneClass>());
        }

        /// <summary>
        /// Returns if the class has a relation(also considering the transitive hull) to the given class.
        /// </summary>
        /// <param name="df">the class, a relation should be searched to</param>
        /// <param name="analyzed">the list which contains the classes which were already analyzed</param>
        /// <returns><code>true</code> if the class has a relation(also indirect) to the given class; <code>false</code> otherwise</returns>
        private Boolean hasRelationTo(DuneClass df, DuneClass root, List<DuneClass> analyzed)
        {
            if (analyzed.Contains(this) || root == this)
            {
                return false;
            }

            if (df == this)
            {
                return true;
            }

            if (!hasDirectRelationTo(df))
            {
                analyzed.Add(this);
                foreach (DuneClass p in parents)
                {
                    if (p != root && !analyzed.Contains(p) && p.hasRelationTo(df, root, analyzed))
                    {
                        return true;
                    }
                }
                foreach (DuneClass c in children)
                {
                    if (c.hasRelationTo(df, root, analyzed))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the classes with which the current class may be replaced with.
        /// </summary>
        /// <param name="root">the root node which is excluded</param>
        /// <returns>the classes in a list of strings with which the current class may be replaced with</returns>
        public override List<string> getVariability(DuneFeature root)
        {
            if (alternatives == null)
            {
                alternatives = getVariability(root, new List<DuneClass>(), this);
            }
            return alternatives;
        }

        /// <summary>
        /// Returns the classes with which the current class may be replaced with.
        /// </summary>
        /// <param name="root">the root node which is excluded</param>
        /// <param name="analyzed">the list which contains the features that were analyzed already</param>
        /// <param name="baseClass">the class the variability is searched for</param>
        /// <returns>the classes in a list of strings with which the current class may be replaced with</returns>
        private List<string> getVariability(DuneFeature root, List<DuneClass> analyzed, DuneClass baseClass)
        {
            List<string> result = new List<string>();

            if (analyzed.Contains(this) || this.Equals(root))
            {
                return result;
            }
            
            analyzed.Add(this);

            if (this.methodNameHashes != null && (baseClass.isPotentialSubclassOff(this) || this.isPotentialSubclassOff(baseClass))) //this.methodNameHashes.Capacity >= baseClass.methodHashes.Capacity)
            {
                result.Add(ToString());
            }

            foreach (DuneClass p in children)
            {
                result.AddRange(p.getVariability(root, analyzed, baseClass));
            }

            foreach (DuneClass p in parents)
            {
                result.AddRange(p.getVariability(root, analyzed, baseClass));
                
            }

            return result;
        }

        /// <summary>
        /// Returns <code>true</code> if the class could be a subclass of the given class(duck typing).
        /// </summary>
        /// <param name="f">the class to be subclass of</param>
        /// <returns><code>true</code> if this class is a subclass of the other class; <code>false</code> otherwise</returns>
        public bool isPotentialSubclassOff(DuneClass f)
        {
            for (int i = 0; i < f.getMethodHashes().Count; i++)
            {
                int methodHash = f.getMethodHashes()[i];
                if (!this.containsMethodHash(methodHash))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Adds a parent feature.
        /// </summary>
        /// <param name="d">the parent feature</param>
        public void addParent(DuneClass d)
        {
            parents.Add(d);
        }

        /// <summary>
        /// Sets the template tree of the <code>DuneClass</code> to the given template tree.
        /// </summary>
        /// <param name="t">the template tree to set to</param>
        public void setTemplateTree(Tree t)
        {
            this.template = t;
        }

        /// <summary>
        /// Sets the reference to the given reference.
        /// </summary>
        /// <param name="reference">the reference on which the feature should be set to</param>
        public void setReference(String reference)
        {
            this.reference = reference;
        }

        /// <summary>
        /// Adds a child feature.
        /// </summary>
        /// <param name="d">the feature to add to the children-list</param>
        public void addChildren(DuneClass d)
        {
            this.children.Add(d);
        }

        /// <summary>
        /// Returns the reference.
        /// </summary>
        /// <returns>the reference of the feature/class</returns>
        public override String getReference()
        {
            return this.reference;
        }

        /// <summary>
        /// Sets the range of the argument template.
        /// </summary>
        /// <param name="min">the lower bound</param>
        /// <param name="max">the uppder bound</param>
        public void setRange(int min, int max)
        {
            this.templateArgumentCount = new Range(min, max);
        }

        /// <summary>
        /// Returns the number of template arguments.
        /// </summary>
        /// <returns>the number of template arguments</returns>
        public Range getTemplateArgumentCount()
        {
            return this.templateArgumentCount;
        }

        /// <summary>
        /// Returns the namespace of the class.
        /// </summary>
        /// <returns>the namespace of the class</returns>
        public override string getNamespace()
        {
            return this.featureNamespace;
        }

        /// <summary>
        /// Returns the name of the class with its template.
        /// </summary>
        /// <returns>the name of the feature/class</returns>
        public override String getFeatureName()
        {
            return this.fullClassName;
            //return this.className + this.rawTemplate;
            //return String.Concat(this.className, this.templateArgumentCount);
        }

        /// <summary>
        /// Returns the name of the class without its template.
        /// </summary>
        /// <returns>the name of the feature/class</returns>
        public override String getFeatureNameWithoutTemplate()
        {
            return this.featureNamespace + "::" + this.className;
        }

        /// <summary>
        /// Returns the name of the class without its template and its namespace.
        /// </summary>
        /// <returns>the name of the feature/class</returns>
        public override String getFeatureNameWithoutTemplateAndNamespace()
        {
            return this.className;
        }

        /// <summary>
        /// Returns the <code>DuneClass</code> as a string.
        /// </summary>
        /// <returns>the string according to the <code>DuneClass</code></returns>
        public override String ToString()
        {
            //if (this.templateArgumentCount > 0)
            //{
            //    return this.className + "<" + this.implementingTemplate + ">";
            //}
            //else
            //{
            //    return this.className;
            //}
            return fullClassName;
        }


        /// <summary>
        /// Returns <code>true</code> if the features template tree was initialized.
        /// </summary>
        /// <returns><code>true</code> if the features template tree was initialised; <code>false</code> otherwise</returns>
        public Boolean hasTree()
        {
            return template != null;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to DuneClass return false.
            DuneClass p = obj as DuneClass;
            if ((System.Object)p == null)
            {
                return false;
            }

            // If both objects have references then match them by reference
            if (this.reference != null && !this.reference.Equals("") && p.reference != null && !p.reference.Equals(""))
            {
                return this.reference.Equals(p.reference);
            }

            // Return true if the fields match:
            return (this.getFeatureName()).Equals(p.getFeatureName());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
