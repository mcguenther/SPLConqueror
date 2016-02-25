using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This tree represents a template related to a specific class.
    /// The tree consists of <code>Tree</code>-objects, which contain 
    /// the ranges of the different template elements or of 
    /// <code>Tree</code>-objects if the template element is another class
    /// with another template.
    /// </summary>
    class Tree
    {
        private List<Tree> children;

        private List<String> range = new List<String>();

        private DuneClass feature = null;

        private String name = null;

        /// <summary>
        /// The constructor of the <code>Tree</code>-element creates a new tree object.
        /// </summary>
        /// <param name="df">the feature the node is related to</param>
        public Tree(DuneClass df)
        {
            this.feature = df;
        }

        /// <summary>
        /// The second constructor of the <code>Tree</code>-element which creates a new tree object from the given name.
        /// </summary>
        /// <param name="name">the name the root node of the tree should have</param>
        public Tree(String name) 
        {
            this.name = name;
        }

        /// <summary>
        /// Returns the range of the template tree as a list of strings(every element is one single variant).
        /// </summary>
        /// <returns>the range of the template tree as a list of strings</returns>
        public List<String> getRange()
        {
            return null;
        }

        /// <summary>
        /// Adds the given element to the range list.
        /// </summary>
        /// <param name="e">the element which should be added to the range list</param>
        public void addRangeElement(String e) {
            this.range.Add(e);
        }

        /// <summary>
        /// Adds a child to the list of children.
        /// </summary>
        /// <param name="child">the child to add</param>
        public void addChildren(Tree child)
        {
            this.children.Add(child);
        }
    }
}
