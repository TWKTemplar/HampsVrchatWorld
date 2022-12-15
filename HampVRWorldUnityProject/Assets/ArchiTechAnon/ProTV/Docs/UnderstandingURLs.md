# Understanding Urls

## You can support me here
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Support me at ko-fi.com' /></a>

## In VRChat...

All video players, generally speaking, work on Quest. Just gonna get that answer out of the way.

What _doesn't_ always work on Quest is the _urls_ themselves. There are a few points to make regarding this:

1) The urls that people are most used to aren't actually urls for _media_ but for _websites_. Most commonly is Youtube, which is mostly in the form of `https://youtube.com/watch?v=VIDEOID`. This url is a website url, not a media (aka direct video) url. Video players do not know how to read a website url, so they would normally fail.  
BUT! VRChat has a trick up its sleeve to handle this. They use a command line tool called [YouTubeDownLoad(YTDL)](https://github.com/yt-dlp/yt-dlp/releases/latest). What this does is fetches the content of the webpage url, and fishes around that content until it finds a url that is a valid media url, then spits that back to VRChat so the program can tell the desired video player (either UnityVideoPlayer or AVPro) to play the direct url.

2) Due to technical limitations beyond the VRChat dev's reach, YTDL is unable to be used natively on the Quest platform. Only PC has access to this tool currently. This means that when a user tries to play a generic youtube video using the common shortform that you see in the browser URL bar... it's not going to work.  
There _are_ work arounds for this problem... but the free options are generally unreliable and the other options require some level of technical skill and money to accomplish.

3) The first workaround option we have is using the YTDL tool externally to VRChat on a PC. What this does is gives you the direct media url which you can then enter into the video player which will then sync to Quest users and they'll able to see it at that point. There are many drawbacks to this method.
    * First is that it requires an external computer to run the YTDL program in order to parse the website url into the longer-form media url.
    * Second is that for pretty much all large-scale media hosting platforms (such as youtube/video/dailymotion/etc) embeds a number of various parameters within the long-form urls, one of which is an expiration timestamps. So once the expiration timestamp has passed, the URL is no longer valid an will no longer load the video. This means that it is not realistic to put the URLs into the world at build time, as they would eventually not work. This also makes it difficult to simply share streams as well because most of those urls expire after an hour or two.

4) The second workaround is to use a custom server external to VRChat that is exposed to the internet which runs the YTDL tool for the user and then returns the media url. This helps because each time the URL is fetched, it has a new timestamp in it, so it can work for extended periods of time. The difficult thing is that it does require server admin knowledge if you set it up on your own. 
    * There is a service that does this publically, referred to as "Jinnai". This service is quite popular to use, but is generally unreliable due to it's overuse. Videos tend to fail 50% of the time.
    * There is an open source option I am developing called [Vroxy](https://github.com/techanon/vroxy) if you want to host your own. It even includes setup and update scripts in the repo for any Debian-based OS (like Ubuntu) to make setup a breeze.
    * One thing to note about using a server proxy like the above two, is if there is too much suspicious traffic from the server's IP, youtube/twitch/vimeo/etc can just rate-limit or straight up block the server's IP causing url resolution to fail.

3) The final workaround is much more involved as it involves hosting the videos yourself from a custom server. You'd basically use YTDL to grab the video itself, then put it on a server you have and have people access the URL directly to your server instead of the mainstream hosting platforms. This is oviously an advanced method that the average world creator is not going to do.

The easiest and most reliable option currently is to use the Jinnai service, even with its instability. Though if you are not shy of doing a bit of server stuff, deploying an instance of Qroxy will generally make the urls more reliable.

All this doesn't even go into the actual media content, like codecs and formats, which can also contribute to video failures. But that's for another time.

TL;DR: Youtube on Quest is not simple.

&nbsp;

## In ProTV...

Putting aside the above URL limitations on Quest, URLs in the scope of ProTV have a few extra features available to them.

These features are triggered by way of url hash params. A url hash is the part of the url that comes after the optional hash `#` symbol.
ProTV extracts the data in the hash part of the URL and splits the data by semicolons `;`. This is the delimiter that separates each of the 'hash params'. Each of those params has a key and an optional value separated via `=`, similar to a query param.

The list of currently available param keys are:
* `start`
    * Defines what timestamp the TV should treat as the beginning of the media, measured in seconds.
    * A side effect of this param is that the MediaControls plugin will have the left edge of the seek slider be that timestamp and the media  duration will be calculated as having the time prior to the start time not being included.
    * In effect, this parameter makes the TV _truncate_ the media prior to the defined time.
* `end`
    * Similar to the `start` param, it defines what timestamp the TV should treat as the ending of the media, measured in seconds.
    * Again, it makes the TV _truncate_ the media, but after the defined time.
* `t`
    * Specifies what timestamp the media should start playing from.
    * Useful when you have a long media and you just want to start watching from a specific timestamp.
    * Compatible with youtube's url share format via backwards compatibility with query params.
* `loop`
    * Tells the TV to repeat the current media some number of times after the media reaches the end.
    * `0` or if the parameter is not present, no looping occurs.
    * `-1` or if the parameter is present without a value, looping occurs indefinitely.
    * For any positive number above `0`, the TV will loop the media that many times before moving on
* `retry`
    * Tells the TV to reload the media _upto_ a given number of times if the media errors out.
    * `0` or if the parameter is not present, no reloading occurs. The error flag `errorOccurred` is set and the TV moves on.
    * `-1` of if the parameter is present without a value, reloading will occur indefinitely. Useful for permanent livestream links.
    * For any positive number above `0`, the TV will reload the media on error that many times before setting `errorOccurred` and moving on.
* `live`
    * An optional param to flag a url explicitly as expected to be a stream of some kind.
    * No value is used for this param.
    * This doesn't have any internal use for handling media, but it does help with error messaging when the stream goes down or is offline.
    * When a url fails to load in the first place (eg: stream is offline), it sends Udon a generic InvalidURL error. When the live param is set, it will tell the TV and plugins to handle the error in a slightly different manner, such as producing context relevant error messaging to let the user know that the stream _actually is_ offline instead of it just being a bad url.

* There are 6 params specific to the Skybox plugin: `Panoramic`, `Cubed`, `180`, `NoLayout`, `SideBySide` and `OverUnder`
    * These can be learned about in the Skybox README file.

To use these params looks something like this: `https://youtube.com/watch?v=r2jgQuOmO48#start=419;end=617`  
You'll notice the syntax: a `#` followed by the first param key, it's corresponding value, then the delimiter and another parameter/value set.
Another example: `rtspt://stream.vrcdn.live/live/vrcdn#live;retry`  
Here you see the `live` param to declare it's expected to be a live media and the `retry` param with no value specifying that it should continually recheck for this URL. This use case is common for things like clubs or cafes that want a specific url to always be checked for.