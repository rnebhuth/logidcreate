using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Entities
{
    public class EventIdStore
    {
        public List<EventIdItem> Ids { get; set; } = new List<EventIdItem>();
        public object FullName { get; internal set; }

        internal string CreateId(string logClassName)
        {
            var baseIdValue = Ids.Where(a => a.Class == logClassName && a.IsBase).FirstOrDefault();
            if (baseIdValue == null)
            {
                var asmBaseId = Ids.FirstOrDefault(a => a.Class == "AssemblyEventIds") ?? throw new Exception("AssemblyEventIds BaseID not found");
                var relativ = Ids.Where(a => a.IsBase && a.Class != "AssemblyEventIds").Max(a => a.Relative) ?? 0;
                relativ = relativ + 1000;
                baseIdValue = new EventIdItem
                {
                    Class = logClassName,
                    Relative = relativ,
                    Name = "BaseId",
                    IsNew = true,
                    IsBase = true,
                    Value = asmBaseId.Value + relativ,
                };
                Ids.Add(baseIdValue);
            }

            var newIdValue = (Ids.Where(a => a.Class == logClassName && !a.IsBase).Max(a => a.Relative) ?? 0) + 1;
            var newId = new EventIdItem
            {
                Class = logClassName,
                Relative = newIdValue,

                IsNew = true,
                Value = baseIdValue.Value + newIdValue,
            };

            //newId.Name = "Id" + String.Format("{0:0000}", newId.Relative);
            newId.Name = "Id" + String.Format("{0:0000}", newId.Value);


            Ids.Add(newId);
            return newId.Name;
        }

        internal void AddOccurence(string logClassName, string id, string value)
        {
            id = id.Replace("EventIds.", "");
            var eventId = Ids.Where(a => a.Class == logClassName && a.Name == id).FirstOrDefault();
            if (eventId != null)
            {
                eventId.Occurrence.Add(value);
            }
        }
    }
}
