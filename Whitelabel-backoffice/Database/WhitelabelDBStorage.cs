using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Database
{
    public class WhitelabelDBStorage : IWhitelabelDBStorage
    {
        public WhitelabelContext context = null;
        public WhitelabelContext Context { get { return context; } }
        public WhitelabelDBStorage(WhitelabelContext dbContext)
        {
            context = dbContext;
        }
    }
}
