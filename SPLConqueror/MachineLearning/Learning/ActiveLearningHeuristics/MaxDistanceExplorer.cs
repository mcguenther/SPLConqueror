using MachineLearning.Solver;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Learning.ActiveLearningHeuristics
{
    class MaxDistanceExplorer : ILearningSetExplorer
    {
        private List<Configuration> globalConfigList;
        private List<Configuration> undiscoveredConfigs;
        private List<Configuration> currentKnowledge;
        private List<List<Configuration>> history = new List<List<Configuration>>();
        private int remainingSleepCycles = 0;
        private int STANDARD_BATCH_SIZE = 5;
        private int batchSize;
        static Random rnd = new Random();
        private int sleepCycles;
        private Dictionary<BinaryOption, int> binaryOptionalOptionUsages;
        Solver.VariantGenerator variantGenerator;
        private VariabilityModel vm;
        private List<BinaryOption> binaryOptions;
        private List<BinaryOption> binaryOptionalOptions;
        private List<BinaryOption> binaryMandatoryOptions;
        private CheckConfigSAT checkSAT;

        public MaxDistanceExplorer(List<Configuration> globalConfigList, VariabilityModel vm)
        {
            this.globalConfigList = globalConfigList;
            this.undiscoveredConfigs = globalConfigList;
            currentKnowledge = new List<Configuration>();
            batchSize = STANDARD_BATCH_SIZE;
            sleepCycles = 0;
            remainingSleepCycles = 0;

            this.binaryOptions = new List<BinaryOption>(vm.BinaryOptions);
            this.binaryOptionalOptions = new List<BinaryOption>();
            // Dictionary<BinaryOption, BinaryOption.BinaryValue> binaryOptions = globalConfigList[0].BinaryOptions;
            //List<BinaryOption> binKeys = new List<BinaryOption>(binaryOptions.Keys);
            //this.binaryOptions = binKeys;
            this.binaryOptionalOptionUsages = new Dictionary<BinaryOption, int>();


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
            this.variantGenerator = new VariantGenerator(); //  null;//new VariantGenerator(null);
            this.vm = vm;

            DiscoverFirstConfig();
        }

        private void DiscoverFirstConfig()
        {
            /* Adds Config that has least least amount of options */
            //Generating new configurations: one per option
            if (checkSAT.checkConfigurationSAT(this.binaryMandatoryOptions, vm, false))
            {
                Configuration newConfig = GetConfigWithBinaryOptions(this.binaryMandatoryOptions);
                if (newConfig != null)
                {
                    this.ExpandCurrentKnowledge(new List<Configuration>() { newConfig });
                }
            }
        }

        private void UpdateOptionUsages(Configuration newConfig)
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

        public MaxDistanceExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize) : this(globalConfigList, vm, batchSize, 0)
        {
        }


        public MaxDistanceExplorer(List<Configuration> globalConfigList, VariabilityModel vm, int batchSize, int sleepCycles) : this(globalConfigList, vm)
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
                    if (newConf == null)
                    {
                        break;
                    }
                    step.Add(newConf);
                }
                else
                {
                    break;
                }
            }
            ExpandCurrentKnowledge(step);
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
        private Configuration DiscoverNewConfig()
        {
            Configuration newConfig = null;
            while (newConfig == null && this.undiscoveredConfigs.Count > 0)
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
                List<BinaryOption> newList = null;
                while (optionsActive.Count > 0 && (newList == null || newList.Count == 0))
                {
                    //List<BinaryOption> newList = variantGenerator.minimizeConfig(optionsActive, this.vm, true, optionsPassive);
                    newList = variantGenerator.minimizeConfig(optionsActive, this.vm, true, optionsPassive);
                    //List<List<BinaryOption>> newLists = variantGenerator.maximizeConfig(optionsActive, this.vm, true, optionsPassive);
                    if(newList == null || newList.Count == 0)
                    {
                        Dictionary<BinaryOption, int> sortdict = new Dictionary<BinaryOption, int>();
                        foreach(BinaryOption opt in optionsActive)
                        {
                            int usages = this.binaryOptionalOptionUsages[opt];
                            sortdict.Add(opt, usages);
                        }
                        IOrderedEnumerable<KeyValuePair<BinaryOption, int>> sortedDict = sortdict.OrderByDescending(entry => entry.Value);
                        KeyValuePair<BinaryOption, int> maxUsedOptionEntry = sortedDict.First();
                        BinaryOption maxUsedOption = maxUsedOptionEntry.Key;
                        optionsActive.Remove(maxUsedOption);
                    }
                }

                if (newList == null || newList.Count == 0)
                {
                    String message = "Could not find a valid Configuration for the following options: " +
                        Environment.NewLine + "Active [";
                    message += string.Join<BinaryOption>(" | ", optionsActive.ToArray());
                    message += "] " + Environment.NewLine + "Unwanted [";
                    message += string.Join<BinaryOption>(" | ", optionsPassive.ToArray());
                    message += "]";
                    throw new Exception(message);
                }
                newConfig = this.GetConfigWithBinaryOptions(newList);
                if (newConfig == null)
                {
                    /* we assume that we already fetched the config we tried to fetch now */
                    continue;
                }
                UpdateOptionUsages(newConfig);
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

