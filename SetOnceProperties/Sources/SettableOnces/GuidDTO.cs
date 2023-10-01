using SetOnceProperties.Sources.SettableOnces.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetOnceProperties.Sources.SettableOnces
{
    internal partial class GuidDTO : IGuidDTO
    {
        public GuidDTO(Guid? guid, int id, string name = "Default_GuidDTO_Name")            
        {
            ((IGuidDTO)this).MyGuid = guid ?? Guid.Empty;
            ((IDTO)this).ID = id;
            ((IDTO)this).Name = name;
        }
    }
}
