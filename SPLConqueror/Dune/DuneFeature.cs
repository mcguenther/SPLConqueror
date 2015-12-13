using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Dune
{
    class DuneFeature
    {
        private String reference;
        private String fullClassName;
        private String className;
        private int templateArgumentCount;
        private String templateForCode;
        private String implementingTemplate;
        private Boolean isStruct = false;
        private Boolean isAbstract = false;
        private List<DuneFeature> parents;
        private List<DuneFeature> children;
        private Tree template;

        private List<string> methodArguments;
        private List<List<int>> replaceableArguments;

        private List<int> methodHashes;
        private List<int> methodNameHashes;
        private List<int> methodArgumentCount;
        private Dictionary<String, List<String>> enums;

        /// <summary>
        /// Constructs a new DuneFeature with the given reference and the given className
        /// </summary>
        /// <param name="reference">the reference of the class</param>
        /// <param name="className">the name of the class</param>
        public DuneFeature(String reference, String className)
        {
            // Separate the classname from the template
            int index = className.IndexOf('<');
            this.templateForCode = "";
            this.implementingTemplate = "";
            if (index > 0)
            {
                this.className = className.Substring(0, index);
                this.templateForCode = className.Substring(index + 1, className.Length - index - 2);
                this.implementingTemplate = this.templateForCode;
                this.templateArgumentCount = XMLParser.getCountOfArgs(this.templateForCode);
                this.fullClassName = this.className;
                if (this.templateArgumentCount > 0)
                {
                    this.fullClassName += "<" + this.templateForCode + ">";
                }
            }
            else
            {
                this.className = className;
                this.templateArgumentCount = 0;
                this.fullClassName = this.className;
            }

            
            this.reference = reference;
            this.parents = new List<DuneFeature>();
            this.children = new List<DuneFeature>();
            this.methodHashes = new List<int>();
            this.enums = null;
        }

        /// <summary>
        /// Constructs a new DuneFeature with the given reference and the given className
        /// </summary>
        /// <param name="reference">the reference of the class</param>
        /// <param name="className">the name of the class</param>
        /// <param name="template">the template of the class</param>
        /// <param name="templateInName">the template in the name of the class (may be null)</param>
        public DuneFeature(String reference, String className, String template, String templateInName)
        {
            this.templateForCode = "";
            this.implementingTemplate = "";

            if (template != null && !template.Equals(""))
            {
                this.className = className;
                this.implementingTemplate = template;
                this.templateArgumentCount = XMLParser.getCountOfArgs(this.implementingTemplate);
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
                this.templateArgumentCount = 0;
                this.fullClassName = this.className;
            }


            this.reference = reference;
            this.parents = new List<DuneFeature>();
            this.children = new List<DuneFeature>();
            this.methodHashes = new List<int>();
            this.enums = null;
        }

        /// <summary>
        /// Returns a list containing all method arguments which match to the given method name hash and the argument count.
        /// </summary>
        /// <param name="name">the name of the method as a hash</param>
        /// <param name="methodArgCount">the number of arguments the method has</param>
        /// <returns></returns>
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
        /// For debugging purpose. This method should be deleted
        /// </summary>
        /// <returns>a list containing the parent-nodes.</returns>
        public List<DuneFeature> getParents()
        {
            return parents;
        }

        /// <summary>
        /// Adds an enum to the dictionary of enums of the class.
        /// </summary>
        /// <param name="key">the name of the enum</param>
        /// <param name="enums">a list containing all enum-options</param>
        public void addEnum(String key, List<String> enums)
        {
            //if (this.enums.ContainsKey(key))
            //{
            //    return;
            //}
            this.enums.Add(key, enums);
        }

        /// <summary>
        /// Sets the enums of the <code>DuneFeature</code> to the given argument.
        /// </summary>
        /// <param name="enums">the <code>Dictionary</code> containing the enums of the class</param>
        public void setEnum(Dictionary<String, List<String>> enums)
        {
            // Should never be the case...
            if (this.enums != null)
            {
                System.Console.Write("");
            }
            this.enums = enums;
        }

        /// <summary>
        /// Returns whether this feature has parents or not.
        /// </summary>
        /// <param name="root">the root feature</param>
        /// <returns><code>true</code> if this feature has parents; <code>false</code> otherwise</returns>
        public Boolean hasParents(DuneFeature root)
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
        public Boolean hasDirectRelationTo(DuneFeature df)
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
        public Boolean hasDirectChildRelationTo(DuneFeature df)
        {
            return this.children.Contains(df);
        }

        /// <summary>
        /// Returns if the class has a relation(also considering the transitive hull) to the given class.
        /// </summary>
        /// <param name="df">the class, a relation should be searched to</param>
        /// <returns><code>true</code> if the class has a relation(also indirect) to the given class; <code>false</code> otherwise</returns>
        public Boolean hasRelationTo(DuneFeature df, DuneFeature root)
        {
            return hasRelationTo(df, root, new List<DuneFeature>());
        }

        /// <summary>
        /// Returns if the class has a relation(also considering the transitive hull) to the given class.
        /// </summary>
        /// <param name="df">the class, a relation should be searched to</param>
        /// <param name="analyzed">the list which contains the classes which were already analyzed</param>
        /// <returns><code>true</code> if the class has a relation(also indirect) to the given class; <code>false</code> otherwise</returns>
        private Boolean hasRelationTo(DuneFeature df, DuneFeature root, List<DuneFeature> analyzed)
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
                foreach (DuneFeature p in parents)
                {
                    if (p != root && !analyzed.Contains(p) && p.hasRelationTo(df, root, analyzed))
                    {
                        return true;
                    }
                }
                foreach (DuneFeature c in children)
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
        /// Returns a list of strings containing the alternatives to the given enum.
        /// </summary>
        /// <param name="enumName">The name of the enum to search for</param>
        /// <returns>a list of strings containing the alternatives to the given enum; if no alternatives then this list is empty</returns>
        public List<string> getAlternativeEnums(string enumName) {
            List<string> result = new List<string>();

            foreach (KeyValuePair<string, List<string>> k in enums)
            {
                if (k.Value.Contains(enumName))
                {
                    foreach (string listEntry in k.Value)
                    {
                        result.Add(this.getClassName() + "::" + listEntry);
                    }

                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the classes with which the current class may be replaced with.
        /// </summary>
        /// <param name="root">the root node which is excluded</param>
        /// <returns>the classes in a list of strings with which the current class may be replaced with</returns>
        public List<string> getVariability(DuneFeature root)
        {
            return getVariability(root, new List<DuneFeature>());
        }

        /// <summary>
        /// Returns the classes with which the current class may be replaced with.
        /// </summary>
        /// <param name="root">the root node which is excluded</param>
        /// <param name="analyzed">the list which contains the features that were analyzed already</param>
        /// <returns>the classes in a list of strings with which the current class may be replaced with</returns>
        private List<string> getVariability(DuneFeature root, List<DuneFeature> analyzed)
        {
            List<string> result = new List<string>();

            if (analyzed.Contains(this) || this.Equals(root))
            {
                return result;
            }
            
            analyzed.Add(this);
            result.Add(ToString());
            foreach (DuneFeature p in children)
            {
                result.AddRange(p.getVariability(root, analyzed));
            }

            foreach (DuneFeature p in parents)
            {
                result.AddRange(p.getVariability(root, analyzed));
            }

            return result;
        }

        /// <summary>
        /// Adds a parent feature.
        /// </summary>
        /// <param name="d">the parent feature</param>
        public void addParent(DuneFeature d)
        {
            parents.Add(d);
        }

        /// <summary>
        /// Sets the template tree of the <code>DuneFeature</code> to the given template tree.
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
        public void addChildren(DuneFeature d)
        {
            this.children.Add(d);
        }

        /// <summary>
        /// Returns the reference.
        /// </summary>
        /// <returns>the reference of the feature/class</returns>
        public String getReference()
        {
            return this.reference;
        }

        /// <summary>
        /// Returns the name of the class with its template.
        /// </summary>
        /// <returns>the name of the feature/class</returns>
        public String getClassName()
        {
            return this.fullClassName;
            //return this.className + this.rawTemplate;
            //return String.Concat(this.className, this.templateArgumentCount);
        }

        /// <summary>
        /// Returns the name of the class without its template.
        /// </summary>
        /// <returns>the name of the feature/class</returns>
        public String getClassNameWithoutTemplate()
        {
            return this.className;
        }

        /// <summary>
        /// Returns the <code>DuneFeature</code> as a string.
        /// </summary>
        /// <returns>the string according to the <code>DuneFeature</code></returns>
        public override String ToString()
        {
            if (this.templateArgumentCount > 0)
            {
                return this.className + "<" + this.implementingTemplate + ">";
            }
            else
            {
                return this.className;
            }
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

            // If parameter cannot be cast to DuneFeature return false.
            DuneFeature p = obj as DuneFeature;
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
            return (this.getClassName()).Equals(p.getClassName());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
