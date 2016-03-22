using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// The DuneFeature is an interface for the different type of features, namely enum and class.
    /// </summary>
    abstract class DuneFeature
    {
        /// <summary>
        /// Returns the name of the feature with its template.
        /// </summary>
        /// <returns>the name of the feature</returns>
        public abstract String getFeatureName();

        /// <summary>
        /// Returns the variability of the enum/class.
        /// </summary>
        /// <param name="root">the root feature. This is needed in case of trees</param>
        /// <returns>the whole list of variability that was found by the program</returns>
        public abstract List<String> getVariability(DuneFeature root);

        /// <summary>
        /// Returns the namespace of the feature.
        /// </summary>
        /// <returns>the namespace of the feature</returns>
        public abstract String getNamespace();

        /// <summary>
        /// Returns the reference of the feature.
        /// </summary>
        /// <returns>the reference of the feature</returns>
        public abstract String getReference();

        /// <summary>
        /// Returns the name of the feature without its template.
        /// </summary>
        /// <returns>the name of the feature</returns>
        public abstract String getFeatureNameWithoutTemplate();

        /// <summary>
        /// Returns the name of the feature without its template and its namespace.
        /// </summary>
        /// <returns>the name of the feature</returns>
        public abstract String getFeatureNameWithoutTemplateAndNamespace();


        public TemplateTree tempTree;
    }
}
