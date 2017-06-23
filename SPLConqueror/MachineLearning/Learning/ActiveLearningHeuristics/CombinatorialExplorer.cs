using MachineLearning.Solver;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    public class CombinatorialExplorer : StepExplorer
    {
        static Random rnd = new Random();
        public List<BinaryOption> unusedBinaryOptions;
        public List<Tuple<BinaryOption, BinaryOption>> unusedBinaryOptionPairs;

        public CombinatorialExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize, int sleepCycles) : base(globalConfigList, vm, batchSize, sleepCycles)
        {
        }



        protected override Configuration DiscoverNewConfig()
        {
            Configuration newConf = null;
            if (this.unusedBinaryOptions.Count > 0 || this.unusedBinaryOptionPairs.Count > 0)
            {
                newConf = DiscoverNewCombinatorialConfig();
            }

            if (newConf == null)
            {
                newConf = DiscoverNewRandomConfig();
            }
            return newConf;
        }

        protected Configuration DiscoverNewRandomConfig()
        {
            RandomExplorer rndExpl = new RandomExplorer(undiscoveredConfigs, this.vm, 1, 0);
            List<Configuration> step = rndExpl.GetKnowledge();
            if (step != null && step.Count > 0)
            {

                return step[0];
            }
            else
            {
                return null;
            }
        }

        protected Configuration DiscoverNewCombinatorialConfig()
        {
            Configuration newConfig = null;

            while (newConfig == null && unusedBinaryOptions.Count > 0)
            {
                BinaryOption option = this.unusedBinaryOptions[0];
                List<BinaryOption> optionList = new List<BinaryOption> { option };
                List<BinaryOption> newList = this.variantGenerator.minimizeConfig(optionList, this.vm, true, null);
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
                    List<BinaryOption> newList = this.variantGenerator.minimizeConfig(optionList, this.vm, true, null);
                    newConfig = this.GetConfigWithBinaryOptions(newList);
                    this.unusedBinaryOptionPairs.Remove(tup);
                }
            }
            return newConfig;
        }

        
        protected override void DiscoverFirstConfig()
        {
            // Dictionary<BinaryOption, BinaryOption.BinaryValue> binaryOptions = globalConfigList[0].BinaryOptions;
            //List<BinaryOption> binKeys = new List<BinaryOption>(binaryOptions.Keys);
            //this.binaryOptions = binKeys;
            this.binaryOptions = vm.BinaryOptions;
            this.unusedBinaryOptions = new List<BinaryOption>(vm.BinaryOptions);
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

    }
}
