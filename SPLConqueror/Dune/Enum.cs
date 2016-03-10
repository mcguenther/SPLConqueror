using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This class represents an enum.
    /// </summary>
    class Enum
    {
        private String name;
        private String reference;
        private List<DuneEnumValue> values;
        private List<string> stringValues = null;

        /// <summary>
        /// The constructor of the <code>Enum</code>-class. It creates an enum by its name, its reference-ID as well as its possible values.
        /// </summary>
        /// <param name="name">the name of the <code>enum</code></param>
        /// <param name="reference">the reference to this enum according to the xml-file created by <code>Doxygen</code></param>
        /// <param name="values">the values of the <code>enum</code></param>
        public Enum(String reference, String name, List<DuneEnumValue> values)
        {
            this.name = name;
            this.reference = reference;
            this.values = values;
        }
        
        /// <summary>
        /// Returns the name of the enum.
        /// </summary>
        /// <returns>the name of the enum</returns>
        public String getName()
        {
            return this.name;
        }

        /// <summary>
        /// Returns the reference of the enum.
        /// </summary>
        /// <returns>the reference of the enum</returns>
        public String getReference()
        {
            return this.reference;
        }

        /// <summary>
        /// Returns the list containing all <code>DuneEnumValues</code>.
        /// </summary>
        /// <returns>the <code>DuneEnumValues</code></returns>
        public List<DuneEnumValue> getValueObjects()
        {
            return this.values;
        }

        /// <summary>
        /// Returns the values of the enum.
        /// </summary>
        /// <returns>the values of the enum</returns>
        public List<string> getValues()
        {
            if (stringValues == null) { 
                stringValues = new List<string>();
                foreach(DuneEnumValue dev in this.values)
                {
                    stringValues.Add(dev.getFeatureName());
                }
            }
            return this.stringValues;
        }
    }
}
