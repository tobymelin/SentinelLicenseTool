# RMS Sentinel License Tool

Tool for checking license use for software using the RMS License Server. Includes CSI and Tekla software such as ETABS, SAFE, SAP2000, Tedds, and many more.

A similar tool currently exists (wlmadmin, distributed with all CSI software), however this requires administrator access and is more aimed at license administration than end user use.

## Requirements

Requires *lsmon.exe* and *lsapiw32.dll*, both of which are distributed with some CSI software in the CsiLicensing subdirectory of the program folder.

## Changelog

### v1.0
- Initial release

### v1.1
- Added usage time per user to the license list

### v1.2
- Set refresh limit to once per minute to avoid spamming
- Add license notifier (currently a hidden feature). Runs indefinitely with checks every minute.
- Press Escape to quit program
- Now hides licenses which have expired

### v1.2.1
- Fixed refresh button not working

### v1.2.2
- Added Connection Designer & CSIxRevit to license list
- Fixes to license notifier code

### v1.3
- Improved exception handling for server timeouts
- Added ability to show all licenses on server

### v1.4
- General code cleanup
- Allow user to specify server
- Improved notification when refreshing licenses
- Fixed UI lockups if server is unavailable
- Fixed license notifier intervals
- Reduced refresh timeout to once every 30s (down from once per minute)

### v1.5
- Continued code cleanup to prepare for Autodesk licenses
- Fixed bug where automatically refreshing a license could crash the program if the update was timing out

## TODO
- Add license expiry date?
- Indicate if a user is locking more than one license

**Low-Prio/Maybe**
- Refactor license parsing code to use a dictionary parser and/or generally improve legibility
