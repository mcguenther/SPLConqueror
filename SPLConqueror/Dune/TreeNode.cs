using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This class represents a node in the tree. The node is an element of the original template and consists of a given range.
    /// An element with a fixed value is in this case a list containing only one single element.
    /// </summary>
    class TreeNode : TreeElement
    {
        List<Object> range;

        /// <summary>
        /// The constructor of the node which builds a new <code>TreeNode</code>-object by setting the range.
        /// </summary>
        /// <param name="range">the range, the respective (template-)node has</param>
        public TreeNode(List<Object> range)
        {
            this.range = range;
        }

        /// <summary>
        /// Returns the range of the respective node.
        /// </summary>
        /// <returns>the range of the respective node as a list</returns>
        public List<Object> getRange()
        {
            return range;
        }
    }
}
