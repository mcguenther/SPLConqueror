using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;
using System.Diagnostics;
using System.Xml;
using System.Threading;

namespace MachineLearning.Optimizer
{
    public class SCIP_Wrapper
    {
        Dictionary<String, Tuple<ConfigurationOption, int>> nameToOptionAndID = new Dictionary<string, Tuple<ConfigurationOption, int>>();

        Dictionary<String, int> numericOptionToPseudoOptionID = new Dictionary<string, int>();

        public SCIP_Wrapper()
        {


        }

        public void useSolver(string solverPath, string fileToSolve, string solutionTarget)
        {
            ProcessStartInfo solverSetup = new ProcessStartInfo(solverPath);
            solverSetup.UseShellExecute = false;
            solverSetup.RedirectStandardInput = true;
            solverSetup.RedirectStandardOutput = true;
            Process solver = Process.Start(solverSetup);
            solver.StandardInput.WriteLine("read " + fileToSolve);
            solver.StandardInput.WriteLine("optimize");
            string output = "";
            while(!output.Equals("SCIP"))
            {
                output = solver.StandardOutput.ReadLine().Split(null)[0];
            }
            solver.StandardInput.WriteLine("write solution");
            solver.StandardInput.WriteLine(solutionTarget);
            solver.StandardInput.WriteLine("quit");
            solver.Close();
            //to assure the file is written
            Thread.Sleep(1000);
        }


        public String configurationOptionsToOsil()
        {
            StringBuilder sb = new StringBuilder();

            // each numeric option needs a additional variable that wont be used in the modelfunction
            int numberOfPseudoOpts = GlobalState.varModel.NumericOptions.Count;
            int totalRealOptions = GlobalState.varModel.getOptions().Count;
            int totalVariables = numberOfPseudoOpts + totalRealOptions;
            List<NumericOption> numericOptions = new List<NumericOption>();

            sb.Append("<variables numberOfVariables=\""+ totalVariables +"\">\n");
            for (int i = 0; i < GlobalState.varModel.getOptions().Count; i++)
            {
                var configOption = GlobalState.varModel.getOptions()[i];
                nameToOptionAndID.Add(configOption.Name, new Tuple<ConfigurationOption, int>(configOption, i));

                if (configOption.GetType().Equals(typeof(BinaryOption)))
                {
                    if (((BinaryOption)configOption).collectAlternativeOptions().Count > 0)
                    {
                        sb.Append("<var lb=\"0\" ub=\"1\" name=\"" + configOption.Name + "\" type=\"I\"/>\n");
                    }
                    else if (((BinaryOption)configOption).Optional == false)
                    {
                        sb.Append("<var lb=\"1\" ub=\"1\" name=\"" + configOption.Name + "\" type=\"I\"/>\n");
                    }
                    else
                    {
                        sb.Append("<var lb=\"0\" ub=\"1\" name=\"" + configOption.Name + "\" type=\"I\"/>\n");
                    }



                }
                else
                {
                    NumericOption asNumericOption = (NumericOption)configOption;
                    numericOptions.Add(asNumericOption);
                    sb.Append("<var lb=\"" + asNumericOption.Min_value + "\" ub=\"" + asNumericOption.Max_value + "\" name=\"" + configOption.Name + "\" type=\"I\"/>\n");
                }

            }

            // add pseudo variables
            int y = 0;
            foreach(NumericOption numOpt in numericOptions)
            {
                sb.Append("<var lb=\"0\" name=\"n_" + numOpt.Name + "\" type=\"I\"/>\n");
                numericOptionToPseudoOptionID.Add(numOpt.Name, totalRealOptions + y);
                y++;
            }
            
            sb.Append("</variables>\n");

            
           return sb.ToString();
        }

        int numberOfConstraints = 0;

