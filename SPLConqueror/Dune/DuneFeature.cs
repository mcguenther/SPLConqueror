using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    /// <summary>
    /// The DuneFeature is an interface for the different type of features, namely enum and class.
    /// </summary>
    interface DuneFeature
    {
        /// <summary>
        /// Returns the name of the feature with its template.
        /// </summary>
        /// <returns>the name of the feature</returns>
        String getFeatureName();

        /// <summary>
        /// Returns the namespace of the feature.
        /// </summary>
        /// <returns>the namespace of the feature</returns>
        String getNamespace();

        /// <summary>
        /// Returns the reference of the feature.
        /// </summary>
        /// <returns>the reference of the feature</returns>
        String getReference();

        /// <summary>
        /// Returns the name of the feature without its template.
        /// </summary>
        /// <returns>the name of the feature</returns>
        String getFeatureNameWithoutTemplate();

        /// <summary>
        /// Returns the name of the feature without its template and its namespace.
        /// </summary>
        /// <returns>the name of the feature</returns>
        String getFeatureNameWithoutTemplateAndNamespace();
    }
}
