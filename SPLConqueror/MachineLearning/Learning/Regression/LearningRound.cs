using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression
{
    public class LearningRound
    {
        internal double learningError = Double.MaxValue;
        internal double validationError = Double.MaxValue;
        internal double learningError_relative = Double.MaxValue;
        internal double validationError_relative = Double.MaxValue;
        private List<Feature> featureSet = new List<Feature>();

        public List<Feature> FeatureSet
        {
            get { return featureSet; }
            set { featureSet = value; }
        }
        internal int round = 0;

        /// <summary>
        /// TODO: Prints the information learned in a round.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(round + ";");
            for (int i = 0; i < featureSet.Count; i++)
            {
                Feature f = featureSet[i];
                sb.Append(f.Constant + " * " + f.ToString());
                if (i < featureSet.Count - 1)
                    sb.Append(" + ");
            }
            sb.Append(";");

            sb.Append(learningError + ";");
            sb.Append(learningError_relative+";");
            sb.Append(validationError + ";");
            sb.Append(validationError_relative + ";");

            return sb.ToString();
        }

        internal LearningRound(List<Feature> featureSet, double learningError, double validationError, int round)
        {
            this.featureSet = featureSet;
            this.learningError = learningError;
            this.validationError = validationError;
            this.round = round;
        }

        internal LearningRound() {}

        public override bool Equals(System.Object obj)
        {
            LearningRound other = ((LearningRound)obj);
            if (this.featureSet.Count != other.featureSet.Count)
                return false;

            for (int i = 0; i < this.featureSet.Count; i++)
            {
                bool found = false;
                int otherIndex = 0;
                do
                {
                    if(other.featureSet[otherIndex].Equals(this.featureSet[i]))
                        found = true;

                    otherIndex += 1;
                } while (!found && otherIndex < this.featureSet.Count);

                if (!found)
                {
                    return false;
                }
                
            }
            return true;
        }


        public override int GetHashCode()
        {
            // this could lead to a bug...
            return this.featureSet.GetHashCode();
        }
    }
}
