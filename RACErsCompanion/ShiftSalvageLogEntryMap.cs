using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using RACErsLedger;

namespace RACErsCompanion
{
    class ShiftSalvageLogEntryMap : ClassMap<ShiftSalvageLogEntry>
    {
        public ShiftSalvageLogEntryMap()
        {
            Map(m => m.ObjectName).Name("objectName");
            Map(m => m.Mass).Name("mass");
            Map(m => m.Categories).Convert(row => row.Row.GetField("categories").Split(';'));
            Map(m => m.SalvagedBy).Name("salvagedBy");
            Map(m => m.Value).Name("value");
            Map(m => m.MassBasedValue).Name("massBasedValue");
            Map(m => m.Destroyed).Name("destroyed");
            Map(m => m.GameTime).Name("gameTime");
            Map(m => m.SystemTime).Convert(row =>
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(row.Row.GetField("epochTimeMs"))).LocalDateTime);

        }
    }
}