        public Tuple<String,String> variabilityModelConstraintsToOsil(String additionalConstraints)
        {
            // the upper and lower bound of the constraint has to be specified
            StringBuilder boundPart = new StringBuilder();
            // the formular of the constraints have to be specified
            StringBuilder constraintPart = new StringBuilder();

            

            var nameToOptionAsArray = nameToOptionAndID.ToArray();

            for (int i = 0; i < nameToOptionAsArray.Length; i++)
            {
                KeyValuePair<String, Tuple<ConfigurationOption, int>> optionOne = nameToOptionAsArray[i];
                if (optionOne.Value.Item1.GetType().Equals(typeof(BinaryOption)))
                {
                    BinaryOption binOption = (BinaryOption) optionOne.Value.Item1;
                    List<ConfigurationOption> alternatives = binOption.collectAlternativeOptions();
                    List<List<ConfigurationOption>> nonAlternatives = binOption.getNonAlternativeExlcudedOptions();

                    // alternative options
                    StringBuilder alternativeString = new StringBuilder();
                    for (int alt = 0; alt < alternatives.Count; alt++)
                    {
                        alternativeString.Append(alternatives[alt].Name + " + ");
                    }
                    alternativeString.Append(binOption.Name);

                    StringBuilder alternativeString_OsilSyntax = new StringBuilder();
                    alternativeString_OsilSyntax.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                    numberOfConstraints++;
                    alternativeString_OsilSyntax.Append((new InfluenceFunction(alternativeString.ToString(), GlobalState.varModel)).toOsil_Syntax(this.nameToOptionAndID));
                    alternativeString_OsilSyntax.Append("</nl>\n");

                    constraintPart.Append(alternativeString_OsilSyntax.ToString());
                    boundPart.Append("<con lb=\"1.0\" ub=\"1.0\"/>\n");


                    // non-alternative options which are exluded
                    foreach(List<ConfigurationOption> excludeLists in nonAlternatives){
                        
                        
                        StringBuilder oneGroup = new StringBuilder();
                        for (int alt = 0; alt < excludeLists.Count; alt++)
                        {
                            oneGroup.Append(excludeLists[alt] + " + ");
                        }
                        oneGroup.Append(binOption.Name);

                        StringBuilder nonalternativeString_OsilSyntax = new StringBuilder();
                        nonalternativeString_OsilSyntax.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                        numberOfConstraints++;
                        nonalternativeString_OsilSyntax.Append((new InfluenceFunction(oneGroup.ToString(), GlobalState.varModel)).toOsil_Syntax(this.nameToOptionAndID));
                        nonalternativeString_OsilSyntax.Append("</nl>\n");

                        constraintPart.Append(nonalternativeString_OsilSyntax.ToString());
                        boundPart.Append("<con lb=\"0.0\" ub=\"1.0\"/>\n");

                    }

                    // implied options function
                    int numberOfImpliedOptions = 0;
                    foreach (List<ConfigurationOption> implications in binOption.Implied_Options)
                    {
                        foreach (ConfigurationOption implication in implications)
                        {
                            numberOfImpliedOptions++;
                        }
                    }
                    if (numberOfImpliedOptions >= 1)
                    {
                        Tuple<string, string> boundAndConstrainPart = BuildImplicationConstraintOSnlBinOpt(binOption, i);
                        boundPart.Append(boundAndConstrainPart.Item1);
                        constraintPart.Append(boundAndConstrainPart.Item2);
                    }


                } else
                {
                    NumericOption numOpt = (NumericOption)optionOne.Value.Item1;

                    // step function
                    Tuple<string, string> boundAndStepConstraint = BuildStepFunctionOSnL(numOpt, i);
                    boundPart.Append(boundAndStepConstraint.Item1);
                    constraintPart.Append(boundAndStepConstraint.Item2);

                    // excluded options
                    if (numOpt.Excluded_Options.Count >= 1 && numOpt.Excluded_Options.First().Count >= 1)
                    {
                        List<Tuple<string, string>> boundAndConstraints = BuildExclusiveOptionsConstraintNumeric(numOpt, i);
                        foreach(Tuple<string, string> boundAndConstraint in boundAndConstraints)
                        {
                            boundPart.Append(boundAndConstraint.Item1);
                            constraintPart.Append(boundAndConstraint.Item2);
                        }
                    }

                    // implied options
                    int numberOfImpliedOptions = 0;
                    foreach(List<ConfigurationOption> implications in numOpt.Implied_Options)
                    {
                        foreach(ConfigurationOption implication in implications)
                        {
                            numberOfImpliedOptions++;
                        }
                    }
                    if (numberOfImpliedOptions >= 1)
                    {
                        Tuple<string, string> boundAndConstrainPart = BuildImplicationConstraintOSnlNumOpt(numOpt, i);
                        boundPart.Append(boundAndConstrainPart.Item1);
                        boundPart.Append(boundAndConstrainPart.Item2);
                    }

                }


            }

            // Non boolean constraints
            List<Tuple<string, string>> nonBooleanConstraints = BuildNonBooleanConstraints(GlobalState.varModel.NonBooleanConstraints);
            foreach(Tuple<string, string> nonBooleanConstraint in nonBooleanConstraints)
            {
                boundPart.Append(nonBooleanConstraint.Item1);
                constraintPart.Append(nonBooleanConstraint.Item2);
            }

            // boolean constraints
            List<Tuple<string, string>> booleanConstraints = BuildBooleanConstraints(GlobalState.varModel.BooleanConstraints);
            foreach(Tuple<string, string> booleanConstraint in booleanConstraints)
            {
                boundPart.Append(booleanConstraint.Item1);
                constraintPart.Append(booleanConstraint.Item2);
            }

            if(additionalConstraints != null)
            {
                Tuple<List<string>, List<string>> booleanAndNonBoolean = parseAdditionalConstraints(additionalConstraints);

                booleanConstraints = BuildBooleanConstraints(booleanAndNonBoolean.Item1);
                foreach (Tuple<string, string> booleanConstraint in booleanConstraints)
                {
                    boundPart.Append(booleanConstraint.Item1);
                    constraintPart.Append(booleanConstraint.Item2);
                }

                List<NonBooleanConstraint> nonBoolean = new List<NonBooleanConstraint>();
                foreach(string constraint in booleanAndNonBoolean.Item2)
                {
                    nonBoolean.Add(new NonBooleanConstraint(constraint, GlobalState.varModel));
                }
                nonBooleanConstraints = BuildNonBooleanConstraints(nonBoolean);
                foreach (Tuple<string, string> nonBooleanConstraint in nonBooleanConstraints)
                {
                    boundPart.Append(nonBooleanConstraint.Item1);
                    constraintPart.Append(nonBooleanConstraint.Item2);
                }
            }

            return new Tuple<String, String>(boundPart.ToString(), constraintPart.ToString());
        }

