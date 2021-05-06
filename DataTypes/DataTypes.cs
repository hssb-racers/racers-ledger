﻿using System;
using System.Text;

namespace RACErsLedger.DataTypes
{


    public interface ILedgerEvent
    {
        // what else could be a ledger event?
        // kiosk purchases
        // tether uses / breaks + objects they attached to / masses / if they're to the same object?
        // work order completion?
        // maybe heartbeats of game time every second??? 
        // cutter temp? maybe part of heartbeats. CuttingToolController#CurrentHeatPercent
        // cut points cut?? still has the same difficulty as in the cut point tracking ticket though (multiple registrations for the same cut point)

    };

    public abstract class LedgerEventBase : ILedgerEvent
    {
        // This is just for serialization's sake, please don't judge me
        // It's just the type of the variant. For compatibility with serde.rs (also since I couldn't easily find ways to have a custom format for #[serde(tag="$type")] decoding.
        // without, using TypeNameHandling: "$type":"RACErsLedger.DataTypes.ShiftSalvageLogEntry, RACErsLedger"
        // with: "type":"ShiftSalvageLogEntry",
        public string Type
        {
            get
            {
                // convert titlecase to camelcase
                var name  = GetType().Name;
                return Char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
        }
    }


    [Serializable]
    public class StartShiftEvent : LedgerEventBase
    {

    }

    [Serializable]
    public class EndShiftEvent : LedgerEventBase
    {

    }

    [Serializable]
    public class SetRACEInfoEvent : LedgerEventBase
    {
        // TODO(sariya): is there any reason we can't just have RACEInfo inherit from LedgerEventBase and use the same class for both things
        // instead of duplicating code here?
        public int Seed { get; }
        public int Version { get; }
        public string StartDateUTC { get; }
        public int MaxTotalValue { get; }
        public int MaxSalvageMass { get; }
        public SetRACEInfoEvent(RACEInfo raceInfo)
        {
            Seed = raceInfo.Seed;
            Version = raceInfo.Version;
            StartDateUTC = raceInfo.StartDateUTC;
            MaxTotalValue = raceInfo.MaxTotalValue;
            MaxSalvageMass = raceInfo.MaxSalvageMass;

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
    [Serializable]
    public class ShiftSalvageLogEntry : LedgerEventBase
    {
        // Localized object name
        public string ObjectName { get; set; }
        // Mass reported at salvage time
        public float Mass { get; set; }
        // Categories HSSB thinks this object is in
        public string[] Categories { get; set; }
        // What salvaged this? (i.e. Furnace, Processor, PickUp, etc.)
        // Should SalvagedBy be an enum? Maybe, but also maybe managing the integrity of what we put in this field
        // should be up to the patches?? 
        public string SalvagedBy { get; set; }
        // How much the object was worth
        public float Value { get; set; }
        // Is the value of the object determined on the mass?
        public bool MassBasedValue { get; set; }
        // If Destroyed is true, then we did NOT get the Value out of this, and it is probably Scrapped now.
        public bool Destroyed { get; set; }
        // Seconds into the shift this object was salvaged
        public float GameTime { get; set; }
        // System time when object was salvaged
        public DateTime SystemTime { get; set; }

        public ShiftSalvageLogEntry()
        {
        }

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
                sb.AppendFormat("{0:F3}kg of ", Mass);
            }
            sb.Append(ObjectName);
            sb.AppendFormat(" worth {0:C} via {1}", Value, SalvagedBy);
            return sb.ToString();
        }
    }


}