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
        private String className;
        private List<DuneFeature> parents;
        private List<DuneFeature> children;
        private Tree template;
        private List<int> methodHashes;
        private Dictionary<String, List<String>> enums;

        /// <summary>
        /// Constructs a new DuneFeature with the given reference and the given className
        /// </summary>
        /// <param name="reference">the reference of the class</param>
        /// <param name="className">the name of the class</param>
        public DuneFeature(String reference, String className)
        {
            this.reference = reference;
            this.className = className;
            this.parents = new List<DuneFeature>();
            this.children = new List<DuneFeature>();
            this.methodHashes = new List<int>();
            this.enums = new Dictionary<string, List<string>>();
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
            this.enums.Add(key, enums);
        }

        /// <summary>
        /// Returns whether this feature has parents or not.
        /// </summary>
        /// <returns><code>true</code> if this feature has parents; <code>false</code> otherwise</returns>
        public Boolean hasParents()
        {
            return parents.Any();
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
        /// Returns <code>true</code> if the specific class has a direct relation to the given feature; <code>false</code> otherwise.
        /// </summary>
        /// <param name="df">the feature a relation is searched to</param>
        /// <returns><code>true</code> if the specific class has a direct relation to the given feature; <code>false</code> otherwise</returns>
        public Boolean hasDirectRelationTo(DuneFeature df)
        {
            return this.parents.Contains(df) || this.children.Contains(df);
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
        public Boolean hasRelationTo(DuneFeature df)
        {
            return hasRelationTo(df, new List<DuneFeature>());
        }

        /// <summary>
        /// Returns if the class has a relation(also considering the transitive hull) to the given class.
        /// </summary>
        /// <param name="df">the class, a relation should be searched to</param>
        /// <param name="analyzed">the list which contains the classes which were already analyzed</param>
        /// <returns><code>true</code> if the class has a relation(also indirect) to the given class; <code>false</code> otherwise</returns>
        private Boolean hasRelationTo(DuneFeature df, List<DuneFeature> analyzed)
        {
            if (analyzed.Contains(this))
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
                    if (p.hasRelationTo(df, analyzed))
                    {
                        return true;
                    }
                }
                foreach (DuneFeature c in children)
                {
                    if (c.hasRelationTo(df, analyzed))
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
        /// Returns the reference.
        /// </summary>
        /// <returns>the reference of the feature/class</returns>
        public String getClassName()
        {
            return this.className;
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

            // Return true if the fields match:
            return this.className.Equals(p.className);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