        private List<Tuple<string, string>> BuildBooleanConstraints(List<string> booleanConstraints)
        {
            string boundPart = "<con lb=\"1\"/>\n";

            List<Tuple<string, string>> toReturn = new List<Tuple<string, string>>();

            foreach(string booleanConstraint in booleanConstraints)
            {
                string[] partialBooleanConstraints = booleanConstraint.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(string partialBooleanConstraint in partialBooleanConstraints)
                {
                    StringBuilder sb = new StringBuilder();
                    string[] literals = partialBooleanConstraint.Split(null);
                    string asArithmeticExpr = ParseLogicalExprToArithmetic(literals);
                    sb.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                    numberOfConstraints++;
                    sb.Append((new InfluenceFunction(asArithmeticExpr)).toOsil_Syntax(this.nameToOptionAndID));
                    sb.Append("</nl>\n");
                    toReturn.Add(Tuple.Create(boundPart, sb.ToString()));
                }
            }
            return toReturn;
        }

        private string ParseLogicalExprToArithmetic(string[] literals)
        {
            StringBuilder sb = new StringBuilder();
            foreach(string literal in literals)
            {
                string toFormat = literal.Trim();
                toFormat = toFormat.Replace('|', '+');
                if (toFormat.Contains("!"))
                {
                    toFormat = toFormat.Replace("!", " 1 + -1 * ");
                }
                
                sb.Append(toFormat);
                sb.Append(" ");
            }
            sb.Length--;
            return sb.ToString();
        }

