using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dune
{
    class TemplateElement
    {
        public String declmame_cont = "";
        public String defval_cont = "";
        public String defVal_cont_ref = "";
        public String defVal_cont_ref_id = "";
        public String defname_cont = "";
        public String deftype_cont = "";
        public DuneFeature o = null;

        public TemplateTree defVal_tree = null;
        public TemplateTree type_tree = null;
    }
}
