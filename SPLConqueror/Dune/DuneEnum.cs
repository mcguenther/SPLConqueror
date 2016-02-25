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
        private String reference;
        private String enumNamespace;
        private String fullEnumName;
        private String enumName;

        private List<string> values;

        public DuneEnum(string reference, string featureNamespace, string enumName) {
            this.enumNamespace = featureNamespace;
            this.reference = reference;
            this.enumName = enumName;
            this.fullEnumName = featureNamespace + "::" + enumName;
        }

        /// <summary>
        /// Returns the name of the enum with its template.
        /// </summary>
        /// <returns>the name of the enum</returns>
        public string getFeatureName()
        {
            return fullEnumName;
        }

        /// <summary>
        /// Returns the namespace of the enum.
        /// </summary>
        /// <returns>the namespace of the enum</returns>
        public string getNamespace()
        {
            return this.enumNamespace;
        }

        /// <summary>
        /// Returns the reference of the enum.
        /// </summary>
        /// <returns>the reference of the enum</returns>
        public string getReference()
        {
            return this.reference;
        }

        /// <summary>
        /// Returns the name of the enum(the same as getFeatureName()).
        /// </summary>
        /// <returns>the name of the enum</returns>
        public string getFeatureNameWithoutTemplate()
        {
            return fullEnumName;
        }

        /// <summary>
        /// Returns the name of the enum without its template and its namespace.
        /// </summary>
        /// <returns>the name of the enum</returns>
        public string getFeatureNameWithoutTemplateAndNamespace()
        {
            return this.enumName;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to DuneClass return false.
            DuneEnum p = obj as DuneEnum;
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