        private List<Tuple<string, string>> BuildNonBooleanConstraints(List<NonBooleanConstraint> nonBoolConstraints)
        {
            List<Tuple<string, string>> nonBooleanConstrs = new List<Tuple<string, string>>();
            foreach(NonBooleanConstraint constraint in nonBoolConstraints)
            {
                string rightSide = constraint.GetRightSide().ToString();
                string leftSide = constraint.GetLeftSide().ToString();
                int asInt;

                // pre-test to Test wether right side only constains only a literal
                if(Int32.TryParse(rightSide, out asInt))
                { 
                    string boundPart = "";
                    StringBuilder constraintBuilder = new StringBuilder();
                    switch (constraint.GetComparator())
                    {
                        case ">":
                            asInt = asInt + 1;
                            boundPart = "<con lb=\"" + asInt + "\"/>\n";
                            break;
                        case ">=":
                            boundPart = "<con lb=\"" + asInt + "\"/>\n";
                            break;
                        case "=":
                            boundPart = "<con lb=\"" + asInt + "\" ub=\"" + asInt + "\"/>\n";
                            break;
                        case "<=":
                            boundPart = "<con ub=\"" + asInt + "\"/>\n";
                            break;
                        case "<":
                            asInt = asInt - 1;
                            boundPart = "<con ub=\"" + asInt + "\"/>\n";
                            break;
                    }
                    constraintBuilder.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                    numberOfConstraints++;
                    constraintBuilder.Append((constraint.GetLeftSide()).toOsil_Syntax(this.nameToOptionAndID));
                    constraintBuilder.Append("</nl>\n");
                    nonBooleanConstrs.Add(Tuple.Create(boundPart, constraintBuilder.ToString()));
                } else if(Int32.TryParse(leftSide, out asInt))
                {
                    string boundPart = "";
                    StringBuilder constraintBuilder = new StringBuilder();
                    switch (constraint.GetComparator())
                    {
                        case ">":
                            asInt = asInt - 1;
                            boundPart = "<con ub=\"" + asInt + "\"/>\n";
                            break;
                        case ">=":
                            boundPart = "<con ub=\"" + asInt + "\"/>\n";
                            break;
                        case "=":
                            boundPart = "<con lb=\"" + asInt + "\" ub=\"" + asInt + "\"/>\n";
                            break;
                        case "<=":
                            boundPart = "<con lb=\"" + asInt + "\"/>\n";
                            break;
                        case "<":
                            asInt = asInt + 1;
                            boundPart = "<con lb=\"" + asInt + "\"/>\n";
                            break;
                    }
                    constraintBuilder.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                    numberOfConstraints++;
                    constraintBuilder.Append((constraint.GetRightSide()).toOsil_Syntax(this.nameToOptionAndID));
                    constraintBuilder.Append("</nl>\n");
                    nonBooleanConstrs.Add(Tuple.Create(boundPart, constraintBuilder.ToString()));
                } else
                {
                    GlobalState.logInfo.log("For finding the optimal option selection nonBoolean constraints can only have a literal on one side");
                }
            }

            return nonBooleanConstrs;
        }

