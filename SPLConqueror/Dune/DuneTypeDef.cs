using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    class DuneTypeDef : DuneFeature
    {
        private string typeNamespace;
        private string name;
        private string wholeName;
        private string reference;
        private TemplateTree type;

        public DuneTypeDef(string reference, string name, string type)
        {
            this.name = name;
            this.reference = reference;
            this.typeNamespace = "";
            this.wholeName = this.name;
            // TODO: Call the analyzeTemplate-method

        }

        public DuneTypeDef(string reference, string typeNamespace, string name, string type) : this(reference, name, type)
        {
            setNamespace(typeNamespace);
        }

        public void setNamespace(string typeNamespace)
        {
            this.typeNamespace = typeNamespace;
            this.wholeName = this.typeNamespace + this.name;
        }

        public override string getFeatureName()
        {
            return this.wholeName;
        }

        public override string getFeatureNameWithoutTemplate()
        {
            return this.wholeName;
        }

        public override string getFeatureNameWithoutTemplateAndNamespace()
        {
            return this.name;
        }

        public override string getNamespace()
        {
            return this.typeNamespace;
        }

        public override string getReference()
        {
            return this.reference;
        }

        public override List<string> getVariability(DuneFeature root)
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
