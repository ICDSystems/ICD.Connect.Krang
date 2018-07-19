# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
 