        private Tuple<string, string> BuildStepFunctionOSnL(NumericOption numOpt, int position)
        {
            string stepExpr = numOpt.StepFunction.ToString();
            string boundPart = "<con lb=\"0\" ub=\"0\"/>\n";
            StringBuilder stepFunctionOSnL = new StringBuilder();
            int positionOfPseudo = numericOptionToPseudoOptionID[numOpt.Name];
            if (stepExpr.Contains("+"))
            {
                // There exists a n>=0 so that numopt - n * stepsize = 0
                int stepSize = (int)Math.Abs((int)numOpt.getNextValue(numOpt.Min_value) - numOpt.Min_value);
                stepFunctionOSnL.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                numberOfConstraints++;
                stepFunctionOSnL.Append("<minus>\n");
                stepFunctionOSnL.Append("<variable coef=\"1.0\" idx=\"" + position + "\"/>\n");
                stepFunctionOSnL.Append("<times>\n");
                stepFunctionOSnL.Append("<number value=\"" + stepSize + "\"/>\n");
                stepFunctionOSnL.Append("<variable coef=\"1.0\" idx=\"" + positionOfPseudo + "\"/>\n");
                stepFunctionOSnL.Append("</times>\n");
                stepFunctionOSnL.Append("</minus>\n");
                stepFunctionOSnL.Append("</nl>\n");
            } else if(stepExpr.Contains("*"))
            {
                string[] expressions = stepExpr.Split(new char[] { '*' });
                int baseValue = Convert.ToInt32(numOpt.getNextValue(numOpt.Min_value) - numOpt.Min_value);

                stepFunctionOSnL.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                numberOfConstraints++;
                stepFunctionOSnL.Append("<minus>\n");
                stepFunctionOSnL.Append("<variable coef=\"1.0\" idx=\"" + position + "\"/>\n");
                stepFunctionOSnL.Append("<power>\n");
                stepFunctionOSnL.Append("<number value=\"" + baseValue + "\"/>\n");
                stepFunctionOSnL.Append("<variable coef=\"1.0\" idx=\"" + positionOfPseudo + "\"/>\n");
                stepFunctionOSnL.Append("</power>\n");
                stepFunctionOSnL.Append("</minus>\n");
                stepFunctionOSnL.Append("</nl>\n");
            }

            return Tuple.Create(boundPart, stepFunctionOSnL.ToString());
        }

        private List<Tuple<string,string>> BuildExclusiveOptionsConstraintNumeric(NumericOption numOpt, int currentVar)
        {
            // make list of pairings with numOpt * exclusiveOption for each exclusive option
            // then require each pairing to be 0 meaning there is the two options of each pairing
            // cant be selected at the same time
            List<Tuple<string, string>> boundariesAndConstraints = new List<Tuple<string, string>>();
            String boundPart = "<con lb=\"0\" ub=\"0\"/>\n";
            
            foreach(List<ConfigurationOption> alternatives in numOpt.Excluded_Options)
            {
                foreach(ConfigurationOption alternativeConf in alternatives)
                {
                    StringBuilder constraint = new StringBuilder();
                    constraint.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
                    numberOfConstraints++;
                    constraint.Append("<times>\n");
                    constraint.Append("<variable coef=\"1.0\" idx=\"" + currentVar + "\"/>\n");
                    constraint.Append("<variable coef=\"1.0\" idx=\"" + nameToOptionAndID[alternativeConf.Name].Item2 + "\"/>\n");
                    constraint.Append("</times>\n");
                    constraint.Append("</nl>\n");
                    boundariesAndConstraints.Add(Tuple.Create(boundPart, constraint.ToString()));
                }
            }

            return boundariesAndConstraints;
        }

        private Tuple<string, string> BuildImplicationConstraintOSnlNumOpt(NumericOption numOpt, int currentVar)
        {
            String boundPart = "<con lb=\"0\" />\n";

            // 0 = Product of implied options - valueOfCurrentOption
            // meaning if valueOfCurrentOption > 0 then all implied options > 0
            StringBuilder nonLinearExpr = new StringBuilder("<nl idx=\"" + numberOfConstraints + "\">\n");
            numberOfConstraints++;
            nonLinearExpr.Append("<minus>\n");
            nonLinearExpr.Append("<product>\n");
            foreach (List<ConfigurationOption> implications in numOpt.Implied_Options)
            {
                foreach (ConfigurationOption implication in implications)
                {
                    nonLinearExpr.Append("<variable coef=\"1.0\" idx=\"" + nameToOptionAndID[implication.Name].Item2 + "\"/>\n");
                }
            }
            nonLinearExpr.Append("<variable coef=\"1.0\" idx=\"" + currentVar + "\"/>\n");
            nonLinearExpr.Append("</product>\n");
            nonLinearExpr.Append("<variable coef=\"1.0\" idx=\"" + currentVar + "\"/>\n");
            nonLinearExpr.Append("</minus>\n");
            nonLinearExpr.Append("</nl>\n");

            return Tuple.Create(boundPart, nonLinearExpr.ToString());
        }

