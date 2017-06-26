using MachineLearning.Learning.Regression;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    public class MaxErrorExplorer : StepExplorer
    {
        public const string command = "explorer-max-error";
        private FeatureSubsetSelection sel;
        private int batchSizeExploit;
        private int batchSizeExplore;
        private int sleepRoundsExplore;
        private int sleepRoundsExploreRemaining;
        private int internalRoundsPerCycle;

        public MaxErrorExplorer(List<Configuration> globalConfigs, VariabilityModel vm,
            FeatureSubsetSelection sel, int internalRoundsPerCycle,
            int batchSizeExploit, int batchSizeExplore,
             int sleepCycles, int sleepRoundsExplore) : base(globalConfigs, vm, batchSizeExploit, sleepCycles)
        {
            this.sel = sel;
            this.internalRoundsPerCycle = internalRoundsPerCycle;
            this.batchSizeExploit = batchSizeExploit;
            this.batchSizeExplore = batchSizeExplore;
            this.sleepRoundsExplore = sleepRoundsExplore;
            this.sleepRoundsExploreRemaining = 0;
        }

        protected override void Explore()
        {
            for (int n = 0; n < this.internalRoundsPerCycle; n++)
            {
                List<Configuration> step = new List<Configuration>();
                if (sleepRoundsExploreRemaining > 0)
                {
                    for (int i = 0; i < this.batchSizeExploit && this.undiscoveredConfigs.Count > 0; i++)
                    {
                        Configuration newConf = DiscoverNewConfig();
                        if (newConf != null)
                        {
                            step.Add(newConf);
                        }
                        else
                        {
                            throw new Exception("Couldn't find a new Config!");
                        }
                    }
                    this.sleepRoundsExploreRemaining--;
                }
                else
                {
                    for (int i = 0; i < this.batchSizeExplore && this.undiscoveredConfigs.Count > 0; i++)
                    {
                        Configuration newConf = DiscoverRandomConfig();
                        if (newConf != null)
                        {
                            step.Add(newConf);
                        }
                        else
                        {
                            throw new Exception("Couldn't find a new Config!");
                        }
                    }
                    this.sleepRoundsExploreRemaining = this.sleepRoundsExplore;
                }
                ExpandCurrentKnowledge(step);
            }
        }






        protected override List<Configuration> DiscoverFirstConfigs()
        {
            List<Configuration> initConfigs = new List<Configuration>();
            return initConfigs;
        }

        protected override Configuration DiscoverNewConfig()
        {
            Configuration newConfig = null;

            if (sel.LearningHistory.Count > 0)
            {
                Dictionary<Configuration, double> misFits = new Dictionary<Configuration, double>();
                LearningRound lastLearningRound = sel.LearningHistory.Last();
                foreach (Configuration conf in this.currentKnowledge)
                {
                    List<Feature> featureSet = lastLearningRound.FeatureSet;
                    List<Configuration> tmpList = new List<Configuration>() { conf };
                    double outVal;
                    double error = sel.computeError(featureSet, tmpList, out outVal);
                    misFits.Add(conf, error);
                }


                IOrderedEnumerable<KeyValuePair<Configuration, double>> sortedDict = misFits.OrderBy(entry => entry.Value);
                KeyValuePair<Configuration, double> maxErrorConfigEntry = sortedDict.First();
                Configuration maxErrorConfig = maxErrorConfigEntry.Key;

                Configuration neighbour = FindClosestConfig(this.undiscoveredConfigs, maxErrorConfig);
                newConfig = neighbour;
            }
            else
            {
                newConfig = RandomExplorer.DrawWithReplacement(this.undiscoveredConfigs);
            }
            this.undiscoveredConfigs.Remove(newConfig);
            return newConfig;
        }
    }
}
