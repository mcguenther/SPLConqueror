using MachineLearning.Learning;
using MachineLearning.Learning.Regression;
using SPLConqueror_Core;
using System.Collections.Generic;
using System.Globalization;

namespace MachineLearning.Optimizer
{
    public class Optimizer
    {
        public List<Configuration> sampleSetLearn;

        public List<Configuration> sampleSetValidation;

        public ML_Settings mlSettings = new ML_Settings();

        public double lowestNfp;

        public Configuration lowestConfiguration;

        public List<Solution> optimizationHistory;

        public InfluenceModel infModel;

        public double minImprovement;

        public double optimumRange;

        public Optimizer(string minImprovement, string optimumRange, List<Configuration> sampleSetLearn, List<Configuration> sampleSetValidation, ML_Settings mlSettings)
        {
            this.minImprovement = double.Parse(minImprovement, CultureInfo.GetCultureInfo("en-US"));
            this.optimumRange = double.Parse(optimumRange, CultureInfo.GetCultureInfo("en-US"));
            this.sampleSetLearn = sampleSetLearn;
            this.sampleSetValidation = sampleSetValidation;
            this.mlSettings = mlSettings;
            optimizationHistory = new List<Solution>();
            infModel = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
            lowestNfp = double.MaxValue;
            foreach (Configuration config in sampleSetLearn)
            {
                if (config.GetNFPValue() < lowestNfp)
                {
                    lowestNfp = config.GetNFPValue();
                    lowestConfiguration = config;
                }
            }
        }

        public double getLowestNFP()
        {
            return this.lowestNfp;
        }

        public int numberOfRounds()
        {
            return this.optimizationHistory.Count;
        }

        public virtual List<Solution> learnWithOptimization(string solverPath, string solPath)
        {
            return null;
        }

        public LearningRound findBestRound(List<LearningRound> lrs)
        {
            double error = double.MaxValue;
            LearningRound bestRound = null;
            foreach (LearningRound lr in lrs)
            {
                if (lr.learningError < error)
                {
                    bestRound = lr;
                    error = lr.learningError;
                }
            }
            return bestRound;
        }
    }
}
