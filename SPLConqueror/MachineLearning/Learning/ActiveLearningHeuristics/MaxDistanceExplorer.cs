using MachineLearning.Solver;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    class MaxDistanceExplorer : StepExplorer
    {
        static Random rnd = new Random();




        public MaxDistanceExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize) : this(globalConfigList, vm, batchSize, 0)
        {
        }


        public MaxDistanceExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize, int sleepCycles) : base(globalConfigList, vm, batchSize, sleepCycles)
        {
        }


        protected void UpdateOptionUsages(Configuration newConfig)
        {
            List<BinaryOption> selectedOptions = newConfig.getBinaryOptions(BinaryOption.BinaryValue.Selected);
            foreach (BinaryOption opt in selectedOptions)
            {
                if (this.binaryOptionalOptionUsages.Keys.Contains(opt))
                {
                    int oldUsages = this.binaryOptionalOptionUsages[opt];
                    this.binaryOptionalOptionUsages[opt] = oldUsages + 1;
                }
            }
        }



        /// <summary>
        /// Discovers a new config that has maximum distance to already known configs in the config space.
        /// </summary>
        /// <returns>The option which has the so far least selected value for each dimension. 
        /// Since we only work with binary options, this returns the configruation that has the minimal sum of 
        /// Manhatten distances to all known configurations. 
        /// If worked in the continous space, we would need to compute a voronoi graph, 
        /// and select from the vertices of it and the space constraints the point which has 
        /// the largest distance to all neighbours.</returns>
        protected override Configuration DiscoverNewConfig()
        {
            bool doRandomFill = false;
            Configuration newConfig = null;

            doRandomFill = (rnd.Next(4) > 2);

            if (doRandomFill)
            {
                newConfig = RandomExplorer.DrawWithReplacement(this.undiscoveredConfigs);
                this.undiscoveredConfigs.Remove(newConfig);
            }
            else
            {
                List<BinaryOption> optionsActive = new List<BinaryOption>();
                List<BinaryOption> optionsPassive = new List<BinaryOption>();
                int configCount = this.currentKnowledge.Count;
                double threshold = configCount / 2.0;
                foreach (BinaryOption opt in this.binaryOptionalOptions)
                {
                    int usages = this.binaryOptionalOptionUsages[opt];
                    if (usages > threshold)
                    {
                        /* option was more often active than it was passive */
                        optionsPassive.Add(opt);
                    }
                    else
                    {
                        optionsActive.Add(opt);
                    }
                }
                newConfig = FindClosestUndiscoveredConfig(this.undiscoveredConfigs, optionsActive, optionsPassive);
            }
            UpdateOptionUsages(newConfig);
            return newConfig;
        }


        protected override List<Configuration> DiscoverFirstConfigs()
        {
            List<Configuration> initConfig = new List<Configuration>();
            /* Adds Config that has least least amount of options */
            //Generating new configurations: one per option
            if (checkSAT.checkConfigurationSAT(this.binaryMandatoryOptions, vm, false))
            {
                Configuration newConfig = GetConfigWithBinaryOptions(this.binaryMandatoryOptions);
                initConfig.Add(newConfig);
            }
            return initConfig;
        }
    }
}

