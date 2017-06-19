using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    class RandomExplorer : ILearningSetExplorer
    {
        private List<Configuration> globalConfigList;
        private List<Configuration> undiscoveredConfigs;
        private List<Configuration> currentKnowledge;
        private List<List<Configuration>> history = new List<List<Configuration>>();
        private int sleepCycles;
        private int STANDARD_STEP_SIZE = 5;
        private int batchSize;
        static Random rnd = new Random();
        private int remainingSleepCycles;

        public RandomExplorer(List<Configuration> globalConfigList)
        {
            this.globalConfigList = globalConfigList;
            this.undiscoveredConfigs = globalConfigList;
            currentKnowledge = new List<Configuration>();
            batchSize = STANDARD_STEP_SIZE;
            sleepCycles = 0;
            remainingSleepCycles = 0;
        }

        public RandomExplorer(List<Configuration> globalConfigList, int batchSize) : this(globalConfigList, batchSize, 0)
        {
        }


        public RandomExplorer(List<Configuration> globalConfigList, int batchSize, int sleepCycles) : this(globalConfigList)
        {
            this.batchSize = batchSize;
            this.sleepCycles = sleepCycles;
        }



        public List<Configuration> GetKnowledge()
        {
            if (remainingSleepCycles <= 0)
            {
                Explore();
                remainingSleepCycles = sleepCycles;
            }
            else
            {
                remainingSleepCycles--;
            }
            return currentKnowledge;
        }

        private void Explore()
        {
            List<Configuration> step = new List<Configuration>();
            for (int i = 0; i < this.batchSize; i++)
            {
                if (this.undiscoveredConfigs.Count > 0)
                {
                    Configuration newConf = DiscoverNewConfig();
                    step.Add(newConf);
                }
            }
            ExpandCurrentKnowledge(step);
        }

        private Configuration DiscoverNewConfig()
        {
            int rndConfId = rnd.Next(undiscoveredConfigs.Count);
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


        private void ExpandCurrentKnowledge(List<Configuration> newConfigs)
        {
            History.Add(newConfigs);
            currentKnowledge = new List<Configuration>(currentKnowledge);
            currentKnowledge.AddRange(newConfigs);
        }

        public List<List<Configuration>> History { get => history; }
    }
}
