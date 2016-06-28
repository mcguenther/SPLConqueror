using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dune.util;

namespace Dune
{
    class TemplateObject
    {

        public enum Kind { concrete, placeholder, value };

        public Kind type;
        public Boolean isTerminal = true;

        public List<DuneFeature> referseTo = null;

        public string artificalString = "";

        public RefersToAliasing refersToAliasing;

        private List<TemplateObject> children;

        private DuneClass defaultValue = null;

        public bool hasUnlimitedNumberOfParameters = false;

        public TemplateTree informationFromTemplateParamlist = null;

    }
}
