using MachineLearning.Solver;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    public class CombinatorialExplorer : ILearningSetExplorer
    {
        private List<Configuration> globalConfigList;
        private List<Configuration> undiscoveredConfigs;
        private List<Configuration> currentKnowledge;
        private List<List<Configuration>> history = new List<List<Configuration>>();
        private int remainingSleepCycles = 0;
        private int STANDARD_STEP_SIZE = 5;
        private int batchSize;
        static Random rnd = new Random();
        private int sleepCycles;
        private List<BinaryOption> binaryOptions;
        private List<BinaryOption> unusedBinaryOptions;
        private List<Tuple<BinaryOption, BinaryOption>> unusedBinaryOptionPairs;
        VariantGenerator variantGenerator;
        private VariabilityModel vm;

        public CombinatorialExplorer(List<Configuration> globalConfigList, VariabilityModel vm)
        {
            this.globalConfigList = globalConfigList;
            this.undiscoveredConfigs = globalConfigList;
            currentKnowledge = new List<Configuration>();
            batchSize = STANDARD_STEP_SIZE;
            sleepCycles = 0;
            remainingSleepCycles = 0;

            // Dictionary<BinaryOption, BinaryOption.BinaryValue> binaryOptions = globalConfigList[0].BinaryOptions;
            //List<BinaryOption> binKeys = new List<BinaryOption>(binaryOptions.Keys);
            //this.binaryOptions = binKeys;
            this.binaryOptions = vm.BinaryOptions;
            this.unusedBinaryOptions = new List<BinaryOption>(vm.BinaryOptions);
            this.variantGenerator = new VariantGenerator();
            this.vm = vm;
            unusedBinaryOptionPairs = new List<Tuple<BinaryOption, BinaryOption>>();
            for (int i = 0; i < this.binaryOptions.Count - 1; i++)
            {
                for (int j = i + 1; j < this.binaryOptions.Count; j++)
                {
                    Tuple<BinaryOption, BinaryOption> newPair = new Tuple<BinaryOption, BinaryOption>(binaryOptions[i], binaryOptions[j]);
                    this.unusedBinaryOptionPairs.Add(newPair);
                }
            }
        }

        public CombinatorialExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize) : this(globalConfigList, vm, batchSize, 0)
        {
        }


        public CombinatorialExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize, int sleepCycles) : this(globalConfigList, vm)
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
            if (this.unusedBinaryOptions.Count > 0 || this.unusedBinaryOptionPairs.Count > 0)
            {
                for (int i = 0; i < this.batchSize; i++)
                {
                    if (this.undiscoveredConfigs.Count > 0)
                    {
                        Configuration newConf = DiscoverNewConfig();
                        if (newConf == null)
                        {
                            /* no new combinations available */
                            // each single feature and each 2-interaction has been seen
                            break;
                        }

                        step.Add(newConf);
                    }
                }
            }
            else
            {
                RandomExplorer rndExpl = new RandomExplorer(this.undiscoveredConfigs, this.batchSize, 0);
                step = rndExpl.GetKnowledge();
            }
            ExpandCurrentKnowledge(step);
        }

        private Configuration DiscoverNewConfig()
        {
            Configuration newConfig = null;

            while (newConfig == null && unusedBinaryOptions.Count > 0)
            {
                BinaryOption option = this.unusedBinaryOptions[0];
                List<BinaryOption> optionList = new List<BinaryOption> { option };
                List<BinaryOption> newList = variantGenerator.minimizeConfig(optionList, this.vm, true, null);
                newConfig = this.GetConfigWithBinaryOptions(newList);
                this.unusedBinaryOptions.Remove(option);
            }

            if (newConfig == null && unusedBinaryOptions.Count == 0)
            {
                /* try to find undiscovered 2-interactions between features*/
                while (newConfig == null && unusedBinaryOptionPairs.Count > 0)
                {
                    // draw random pair to avoid fetching one option repeatedly
                    int id = rnd.Next(this.unusedBinaryOptionPairs.Count);
                    Tuple<BinaryOption, BinaryOption> tup = this.unusedBinaryOptionPairs[id];
                    List<BinaryOption> optionList = new List<BinaryOption> { tup.Item1, tup.Item2 };
                    List<BinaryOption> newList = variantGenerator.minimizeConfig(optionList, this.vm, true, null);
                    newConfig = this.GetConfigWithBinaryOptions(newList);
                    this.unusedBinaryOptionPairs.Remove(tup);
                }
            }
            return newConfig;
        }

        private Configuration GetConfigWithBinaryOptions(List<BinaryOption> newList)
        {
            Configuration configTemplate = new Configuration(newList);
            Configuration configMatch = null;
            foreach (Configuration config in this.undiscoveredConfigs)
            {
                if (config.Equals(configTemplate))
                {
                    configMatch = config;
                    break;
                }
            }
            this.undiscoveredConfigs.Remove(configMatch);
            return configMatch;
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
