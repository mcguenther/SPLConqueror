using MachineLearning.Learning.Regression;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    class HighErrorExplorer : ILearningSetExplorer
    {
        private List<Configuration> testSet;
        private FeatureSubsetSelection sel;
        private int stepSize;

        public HighErrorExplorer(List<Configuration> testSet, FeatureSubsetSelection sel, int stepSize)
        {
            this.testSet = testSet;
            this.sel = sel;
            this.stepSize = stepSize;
        }

        public List<Configuration> GetKnowledge()
        {
            //List<Configuration> measuredConfigs = this.sel.GetLearningSet();
            //measuredConfigs.Aggregate((a,b)=>a. () > b.GetNFPValue() ? a : b);

            throw new NotImplementedException();
            //return null;
        }
    }
}
