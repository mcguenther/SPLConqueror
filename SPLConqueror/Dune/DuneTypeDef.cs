using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Dune
{
    class DuneTypeDef : DuneFeature
    {
        private string typeNamespace;
        private string name;
        private string wholeName;
        private string reference;

        /// <summary>
        /// Creates a new <code>DuneTypeDef</code> by the given reference, name and type.
        /// </summary>
        /// <param name="reference">the reference id of the typedef</param>
        /// <param name="name">the name of the typedef</param>
        /// <param name="type">the type(or definition) of the typedef</param>
        public DuneTypeDef(string reference, string name)
        {
            this.name = name;
            this.reference = reference;
            this.typeNamespace = "";
            this.wholeName = this.name;

        }

        /// <summary>
        /// Creates a new <code>DuneTypeDef</code> by the given reference, namespace, name and type.
        /// </summary>
        /// <param name="reference">the reference id of the typedef</param>
        /// <param name="typeNamespace">the namespace of the typedef</param>
        /// <param name="name">the name of the typedef</param>
        /// <param name="type">the type(or definition) of the typedef</param>
        public DuneTypeDef(string reference, string typeNamespace, string name) : this(reference, name)
        {
            setNamespace(typeNamespace);
        }

        /// <summary>
        /// Sets the namespace of the typedef.
        /// </summary>
        /// <param name="typeNamespace">the namespace of the typedef</param>
        public void setNamespace(string typeNamespace)
        {
            this.typeNamespace = typeNamespace;
            this.wholeName = this.typeNamespace + this.name;
        }

        /// <summary>
        /// Returns the name of the typedef including its namespace.
        /// </summary>
        /// <returns>the name of the typedef</returns>
        public override string getFeatureName()
        {
            return this.wholeName;
        }

        /// <summary>
        /// Returns the name of the typedef including its namespace.
        /// </summary>
        /// <returns>the name of the typedef</returns>
        public override string getFeatureNameWithoutTemplate()
        {
            return this.wholeName;
        }

        /// <summary>
        /// Returns only the name of the typedef.
        /// </summary>
        /// <returns>only the name of the typedef</returns>
        public override string getFeatureNameWithoutTemplateAndNamespace()
        {
            return this.name;
        }

        /// <summary>
        /// Returns the namespace of the typedef.
        /// </summary>
        /// <returns>the namespace of the typedef</returns>
        public override string getNamespace()
        {
            return this.typeNamespace;
        }

        /// <summary>
        /// Returns the reference id of the typedef.
        /// </summary>
        /// <returns>the reference id of the typedef</returns>
        public override string getReference()
        {
            return this.reference;
        }

        public override Dictionary<string, DuneFeature> getVariability()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to DuneClass return false.
            DuneTypeDef p = obj as DuneTypeDef;
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
