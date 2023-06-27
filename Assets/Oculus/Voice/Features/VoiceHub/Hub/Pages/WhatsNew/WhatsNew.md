# v54 Release Notes and Upgrade Guide

## Upgrading
### From v53
If you’re coming from v53, upgrading should be relatively smooth. You should be able to simply install the latest package on top of your existing install. As always deleting the Oculus/Voice directory before upgrading will provide the smoothest experience.

### From <v49
It is recommended that you delete your existing Assets/Oculus/Voice directory before making this upgrade (especially if coming from one of the standalone packages). Simply installing the package should work and the Oculus cleanup tools will remove old files. As of v49 we made a change to update our namespaces to match the new Meta branding. This means any code references you may have had to Voice SDK will need to have their using statements updated.


## What’s New
* Out of Domain now includes Transcription
* String to String Event adds a quick utility for formatting responses from Unity events.
* What’s Fixed
* Voice SDK Hub



## Feature Documentation
### Out of Domain
Out of domain now provides the final transcription in its event. This means you can easily pass it on to a LLM for general response or to TTS via string formatting to speak an out of domain message.

Example:
![image](/images/ood.png)

### Float to String
Float to string now supports string formats in addition to float formats. You can fill out either string or float format field (both are optional) to provide additional formatting on a string that is passed from another unity event via ConvertFloatToString(float)

