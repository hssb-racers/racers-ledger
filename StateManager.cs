using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly string DataFolder;
        public StateManager(string dataFolder)
        {
            ShiftLogs = new List<ShiftLog>();
            DataFolder = dataFolder;
        }

        public ShiftLog StartShift()
        {
            var newShiftLog = new ShiftLog();
            ShiftLogs.Add(newShiftLog);
            return newShiftLog;
        }
        public void EndShift()
        {
            try
            {
                Plugin.Log(LogLevel.Debug, "ending shift now...");
                CurrentShift.EndShift();
                var shift = CurrentShift;
                StringBuilder sb = new StringBuilder();
                if (shift.RaceInfo != null)
                {
                    sb.AppendFormat("RACE{0}-", shift.RaceInfo.Version + 1);
                }
                sb.Append(shift.ShiftStartedTime.ToString("yyyyMMddTHHmmss"));
                var shiftFilenameBase = sb.ToString();

                var shiftSummaryFileName = shiftFilenameBase + "_summary.txt";
                var shiftLedgerFilename = shiftFilenameBase + "_ledger.csv";
                Plugin.Log(LogLevel.Info, $"writing summary and ledger to {shiftSummaryFileName} and {shiftLedgerFilename}...");
                // does this need a try/catch for IO stuff???
                // maybe these methods should be inherently async???
                shift.WriteShiftSummary(Path.Combine(DataFolder, shiftSummaryFileName));
                shift.WriteSalvageLedger(Path.Combine(DataFolder, shiftLedgerFilename));
                Plugin.Log(LogLevel.Info, "writing summary and ledger successful!");
            }
            catch (Exception e) { Plugin.Log(LogLevel.Error, e.ToString()); }

        }
    }
    public class ShiftLog
    {
        // ↑ do i want to call this ShiftSalvageLog....
        public List<ShiftSalvageLogEntry> SalvageLogEntries;
        public RACEInfo RaceInfo;
        public DateTime ShiftStartedTime;
        public DateTime ShiftEndedTime;

        public float TotalValueSalvaged => SalvageLogEntries.Where(entry => !entry.Destroyed).Sum(entry => entry.Value);
        public float TotalValueDestroyed => SalvageLogEntries.Where(entry => entry.Destroyed).Sum(entry => entry.Value);
        // TODO(sariya) add TotalMassSalvaged / TotalMassDestroyed? or more fun stuff with linq like everything destroyed by furnace/salvage so you can see what all you're sacrificing to the ~~wrong hole~~furnace gods?
        public ShiftLog()
        {
            ShiftStartedTime = DateTime.Now;
            SalvageLogEntries = new List<ShiftSalvageLogEntry>();
        }
        public void SetRACEInfo(int seed, int version, string startDateUTC, int MaxTotalValue, int MaxSalvageMass)
        {
            RaceInfo = new RACEInfo(seed, version, startDateUTC, MaxTotalValue, MaxSalvageMass);
        }
        public DateTime EndShift()
        {
            // TODO(sariya): do i want to throw some kind of a warning or info or whatever when you addsalvage and there's a shiftendedtime ....
            ShiftEndedTime = DateTime.Now;
            TimeSpan duration = ShiftEndedTime - ShiftStartedTime;
            Plugin.Log(LogLevel.Info, $"Shift summary (started {ShiftStartedTime:u}, ended {ShiftEndedTime:u}, duration {duration}, salvaged {TotalValueSalvaged}, destroyed {TotalValueDestroyed})");
            foreach (var entry in SalvageLogEntries)
            {
                // Maybe this should be Debug once we log to files? This is fine for now though.
                Plugin.Log(LogLevel.Info, entry.ToString());
            }
            return ShiftEndedTime;
        }
        // TODO(sariya): how to design this API? it COULD just take the entire SalvageableChangedEvent and process that event here instead of doing it in the patched in handler?
        //               honestly, not sure what the right thing to do with C# design there is.
        public void AddSalvage(string objectName, float mass, string[] categories, string salvagedBy, float value, bool massBasedValue, bool destroyed, float gameTime, DateTime systemTime)
        {
            SalvageLogEntries.Add(new ShiftSalvageLogEntry(objectName, mass, categories, salvagedBy, value, massBasedValue, destroyed, gameTime, systemTime));
        }

        public void WriteSalvageLedger(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                string headerLine = "objectName,mass,categories,salvagedBy,value,massBasedValue,destroyed,gameTime,epochTimeMs";
                sw.WriteLine(headerLine);
                foreach (var entry in SalvageLogEntries)
                {
                    sw.WriteLine(
                        $"{entry.ObjectName},{entry.Mass},{string.Join(";", entry.Categories)},{entry.SalvagedBy},{entry.Value},{entry.MassBasedValue},{entry.Destroyed},{entry.GameTime},{((DateTimeOffset)entry.SystemTime.ToUniversalTime()).ToUnixTimeMilliseconds()}");
                }
            }
        }
        public void WriteShiftSummary(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine($"Started: {ShiftStartedTime}");
                sw.WriteLine($"Ended: {ShiftEndedTime}");
                sw.WriteLine($"Duration: {ShiftEndedTime - ShiftStartedTime}");
                sw.WriteLine($"Total value salvaged: {TotalValueSalvaged}");
                sw.WriteLine($"Total value destroyed: {TotalValueDestroyed}");
                sw.WriteLine($"RACE?: {RaceInfo != null}");
                if (RaceInfo != null)
                {
                    sw.WriteLine("");
                    sw.WriteLine("RACE Info");
                    sw.WriteLine("--------------------------------------");
                    sw.WriteLine($"Seed: {RaceInfo.Seed}");
                    sw.WriteLine($"Version: {RaceInfo.Version} (probably week {RaceInfo.Version + 1})");
                    sw.WriteLine($"Start date: {RaceInfo.StartDateUTC}");
                    sw.WriteLine($"Maximum possible salvage: ${RaceInfo.MaxTotalValue.ToString("N")}");
                    sw.WriteLine($"Total mass: {RaceInfo.MaxSalvageMass.ToString("N")}kg");
                }
            }
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
        public int Version { get; }
        public string StartDateUTC { get; }
        public int MaxTotalValue { get; }
        public int MaxSalvageMass { get; }
        public RACEInfo(int seed, int version, string startDateUTC, int maxTotalValue, int maxSalvageMass)
        {
            Seed = seed;
            Version = version;
            StartDateUTC = startDateUTC;
            MaxTotalValue = maxTotalValue;
            MaxSalvageMass = maxSalvageMass;

        }
    }
}
