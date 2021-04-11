using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RACErsLedger
{
    public class StateManager
    {
        public List<ShiftLog> ShiftLogs { get; }
        public ShiftLog CurrentShift
        {
            get
            {
                return ShiftLogs.Last();
            }
        }
        public StateManager()
        {
            ShiftLogs = new List<ShiftLog>();
        }

        public ShiftLog StartShift()
        {
            var newShiftLog = new ShiftLog();
            ShiftLogs.Add(newShiftLog);
            return newShiftLog;
        }
        public void EndShift() => CurrentShift.EndShift();

    }
    public class ShiftLog
    {
        // ↑ do i want to call this ShiftSalvageLog....
        public List<ShiftSalvageLogEntry> SalvageLogEntries;
        public RACEInfo RaceInfo;
        public DateTime ShiftStartedTime;
        public DateTime ShiftEndedTime;
        public ShiftLog()
        {
            ShiftStartedTime = DateTime.Now;
            SalvageLogEntries = new List<ShiftSalvageLogEntry>();
        }
        public void SetRACEInfo(int seed, string startDateUTC, int MaxTotalValue, int MaxSalvageMass)
        {
            RaceInfo = new RACEInfo(seed, startDateUTC, MaxTotalValue, MaxSalvageMass);
        }
        public DateTime EndShift()
        {
            // TODO(sariya): do i want to throw some kind of a warning or info or whatever when you addsalvage and there's a shiftendedtime ....
            ShiftEndedTime = DateTime.Now;
            TimeSpan duration = ShiftEndedTime - ShiftStartedTime;
            float totalValueSalvaged = SalvageLogEntries.Where(entry => !entry.Destroyed).Sum(entry => entry.Value);
            float totalValueDestroyed = SalvageLogEntries.Where(entry => entry.Destroyed).Sum(entry => entry.Value);
            Plugin.Log(LogLevel.Info, $"Shift summary (started {ShiftStartedTime:u}, ended {ShiftEndedTime:u}, duration {duration}, salvaged {totalValueSalvaged}, destroyed {totalValueDestroyed})");
            foreach (var entry in SalvageLogEntries)
            {
                // Maybe this should be Debug once we log to files? This is fine for now though.
                Plugin.Log(LogLevel.Info, entry.ToString());
            }
            return ShiftEndedTime;
        }
        // TODO(sariya): how to design this API? it COULD just take the entire SalvageableChangedEvent and store the right thing from there instead of doing it in the patched in handler?
        //               honestly, not sure what the right thing to do with C# design there is.
        public void AddSalvage(string objectName, float mass, string[] categories, string salvagedBy, float value, bool massBasedValue, bool destroyed, float gameTime, DateTime systemTime)
        {
            SalvageLogEntries.Add(new ShiftSalvageLogEntry(objectName, mass, categories, salvagedBy, value, massBasedValue, destroyed, gameTime, systemTime));
        }
    }
    public class ShiftSalvageLogEntry
    {
        // Localized object name
        public string ObjectName { get; private set; }
        // Mass reported at salvage time
        public float Mass { get; private set; }
        // Categories HSSB thinks this object is in
        public string[] Categories { get; private set; }
        // What salvaged this? (i.e. Furnace, Processor, PickUp, etc.)
        // Should SalvagedBy be an enum? Maybe, but also maybe managing the integrity of what we put in this field
        // should be up to the patches?? 
        public string SalvagedBy { get; private set; }
        // How much the object was worth
        public float Value { get; private set; }
        // Is the value of the object determined on the mass?
        public bool MassBasedValue { get; private set; }
        // If Destroyed is true, then we did NOT get the Value out of this, and it is probably Scrapped now.
        public bool Destroyed { get; private set; }
        // Seconds into the shift this object was salvaged
        public float GameTime { get; private set; }
        // System time when object was salvaged
        public DateTime SystemTime { get; private set; }

        public ShiftSalvageLogEntry(string objectName, float mass, string[] categories, string salvagedBy, float value, bool massBasedValue, bool destroyed, float gameTime, DateTime systemTime)
        {
            ObjectName = objectName;
            Mass = mass;
            Categories = categories;
            SalvagedBy = salvagedBy;
            Value = value;
            MassBasedValue = massBasedValue;
            Destroyed = destroyed;
            GameTime = gameTime;
            SystemTime = systemTime;
        }
        public new string ToString()
        {
            // TODO(sariya) add categories here... at some point probably
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} ({1:O}) ", GameTime, SystemTime);
            sb.Append(Destroyed ? "Destroyed: " : "Salvaged: ");
            if (MassBasedValue)
            {
                sb.AppendFormat("{0}kg of ", Mass);
            }
            sb.Append(ObjectName);
            sb.AppendFormat(" worth ${0} via {1}", Value, SalvagedBy);
            return sb.ToString();
        }
    }
    public class RACEInfo
    {
        public int Seed { get; }
        public string StartDateUTC { get; }
        public int MaxTotalValue { get; }
        public int MaxSalvageMass { get; }
        public RACEInfo(int seed, string startDateUTC, int maxTotalValue, int maxSalvageMass)
        {
            Seed = seed;
            StartDateUTC = startDateUTC;
            MaxTotalValue = maxTotalValue;
            MaxSalvageMass = maxSalvageMass;
        }
    }
}
