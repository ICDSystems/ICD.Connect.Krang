# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
###Changed
 - Removed Obfuscation

## [16.3.0] - 2023-03-21
### Removed
 - Removed system key validation

## [16.2.3] - 2022-07-11
### Changed
 - Fixed package reference for netstandard target

## [16.2.2] - 2022-07-01
### Changed
 - Updated Crestron SDK to 2.18.96

## [16.2.1] - 2022-06-23
### Changed
 - Fixed null reference exception in SPlusKrangBootstrap
 - Removed timeout removals for remote cores

## [16.2.0] - 2022-05-23
### Added
 - KrangCoreSettings now adds theme's child originators

## [16.1.0] - 2021-08-03
### Added
 - Flush logging service on program stop
 - Add theme child originators to the core

### Changed
 - Health console command - panels removed from devices section

## [16.0.0] - 2021-05-14
### Added
 - ICD.Connect.Core runs as a windows service
 - Added a firmware column to the controlsystem health console table
 - NetStandard program subscribes to Windows Service device added and removed events and raises them for the environment
 - NetStandard release builds log to the Windows Event Logger
 - NetStandard builds log to file

### Changed
 - Generated destination groups are now given uuids
 - No longer capturing console input when running as a service

### Removed
 - Removed old NVRAM migration code

## [15.3.0] - 2021-01-14
### Added
 - Added Krang.CrestronSPlus project to contain Krang SPlus Bootstrap
 - Fixed Krang SPlus Bootstrap to use Lifecycle State
 - Added Environment to core status

### Changed
 - Changed InterCore service to start communications on StartSettings instead of ApplySettings
 - Changed IcdEnvironment to use new properties

## [15.2.1] - 2020-09-24
### Changed
 - Fixed NVRAM path for 4-series processors

## [15.2.0] - 2020-08-13
### Changed
 - Services are loaded before all other originators

## [15.1.0] - 2020-07-14
### Added
 - Added telemetry for program status

### Changed
 - Simplified external telemetry providers
 - Telemetry is disposed before disposing the rest of the system

## [15.0.0] - 2020-06-19
### Added
 - Added runtime generation of DestinationGroups based on DestinationGroupString on Destinations
 - Added CalendarPoints to the krang config
 - Added OccupancyPoints to the krang config

### Changed
 - Using new logging context
 - References to "Room Config" changed to "System Config"
 - Telemetry and Multi-Krang restructured into Services and ServiceProviders

## [14.3.0] - 2020-10-06
### Changed
 - Loading/Starting Krang now runs StartSettings on the core
 - KrangBootstrap Start has a callback for post-load but pre-start actions
 - KrangControlSystem (Crestron) uses KrangBootstrap post-load callback to set program as started

## [14.2.1] - 2020-05-06
### Changed
 - Reverted "PrintVersions" console command back to "VersionInfo"

## [14.2.0] - 2020-03-20
### Added
 - Added "icd controlsystem health" console command for printing the online states of panels, ports and devices

### Changed
 - Debug builds validate the SystemKey while still allowing the use of invalid SystemKey
 - Clarifying MAC address mismatch when validating system key
 - Remote core discovery uses UTC for tracking age

## [14.1.1] - 2019-12-09
### Changed
 - Fixed deadlock in core discovery broadcast

## [14.1.0] - 2019-11-19
### Changed
 - Potential fix for exceptions on program stop
 - Incremented informational version to 1.5

## [14.0.0] - 2019-09-16
### Added
 - KrangCore contains SourceGroups and DestinationGroups

### Removed
 - Moved residential features into ICD.Profound.Residential project
 - Moved localization settings into the ICD.Connect.Settings project

## [13.0.0] - 2019-08-15
### Changed
 - Substantial changes for Multi-Krang

## [12.2.0] - 2019-05-24
### Added
 - Added localization settings to Krang Configuration
 
### Changed
 - Renamed Core directory to Cores for consistency
 - Failing gracefully when a cofigured localization is not valid
 
## [12.1.0] - 2019-02-13
### Added
 - Added VolumePoints to the core console
 - Added ConferencePoints to the core

### Changed
 - Only printing licensing information when licensing is enabled

## [12.0.0] - 2019-01-10
### Changed
 - Core namespace renamed

