using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Entities
{
    public class EventIdItem
    {
        public string Class { get; set; }
        public string Name { get; set; }

        public int Value { get; set; }

        public int? Relative { get; set; }

        public bool IsNew { get; set; }

        public string? FullName { get; set; }

        public string? Assembly { get; set; }
        public string LineText { get; internal set; }
        public string PosText { get; internal set; }
        public bool IsBase { get; internal set; }

        public List<string> Occurrence { get; set; } = new List<string>();
    }
}
