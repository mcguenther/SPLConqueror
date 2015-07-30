using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This interface represents the elements of the tree which can be a <code>TreeNode</code> or a <code>Tree</code> itself.
    /// </summary>
    interface TreeElement
    {
        /// <summary>
        /// Returns the range of the respective element.
        /// </summary>
        /// <returns>the range of the respective element</returns>
        public List<Object> getRange();
    }
}
