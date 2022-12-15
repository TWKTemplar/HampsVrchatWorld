# AudioLinkAdapter Plugin for ProTV

### Instructions
Prerequisites:
- AudioLink 0.2.8 or later 0.2.x version (Earlier versions will _not_ properly detect the AudioLink asset!)

Steps:
- Add the AudioLinkPlugin prefab from this folder into the scene, doesn't matter where.
- In the inspector, connect up your desired ProTV and AudioLink instances into the respective reference slots.
- In the ProTV hierarchy, decide what speaker you want to be used by AudioLink. The example scene uses an existing speaker named "RightFront" for it. So you can do that or make a separate speaker if you want:  
![AudioLink specific speakers example](./Images/AudioLinkSpeakers.png)
- If you make a new speaker, do the same for all video player options you want the TV to connect with AudioLink.
    - Also, be sure to add it into the managed speakers array on the VideoManager node.
- If you want AudioLink to react to audio around the world even if you can't hear it (being outside of a volumetric audio area) ensure that the desired AudioLink audio sources are set to 2D:  
![2d audio slider example](./Images/2DAudio.png)
- Make sure that the game objects for each of the speakers being used for audio link have the EXACT same name. Then in the AudioLinkPlugin prefab in scene, make sure the `Speaker Name` field is the same.  
- Build & Test to make sure everything works.