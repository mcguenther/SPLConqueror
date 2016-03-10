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
        TemplateTree parent;

        TemplateTree lastElement;

        String furtherInformation = "";

        public TemplateTree()
        {
            currElement = this;
            
        }


        internal TemplateTree lastChild()
        {
            return currElement.children[currElement.children.Count - 1];
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

        internal void addFurtherInformation(string token)
        {
            this.furtherInformation = token;
        }

        internal void addInformation(string token)
        {
            TemplateTree newPart = new TemplateTree();

            if (XMLParser.nameWithoutPackageToDuneFeatures.ContainsKey(token))
            {

                XMLParser.easyToFind += 1;
                if (XMLParser.nameWithoutPackageToDuneFeatures[token].Count > 1)
                {
                    XMLParser.mehrdeutigkeit += 1;
                    Console.WriteLine("TODO:: addInformation with mehrdeutigkeit");
                }
                
                newPart.referseTo = XMLParser.nameWithoutPackageToDuneFeatures[token].First();
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

        public String toString()
        {
            StringBuilder sb = new StringBuilder();

            if (this.referseTo != null)
                sb.Append(this.referseTo.ToString());
            else
                sb.Append(artificalString);

            if(this.children.Count > 0)
                sb.Append(" ( ");

            for (int i = 0; i < this.children.Count; i++)
            {
                sb.Append(this.children[i].toString()+ " ");
            }

            if (this.children.Count > 0)
                sb.Append(" ) ");

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
