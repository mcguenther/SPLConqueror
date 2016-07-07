using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// This list contains options that are needed to save the methods of the different classes in Dune.
    /// </summary>
    class MethodList
    {
        private List<int> allPossibleMethodHashes;
        private List<int> methodHashes;
        private List<int> methodNameHashes;
        private List<int> argumentCount;
        private List<string> methodArguments;
        private List<List<int>> replaceableArgs;
        private bool hasNormalMethods;

        /// <summary>
        /// The constructor of this list.
        /// </summary>
        /// <param name="allPossibleMethodHashes">all possible hashes of the methods according to default values</param>
        /// <param name="methodHashes">the hashes of the methods</param>
        /// <param name="methodNameHashes">the hashes of the names of the methods</param>
        /// <param name="argumentCount">the number of arguments of the methods</param>
        /// <param name="methodArguments">the arguments of the methods</param>
        /// <param name="replaceableArgs">the arguments that may be replaced by other arguments</param>
        /// <param name="hasNormalMethods">indicates if the class includes not only constructors</param>
        public MethodList(List<int> allPossibleMethodHashes, List<int> methodHashes, List<int> methodNameHashes, List<int> argumentCount, List<string> methodArguments, List<List<int>> replaceableArgs, bool hasNormalMethods)
        {
            this.allPossibleMethodHashes = allPossibleMethodHashes;
            this.methodHashes = methodHashes;
            this.methodNameHashes = methodNameHashes;
            this.argumentCount = argumentCount;
            this.methodArguments = methodArguments;
            this.replaceableArgs = replaceableArgs;
            this.hasNormalMethods = hasNormalMethods;
        }

        /// <summary>
        /// Returns a list including all possible method hashes regarding default values.
        /// </summary>
        /// <returns>a list including all possible method hashes</returns>
        public List<int> getAllPossibleMethodHashes()
        {
            return this.allPossibleMethodHashes;
        }

        /// <summary>
        /// Returns the hashes of the methods the class has.
        /// </summary>
        /// <returns>the hashes of the methods the class has</returns>
        public List<int> getMethodHashes()
        {
            return this.methodHashes;
        }

        /// <summary>
        /// Returns the hashes of the method names of the class.
        /// </summary>
        /// <returns>the hashes of the method names of the class</returns>
        public List<int> getMethodNameHashes()
        {
            return methodNameHashes;
        }

        /// <summary>
        /// Returns the number of arguments each method has.
        /// </summary>
        /// <returns>the number of arguments each method has</returns>
        public List<int> getArgumentCount()
        {
            return this.argumentCount;
        }

        /// <summary>
        /// Returns a list that contains the method arguments of each method.
        /// </summary>
        /// <returns>a list that contains the method arguments of each method</returns>
        public List<string> getMethodArguments()
        {
            return this.methodArguments;
        }

        /// <summary>
        /// Returns a list of arguments that may be replaced by other arguments.
        /// </summary>
        /// <returns>a list of arguments that may be replaced by other arguments</returns>
        public List<List<int>> getReplaceableArguments()
        {
            return this.replaceableArgs;
        }

        /// <summary>
        /// Returns <code>true</code> if the class has also other methods than constructors; <code>false</code> otherwise.
        /// </summary>
        /// <returns><code>true</code> if the class has also other methods than constructors; <code>false</code> otherwise</returns>
        public Boolean classHasNormalMethods()
        {
            return hasNormalMethods;
        }
        
    }
}
