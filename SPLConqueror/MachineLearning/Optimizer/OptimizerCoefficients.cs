using MachineLearning.Learning;
using MachineLearning.Learning.Regression;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MachineLearning.Optimizer
{
    public class OptimizerCoefficients
    {
        private List<Configuration> sampleSetLearn;

        private List<Configuration> sampleSetValidation;

        private ML_Settings mlSettings = new ML_Settings();

        private double lowestNfp;

        private List<Solution> optimizationHistory;

        private InfluenceModel infModel;

        private double minImprovement;

        public OptimizerCoefficients(double minImprovement, List<Configuration> sampleSetLearn, List<Configuration> sampleSetValidation, ML_Settings mlSettings)
        {
            this.minImprovement = minImprovement;
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
            FeatureSubsetSelection learning = new FeatureSubsetSelection(infModel, mlSettings);
            learning.setLearningSet(sampleSetLearn);
            learning.setValidationSet(sampleSetValidation);
            learning.learn();
            LearningRound bestFunc = findBestRound(learning.LearningHistory);

            while (minImprovement < roundImprovement)
            {
                SCIP_Wrapper optimizer = new SCIP_Wrapper();
                string[] columnsFunction = bestFunc.ToString().Split(new char[] { ';' });
                string ModelAsOSiL = optimizer.generateOsil_Syntax(columnsFunction[1], null);
                string osilFileName = "OSIL_MODEL_" + bestFunc.ToString().GetHashCode() + ".osil";
                string solFileName = "SOL_MODEL_" + bestFunc.ToString().GetHashCode() + ".sol";
                StreamWriter osilWriter = new StreamWriter(solPath + osilFileName);
                osilWriter.Write(ModelAsOSiL);
                osilWriter.Flush();
                osilWriter.Close();

                optimizer.useSolver(solverPath, solPath + osilFileName, solPath + solFileName);
                optimizer = null;

                Solution optimalSolution = Solution.extractSolution(solPath + solFileName);
                optimalSolution.saveFunction(bestFunc);
                optimalSolution.setNumberOfSamples(sampleSetLearn);
                if (!optimalSolution.wasFeasable && optimizationHistory.Count() == 0)
                {
                    return optimizationHistory;
                } else if (!optimalSolution.wasFeasable)
                {
                    optimizationHistory.Add(optimalSolution);
                    return optimizationHistory;
                }
                optimalSolution.testIfConfigIsInSampleSet(optimalSolution.toConfiguration(), sampleSetLearn);

                if (optimizationHistory.Count >= 1)
                {
                    Solution previousSolution = optimizationHistory.Last();
                    optimalSolution.calculateImprovement(previousSolution);
                    roundImprovement = optimalSolution.getImprovement();
                }

                optimizationHistory.Add(optimalSolution);

                if (optimalSolution.getOptimalNfp() < lowestNfp)
                {
                    return optimizationHistory;
                }

                sampleSetLearn.Add(optimalSolution.toConfiguration());
                if(sampleSetLearn != sampleSetValidation)
                {
                    sampleSetValidation.Add(optimalSolution.toConfiguration());
                }
                learning = new FeatureSubsetSelection(infModel, mlSettings);
                learning.setLearningSet(sampleSetLearn);
                learning.setValidationSet(sampleSetValidation);
                List<Feature> newModel = learning.refitCoefficients(bestFunc.FeatureSet);

                bestFunc = new LearningRound(newModel, 0, 0, 0);
            }

            return this.optimizationHistory;
        }

        private LearningRound findBestRound(List<LearningRound> lrs)
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
