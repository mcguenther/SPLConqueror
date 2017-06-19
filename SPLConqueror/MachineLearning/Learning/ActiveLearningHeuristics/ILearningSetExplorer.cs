using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    public interface ILearningSetExplorer
    {
        List<Configuration> GetKnowledge();
    }
}
