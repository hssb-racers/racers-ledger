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
    #[serde(rename_all = "camelCase")]
    GameStateChangedEvent {
        // the state the game is now in
        current_game_state: String,
        // the state the game was in
        previous_game_state: String,
        // System time when the state change
        system_time: DateTime<Utc>,
    },
    #[serde(rename_all = "camelCase")]
    StartShiftEvent {
        // System time when shift was started
        system_time: DateTime<Utc>,
    },
    #[serde(rename_all = "camelCase")]
    EndShiftEvent {
        // System time when shift ended
        system_time: DateTime<Utc>
    },
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
        max_salvage_mass: i64,
        // System time when RACEInfo was queried
        system_time: DateTime<Utc>
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
            SalvageEvent::GameStateChangedEvent{current_game_state, previous_game_state, system_time} => {
                write!(f, "({}) game state changed from {} to {}", system_time.to_rfc3339_opts(SecondsFormat::Secs, true), previous_game_state, current_game_state)
            },
            SalvageEvent::StartShiftEvent{system_time} => {
                write!(f, "({}) started new shift", system_time.to_rfc3339_opts(SecondsFormat::Secs, true))
            },
            SalvageEvent::EndShiftEvent{system_time} => {
                write!(f, "({}) ended shift", system_time.to_rfc3339_opts(SecondsFormat::Secs, true))
            },
            SalvageEvent::SetRACEInfoEvent {seed, version, start_date_utc, max_total_value, max_salvage_mass, system_time} => {
                write!(f, "({}) current shift is a RACE: seed={} version={} start_date_utc={} max_total_value={} max_salvage_mass={}", system_time.to_rfc3339_opts(SecondsFormat::Secs, true), seed, version, start_date_utc, max_total_value, max_salvage_mass)
            }
        }
    }
}