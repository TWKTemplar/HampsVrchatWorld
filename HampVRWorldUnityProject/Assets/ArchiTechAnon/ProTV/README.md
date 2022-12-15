# ArchiTechAnon ProTV Asset

## SUPPORT ME!
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Support me at ko-fi.com' /></a>

## BEFORE IMPORTING PROTV YOU MUST:
- Ensure latest VRCSDK3 (Udon) is imported (last tested with v2022.06.03.00.03)
- Ensure you have reloaded the SDK plugins (DO THIS TO AVOID ISSUES WITH URL INPUT FIELDS)
    - Open the VRCSDK unity menu and select `Reload SDK`
    - If the VRCSDK menu isn't available, then right click on the `Assets/VRCSDK/Plugins` folder and select `Reimport` (Do _NOT_ select `Reimport All`)
- Ensure latest UdonSharp version is imported (last tested with [v0.20.3](https://github.com/vrchat-community/UdonSharp/releases/download/0.20.3/UdonSharp_v0.20.3.unitypackage) & v1.X ala VCC)

- IMPORTANT NOTE: It is only recommended to import ProTV into a _brand new_ project when using UdonSharp 1.x. There are known issues with nested prefabs when UdonSharp does it's auto-upgrade-on-import thing.
- If you _must_ upgrade an existing project, prior to begining the upgrade, either delete all ProTV stuff from the scene OR you can try unpacking all ProTV prefabs in the scene. That should allow UdonSharp to upgrade ProTV udon stuff cleanly.

## Basic Usage
- Drag a ProTV prefab (located at `Assets/ArchiTechAnon/ProTV/Prefabs`) into your scene wherever you like, rotate in-scene and customize as needed.
- You can find plugins to customize your ProTV with in the `Assets/ArchiTechAnon/ProTV/Plugins/*/Prefabs` folders.

You can find more about the ready-made TVs in the [`Prefabs Document`](./Docs/Prefabs.md).

## Features
- Full media synchronization (play/pause/stop/seek/loop)
- Resiliant and automatic sync correction for both Audio/Video and Time sync
- Sub-second sync delta between viewers
- Automatic ownership management
- Local only mode, for TVs that need to operate independently for all users.
- Media resync and reload capability
- 3D/2D audio toggle
- Near frame-perfect media looping (audio looping isn't always frame-perfect, depends on the media's codec)
- Media autoplay URL support
- Media autoplay delay offsets which help mitigate ratelimit issues with multiple TVs
- Media url hash params support (t/start/end/loop/live/retry) (see [Understanding Urls](./Docs/UnderstandingURLs.md))
- Video player swap management for multiple video player configurations
- Pub/Sub event system for modular extension
- Instance owner/master/whitelist locking support (master control is configurable, instance owner is always allowed)

## Core Architecture
In addition to the standard proxy controls for video players (play/pause/stop/volume/seek/etc), the two main unique driving factors that the core architecture accomplishes is event driven modularity as well as the multi-configuration management/swap mechanism.

ProTV has been architected to be more modular and extensible. This is done through a pseudo pub/sub system. In essence, a behavior will pass its own reference to the TV (supports all udon compilers) and then will receive custom events (see the [`Events Document`](./Docs/Events.md)) based on the TV's activity and state. The types of events directly reflect the various supported core features of the TV, such as the standard video and audio controls, as well as the video player swap mechanism for managing multiple configurations.

More details about the core architecture can be found in the [`Architecture Document`](./Docs/Architecture.md).  
Details for ready-made plugins for the TV can be found in the [`Plugins Document`](./Docs/Plugins.md).  

## Core Settings

- __`Video Managers`__  
The list of video managers (VideoManagerV2) that the TV will operate with.

### Autoplay Settings
- __`Autoplay URL`__  
Pretty straight forward. This field is a place to put a media url that you wish to automatically start upon first load into the world. This only is true for the instance owner (aka master) upon first visit of a new instance (which is then synced to any late-joiners). Leave empty to not have any autoplay.

- __`Autoplay URL Alt`__  
This field is the complementary url field to the above one. Quest users default to using the alternate url, but will fall back to the main url if it's not present. Leave both this and the above field empty to not have any autoplay.

- __`Autoplay Label`__  
If you want your autoplay urls to not display the domain name, you can specify a custom text to be made available to UIs instead.

### Default TV Settings

- __`Initial Volume`__  
Pretty straight forward. This specifies what the volume should be for the TV at load time between 0% and 100% (represented as a 0.0 to 1.0 decimal)

- __`Initial Player`__  
This integer value specifies which index of the related _Video Managers_ array option the TV should start off with. The array of video managers is 0-index based. This means that if you have, say, 3 video players in the list, if you wanted to have the TV default to the second video manger in that list, this option would need the value of `1` in it. If you only have one video manager, then set this option to `0`.

- __`Start With 2D Audio`__  
Flag to specify if the TV should initialize with stereo audio mode or full spacial audio mode.

### Sync Options

- __`Sync To Owner`__  
This setting determines whether the TV will sync with the data that the owner delivers. Untick if you want the TV to be local only (like for a music player or something).

- __`Automatic Resync Interval`__  
This option defines how many seconds to wait before attempting to resync. This affects two separate things. 
    - First is the Audio/Video sync. This particularly affects AVPro for long form videos (like movies) where after some time or excessive CPU hitching (such as repeated alt-tabbing) can cause the audio to eventually become out-of-sync with the video (usually it's the video that falls behind).
    - Second is the Owner sync. This is where the owner of the current video determines the target timestamp that everyone should be at. ProTV has a loose owner sync mechanism where it will only try to enforce sync at critical points (like switching videos, the owner seeks to a new time in the video, owner pauses the video for everyone, etc).
    
    The defaule value of 300 seconds is usually good enough for most cases, and shouldn't need to be changed unless for a specific need.

- __`Paused Resync Threshold`__  
This is more so a visual feedback feature where if the video is paused locally but the video owner is still playing, the video will update the current playback timestamp every some number of seconds determined by this option. If you don't want the video to resync while paused, set this value to `Infinity` (yes the actual word) to have it never update until the user resumes the video.
One way to view it is as a live slideshow of what is currently playing. This is intended to allow people to see what is visible on the TV without actually having the media actively running.  

- __`Sync Video Player Selection`__  
This setting, when combined with `Sync To Owner` will restrict the video player swap mechanism to the owner, and sync the active video player selection to other users.

### Media Load Options

- __`Play Video After Load`__  
This setting determines whether or not the media that has been loaded will automatically start to play, or require manually clicking the play button to start it. This affects all video loads, including any autoplay media.

- __`Buffer Delay After Load`__  
If you wish to have the TV implicitly wait some period of time before allowing the media to play (eg: give the media extra time to buffer)  
Note: There will always be a minimum of 0.5 seconds of buffer delay to ensure necessary continuity of the TV's internals.

- __`Max Allowed Loading Time`__  
This specifies the maximum time that a media should be allowed to take to load. If this time is exceeded, the media error event will be triggered with a `VideoError.PlayerError`.

## Security Options

- __`Allow Master Control`__  
This is a setting that specifies whether or not the instance master is allowed to lock down the TV to master use only. This will prevent all other users from being able to tell the TV to play something else. NOTE: The instance _Owner_ will always have access to take control no matter what.

- __`Locked By Default`__  
This is a setting that specifies whether or not the TV is master locked from the start. This only affects when the first user joins the instance as master. The locked state is synced for non-owners/late-joiners after that point.

## Error/Retry Options

- __`Default Retry Count`__  
This is the number of times a URL should be implicitly reloaded if the media fails to load. If the URL specifies an explicit `retry=X` hash param value, it will be used instead.

- __`Repeating Retry Delay`__  
This value specifies how long to wait between each retry when a URL fails to load. This only applies to multiple sequential failures after the first retry attempt.

- __`Retry Using Alternate Url`__  
This flag makes the TV attempt to load the alternate (or main if on quest) URL if the main (or alternate if on quest) URL fails to load the first time. It will only attempt it if the other URL is present and different than the current one. Because it has graceful fallback logic, this feature is enabled by default.

### Misc Options

- __`Start Hidden`__  
This toggle specifies that the active video manager should behave as if it was manually hidden. This primarily helps with music where you might not want the screen visible by default even though the media is playing.

- __`Start Disabled`__  
This toggle makes it so the TV disables itself (via SetActive) after it has completed all initialization.  

- __`Debug`__  
This enables all logging statements to be printed to the console. Disabling this does NOT prevent warnings or errors from printing.


## Caveats
- General reminder: Not all websites are supported, especially those that implement custom javascript media players. Sometimes the player is able to resolve those to a raw media url. Feel free to see what works.

- Due to a limitation in Udon, I cannot completely remove the directionality of the default speakers when switching from 3D audio to 2D audio. If that limitation is lifted, I will fix that.

- Quest does not have access to the YoutubeDL tool that desktop uses for resolving youtube/twitch/etc urls, due to technical limitations (not VRC devs fault). Read more in the [Understanding Urls](./Docs/UnderstandingURLs.md) documnet.
    - *Be aware* that all youtube and twitch long-form urls have an embedded expiration for that url. This means that when you put the long-url media into the player, anytime someone tries to (re)load the media (such as a late joiner), if the expiration has passed the media won't load. You'll need to refresh the URL every like 15 minutes or so.  
    Not all sites have that, but it is known that youtube and twitch both implement the expiry limitation in their direct urls.  

- There is a known issue with _low-latency mode_ on AMD GPUs (specifically older ones) where if an HLS livestream (like twitch) is sending too much data (such as a 1080p stream), there is significant artifacting and flickering that will likely occur. You can either try a lower resolution videoplayer configuration or have a player dropdown option for a non-low-latency configuration.
