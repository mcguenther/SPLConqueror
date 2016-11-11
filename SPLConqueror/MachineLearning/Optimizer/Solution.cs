using MachineLearning.Learning.Regression;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MachineLearning.Optimizer
{
    public class Solution
    {
        public bool wasFeasable;

        private int numberOfDistinctLearnConfigs;

        private bool resultIsInSampleSet;

        private double improvement = 0;

        double averageError = double.MaxValue;

        private LearningRound learnedFunction;

        private double optimalNfp;

        private List<Tuple<string, int>> settingsAsString;

        private static string SOLUTION_FOUND = "solution status: optimal solution found";

        private static string READ_END = "objvar";

        private static string PSEUDO_OPTION = "n_";

        private Solution(double optimalNfp, List<Tuple<string, int>> settingsAsString)
        {
            this.optimalNfp = optimalNfp;
            this.settingsAsString = settingsAsString;
            wasFeasable = true;
        }

        private Solution(Boolean feasability)
        {
            this.wasFeasable = feasability;
        }

        public void setNumberOfSamples(List<Configuration> samples)
        {
            numberOfDistinctLearnConfigs =  samples.Distinct().Count();
        }

        public void testIfConfigIsInSampleSet(Configuration conf, List<Configuration> sampleSet)
        {
            resultIsInSampleSet = sampleSet.Contains(conf);
        }

        
        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if(wasFeasable)
            {
                sb.Append("optimal solution found; ");
                sb.Append("Nfp value: " + optimalNfp + " ; ");
                sb.Append("improvement: " + improvement + " ; ");
                sb.Append("averageError: " + averageError + " ; ");
                sb.Append("result config was in sample set: " + resultIsInSampleSet + " ; ");
                sb.Append("number of distinct configs: " + numberOfDistinctLearnConfigs + " ; ");
                sb.Append("used settings: ");
                foreach(Tuple<string, int> setting in settingsAsString)
                {
                    sb.Append(setting.Item1 + ": ");
                    sb.Append(setting.Item2 + " ");
                }
                sb.Append(" ; ");
            } else
            {
                sb.Append("no solution found; ");
            }
            sb.Append("function used for optimization: ");
            sb.Append((learnedFunction.ToString().Split(new char[] { ';' }))[1]);
            sb.Append(" ; ");
            return sb.ToString();
        }

        public double getImprovement()
        {
            return this.improvement;
        }

        public void saveFunction(LearningRound lr)
        {
            this.learnedFunction = lr;
        }

        public LearningRound getFunction()
        {
            return this.learnedFunction;
        }

        public void calculateImprovement(Solution previousSolution)
        {
            if(wasFeasable)
            {
                if(previousSolution == null)
                {
                    this.improvement = double.MaxValue;
                } else
                {
                    this.improvement = Math.Abs(this.optimalNfp - previousSolution.optimalNfp) / previousSolution.optimalNfp;
                }
            } else
            {
                throw new InvalidOperationException("cant calculate improvement of infeasable solutions");
            }
        }

        public double getOptimalNfp()
        {
            return this.optimalNfp;
        }

        public static Solution extractSolution(string file)
        {
            bool fileWritten = false;
            while (!fileWritten)
            {
                try
                {
                    StreamReader test = new StreamReader(file);
                    fileWritten = true;
                }
                catch (FileNotFoundException a)
                {
                    Thread.Sleep(100);
                }
                catch(IOException b)
                {
                    Thread.Sleep(100);
                    GlobalState.logInfo.log(DateTime.Now.ToShortTimeString() + ": Its assumed the optimizer claims ownership to the solution file.");
                }
            }
            StreamReader solutionReader = new StreamReader(file);
            string line = solutionReader.ReadLine();
            while(line == null || line.Length < 1)
            {
                line = solutionReader.ReadLine();
            }
            if (line.Equals(SOLUTION_FOUND))
            {
                string[] lineData = solutionReader.ReadLine().Split(new char[] { ':' });
                double nfpVal = double.Parse(lineData[1].Trim(), CultureInfo.GetCultureInfo("en-US"));
                List<Tuple<string, int>> settings = new List<Tuple<string, int>>();
                lineData = solutionReader.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                while (!(lineData[0].Trim()).Equals(READ_END))
                {
                    if (!lineData[0].StartsWith(PSEUDO_OPTION))
                    {
                        double value = double.Parse(lineData[1].Trim(), CultureInfo.GetCultureInfo("en-US"));
                        settings.Add(Tuple.Create(lineData[0].Trim(), Convert.ToInt32(value)));
                    }
                    lineData = solutionReader.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }

                return new Solution(nfpVal, settings);
            }
            else
            {
                return new Solution(false);
            }
        }

        public void computeError(List<Configuration> configurations)
        {
            double[] errorPerConfig = new double[configurations.Count];
            for(int i = 0; i < errorPerConfig.Length; ++i)
            {
                Configuration currConfig = configurations[i];
                double measuredNfp = currConfig.GetNFPValue();
                double predictedValue = 0.0;
                foreach(Feature feature in this.learnedFunction.FeatureSet)
                {
                    predictedValue += feature.eval(currConfig) * feature.Constant;
                }
                errorPerConfig[i] = Math.Abs(predictedValue - measuredNfp) / measuredNfp;
            }
            this.averageError = errorPerConfig.Average();
        }
        
        public Configuration toConfiguration()
        {
            Dictionary<BinaryOption, BinaryOption.BinaryValue> binOpts = new Dictionary<BinaryOption, BinaryOption.BinaryValue>();
            Dictionary<NumericOption, double> numOpts = new Dictionary<NumericOption, double>();
            Dictionary<NFProperty, double> measurements = new Dictionary<NFProperty, double>();
            measurements.Add(GlobalState.currentNFP, optimalNfp);

            foreach(BinaryOption binaryOption in GlobalState.varModel.BinaryOptions)
            {
                Boolean found = false;
                foreach(Tuple<string, int> settingInSolution in settingsAsString)
                {
                    if(binaryOption.Name.Equals(settingInSolution.Item1))
                    {
                        found = true;
                    }
                }

                if(found)
                {
                    binOpts.Add(binaryOption, BinaryOption.BinaryValue.Selected);
                } else
                {
                    binOpts.Add(binaryOption, BinaryOption.BinaryValue.Deselected);
                }
            }

            foreach(NumericOption numericOption in GlobalState.varModel.NumericOptions)
            {
                double value = 0;
                foreach (Tuple<string, int> settingInSolution in settingsAsString)
                {
                    if (numericOption.Name.Equals(settingInSolution.Item1))
                    {
                        value = Convert.ToDouble(settingInSolution.Item2);
                    }
                }

                numOpts.Add(numericOption, value);
            }

            return new Configuration(binOpts, numOpts, measurements);
        }
    }
}
