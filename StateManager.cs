using BepInEx.Logging;
using RACErsLedger.DataTypes;
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
        public ShiftLog CurrentShift => ShiftLogs.Last();
        private readonly string _dataFolder;
        public StateManager(string dataFolder)
        {
            ShiftLogs = new List<ShiftLog>();
            _dataFolder = dataFolder;
        }

        public ShiftLog StartShift()
        {
            var newShiftLog = new ShiftLog();
            ShiftLogs.Add(newShiftLog);

            var @event = new StartShiftEvent();
            Plugin.LampreyManager.SendEvent(@event);

            return newShiftLog;
        }
        public void EndShift(string ExitCause)
        {
            try
            {
                Plugin.Log(LogLevel.Debug, "ending shift now...");
                CurrentShift.EndShift(ExitCause);
                var shift = CurrentShift;

                var @event = new EndShiftEvent();
                Plugin.LampreyManager.SendEvent(@event);

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
                shift.WriteShiftSummary(Path.Combine(_dataFolder, shiftSummaryFileName));
                shift.WriteSalvageLedger(Path.Combine(_dataFolder, shiftLedgerFilename));
                Plugin.Log(LogLevel.Info, "writing summary and ledger successful!");
                ShiftLogs.Remove(shift);
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
        public string ExitCause;

        public float TotalValueSalvaged => SalvageLogEntries.Where(entry => !entry.Destroyed).Sum(entry => entry.Value);
        public float TotalValueDestroyed => SalvageLogEntries.Where(entry => entry.Destroyed).Sum(entry => entry.Value);
        // TODO(sariya) add TotalMassSalvaged / TotalMassDestroyed? or more fun stuff with linq like everything destroyed by furnace/salvage so you can see what all you're sacrificing to the ~~wrong hole~~furnace gods?
        public ShiftLog()
        {
            ShiftStartedTime = DateTime.Now;
            SalvageLogEntries = new List<ShiftSalvageLogEntry>();
        }
        public void SetRACEInfo(int seed, int version, string startDateUTC, int maxTotalValue, int maxSalvageMass)
        {
            RaceInfo = new RACEInfo(seed, version, startDateUTC, maxTotalValue, maxSalvageMass);

                var @event = new SetRACEInfoEvent(RaceInfo);
                Plugin.LampreyManager.SendEvent(@event);


        }
        public DateTime EndShift(string ExitCause)
        {
            this.ExitCause = ExitCause;
            ShiftEndedTime = DateTime.Now;
            TimeSpan duration = ShiftEndedTime - ShiftStartedTime;
            Plugin.Log(LogLevel.Info,
                $"Shift summary (started {ShiftStartedTime:u}"
                + $", ended {ShiftEndedTime:u}"
                + $" via {ExitCause}"
                + $", duration {duration}"
                + $", salvaged {TotalValueSalvaged:C}"
                + $", destroyed {TotalValueDestroyed:C})"
            );
            return ShiftEndedTime;
        }
        // TODO(sariya): how to design this API? it COULD just take the entire SalvageableChangedEvent and process that event here instead of doing it in the patched in handler?
        //               honestly, not sure what the right thing to do with C# design there is.
        public void AddSalvage(string objectName, float mass, string[] categories, string salvagedBy, float value, bool massBasedValue, bool destroyed, float gameTime, DateTime systemTime)
        {
            // DIRTY HACK ALERT! I hate this but it fixes a bug. let's do it better honestly??
            // if the shift is over, no more adding stuff!
            if (ShiftEndedTime != DateTime.MinValue)
            {
                Plugin.Log(LogLevel.Warning, "Tried to add salvage after shift ended! bug sariya to make this better!");
                return;
            }
            ShiftSalvageLogEntry entry = new ShiftSalvageLogEntry(objectName, mass, categories, salvagedBy, value, massBasedValue, destroyed, gameTime, systemTime);
            SalvageLogEntries.Add(entry);
            Plugin.LampreyManager.SendEvent(entry);
            Plugin.Log(LogLevel.Info, entry.ToString());
        }

        public void WriteSalvageLedger(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                string headerLine = string.Join(",", new string[]{
                    "objectName",
                    "mass",
                    "categories",
                    "salvagedBy",
                    "value",
                    "massBasedValue",
                    "destroyed",
                    "gameTime",
                    "epochTimeMs"
                });
                sw.WriteLine(headerLine);
                foreach (var entry in SalvageLogEntries)
                {
                    sw.WriteLine(string.Join(",", new string[] {
                        $"{entry.ObjectName}",
                        $"{entry.Mass:F3}",
                        $"{string.Join(";", entry.Categories)}",
                        $"{entry.SalvagedBy}",
                        $"{entry.Value:F2}",
                        $"{entry.MassBasedValue}",
                        $"{entry.Destroyed}",
                        $"{entry.GameTime:F1}",
                        $"{((DateTimeOffset)entry.SystemTime.ToUniversalTime()).ToUnixTimeMilliseconds()}"
                    }));
                }
            }
        }
        public void WriteShiftSummary(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine($"Started: {ShiftStartedTime}");
                sw.WriteLine($"Ended: {ShiftEndedTime}");
                sw.WriteLine($"EndedBy: {ExitCause}");
                sw.WriteLine($"Duration: {ShiftEndedTime - ShiftStartedTime}");
                sw.WriteLine($"Total value salvaged: ${TotalValueSalvaged:F3}");
                sw.WriteLine($"Total value destroyed: ${TotalValueDestroyed:F3}");
                sw.WriteLine($"RACE?: {RaceInfo != null}");
                if (RaceInfo != null)
                {
                    sw.WriteLine("");
                    sw.WriteLine("RACE Info");
                    sw.WriteLine("--------------------------------------");
                    sw.WriteLine($"Seed: {RaceInfo.Seed}");
                    sw.WriteLine($"Version: {RaceInfo.Version} (probably week {RaceInfo.Version + 1})");
                    sw.WriteLine($"Start date: {RaceInfo.StartDateUTC}");
                    sw.WriteLine($"Maximum possible salvage: ${RaceInfo.MaxTotalValue:F}");
                    sw.WriteLine($"Total mass: {RaceInfo.MaxSalvageMass:N}kg");
                }
                sw.WriteLine("--------------------------------------");
                sw.WriteLine("Top 5 most valuable destroyed objects:");
                foreach (var salvage in SalvageLogEntries.FindAll((entry => entry.Destroyed))
                    .OrderByDescending(entry => entry.Value).Take(5))
                {
                    sw.WriteLine(salvage.ToString());
                }
            }
        }
    }
}