## [11.6.1] - 2019-09-03
### Changed
 - When a system key is missing or invalid the service logger logs an emergency message

## [11.6.0] - 2019-08-13
### Added
 - Support for license files saved as system key

## [11.5.0] - 2019-07-16
### Added
 - Services are exposed in the console
 - VolumePoints are exposed in the core console

## [11.4.0] - 2019-06-06
### Added
 - Setting the program initialization complete state as final step in program start

## [11.3.0] - 2019-05-17
### Added
 - Added telemetry aggregation service to services
 - Added telemetry features to remote switcher device

## [11.2.1] - 2019-05-16
### Changed
 - NVRAM to USER Migration improvements - only migrate needed directories, don't migrate if new config directory already exists
 - Remove originators from room during krang clear, solves a lot of program shutdown issues

## [11.2.0] - 2019-04-12
### Changed
 - Release builds set default logging severity to Notice

## [11.1.2] - 2019-03-27
### Changed
 - Remove ids from room settings when a settings instance is removed from the core settings 

## [11.1.1] - 2019-02-13
### Changed
 - NVRAM no longer bails if USER directory is not empty due to EDID, AvF, etc generation

## [11.1.0] - 2019-01-02
### Added
 - Added command line argument for setting program number on Net Standard
 - Added features for getting the header from a core configuration

### Changed
 - Proxies are no longer instantiated on discovery and must be pre-configured

## [11.0.1] - 2018-11-20
### Changed
 - Fixed bug where USER migration would create redundant CommonConfig directory

## [11.0.0] - 2018-11-08
### Added
 - Logging stacktraces when failing to dispose originators

### Changed
 - Potential performance improvement when removing settings

### Removed
 - Removed DestinationGroups

## [10.0.0] - 2018-10-30
### Added
 - NVRAM is automatically migrated to USER directory

## [9.0.1] - 2019-04-16
### Changed
 - No longer obfuscating the Core project for SimplSharp to solve for Releases failing to start
 - Release builds set default logging severity to Notice

## [9.0.0] - 2018-09-14
### Changed
 - Originators are constrained to class type
 - Significant improvements to routing performance
 - Only starting the direct message manager when broadcasting is configured

## [8.1.0] - 2018-07-19
### Added
 - Added ActionSchedulerService to ServiceProvider

## [8.0.2] - 2018-06-19
### Changed
 - Clear originators first, then dispose, fixing issue where id would be 0'ed out by dispose, causing the originators to never be cleared.
 - Fixed bug with serialization of nested originator settings

## [8.0.1] - 2018-06-04
### Changed
 - Fixes for license encoding

## [8.0.0] - 2018-05-24
### Changed
 - Changed SPlusOnKrangLoadedEventRelay to SPlusKrangEventRelay
 
### Added
 - Added OnKrangCleared event to SPlusKrangEventRelay for completeness
 
### Removed
 - Removed the SPlusPanelShim, as it doesnt make sense
 - Removed the SPlusSwitcherShim, relocated to ICD.Connect.Routing
 
### Changed
 - Drastically increasing thread count for pooling

## [7.0.0] - 2018-05-18
### Added
 - Adding license validation, prevent loading Krang config if license is invalid

## [6.0.0] - 2018-05-09
### Changed
 - Renamed ambiguous s+interface to shim

## [5.1.0] - 2018-05-03
### Changed
 - Volume point interfaces and abstractions moved to Audio project

## [5.0.0] - 2018-04-27
### Added
 - Added volume points to krang settings

## [4.1.0] - 2018-04-25
### Changed
 - Multi-routing better resistant to the routing graph being reloaded
 - Broadcast configuration no longer a simple bool, adding elements for specifying addresses
 - Moving broadcast/messaging features out of KrangCore

## [4.0.0] - 2018-04-23
### Added
 - Adding API attributes
 - Adding API messaging features for MultiKrang communication
 - Adding remote core mechanism for creating and managing proxy originators
 - Added VersionInfo console command to KrangBootstrap
 - Added informational version info to AssemblyInfo

### Changed
 - Singificant overhauling of existing message/broadcast features

## [3.1.0] - 2018-04-15
### Changed
 - Using new DirectMessageManager features for inter-Krang communication
 