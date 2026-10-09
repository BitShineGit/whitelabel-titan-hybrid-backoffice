using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Database
{
    public interface IWhitelabelDBStorage
    {
        WhitelabelContext Context { get; }
    }
}
