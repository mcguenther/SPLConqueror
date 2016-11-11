using System.Collections.Generic;
using SPLConqueror_Core;
using MachineLearning.Learning.Regression;
using MachineLearning.Learning;
using System.IO;
using System.Linq;
using System.Globalization;

namespace MachineLearning.Optimizer
{
    public class OptimizerSampleSet
    {
        private List<Configuration> sampleSetLearn;

        private List<Configuration> sampleSetValidation;

        private  ML_Settings mlSettings = new ML_Settings();

        private double lowestNfp;

        private Configuration lowestConfiguration;

        private List<Solution> optimizationHistory;

        private InfluenceModel infModel;

        private double minImprovement;

        private double optimumRange;

        public OptimizerSampleSet(string minImprovement, string optimumRange, List<Configuration> sampleSetLearn, List<Configuration> sampleSetValidation, ML_Settings mlSettings)
        {
            this.minImprovement = double.Parse(minImprovement, CultureInfo.GetCultureInfo("en-US"));
            this.optimumRange = double.Parse(optimumRange, CultureInfo.GetCultureInfo("en-US"));
            this.sampleSetLearn = sampleSetLearn;
            this.sampleSetValidation = sampleSetValidation;
            this.mlSettings = mlSettings;
            optimizationHistory = new List<Solution>();
            infModel = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
            lowestNfp = double.MaxValue;
            foreach(Configuration config in sampleSetLearn)
            {
                if(config.GetNFPValue() < lowestNfp)
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

        public List<Solution> learnWithOptimization(string solverPath, string solPath)
        {
            double roundImprovement = double.MaxValue;
            while(minImprovement < roundImprovement)
            {
                List<LearningRound> iterationHistory = learningIteration();
                LearningRound bestFunction = findBestRound(iterationHistory);
                SCIP_Wrapper optimizer = new SCIP_Wrapper();
                string[] columnsFunction = bestFunction.ToString().Split(new char[] { ';' });
                string ModelAsOSiL = optimizer.generateOsil_Syntax(columnsFunction[1], null);
                string osilFileName = "OSIL_MODEL_" + bestFunction.ToString().GetHashCode() + ".osil";
                string solFileName = "SOL_MODEL_" + bestFunction.ToString().GetHashCode() + ".sol";
                StreamWriter osilWriter = new StreamWriter(solPath + osilFileName);
                osilWriter.Write(ModelAsOSiL);
                osilWriter.Flush();
                osilWriter.Close();

                optimizer.useSolver(solverPath, solPath + osilFileName, solPath + solFileName);
                optimizer = null;

                Solution optimalSolution = Solution.extractSolution(solPath + solFileName);
                optimalSolution.saveFunction(bestFunction);
                optimalSolution.setNumberOfSamples(sampleSetLearn);
                if (!optimalSolution.wasFeasable && optimizationHistory.Count == 0)
                {
                    return optimizationHistory;
                } else if(!optimalSolution.wasFeasable)
                {
                    optimizationHistory.Add(optimalSolution);
                    return optimizationHistory;
                }

                optimalSolution.testIfConfigIsInSampleSet(optimalSolution.toConfiguration(), sampleSetLearn);
                optimalSolution.computeError(sampleSetLearn);
                optimalSolution.testOptimalConfiguration(lowestConfiguration);

                if (optimizationHistory.Count >= 1)
                {
                    Solution previousSolution = optimizationHistory.Last();
                    optimalSolution.calculateImprovement(previousSolution);
                    roundImprovement = optimalSolution.getImprovement();
                }

                optimizationHistory.Add(optimalSolution);

                expandSampleSet(optimalSolution.toConfiguration());

                if ((optimalSolution.getOptimalNfp() < lowestNfp) && optimalSolution.isOptimalConfigurationInSampleSet)
                {
                    return optimizationHistory;
                }
                else if (optimalSolution.getOptimalNfp() < (optimumRange * lowestNfp))
                {
                    return optimizationHistory;
                }
            }
            return optimizationHistory;
        }

        private List<LearningRound> learningIteration()
        {
            FeatureSubsetSelection learning = new FeatureSubsetSelection(infModel, mlSettings);
            learning.setLearningSet(sampleSetLearn);
            learning.setValidationSet(sampleSetValidation);
            learning.learn();
            return learning.LearningHistory;
        }

        private LearningRound findBestRound(List<LearningRound> lrs)
        {
            double error = double.MaxValue;
            LearningRound bestRound = null;
            foreach(LearningRound lr in lrs)
            {
                if(lr.learningError < error)
                {
                    bestRound = lr;
                    error = lr.learningError;
                }
            }
            return bestRound;
        }

        private void expandSampleSet(Configuration toAdd)
        {
            sampleSetLearn.Add(toAdd);
            if (sampleSetLearn != sampleSetValidation)
            {
                sampleSetValidation.Add(toAdd);
            }
        }


    }
}
