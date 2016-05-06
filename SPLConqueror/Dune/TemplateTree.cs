using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    class TemplateTree : TemplateObject
    {
        List<TemplateTree> children = new List<TemplateTree>();

        TemplateTree currElement;
        TemplateTree parent = null;

        TemplateTree lastElement;

        String furtherInformation = "";
        String methodInvocation = "";

        public TemplateTree()
        {
            currElement = this;
            this.refersToAliasing = new util.RefersToAliasing();
        }

        public TemplateTree(TemplateTree tt)
        {
            currElement = this;
            this.refersToAliasing = tt.refersToAliasing;
        }

        /// <summary>
        /// Add the refering dune feature.
        /// </summary>
        /// <param name="df">the dune feature the current element refers to</param>
        public void addInformation(DuneFeature df)
        {
            
            TemplateTree newPart = new TemplateTree(this);
            List<DuneFeature> dfs = new List<DuneFeature>();
            dfs.Add(df);
            newPart.referseTo = dfs;
            newPart.type = Kind.concrete;
            newPart.artificalString = df.getFeatureNameWithoutTemplateAndNamespace();
            newPart.isTerminal = true;

            newPart.parent = currElement;
            currElement.children.Add(newPart);

            lastElement = newPart;
        }

        public void addInformation(List<DuneFeature> dfs)
        {
            if (this.referseTo != null)
            {
                this.referseTo.AddRange(dfs);
            } else
            {
                this.referseTo = dfs;
            }
            TemplateTree newPart = new TemplateTree(this);
            newPart.type = Kind.concrete;
            newPart.isTerminal = true;

            newPart.parent = currElement;
            currElement.children.Add(newPart);

            lastElement = newPart;
        }


        internal TemplateTree lastChild()
        {
            return currElement.children[currElement.children.Count - 1];
        }

        internal bool isClass()
        {
            if (lastElement != null)
                return lastElement.referseTo != null;
            else
                return false;
        }

        internal void incHierarchy()
        {
            TemplateTree nonTerminal = lastChild();
            nonTerminal.isTerminal = false;
            currElement = nonTerminal;
        }

        internal void decHierarchy()
        {
            currElement = currElement.parent;
            lastElement = lastElement.parent;
        }

        /// <summary>
        /// Returns <code>true</code> if the <code>TemplateTree</code> has no parents; <code>false</code> otherwise.
        /// </summary>
        /// <returns><code>true</code> if the <code>TemplateTree</code> has no parents; <code>false</code> otherwise</returns>
        internal bool isRoot()
        {
            return this.parent == null;
        }

        internal void addFurtherInformation(string token)
        {
            this.furtherInformation = token;
        }

        internal void addAlias(string templateargument, string alias)
        {
            string value = this.refersToAliasing.get(templateargument);
            if (value != null && value.Equals(alias))
            {
                return;
            }
            this.refersToAliasing.add(templateargument, alias);
        }


        internal void addNumericValue(string token)
        {
            TemplateTree newPart = new TemplateTree(this);
            newPart.artificalString = token;
            newPart.type = Kind.value;
            newPart.isTerminal = true;

            newPart.parent = currElement;
            currElement.children.Add(newPart);
            lastElement = newPart;
        }


        internal void addInformation(string token)
        {
            TemplateTree newPart = new TemplateTree(this);

            if (XMLParser.nameWithoutPackageToDuneFeatures.ContainsKey(token))
            {

                XMLParser.easyToFind += 1;
                if (XMLParser.nameWithoutPackageToDuneFeatures[token].Count > 1)
                {
                    XMLParser.mehrdeutigkeit += 1;
                    Console.WriteLine("TODO:: addInformation with mehrdeutigkeit");
                }

                List<DuneFeature> dfs = new List<DuneFeature>();
                dfs.Add(XMLParser.nameWithoutPackageToDuneFeatures[token].First());
                newPart.referseTo = dfs;
                newPart.artificalString = token;
                newPart.type = Kind.concrete;
                newPart.isTerminal = true;
            }
            else
            {
                newPart.artificalString = token;
                newPart.type = Kind.placeholder;
                newPart.isTerminal = true;
            }

            newPart.parent = currElement;
            currElement.children.Add(newPart);

            lastElement = newPart;
        }

        internal void addInformation(string token, TemplateElement furtherInformation)
        {
            TemplateTree newPart = new TemplateTree(this);

            newPart.artificalString = token;
            newPart.type = Kind.concrete;

            newPart.informationFromTemplateParamlist = furtherInformation;

            // TODO 
            newPart.isTerminal = true;


            newPart.parent = currElement;
            currElement.children.Add(newPart);
            lastElement = newPart;
        }

        internal void addInvocation(string method)
        {
            this.lastElement.methodInvocation = method;
        }


        public String toString()
        {
            StringBuilder sb = new StringBuilder();

            if (this.referseTo != null)
                sb.Append(this.referseTo.ToString());
            else
                sb.Append(artificalString);

            if (this.children.Count > 0)
                sb.Append(" < ");

            for (int i = 0; i < this.children.Count; i++)
            {
                sb.Append(this.children[i].toString() + " ");
            }

            if (this.children.Count > 0)
                sb.Append(" > ");

            sb.Append(furtherInformation);
            return sb.ToString();

        }


        /// <summary>
        /// This methods return a preorder flattering of the template tree. 
        /// </summary>
        /// <returns></returns>
        public List<TemplateTree> flatten()
        {
            List<TemplateTree> elements = new List<TemplateTree>();

            elements.Add(this);
            for (int i = 0; i < this.children.Count; i++)
            {
                elements.AddRange(this.children[i].flatten());
            }
            return elements;
        }


        internal void parentHasUnlimitedNumberOfParameters()
        {
            lastElement.parent.hasUnlimitedNumberOfParameters = true;
        }
    }
}
