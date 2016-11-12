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
    public class OptimizerCoefficients : Optimizer
    {

        public OptimizerCoefficients(string minImprovement, string optimumRange, List<Configuration> sampleSetLearn, List<Configuration> sampleSetValidation, ML_Settings mlSettings) : base(minImprovement, optimumRange, sampleSetLearn, sampleSetValidation, mlSettings)
        {
        }

        override public List<Solution> learnWithOptimization(string solverPath, string solPath)
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
                }
                else if (!optimalSolution.wasFeasable)
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


                if ((optimalSolution.getOptimalNfp() < lowestNfp) && optimalSolution.isOptimalConfigurationInSampleSet)
                {
                    return optimizationHistory;
                }
                else if (optimalSolution.getOptimalNfp() < (optimumRange * lowestNfp))
                {
                    return optimizationHistory;
                }

                sampleSetLearn.Add(optimalSolution.toConfiguration());
                if (sampleSetLearn != sampleSetValidation)
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
    }
}
