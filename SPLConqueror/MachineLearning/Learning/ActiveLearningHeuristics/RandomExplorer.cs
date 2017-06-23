using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    class RandomExplorer : StepExplorer 
    {
        static Random rnd = new Random();
    

        public RandomExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize) : this(globalConfigList, vm, batchSize, 0)
        {
        }


        public RandomExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize, int sleepCycles) : base(globalConfigList, vm, batchSize,sleepCycles)
        {
        }



        protected override Configuration DiscoverNewConfig()
        {
            int rndConfId = rnd.Next(this.undiscoveredConfigs.Count);
            Configuration newConf = undiscoveredConfigs[rndConfId];
            undiscoveredConfigs.Remove(newConf);
            return newConf;
        }

        public static Configuration DrawWithoutReplacement(List<Configuration> configs)
        {
            int rndConfId = rnd.Next(configs.Count);
            Configuration newConf = configs[rndConfId];
            configs.Remove(newConf);
            return newConf;
        }

        protected override void DiscoverFirstConfig()
        {
           /* No need for speacial initialization */
        }
        
        
    }
}
