// Datatypes used in RACErs Ledger Extended Universe. Should generally be kept in sync with racers-ledger/DataTypes/DataTypes.cs.
use serde::{Deserialize, Serialize};
use chrono::prelude::*;
use std::fmt;
use colored::Colorize;

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

impl fmt::Display for SalvageEvent {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match &*self {
            SalvageEvent::ShiftSalvageLogEntry{object_name, mass, categories, salvaged_by, value, mass_based_value, destroyed, game_time, system_time} => {
                write!(f, "{:.2} ({}) {}{}{} worth {} via {} (item categories: [{}])",
                game_time, 
                system_time.to_rfc3339_opts(SecondsFormat::Secs, true),
                if *destroyed {"Destroyed ".red().bold()} else {"Salvaged ".green().bold()},
                if *mass_based_value {format!("{} kg of ", mass)} else {"".into()},
                object_name,
                value,
                salvaged_by,
                categories.join(",") // TODO(sariya) have some highlight override colors for these for common RACE categories???
            )
            },
            SalvageEvent::StartShiftEvent => {
                write!(f, "started new shift")
            },
            SalvageEvent::EndShiftEvent => {
                write!(f, "ended shift")
            },
            SalvageEvent::SetRACEInfoEvent {seed, version, start_date_utc, max_total_value, max_salvage_mass} => {
                write!(f, "current shift is a RACE: seed={} version={} start_date_utc={} max_total_value={} max_salvage_mass={}", seed, version, start_date_utc, max_total_value, max_salvage_mass)
            }
        }
    }
}