using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd
{
    public class LogIdConfig
    {
        public AssemblyEventIdConfig AssemblyEventId { get; set; } = new AssemblyEventIdConfig();
    }

    public class AssemblyEventIdConfig
    {
        public string FileName { get; set; } = "NOTSET";

        public string Template { get; set; } = "NOTSET";
    }
}
