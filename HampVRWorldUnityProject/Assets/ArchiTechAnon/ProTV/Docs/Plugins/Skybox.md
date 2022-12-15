# Skybox Swapper Plugin

## You can support me here
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

This plugin is intended for 360 videos that are rendered in a panoramic 360 style (as opposed to Over/Under or 6GridLayout styles).  
Here is the video that is available within the demo scene https://vimeo.com/414723915. This is a good example as to the video style that the skybox material shader expects. If it is not in this style it will not render correctly, obviously.

All you need to do is to drag the SkyboxVideoScreen prefab into the scene, connect up the AVProVideoPlayer reference to the AVProVideoScreen component and the related TVManagerV2 reference to the udon behavior's TV input field.

Once it's hooked up, the skybox will be swapped to the video during media play. Anytime the video player stops or fails, it will revert the previous skybox material prior to when the video started playing.

Alternatively there is also a pre-made TV prefab with it setup already. You can drag that into the scene and modify as you need.

Here's what it can look like:  
![ProTV Skybox exmaple picture](./Images/SkyboxExample.png)

### Skybox Settings
There are additional setting for the skybox shader that can be modified via events on the SkyboxSwapper script. These include flipping the video, swapping the eyes for 3D content, changing between panoramic/cubed/halved(180) layout modes and switching between popular 3D modes.  
In order to handle the various combinations of settings automatically on a per-video basis, the plugin makes use of the TV's meta data field `urlMeta`. It will look for known tags, and then apply the respective shader settings.  

The available known tags are:
- `Panoramic`: Equirectangualr (also know as latitude/longitude) Mode
- `Cubed`: Operates similar to a Cube Map, but has specific layouts it supports
- `180`: Similar to Panoramic, but only renders the video onto one-half of the skybox, the other half renders as black.
- `NoLayout`: Means that 3D mode is disabled and renders the same thing to both eyes when in VR.
- `SideBySide`: This mode assumes that the video was rendered such that the image data for each eye was rendered between the left half and right half of the video. This form is more commonly associated with 180deg video.
- `OverUnder`: This mode assumes that the video was rendered such that the image data for each eye was rendered between the top half and bottom half of the video. This form is more commonly associated with 360deg video.

Two other settings that are available for the user is `_Flip` which flips the video vertically and `_SwapEyes` to switch which part of the video is rendered to which eye (helps when the video seems like the 3D effect isn't lining up).

All of these settings are also made available in a settings UI panel in the Skybox TV prefab.