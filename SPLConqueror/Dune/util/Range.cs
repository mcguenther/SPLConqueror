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

        /// <summary>
        /// Returns <code>true</code> iff the specified number corresponds to the upper bound.
        /// </summary>
        /// <param name="number">the number to check</param>
        /// <returns><code>true</code> iff the specified number corresponds to the upper bound</returns>
        public bool isUpperBound(int number)
        {
            return number == this.upperBound;
        }

        /// <summary>
        /// Returns the upper bound of the range.
        /// </summary>
        /// <returns>the upper bound</returns>
        public int getUpperBound()
        {
            return this.upperBound;
        }

        /// <summary>
        /// Returns the lower bound of the range.
        /// </summary>
        /// <returns>the lower bound</returns>
        public int getLowerBound()
        {
            return this.lowerBound;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Range return false.
            Range p = obj as Range;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return this.lowerBound == p.lowerBound && this.upperBound == p.upperBound;
        }

    }
}
