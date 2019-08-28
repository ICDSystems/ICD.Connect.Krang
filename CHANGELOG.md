# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
 