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
        /// 
        /// </summary>
        /// <param name="d"></param>
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
