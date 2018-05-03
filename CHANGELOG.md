# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
 