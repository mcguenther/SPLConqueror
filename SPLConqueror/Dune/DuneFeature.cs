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
        private List<DuneFeature> template;
        private List<String> unknownTemplates;
        private List<int> methodHashes;

        /// <summary>
        /// Constructs a new DuneFeature with the given reference and the given className
        /// </summary>
        /// <param name="reference">The reference of the class</param>
        /// <param name="className">The name of the class</param>
        public DuneFeature(String reference, String className)
        {
            this.reference = reference;
            this.className = className;
            this.parents = new List<DuneFeature>();
            this.children = new List<DuneFeature>();
            this.template = new List<DuneFeature>();
            this.unknownTemplates = new List<String>();
            this.methodHashes = new List<int>();
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
        /// Adds a parent feature.
        /// </summary>
        /// <param name="d">the parent feature</param>
        public void addParent(DuneFeature d)
        {
            parents.Add(d);
        }

        /// <summary>
        /// Adds a template class to the template
        /// </summary>
        /// <param name="d">the template class as a <code>DuneFeature</code> to add</param>
        public void addTemplateClass(DuneFeature d)
        {
            this.template.Add(d);
        }

        public void addUnknownTemplate(String template) {

            this.unknownTemplates.Add(template);
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
            return this.className.Equals(p.className) && this.reference.Equals(p.reference);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
