using SetOnceGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetOnceProperties.Sources.SettableOnces.Interfaces
{
    public interface IGuidDTO : IDTO
    {
        [SetOnce]
        Guid? MyGuid { get; set; }
    }
}
