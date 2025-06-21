# Changes
- ### Dip Switch Input Behavior
	- Pull-up is now detected correctly by ICs
		- Input should be between dip switch and resistor - PWR / not between dip switch and GND
		- Before this update, this was not working as intended.
		- Each IC simulation logic has been refactored for this
			- https://github.com/gisketch/vr-breadboard-sim/blob/65a204603fa22222588f5e341850cbc17abe6583/Assets/Scripts/Controllers/BreadboardSimulator.cs#L997
- ### Tutorial
	- Instruction show mouse controls
	- Student show VR controls
	- Reference: https://github.com/gisketch/vr-breadboard-sim/blob/65a204603fa22222588f5e341850cbc17abe6583/Assets/Scripts/Managers/MenuManager.cs#L114
- ### Experiments
	- Now reversible
	- Input Checking
		- Conflict message
	- Output Checking
		- Conflict Message
	- Output Evaluation
		- Tracked and still reversible
	- Progress Bar now is individually stored
		- [progress bar desync bug fix · gisketch/vr-breadboard-sim@65a2046 · GitHub](https://github.com/gisketch/vr-breadboard-sim/commit/65a204603fa22222588f5e341850cbc17abe6583)

# Detailed Experiment Changes + Refactor
- #### 74138
	- [vr-breadboard-sim/Assets/Scripts/Controllers/74138Evaluation.md at main · gisketch/vr-breadboard-sim · GitHub](https://github.com/gisketch/vr-breadboard-sim/blob/main/Assets/Scripts/Controllers/74138Evaluation.md)
	- CODE - [vr-breadboard-sim/Assets/Scripts/Controllers/Evaluate74138.cs at main · gisketch/vr-breadboard-sim · GitHub](https://github.com/gisketch/vr-breadboard-sim/blob/main/Assets/Scripts/Controllers/Evaluate74138.cs)
- #### BCD
	- [vr-breadboard-sim/Assets/Scripts/Controllers/BCDEvaluation.md at main · gisketch/vr-breadboard-sim · GitHub](https://github.com/gisketch/vr-breadboard-sim/blob/main/Assets/Scripts/Controllers/BCDEvaluation.md)
	- CODE - [vr-breadboard-sim/Assets/Scripts/Controllers/EvaluateBCD.cs at main · gisketch/vr-breadboard-sim · GitHub](https://github.com/gisketch/vr-breadboard-sim/blob/main/Assets/Scripts/Controllers/EvaluateBCD.cs)
- #### 74148
	- [vr-breadboard-sim/Assets/Scripts/Controllers/74148Evaluation.md at main · gisketch/vr-breadboard-sim · GitHub](https://github.com/gisketch/vr-breadboard-sim/blob/main/Assets/Scripts/Controllers/74148Evaluation.md)
	- CODE - [vr-breadboard-sim/Assets/Scripts/Controllers/Evaluate74148.cs at main · gisketch/vr-breadboard-sim · GitHub](https://github.com/gisketch/vr-breadboard-sim/blob/main/Assets/Scripts/Controllers/Evaluate74148.cs)

# Android Disconnection Issue

#### Problem
- App disconnects when minimized on Android devices
- Tested on some devices - works in background
- MOST devices it doesn't work - can't do shit about that

#### Technical Details
- Unity "Run in Background" is enabled: https://docs.unity3d.com/Manual/class-PlayerSettings.html
- Android Doze Mode kills background apps: https://developer.android.com/training/monitoring-device-state/doze-standby
- Manufacturer battery optimization overrides Unity settings: https://dontkillmyapp.com/
- Mirror Networking doesn't handle reconnection automatically: https://mirror-networking.gitbook.io/docs/

#### Why This Happens
- Android 6.0+ has Doze Mode that puts apps to sleep
- Manufacturers like Xiaomi, Huawei have their own battery management
- Network connections get killed at OS level
- App loses connection, no way to restore session state

#### Comparison to Facebook/Messenger
- Those apps store data on servers
- When you reconnect, they just load data from server
- Our app stores everything locally
- Would need massive refactor to add server-side state management

#### Current Limitations
- Would need complete architecture restructure
- Server-side state management implementation
- Reconnection logic with state restoration
- Way beyond scope of current revision

#### Testing Results
- Works fine on development device
- Doesn't work well on most devices
- Issue is device-specific, not code issue