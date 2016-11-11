using MachineLearning.Learning;
using MachineLearning.Learning.Regression;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MachineLearning.Optimizer
{
    public class OptimizerFeatures
    {
        private List<Configuration> sampleSetLearn;

        private List<Configuration> sampleSetValidation;

        private ML_Settings mlSettings = new ML_Settings();

        private double lowestNfp;

        private Configuration lowestConfiguration;

        private List<Solution> optimizationHistory;

        private InfluenceModel infModel;

        private double minImprovement;

        private double optimumRange;

        public OptimizerFeatures(string minImprovement, string optimumRange,  List<Configuration> sampleSetLearn, List<Configuration> sampleSetValidation, ML_Settings mlSettings)
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

        public List<Solution> learnWithOptimization(string solverPath, string solPath)
        {
            FeatureSubsetSelection learning = new FeatureSubsetSelection(infModel, mlSettings);
            learning.setLearningSet(sampleSetLearn);
            learning.setValidationSet(sampleSetValidation);
            learning.learn();
            List<LearningRound> learnHistory = learning.LearningHistory;
            LearningRound bestFunction = findBestRound(learnHistory);

            SCIP_Wrapper optimizer = new SCIP_Wrapper();
            string[] columnsFunction = bestFunction.ToString().Split(new char[] { ';' });
            string ModelAsOSiL = optimizer.generateOsil_Syntax(columnsFunction[1], null);
            string osilFileName = "OSIL_MODEL_" + bestFunction.ToString().GetHashCode() + ".osil";
            string solFileName = "SOL_MODEL_" + bestFunction.ToString().GetHashCode() + ".sol";
            StreamWriter osilWriter = new StreamWriter(solPath + osilFileName);
            osilWriter.Write(ModelAsOSiL);
            osilWriter.Flush();
            osilWriter.Close();

            Solution optimalSolution = Solution.extractSolution(solPath + solFileName);
            optimalSolution.saveFunction(bestFunction);
            optimalSolution.setNumberOfSamples(sampleSetLearn);
            if (!optimalSolution.wasFeasable)
            {
                return optimizationHistory;
            }

            optimalSolution.testIfConfigIsInSampleSet(optimalSolution.toConfiguration(), sampleSetLearn);
            optimalSolution.computeError(sampleSetLearn);

            optimizationHistory.Add(optimalSolution);
            sampleSetLearn.Add(optimalSolution.toConfiguration());
            if(sampleSetValidation != sampleSetLearn)
            {
                sampleSetValidation.Add(optimalSolution.toConfiguration());
            }
            if (! (optimalSolution.getOptimalNfp() < lowestNfp))
            {
                foreach (Feature toRemove in bestFunction.FeatureSet)
                {
                    List<Feature> funcWithRemovedFeature = new List<Feature>();
                    foreach(Feature feat in bestFunction.FeatureSet)
                    {
                        if(!feat.Equals(toRemove))
                        {
                            funcWithRemovedFeature.Add(feat);
                        }
                    }

                    funcWithRemovedFeature = learning.refitCoefficients(funcWithRemovedFeature);
                    LearningRound learningWithRemoved = new LearningRound(funcWithRemovedFeature, 0, 0, 0);

                    optimizer = new SCIP_Wrapper();
                    columnsFunction = learningWithRemoved.ToString().Split(new char[] { ';' });
                    ModelAsOSiL = optimizer.generateOsil_Syntax(columnsFunction[1], null);
                    osilFileName = "OSIL_MODEL_" + learningWithRemoved.ToString().GetHashCode() + ".osil";
                    solFileName = "SOL_MODEL_" + learningWithRemoved.ToString().GetHashCode() + ".sol";
                    osilWriter = new StreamWriter(solPath + osilFileName);
                    osilWriter.Write(ModelAsOSiL);
                    osilWriter.Flush();
                    osilWriter.Close();

                    optimizer.useSolver(solverPath, solPath + osilFileName, solPath + solFileName);
                    optimizer = null;

                    optimalSolution = Solution.extractSolution(solPath + solFileName);
                    optimalSolution.saveFunction(learningWithRemoved);
                    optimalSolution.setNumberOfSamples(sampleSetLearn);

                    if(optimalSolution.wasFeasable)
                    {
                        optimalSolution.testIfConfigIsInSampleSet(optimalSolution.toConfiguration(), sampleSetLearn);
                        optimalSolution.computeError(sampleSetLearn);
                        optimalSolution.calculateImprovement(optimizationHistory.Last());
                    }

                    optimizationHistory.Add(optimalSolution);
                }
            }
            return optimizationHistory;
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
