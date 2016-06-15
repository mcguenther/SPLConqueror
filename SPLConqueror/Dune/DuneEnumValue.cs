using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This class represents a value within an enum
    /// </summary>
    class DuneEnumValue : DuneFeature
    {
        private string reference;
        private string enumNamespace;
        private string value;

        /// <summary>
        /// Constructs a <code>DuneEnumValue</code> by the reference, the namespace and its value.
        /// </summary>
        /// <param name="reference">the refid</param>
        /// <param name="enumNamespace">the namespace of the enum value</param>
        /// <param name="value">the value</param>
        public DuneEnumValue(string reference, string enumNamespace, string value)
        {
            this.reference = reference;
            this.enumNamespace = enumNamespace;
            this.value = value;
        }

        /// <summary>
        /// Returns the name of the <code>DuneEnumValue</code>, namely the value with the preceeding namespace.
        /// </summary>
        /// <returns>the value including its namespace as a prefix</returns>
        public override string getFeatureName()
        {
            return this.enumNamespace + "::" + this.value;
        }

        /// <summary>
        /// Returns the name of the <code>DuneEnumValue</code>, namely the value with the preceeding namespace.
        /// </summary>
        /// <returns>the value including its namespace as a prefix</returns>
        public override string getFeatureNameWithoutTemplate()
        {
            return this.enumNamespace + "::" +  this.value;
        }

        /// <summary>
        /// Returns the value of the enum without the namespace.
        /// </summary>
        /// <returns>the value of the enum</returns>
        public override string getFeatureNameWithoutTemplateAndNamespace()
        {
            return this.value;
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
        /// Returns the reference ID (refID) of the enum value.
        /// </summary>
        /// <returns>the reference ID of the enum value</returns>
        public override string getReference()
        {
            return this.reference;
        }

        /// <summary>
        /// Returns a list with one element.
        /// </summary>
        /// <param name="root">the root-feature. Not needed here</param>
        /// <returns>a list containing the value</returns>
        public override Dictionary<string, DuneFeature> getVariability(DuneFeature root)
        {
            Dictionary<string, DuneFeature> result = new Dictionary<string, DuneFeature>();
            result.Add(this.enumNamespace + "::" + this.value, this);
            return result;
        }
    }
}
