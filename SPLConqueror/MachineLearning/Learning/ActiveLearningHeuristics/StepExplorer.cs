using MachineLearning.Solver;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    public abstract class StepExplorer : ILearningSetExplorer
    {
        protected List<Configuration> globalConfigList;
        protected List<Configuration> undiscoveredConfigs;
        protected List<Configuration> currentKnowledge;
        private List<List<Configuration>> history = new List<List<Configuration>>();
        protected int remainingSleepCycles = 0;
        public static int STANDARD_BATCH_SIZE = 5;
        public static int STANDARD_SLEEP_CYCLES = 0;
        protected int batchSize;
        static Random rnd = new Random();
        protected int sleepCycles;
        protected List<BinaryOption> binaryOptions;
        protected VariantGenerator variantGenerator = new VariantGenerator();
        protected VariabilityModel vm;
        protected List<BinaryOption> binaryMandatoryOptions;
        protected CheckConfigSAT checkSAT;
        protected List<BinaryOption> binaryOptionalOptions;
        protected Dictionary<BinaryOption, int> binaryOptionalOptionUsages;

        public StepExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize, int sleepCycles)
        {
            this.vm = vm;
            this.binaryOptions = new List<BinaryOption>(vm.BinaryOptions);
            this.binaryOptionalOptions = new List<BinaryOption>();
            this.binaryOptionalOptionUsages = new Dictionary<BinaryOption, int>();
            this.globalConfigList = new List<Configuration>(globalConfigList);
            this.undiscoveredConfigs = new List<Configuration>(globalConfigList);
            currentKnowledge = new List<Configuration>();
            this.batchSize = batchSize;
            this.sleepCycles = sleepCycles;
            this.binaryOptions = new List<BinaryOption>(vm.BinaryOptions);

            InitOptions(vm);
            List<Configuration> initConfigs = DiscoverFirstConfigs();
            if (initConfigs != null && initConfigs.Count > 0)
            {
                this.ExpandCurrentKnowledge(initConfigs);
            }
        }


        public StepExplorer(List<Configuration> globalConfigList, VariabilityModel vm)
            : this(globalConfigList, vm, STANDARD_BATCH_SIZE, STANDARD_SLEEP_CYCLES)
        { }

        public StepExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize)
            : this(globalConfigList, vm, batchSize, STANDARD_SLEEP_CYCLES)
        { }



        protected void InitOptions(VariabilityModel vm)
        {
            //first filter out mandatory options
            this.binaryMandatoryOptions = new List<BinaryOption>();
            foreach (BinaryOption binOpt in binaryOptions)
            {
                if ((binOpt.Parent == null || binOpt.Parent == vm.Root) && !binOpt.Optional && !binOpt.hasAlternatives())
                {
                    this.binaryMandatoryOptions.Add(binOpt);
                    //Todo: Recursive down search
                    /*List<BinaryOption> tmpList = (List<BinaryOption>)vm.getMandatoryChildsRecursive(binOpt);
                    if (tmpList != null && tmpList.Count > 0)
                        firstLevelMandatoryFeatures.AddRange(tmpList);*/
                }
                else
                {
                    this.binaryOptionalOptions.Add(binOpt);
                }

            }

            this.checkSAT = new Solver.CheckConfigSAT();

            // initialize number of usages for each option
            foreach (BinaryOption opt in this.binaryOptionalOptions)
            {
                this.binaryOptionalOptionUsages.Add(opt, 0);
            }
        }


        protected abstract List<Configuration> DiscoverFirstConfigs();

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

        protected virtual void Explore()
        {
            List<Configuration> step = new List<Configuration>();
            for (int i = 0; i < this.batchSize; i++)
            {
                if (this.undiscoveredConfigs.Count > 0)
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
            }
            ExpandCurrentKnowledge(step);
        }

        protected abstract Configuration DiscoverNewConfig();

        protected Configuration DiscoverRandomConfig()
        {
            int rndConfId = rnd.Next(this.undiscoveredConfigs.Count);
            Configuration newConf = this.undiscoveredConfigs[rndConfId];
            this.undiscoveredConfigs.Remove(newConf);
            return newConf;
        }


        protected Configuration GetConfigWithBinaryOptions(List<BinaryOption> newList)
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
            if (configMatch != null)
            {
                this.undiscoveredConfigs.Remove(configMatch);
            }
            return configMatch;
        }

        protected void ExpandCurrentKnowledge(List<Configuration> newConfigs)
        {
            History.Add(newConfigs);
            currentKnowledge = new List<Configuration>(currentKnowledge);
            currentKnowledge.AddRange(newConfigs);
        }

        public List<List<Configuration>> History { get => history; }

        protected Configuration FindClosestUndiscoveredConfig(List<Configuration> searchSpace, List<BinaryOption> wanted, List<BinaryOption> unWanted)
        {
            Dictionary<Configuration, int> distances = new Dictionary<Configuration, int>();
            foreach (Configuration conf in searchSpace)
            {
                int dist = ManhattenDistance(conf, wanted, unWanted);
                distances.Add(conf, dist);
            }

            IOrderedEnumerable<KeyValuePair<Configuration, int>> sortedDict = distances.OrderBy(entry => entry.Value);
            KeyValuePair<Configuration, int> minDistConfigEntry = sortedDict.First();
            Configuration minDistConfig = minDistConfigEntry.Key;

            return minDistConfig;
        }

        protected int ManhattenDistance(Configuration centralConf, List<BinaryOption> wanted, List<BinaryOption> unWanted)
        {
            Configuration compareConf = new Configuration(wanted);
            return ManhattenDistance(centralConf, compareConf);

        }

        static protected int ManhattenDistance(Configuration centralConf, Configuration remoteConf)
        {
            Dictionary<BinaryOption, BinaryOption.BinaryValue> opts = remoteConf.BinaryOptions;
            int confDistance = 0;
            foreach (KeyValuePair<BinaryOption, BinaryOption.BinaryValue> centralOpt in centralConf.BinaryOptions)
            {

                if (!opts.ContainsKey(centralOpt.Key))
                {
                    if (centralOpt.Value != BinaryOption.BinaryValue.Deselected)
                    {
                        confDistance += 1;
                    }
                }
                else
                {
                    BinaryOption.BinaryValue compOpt = opts[centralOpt.Key];
                    if (centralOpt.Value != compOpt)
                    {
                        confDistance += 1;
                    }
                }
            }
            return confDistance;
        }


        static protected Configuration FindClosestConfig(List<Configuration> searchSpace, Configuration centerConf)
        {
            Dictionary<Configuration, int> distances = new Dictionary<Configuration, int>();
            foreach (Configuration conf in searchSpace)
            {
                int dist = ManhattenDistance(conf, centerConf);
                distances.Add(conf, dist);
            }

            IOrderedEnumerable<KeyValuePair<Configuration, int>> sortedDict = distances.OrderBy(entry => entry.Value);
            KeyValuePair<Configuration, int> minDistConfigEntry = sortedDict.First();
            Configuration minDistConfig = minDistConfigEntry.Key;

            return minDistConfig;
        }
    }
}
