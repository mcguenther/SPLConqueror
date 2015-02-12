﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Sampling.Heuristics
{
    public class FeatureWise
    {
        private List<List<BinaryOption>> configurations = new List<List<BinaryOption>>();


        /// <summary>
        /// Generates configurations based on the feature-wise heuristic: The method utilizes the structure of the variability model by first determining the set of options that are always selected.
        /// In each iteration, it starts from this set of options (partial configuration) and adds a single option to this partial configurations. 
        /// It then checks whether configuration is valid and if not calls a CSP solver to make this configuration valid with as few selected options as possible.
        /// </summary>
        /// <param name="vm">The variability model for which the feature-wise configurations should be generated.</param>
        /// <returns>A list of configurations, in which each configuration is a list of binary options that represent the SELECTED options.</returns>
        public List<List<BinaryOption>> generateFeatureWiseConfigurations(VariabilityModel vm)
        {
            configurations.Clear();
            List<BinaryOption> optionalFirstLevelElements = new List<BinaryOption>();
            List<BinaryOption> binOptions = vm.BinaryOptions;

            //First: Add options that are present in all configurations
            List<BinaryOption> firstLevelMandatoryFeatures = new List<BinaryOption>();
            foreach (BinaryOption binOpt in binOptions)
            {
                if (binOpt.Parent == null || binOpt.Parent == vm.Root)
                {
                    if (!binOpt.Optional)
                    {
                        if (!binOpt.hasAlternatives())
                        {
                            firstLevelMandatoryFeatures.Add(binOpt);
                            //Todo: Recursive down search
                            /*List<BinaryOption> tmpList = (List<BinaryOption>)vm.getMandatoryChildsRecursive(binOpt);
                            if (tmpList != null && tmpList.Count > 0)
                                firstLevelMandatoryFeatures.AddRange(tmpList);*/
                        }
                    }
                    else
                        optionalFirstLevelElements.Add(binOpt);
                }
            }
            Solver.CheckConfigSAT checkSAT = new Solver.CheckConfigSAT();
            Solver.VariantGenerator generator = new Solver.VariantGenerator();
            //Generating new configurations: one per option
            foreach (BinaryOption e in binOptions)
            {
                BinaryOption[] temp = new BinaryOption[firstLevelMandatoryFeatures.Count];
                firstLevelMandatoryFeatures.CopyTo(temp);
                List<BinaryOption> tme = temp.ToList<BinaryOption>();
                if (!tme.Contains(e))
                {
                    tme.Add(e);
                    if (checkSAT.checkConfigurationSAT(tme, vm))
                    {
                        if (!this.configurations.Contains(tme))
                            this.configurations.Add(tme);
                    }
                    else
                    {
                        tme = generator.minimizeConfig(tme, vm, true, null);
                        if (tme != null && this.configurations.Contains(tme) == false)
                            this.configurations.Add(tme);
                    }
                }
                else
                    continue;
            }

            return this.configurations;
        }

        /// <summary>
        /// This algorithm calls for each binary option in the variability model the CSP solver to generate a valid, minimal configuration containing that option.
        /// </summary>
        /// <param name="vm">The variability model for which the feature-wise configurations should be generated.</param>
        /// <returns>A list of configurations, in which each configuration is a list of binary options that represent the SELECTED options.</returns>
        public List<List<BinaryOption>> generateFeatureWiseConfigsCSP(VariabilityModel vm)
        {
            this.configurations.Clear();
            Solver.VariantGenerator generator = new Solver.VariantGenerator();
            foreach (var opt in vm.BinaryOptions)
            {
                List<BinaryOption> temp = new List<BinaryOption>();
                temp.Add(opt);
                temp = generator.minimizeConfig(temp, vm, true, null);
                if (temp != null && this.configurations.Contains(temp) == false)
                    this.configurations.Add(temp);

                //Now finding a configuration without the current option, but with all other options to be able to compute a delta
                List<BinaryOption> withoutOpt = new List<BinaryOption>();
                BinaryOption[] tempArray = temp.ToArray();
                withoutOpt = tempArray.ToList<BinaryOption>();
                withoutOpt.Remove(opt);
                List<BinaryOption> excluded = new List<BinaryOption>();
                excluded.Add(opt);
                withoutOpt = generator.minimizeConfig(withoutOpt, vm, true, excluded);
                if (withoutOpt != null && this.configurations.Contains(withoutOpt) == false)
                    this.configurations.Add(withoutOpt);
            }

            return this.configurations;
        }
    }
}