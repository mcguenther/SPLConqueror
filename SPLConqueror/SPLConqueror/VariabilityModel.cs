﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace SPLConqueror_Core
{
    /// <summary>
    /// Central model to store all configuration options and their constraints.
    /// </summary>
    public class VariabilityModel
    {
        private List<NumericOption> numericOptions = new List<NumericOption>();

        /// <summary>
        /// The set of numeric configuration options of the variability model.
        /// </summary>
        public List<NumericOption> NumericOptions
        {
            get { return numericOptions; }
        }

        private List<BinaryOption> binaryOptions = new List<BinaryOption>();

        /// <summary>
        /// The set of all binary configuration options of the system.
        /// </summary>
        public List<BinaryOption> BinaryOptions
        {
            get { return binaryOptions; }
        }

        /// <summary>
        /// A mapping from the index of an option to the object providing all information of the configuratio option.
        /// </summary>
        public Dictionary<int, ConfigurationOption> optionToIndex = new Dictionary<int, ConfigurationOption>();

        /// <summary>
        /// A mapping from a configuration option to its index.
        /// </summary>
        public Dictionary<ConfigurationOption, int> indexToOption = new Dictionary<ConfigurationOption, int>();

        String name = "empty";

        /// <summary>
        /// Name of the variability model or configurable system.
        /// </summary>
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        String path = "";

        /// <summary>
        /// Local path, in which the model is located
        /// </summary>
        public String Path
        {
            get { return path; }
            set { path = value; }
        }

        private BinaryOption root = null;

        /// <summary>
        /// The root binary configuration option.
        /// </summary>
        public BinaryOption Root
        {
            get { return root; }
        }

        private List<String> binaryConstraints = new List<string>();

        /// <summary>
        /// The set of all constraints among the binary configuration options.
        /// </summary>
        public List<String> BinaryConstraints
        {
            get { return binaryConstraints; }
            set { binaryConstraints = value; }
        }

        private List<NonBooleanConstraint> nonBooleanConstraints = new List<NonBooleanConstraint>();

        /// <summary>
        /// The list of all non-boolean constraints of the variability model. Non-boolean constraints are constraints among different numeric 
        /// options or binary and numeric options.
        /// </summary>
        public List<NonBooleanConstraint> NonBooleanConstraints
        {
            get { return nonBooleanConstraints; }
            set { nonBooleanConstraints = value; }
        }

        private List<MixedConstraint> mixedConstraints = new List<MixedConstraint>();

        public List<MixedConstraint> MixedConstraints
        {
            get { return mixedConstraints; }
            set { mixedConstraints = value; }
        }
     
        /// <summary>
        /// Retuns a list containing all numeric configuration options that are considered in the learning process.
        /// </summary>
        /// <param name="blacklist">A list containing all numeric options that should not be considered in the learning process.</param>
        /// <returns>A list containing all numeric configuartion options that are considered in the learning process.</returns>
        public List<NumericOption> getNonBlacklistedNumericOptions(List<String> blacklist)
        {
            List<NumericOption> result = new List<NumericOption>();            foreach (NumericOption opt in this.numericOptions)
            {
                if (blacklist != null)
                {
                    if (!blacklist.Contains(opt.Name.ToLower()))
                    {
                        result.Add(opt);
                    }
                } else
                {
                    result.Add(opt);
                }
            }

            return result;
        }

        /// <summary>
        /// Creastes a new variability model with a given name that consists only of a binary root option.
        /// </summary>
        /// <param name="name">The name of the variability model.</param>        
        
        public VariabilityModel(String name)
        {
            this.name = name;
            if (root == null)
                root = new BinaryOption(this, "root");
            this.BinaryOptions.Add(root);
        }

        /// <summary>
        /// Stores the current variability model into an XML file with the already stored path
        /// </summary>
        /// <returns>Returns true if sucessfully saved, false otherwise</returns>
        public bool saveXML()
        {
            if (this.path.Length > 0)
                return saveXML(this.path);
            else
                return false;
        }

        /// <summary>
        /// Stores the current variability model into an XML file
        /// </summary>
        /// <param name="path">Path to which the XML file is stored</param>
        /// <returns>Returns false if the saving was not successfull</returns>
        public bool saveXML(String path)
        {
            //Create XML Document
            XmlDocument doc = new XmlDocument();

            //Create an XML declaration. 
            XmlDeclaration xmldecl;
            xmldecl = doc.CreateXmlDeclaration("1.0", null, null);

            //Add a root node to the document.
            XmlNode xmlroot = doc.CreateNode(XmlNodeType.Element, "vm", ""); // ***

            XmlAttribute xmlattr = doc.CreateAttribute("name");
            xmlattr.Value = this.name;
            xmlroot.Attributes.Append(xmlattr);

            //Add binary options
            XmlNode xmlBin = doc.CreateNode(XmlNodeType.Element, "binaryOptions", "");
            foreach (BinaryOption binOpt in this.binaryOptions)
            {
                xmlBin.AppendChild(binOpt.saveXML(doc));
            }
            xmlroot.AppendChild(xmlBin);

            //Add numeric options
            XmlNode xmlNum = doc.CreateNode(XmlNodeType.Element, "numericOptions", "");
            foreach (NumericOption numOpt in this.numericOptions)
            {
                xmlNum.AppendChild(numOpt.saveXML(doc));
            }
            xmlroot.AppendChild(xmlNum);

            //Add boolean constraints
            XmlNode boolConstraints = doc.CreateNode(XmlNodeType.Element, "booleanConstraints", "");
            foreach (var constraint in this.binaryConstraints)
            {
                XmlNode conNode = doc.CreateNode(XmlNodeType.Element, "constraint", "");
                conNode.InnerText = constraint;
                boolConstraints.AppendChild(conNode);
            }
            xmlroot.AppendChild(boolConstraints);

            //Add non-boolean constraints
            XmlNode nonBooleanConstraints = doc.CreateNode(XmlNodeType.Element, "nonBooleanConstraints", "");
            foreach (var constraint in this.nonBooleanConstraints)
            {
                XmlNode conNode = doc.CreateNode(XmlNodeType.Element, "constraint", "");
                conNode.InnerText = constraint.ToString();
                nonBooleanConstraints.AppendChild(conNode);
            }
            xmlroot.AppendChild(nonBooleanConstraints);


            doc.AppendChild(xmlroot);

            try
            {
                doc.Save(path);
            }
            catch (Exception e)
            {
                GlobalState.logError.logLine(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Static method that reads an xml file and constructs a variability model using the stored information
        /// </summary>
        /// <param name="path">Path to the XML File</param>
        /// <returns>The instantiated variability model or null if there is no variability model at the path or if the model could not be parsed.</returns>
        public static VariabilityModel loadFromXML(String path)
        {
            VariabilityModel vm = new VariabilityModel("temp");
            if (vm.loadXML(path))
                return vm;
            else
                return null;
        }

        /// <summary>
        /// Loads an XML file containing information about the variability model.
        /// </summary>
        /// <param name="path">Path to the XML file</param>
        /// <returns>True if the variability model could be parsed, false otherwise.</returns>
        public Boolean loadXML(String path)
        {
            XmlDocument dat = new System.Xml.XmlDocument();
            if (!File.Exists(path))
                return false;
            dat.Load(path);
    
            XmlElement currentElemt = dat.DocumentElement;
            this.name = currentElemt.Attributes["name"].Value.ToString();
            foreach (XmlElement xmlNode in currentElemt.ChildNodes)
            {
                switch (xmlNode.Name)
                {
                    case "binaryOptions":
                        loadBinaryOptions(xmlNode);
                        break;
                    case "numericOptions":
                        loadNumericOptions(xmlNode);
                        break;
                    case "booleanConstraints":
                        loadBooleanConstraints(xmlNode);
                        break;
                    case "nonBooleanConstraints":
                        loadNonBooleanConstraint(xmlNode);
                        break;
                    case "mixedConstraints":
                        loadMixedConstraints(xmlNode);
                        break;
                }
            }

            initOptions();
            return true;
        }

        /// <summary>
        /// After loading all options, we can replace the names for children, the parent, etc. with the actual objects.
        /// </summary>
        private void initOptions()
        {
            foreach (var binOpt in binaryOptions)
                binOpt.init();
            foreach (var numOpt in numericOptions)
                numOpt.init();

            foreach (var opt in getOptions())
                opt.updateChildren();
        }

        private void loadBooleanConstraints(XmlElement xmlNode)
        {
            foreach (XmlElement boolConstr in xmlNode.ChildNodes)
            {
                this.binaryConstraints.Add(boolConstr.InnerText);
            }
        }

        private void loadNonBooleanConstraint(XmlElement xmlNode)
        {
            foreach (XmlElement nonBoolConstr in xmlNode.ChildNodes)
            {
                this.nonBooleanConstraints.Add(new NonBooleanConstraint(nonBoolConstr.InnerText,this));
            }
        }

        private void loadMixedConstraints(XmlElement xmlNode)
        {
            foreach (XmlElement mixedConstraint in xmlNode.ChildNodes)
            {
                if (mixedConstraint.Attributes.Count == 1)
                {
                    this.mixedConstraints.Add(new MixedConstraint(mixedConstraint.InnerText, this, this, mixedConstraint.Attributes[0].Value));
                }
                else
                {
                    this.mixedConstraints.Add(new MixedConstraint(mixedConstraint.InnerText, this, this, mixedConstraint.Attributes[0].Value, mixedConstraint.Attributes[1].Value));
                }
            }
        }

        private void loadNumericOptions(XmlElement xmlNode)
        {
            foreach (XmlElement numOptNode in xmlNode.ChildNodes)
            {
                if (!addConfigurationOption(NumericOption.loadFromXML(numOptNode, this)))
                    GlobalState.logError.logLine("Could not add option to the variability model. Possible reasons: invalid name, option already exists.");
            }
        }

        private void loadBinaryOptions(XmlElement xmlNode)
        {
            foreach (XmlElement binOptNode in xmlNode.ChildNodes)
            {
                if (!addConfigurationOption(BinaryOption.loadFromXML(binOptNode, this)))
                    GlobalState.logError.logLine("Could not add option to the variability model. Possible reasons: invalid name, option already exists.");
            }
        }

        /// <summary>
        /// Adds a configuration option to the variability model.
        /// The method checks whether an option with the same name already exists and whether invalid characters are within the name
        /// </summary>
        /// <param name="option">The option to be added to the variability model.</param>
        /// <returns>True if the option was added to the model, false otherwise</returns>
        public bool addConfigurationOption(ConfigurationOption option)
        {
            if (option.Name.Contains('-') || option.Name.Contains('+'))
                return false;

            // the vitrual root configuration option does not have to be added to the variability model. 
            if (option.Name.Equals("root"))
                return true;

            foreach (var opt in binaryOptions)
            {
                if (opt.Name.Equals(option.Name))
                    return false;
            }
            foreach (var opt in numericOptions)
            {
                if (opt.Name.Equals(option.Name))
                    return false;
            }

            if (this.hasOption(option.ParentName))
                option.Parent = this.getOption(option.ParentName);


            //Every option must have a parent
            if (option.Parent == null)
                option.Parent = this.root;
   
            if (option is BinaryOption)
                this.binaryOptions.Add((BinaryOption)option);
            else
                this.numericOptions.Add((NumericOption)option);

            // create Index 
            optionToIndex.Add(optionToIndex.Count, option);
            indexToOption.Add(option, indexToOption.Count);


            return true;
        }

        /// <summary>
        /// Searches for a binary option with the given name.
        /// </summary>
        /// <param name="name">Name of the option</param>
        /// <returns>Either the binary option with the given name or NULL if not found</returns>
        public BinaryOption getBinaryOption(String name)
        {

            name = ConfigurationOption.removeInvalidCharsFromName(name);
            foreach (var binO in binaryOptions)
            {
                if (binO.Name.Equals(name))
                    return binO;
            }
            return null;
        }

        /// <summary>
        /// Searches for a numeric option with the given name.
        /// </summary>
        /// <param name="name">Name of the option</param>
        /// <returns>Either the numeric option with the given name or NULL if not found</returns>
        public NumericOption getNumericOption(String name)
        {
            name = ConfigurationOption.removeInvalidCharsFromName(name);
            foreach (var numO in numericOptions)
            {
                if (numO.Name.Equals(name))
                    return numO;
            }
            return null;
        }

        /// <summary>
        /// This method retuns the configuration with the given name.
        /// </summary>
        /// <param name="name">Name of the option under consideration.</param>
        /// <returns>The option with the given name or NULL of no option with the name is defined.</returns>
        public ConfigurationOption getOption(String name)
        {
            name = ConfigurationOption.removeInvalidCharsFromName(name);
            ConfigurationOption opt = getNumericOption(name);
            if (opt != null)
                return opt;
            opt = getBinaryOption(name);

            return opt;
        }

        /// <summary>
        /// Returns a list containing all configuration options of the variability model. 
        /// </summary>
        /// <returns>List of all options of the variability model.</returns>
        public List<ConfigurationOption> getOptions()
        {
            List<ConfigurationOption> options = new List<ConfigurationOption>();
            options.AddRange(binaryOptions);
            options.AddRange(numericOptions);
            return options;
        }

        /// <summary>
        /// Tests whether a configuration is valid with respect to all non-boolean constraints.
        /// </summary>
        /// <param name="c">The configuration to test.</param>
        /// <returns>True if the configuration is valid.</returns>
        public bool configurationIsValid(Configuration c)
        {
            foreach (NonBooleanConstraint nonBC in this.nonBooleanConstraints)
            {
                if (!nonBC.configIsValid(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Removes a configuration from the variability model.
        /// </summary>
        /// <param name="toDelete"></param>
        public void deleteOption(ConfigurationOption toDelete)
        {
            // Removing all children
            List<ConfigurationOption> children = new List<ConfigurationOption>();

            foreach (ConfigurationOption opt in toDelete.Children)
                children.Add(opt);

            foreach (ConfigurationOption child in children)
                deleteOption(child);

            // Removing option from other options
            foreach (ConfigurationOption opt in getOptions())
            {
                for (int i = opt.Excluded_Options.Count - 1; i >= 0; i--)
                {
                    if (opt.Excluded_Options[i].Contains(toDelete))
                        opt.Excluded_Options.RemoveAt(i);
                }

                for (int i = opt.Implied_Options.Count - 1; i >= 0; i--)
                {
                    if (opt.Implied_Options[i].Contains(toDelete))
                        opt.Implied_Options.RemoveAt(i);
                }
            }

            // Removing option from constraints
            binaryConstraints.RemoveAll(x => x.Contains(toDelete.ToString()));
            nonBooleanConstraints.RemoveAll(x => x.ToString().Contains(toDelete.ToString()));

            toDelete.Parent.Children.Remove(toDelete);

            if (toDelete is BinaryOption)
                binaryOptions.Remove((BinaryOption)toDelete);
            else if (toDelete is NumericOption)
                numericOptions.Remove((NumericOption)toDelete);
            else
                throw new Exception("An illegal option was found while deleting.");
        }

        internal bool hasOption(string name)
        {
            return (this.getBinaryOption(name) != null) || (this.getNumericOption(name) != null); 
        }
    }
}
