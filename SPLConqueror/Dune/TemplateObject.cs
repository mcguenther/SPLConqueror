using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    class TemplateObject
    {

        public enum Kind {concrete, placeholder, value};

        public Kind type;
        public Boolean isTerminal = true;

        public DuneFeature referseTo = null;

        public string artificalString = "";

        public Dictionary<String, String> referseToAliasing = new Dictionary<string, string>();

        private List<TemplateObject> children;

        private DuneClass defaultValue = null;

        public bool hasUnlimitedNumberOfParameters = false;

    }
}
