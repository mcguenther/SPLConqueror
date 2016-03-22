using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This class encapsulates an enumeration and contains several information about it.
    /// </summary>
    class DuneEnum : DuneFeature
    {
        private String enumNamespace;
        private String fullEnumName;
        private Enum enumObject;
        

        /// <summary>
        /// The constructor of the class <code>DuneEnum</code>.
        /// </summary>
        /// <param name="enumObject">the enum containing its name, values as well as its reference in the Doxygen xml-file.</param>
        /// <param name="featureNamespace">the namespace of the enum</param>
        public DuneEnum(string featureNamespace, Enum enumObject) {
            this.enumNamespace = featureNamespace;
            this.fullEnumName = featureNamespace + "::" + enumObject.getName();
            this.enumObject = enumObject;
        }

        /// <summary>
        /// Returns the name of the enum with its template.
        /// </summary>
        /// <returns>the name of the enum</returns>
        public override string getFeatureName()
        {
            return fullEnumName;
        }

        /// <summary>
        /// Returns the namespace of the enum.
        /// </summary>
        /// <returns>the namespace of the enum</returns>
        public override string getNamespace()
        {
            return this.enumNamespace;
        }

        /// <summary>
        /// Returns the reference of the enum.
        /// </summary>
        /// <returns>the reference of the enum</returns>
        public override string getReference()
        {
            return this.enumObject.getReference();
        }

        /// <summary>
        /// Returns the name of the enum(the same as getFeatureName()).
        /// </summary>
        /// <returns>the name of the enum</returns>
        public override string getFeatureNameWithoutTemplate()
        {
            return fullEnumName;
        }

        /// <summary>
        /// Returns the name of the enum without its template and its namespace.
        /// </summary>
        /// <returns>the name of the enum</returns>
        public override string getFeatureNameWithoutTemplateAndNamespace()
        {
            return this.enumObject.getName();
        }

        /// <summary>
        /// Returns the values of the enum.
        /// </summary>
        /// <returns>the values of the enum as a list</returns>
        public List<string> getValues()
        {
            return this.enumObject.getValues();
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

            String ownRef = this.enumObject.getReference();
            String objRef = p.getReference();

            // If both objects have references then match them by reference
            if (ownRef != null && !ownRef.Equals("") && objRef != null && !objRef.Equals(""))
            {
                return ownRef.Equals(objRef);
            }

            // Return true if the fields match:
            return (this.getFeatureName()).Equals(p.getFeatureName());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override List<string> getVariability(DuneFeature root)
        {
            // The root feature is not needed until now (but DuneClass needs it because of the tree-like structure)
            return this.enumObject.getValues();
        }
    }
}
