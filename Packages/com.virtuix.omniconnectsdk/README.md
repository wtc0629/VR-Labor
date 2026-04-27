# Omni Connect SDK

Unity plugin to interface with Omni One using shared memory.

To install the plugin in:
	- Simply drop it into the 'Packages/' folder of your project
	OR
	- Install from a local folder (https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html)




OmniPlayerMovementExample.cs
This script demonstrates how to apply Omni One movement input to a player character in Unity. It is intended as a starting point for developers integrating the Omni Connect SDK into their own movement systems.

	The OmniPlayerMovementExample script shows how to:
		- Retrieve real-time movement data from the OmniConnectManager
		- Convert 2D movement input into a 3D world-space vector
		- Rotate that vector based on the player's camera yaw
		- Apply movement to a character using Transform.Translate
		- Optionally read Omni Arm rotation data for use in character animation.
		
	How to Use
	1. Attach the OmniPlayerMovementExample.cs script to a GameObject in a scene.
	2. Ensure Omni Connect (available at https://virtuix.com/pc) is installed and running
	3. Verify, in the Omni Connect application, that your Omni One device is connected to your PC.
	4. Play the Scene.
	5. Look at the "Console" in Unity. You should see a log message that is outputing the Omni Movement Vector and Omni Arm Yaw. If this data is not showing 0.00, then you are getting Omni input!
		

	Next Steps:
	For more advanced movement systems, replace Transform.Translate with a Charater Controller, Rigidbody, or NavMeshAgent. If you already have an existing movement system, integrate the 
	logic in the OmniPlayerMovementExample script into your existing script that handles player movement. Good Luck!
