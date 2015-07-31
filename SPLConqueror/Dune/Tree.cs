using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This tree represents a template related to a specific class.
    /// The tree consists of <code>TreeNode</code>-objects, which contain 
    /// the ranges of the different template elements or of 
    /// <code>Tree</code>-objects if the template element is another class
    /// with another template.
    /// </summary>
    class Tree : TreeElement
    {
        private List<TreeNode> children;

        /// <summary>
        /// The constructor of the <code>Tree</code>-element analyzes the given template recursively.
        /// </summary>
        /// <param name="templateToAnalyze">the template to analyze</param>
        public Tree(String templateToAnalyze)
        {
            // TODO: implement
        }

        public List<Object> getRange()
        {
            return null;
        }

        /// <summary>
        /// This method analyzes the template and constructs a new <code>Tree</code> corresponding to the given template.
        /// </summary>
        /// <param name="templateToAnalyze">the template to analyze</param>
        public void analyzeTemplate(String templateToAnalyze) {
            // TODO: implement
        }
    }
}
