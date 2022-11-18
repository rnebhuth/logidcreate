using LogIdCreate.Core.Cmd.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    public interface IExecuterWalker
    {
        public void Run(CreateIdScope scope);
    }
}
