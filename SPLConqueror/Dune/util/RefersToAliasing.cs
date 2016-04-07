using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune.util
{
    /// <summary>
    /// This class is concerned with the alias mapping.
    /// </summary>
    class RefersToAliasing
    {
        Dictionary<string, string> refersToAlias = new Dictionary<string, string>();

        /// <summary>
        /// Adds the template argument with its alias.
        /// </summary>
        /// <param name="templateArgument">the name of the template argument</param>
        /// <param name="alias">the corresponding alias</param>
        public void add(string templateArgument, string alias)
        {
            this.refersToAlias.Add(templateArgument, alias);
        }

        /// <summary>
        /// Returns <code>true</code> if the corresponding entry is already in the dictionary; <code>false</code> otherwise.
        /// </summary>
        /// <param name="templateArgument">the template argument to check</param>
        /// <returns><code>true</code> if the corresponding entry is already in the dictionary; <code>false</code> otherwise</returns>
        public bool isIn(string templateArgument)
        {
            string output = null;
            return this.refersToAlias.TryGetValue(templateArgument, out output);
        }

        /// <summary>
        /// Returns the corresponding alias to the given argument.
        /// </summary>
        /// <param name="templateArgument">the argument to retrieve the alias for</param>
        /// <returns>the corresponding alias</returns>
        public string get(string templateArgument)
        {
            string output = null;
            this.refersToAlias.TryGetValue(templateArgument, out output);
            return output;
        }
        
    }
}
