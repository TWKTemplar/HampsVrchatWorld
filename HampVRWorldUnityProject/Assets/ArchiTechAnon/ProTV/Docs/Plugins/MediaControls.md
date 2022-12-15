# MEDIA CONTROLS PLUGIN
This plugin is a composition of various controls that you can manipulate the TV with. This plugin is intended to be very versitile
by allowing you to pick and choose which parts of it are used.

## You can support me here
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

### Advanced Controls Prefab
This plugin is uses the most features of MediaControls out of all of the prefabs.

The features of this plugin are:
- Standard Play/Pause/Stop buttons
- Volume control via slider and mute button
- Media owner and custom title display (falls back to url's domain name)
- Media error display with helpful info
- Media seeking via slider and adjustment buttons (10 second increments)
- URL input field with a GO button (keyboard enter button not required)
- 3D/2D toggle for positional/stereo audio swapping
- Media Resync and Reload buttons
- Dropdown menu for managing the video player configuration swapping
- Timestamps for the current playing time and the ending time

This plugin doesn't do anything too fancy other than managing the visual state of its components to reflect the TV's current activity.

Note: If an error occurs, there will be some useful text that is displayed. If there is a PlayerError, it's likely that bad data was returned when trying to resolve the URL. Also double check that you didn't accidentally type extra characters into the text field after pasting. Such an input error has produced both InvalidURL errors and PlayerError errors during tests.

Here's what it looks like:  
![Advanced Controls exmaple picture](./Images/GeneralControlsSample.png)

### Standard Controls Prefab
This prefab is the one that most people are familiar with for ProTV.
Compared to the Advanced Controls, it has removed the following interactions:
- 3D/2D audio swap
- Reload button
- Resolution swap
- Loop toggle
- Seek increment/decrement
- Mute toggle (volume slider remains though)

Here's what it looks like:  
![Standard Controls exmaple picture](./Images/SlimControlsSample.png)

### Mini Controls Prefab
This plugin is a variation on the regular Slim Controls.  
It has further removed the following:
- URL input
- Master Lock Toggle
- Info display

This is intended to make it like a simple music player control or as a supplement to another set of controls that has more features available.

Here's what it looks like:  
![Mini Controls exmaple picture](./Images/MinifiedControlsSample.png)

### Vert Controls Prefab
This plugin is a variation on the regular Slim Controls, but with a vertical orientation.  
It has further removed the following:
- URL Input
- Info display

Here's what it looks like:  
![Vert Controls exmaple picture](./Images/VertControlsSample.png)

---
## Micro Controls
If you want to add a single purpose control to your scene (maybe like an info display at the front of a theater as an example) the prefabs in the `ProTV/Plugins/MediaControls/Prefabs/Micro` folder can help. They all use the same controls logic as the composite controls, but they are split into individual pieces that you can put into the world as needed.

## Retro Controls
If you want an alternate UI themed in a 2000's retro style, look in the Retro prefabs folder of this plugin.

#### **NOTE**
It it HIGHLY recommended to use composite controls as for each behavior that you attach to the TV it will take a bit longer for each event that is called. Only attach as many plugins as necessary. You can create separate canvases as desired and spread them across the world then connect it all into a single or just a few controls scripts easily.

### One-Off Play Button
There is a special prefab called `ProTV/Plugins/MediaControls/Prefabs/Micro/PlayURL (Micro).prefab` that has it's own script to play a predetermined URL with optional title. This prefab comes with both `Interact` and `UI pointer` options. By default it uses the UI pointer (VRC blue laser) for it's activation, but you can enable the Interact mode (the blue outline with tooltip text) by simply removing the VRCUIShape component from the prefab (no unpacking required). This will enable the collider to act as a well-known interact button.

