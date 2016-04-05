using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune.util
{
    class Range
    {
        int lowerBound;
        int upperBound;

        /// <summary>
        /// Sets the lower and the upper bound.
        /// </summary>
        /// <param name="min">the lower bound</param>
        /// <param name="max">the upper bound</param>
        public Range (int min, int max)
        {
            if (lowerBound <= upperBound)
            {
                this.lowerBound = min;
                this.upperBound = max;
            } else
            {
                this.upperBound = min;
                this.lowerBound = max;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if the number is within the range; <code>false</code> otherwise.
        /// </summary>
        /// <param name="number">the number to check</param>
        /// <returns><code>true</code> if the number is within the range; <code>false</code> otherwise</returns>
        public bool isIn(int number)
        {
            return number >= this.lowerBound && number <= this.upperBound;
        }
    }
}