        private Tuple<string, string> BuildImplicationConstraintOSnlBinOpt(BinaryOption binOpt, int currentVar)
        {
            // similar to implications in numeric options
            // 0 = product(impliedOptions & rootOption) - rootOption
            // if rootOption = 1 then impliedOptions all have to be 1
            // else the product and rootOption will be 0 and the the constraint will work for all variations
            string boundPart = "<con lb=\"0\" ub=\"0\"/>\n";

            StringBuilder constraint = new StringBuilder();
            constraint.Append("<nl idx=\"" + numberOfConstraints + "\">\n");
            numberOfConstraints++;
            constraint.Append("<minus>\n");
            constraint.Append("<product>\n");
            constraint.Append("<variable coef=\"1.0\" idx=\"" + currentVar + "\"/>\n");
            foreach(List<ConfigurationOption> impliedOptions in binOpt.Implied_Options)
            {
                foreach(ConfigurationOption impliedOption in impliedOptions)
                {
                    constraint.Append("<variable coef=\"1.0\" idx=\"" + nameToOptionAndID[impliedOption.Name].Item2 + "\"/>\n");
                }
            }
            constraint.Append("</product>\n");
            constraint.Append("<variable coef=\"1.0\" idx=\"" + currentVar + "\"/>\n");
            constraint.Append("</minus>\n");
            constraint.Append("</nl>\n");

            return Tuple.Create(boundPart, constraint.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"> The model in Osil Syntax.</param>
        /// <returns></returns>
        public String generateOsil_Syntax(String model, String additionalConstraints)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<osil>\n");
            sb.Append("<instanceHeader>\n");
            sb.Append("<name>"+GlobalState.varModel.Name+"</name>\n");
            sb.Append("</instanceHeader>\n");
            sb.Append("<instanceData>\n");
            sb.Append(configurationOptionsToOsil());
            
            // objective functions
            sb.Append("<objectives numberOfObjectives=\"1\">\n");
            sb.Append("<obj maxOrMin=\"min\" name=\"minCost\" numberOfObjCoef=\"1\">\n");
            sb.Append("</obj>");
            sb.Append("</objectives>");


            // adding of constraints
            Tuple<String, String> constraints = variabilityModelConstraintsToOsil(additionalConstraints);

            sb.Append("<constraints numberOfConstraints=\""+numberOfConstraints+"\">\n");
            sb.Append(constraints.Item1);
            sb.Append("</constraints>\n");
            sb.Append("<nonlinearExpressions numberOfNonlinearExpressions=\""+(numberOfConstraints+1)+"\">\n");

            sb.Append("<nl idx=\"-1\">\n");
            sb.Append((new InfluenceFunction(model)).toOsil_Syntax(this.nameToOptionAndID));
            sb.Append("</nl>\n");

            sb.Append(constraints.Item2);
            

            sb.Append("</nonlinearExpressions>");

            sb.Append("</instanceData>\n");
            sb.Append("</osil>\n");

            return sb.ToString();
        }

        public static Tuple<List<string>, List<string>> parseAdditionalConstraints(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            List<string> booleanConstraints = new List<string>();
            List<string> nonBooleanConstraints = new List<string>();
            if (xmlDoc.HasChildNodes)
            {
                XmlNode root = xmlDoc.FirstChild;
                XmlNode constraints = root.FirstChild;
                while (constraints != null && root.Name.Equals("constraints"))
                {
                    if (constraints.HasChildNodes)
                    {
                        XmlNode constraint = constraints.FirstChild;
                        while (constraint != null)
                        {
                            Console.WriteLine(constraint.Name);
                            if (constraints.Name.Equals("nonBooleanConstraints"))
                            {
                                nonBooleanConstraints.Add(constraint.InnerText);
                            }
                            else if (constraints.Name.Equals("booleanConstraints"))
                            {
                                booleanConstraints.Add(constraint.InnerText);
                            }
                            constraint = constraint.NextSibling;
                        }
                    }
                    constraints = constraints.NextSibling;
                }
            }
            return Tuple.Create(booleanConstraints, nonBooleanConstraints);
        }

    }
}
