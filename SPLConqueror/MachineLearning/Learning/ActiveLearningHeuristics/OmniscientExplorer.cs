using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    class OmniscientExplorer : ILearningSetExplorer
    {
        private List<Configuration> globalConfigList;
        public OmniscientExplorer(List<Configuration> globalConfigList)
        {
            this.globalConfigList = globalConfigList;
        }
        public List<Configuration> GetKnowledge()
        {
            return this.globalConfigList;
        }
    }
}
