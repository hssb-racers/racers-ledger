// Datatypes used in RACErs Ledger Extended Universe. Should generally be kept in sync with racers-ledger/DataTypes/DataTypes.cs.
use serde::{Deserialize, Serialize};
use chrono::prelude::*;

#[derive(Deserialize, Serialize, Debug)]
#[serde(rename_all = "camelCase", tag = "type")]
pub enum SalvageEvent {
    #[serde(rename_all = "camelCase")]
    ShiftSalvageLogEntry {
        // Localized object name
        object_name: String,
        // Mass reported at salvage time
        mass: f64,
        // Categories HSSB thinks this object is in
        categories: Vec<String>,
        // What salvaged this? (i.e. Furnace, Processor, PickUp, etc.)
        salvaged_by: String,
        // How much the object was worth
        value: f64,
        // Is the value of the object determined on the mass?
        mass_based_value: bool,
        // If Destroyed is true, then we did NOT get the Value out of this, and it is probably Scrapped now.
        destroyed: bool,
        // Seconds into the shift this object was salvaged
        game_time: f32,
        // System time when object was salvaged
        system_time: DateTime<Utc>,
    },
    StartShiftEvent,
    EndShiftEvent,
    #[serde(rename_all = "camelCase")]
    SetRACEInfoEvent {
        // ship seed
        seed: i64,
        // dev set "version" (typically week minus one)
        version: i64,
        // dev representation of when start date is
        #[serde(rename = "startDateUTC")]
        start_date_utc: String,
        // dev claimed max theoretical value
        max_total_value: i64,
        // dev claimed salvage mass
        max_salvage_mass: i64
    }
}

/* 
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
    */
