# Playlist Plugin
This plugin is a list of external media that are pre-defined and setup ahead of time by the world creator.  
The playlist has two parts to it: the inspector and the template  

## You can support me here
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

## The Quick and Easy
ProTV comes with some premade setups for the Playlist plugin. These can be found in the `Assets/ArchiTechAnon/ProTV/Plugins/Playlist/Prefabs` folder. Just drag one of the prefabs into the scene and assign your desired TV to it. You can adjust various aspects of the playlists as desired.
Here is a walkthrough demostrating some of these features: 
[ProTV Playlist Setup](https://odysee.com/@architechvr:7/ProTV-Playlist-Setup-(v2.3+):b)


## The In-Depth

### Inspector - General Settings
In the playlist inspector editor, there are a handful of settings to mess with.  
The first option is the `Tv` object reference that the playlist will associate itself with. You will need to insert the desired TV reference (the game object with the `TVManagerV2` script on it), or if you are using a ready-made prefab, it should already be connected. 

The second is an optional field where you can specify a [Queue plugin](Queue.md) object that you wish to make the playlist connect with. If a Queue is provided, then instead of immediately playing the URL a user clicks on, the playlist will forward the desired URL entry into the Queue directly and the Queue then handles the rest.

Next up is the two template references, one is the container, the other is the actual template itself. These come with default references out of the box in the prebuilt plugin prefabs. Have a look at the [Templating Section](#customizing-the-layout) for more information.  

Following that is the flags section. These options are toggles to determine how the playlist handles itself.

* __`Shuffle Playlist on Load`__: Specifies whether to scramble the playlist order when the world loads or not. While the exact shuffle ordering itself isn't synced across users, this is not an issue as the playlist was designed to handle arbitrary ordering from the start.

* __`Autoplay?`__: This flag determines whether the playlist should care about autoplaying its contents at all. When enabled, the following additional options then become available:

    * __`Autoplay on Load`__: Specifies whether this playlist should attempt to start playing media automatically as soon as the first user joins into a world. This setting only affects the first user to join or if the TV is set to not sync to the owner.

    * __`Start From Random Entry`__: When enabled, the playlist will initialize at some random entry in the list. This is generally paired with the `Autoplay on Load` option.

    * __`Loop Playlist`__: Determines if the playlist should continue playing media from the start of the entries or not once it reaches the end of the list. Having this disabled is useful for playlists that handle show series or a specific sequential list that you might not want to force the users to continue watching once it finishes.

    * __`Continue From Last Known Entry`__: The playlist has an internal value that keeps track of what index it triggered last. If this is enabled, it will use that value to determine which media to play next. Otherwise the playlist will start at the very first entry.

    * __`Prioritize Playlist on Interact`__: This flag determines if the playlist should make the attached TV prioritize itself above all other connected playlists. This means that if a TV has multiple playlists associated with it, when a user interacts with a given playlist that has this flag enabled, it will force itself to the front of the line for events from the TV, which makes it able to autoplay before any other playlists might try.

* __`Show Urls in Playlist?`__: This toggle makes the playlist template skip displaying the URL for each entry. This is useful if you want to hide what domain/source of the media.

* __`Autofill Quest Urls`__: This toggle enables the autofill feature for dynamically generating alternate/quest urls for the corresponding main url.  
__NOTE__: Autofill will only happen with this flag enabled and only when clicking Import _OR_ when manually modifying a main URL field.

* __`Autofill Format`__: Text field that specifies the format for the autofill feature. It does a basic string replace where the text `$URL` is replaced with the main url value.  
__Example__: If main url is `https://youtu.be/VIDEO_ID` and format is `https://mysite.tld/?url=$URL`, the alternate url would be like `https://mysite.tld/?url=https://youtu.be/VIDEO_ID`.

&nbsp;

### Inspector - Media Entries

First is a simple helper button.
* __`Update Scene`__: This button is for manually trigger the scene to recalculate stuff for the playlist layout and content. For most actions, this is triggered automatically (like while importing a playlist), but for things like modifying the [Template](#customizing-the-layout) content, clicking this button is required to have the layout update itself.

Next, the playlist entries list has two operating modes. These modes are swapped between by the `Load Text From File` toggle.  

* __Manual Entry__  
When disabled, the playlist has various buttons for manipulating the contents of the list. The two global buttons are self-explanitory `Add Entry` and `Remove All`.  
For each entry there are `Main URL`, `Alt URL`, `Title` and `Tags` fields as well as an `Image` input.

* __Importing__  
In order to make handling large playlists less painful, ProTV comes with an option to export and import playlists into a tokenized file format.  
When `Load Text From File` is enabled, the playlist switches to the import mode. All entry manipulation actions are disabled to signify the data is sourced from an asset file, and the global buttons are replaced with a `TextAsset` field. Put the desired playlist file into this slot and click the `Import` button to update the contents of the playlist to that of the given file.

* __Exporting__  
To handle exporting, there are two convenience buttons to help out:

   * __Copy__: This button will tokenize the playlist information into a string of text and add it to your clipboard. This will allow you to paste it wherever with ease. Dump it to a text file, share it with a friend, append it to an existing playlist file, etc.  

   * __Save__: This button is only available when the `Load Text From File` option is unchecked. When you click this, it will open a save dialog where you can choose where to output the playlist content into. Once a location is chosen, the data will export and then the playlist will automatically be updated with the reference to that file in the import `TextAsset` field.  

&nbsp;

### Playlist File Formatting
Here's an example of what the tokenized format looks like:
```txt
@https://www.youtube.com/watch?v=BHuF3cUf4n8
^https://soundcloud.com/stessieofficial/together
Stessie - Together
This song is pretty cool.

@https://www.youtube.com/watch?v=gyR9bdqsv_4
#music,melodic,bouncy,upbeat
Stessie - Close To You

@https://www.youtube.com/watch?v=JWP5e6u4ryE
/Assets/CustomImages/stessie.candy.heart.png
Stessie ; BLOOD CODE - Candy Heart
```
The playlist file format is defined as follows:
* Each playlist entry is defined by the `@` symbol at the start of a line. This is immediately followed by the Main Url for that particular entry.
* On any of the following lines until the next playlist entry defined by the `@` symbol, a single line of any of the following can optionally be provided:

    * An alternate url (see the TV's definition of an alt URL) can be provided and is prefixed by a `^` symbol.  
    (_See first entry in above example_)
    * A comma `,` separated list of [tags](#tagging) can be provided and is prefixed by a _`#`_ at the start of the line. If you want to use white spaces near the commas in the file, you can. They are automatically sanitized on import.  
    (_See second entry in above example_)

    * A path to an image can be provided. This must be an image that is within the Assets folder. It is detected by the `/` prefix and the line should always start with the `/Assets/` prefix. If the image is not a `Sprite` type, it will not be able to be loaded. If it was manually typed, double check for typos. If it was exported, it should already be compatible.  
    (_See third entry in above example_)

* If any line after the playlist entry `@` line does not start with one of the above tokens (`^`, `#`, `/`) then it will assume that the rest of the content until the next detect playlist entry is part of the entry title and will be greedily added to the title text. 


### NOTE
- When working with the images for a playlist, you can attach them to a sprite atlas for more performant rendering, but depending on the number of images, it may increase the size of the world beyond a desired size. It can be useful, but be aware. See:  
https://docs.unity3d.com/2018.4/Documentation/Manual/SpriteAtlasWorkflow.html

&nbsp;

### Tagging
Playlist tagging is the way to provide extra metadata to an entry without affecting the title. Tags are a comma `,` separated list of values defined by the world creator for each entry. The actual tag values can be anything you want, but there is a special format use case built-in to facilitate playlist sorting.

### Searching
For playlist searching, there is a Prefab available containing two input fields, one for searching titles and one for searching tags. Searching goes by tags first and then filters by title search.  
Searching titles is a simple contains check, but searching by tags has some formatting available to customize the search.  
The tag search term will be split by a comma `,` symbol first. These are considered `OR` statements. Within each OR statement, the sub-term will then be split by a plus `+` symbol. These are considered `AND` statements.  
The search is resolved as such:
* For the current playlist entry...
* For each of the OR statements, evaluate each AND statement.
    * An AND statement will resolve to true if the searched tag is present in the tag list for the currently evaluating playlist entry.
* If _all_ AND statements are true, the containing OR statement resolves to true and marks the given playlist entry as `visible` in the search and moves to the next entry.
* Otherwise, if _any_ AND statements are false, the OR statement resolves to false and the next OR statement is evaluated.
* If _all_ OR statements resolve to false, the playlist entry is marked as `hidden` in the search results.
* Move to next playlist entry.

Here's an example tag search: `movie+horror+2000s,tvshow+comedy+1990s`

Another thing you can do is disable tag search gameobject and add some buttons that update the tag search input field with a premade search. This is great for doing specific custom category searches.

Finally, all searching is now done ASYNCHRONOUSLY! No more lag when trying to search for things in large playlists!


### Sorting
For playlist sorting, there is also a prefab available that comes with some premade sort terms. One thing you'll notice is that the sort term syntax is different than the search term syntax.  
The syntax is defined as `MODE:PREFIX`. The `MODE` is a number from 0 to 5 representing the following:  
* `0` - Reset the sort to the original order that the playlist was in at world load.
* `1` - Sort the playlist into a random order (aka shuffle)
* `2` - Sort the playlist by title in ascending unicode order (aka a-z)
* `3` - Sort the playlist by title in descending unicode order (aka z-a)
* `4` - Sort the playlist by the given tag value in ascending order
* `5` - Sort the playlist by the given tag value in descending order

And the `:PREFIX` is a value that is exclusively used by the `4` and `5` modes. It specifies what "category" aka prefix should be used to sort the tags by. This prefix is optionally part of the tags list and is delimited by a colon `:` symbol for each desired tag.

So for example, if we wanted to specify a playlist entry has being made in a particular year and belongs to a particular show season, our tags list might look like  
`year:1994,season:3,1990s,comedy,tvshow,etc`

This would enable the entry to be sorted by such a year value. So if multiple entries had a `year:` prefix, they would be ordered in the playlist based on that value. One with a year of 1992 would be above one with a year of 1994 for mode 4 and inversely for mode 5.

NOTE: When doing a tag _search_, the prefix is ignored completely and only looks at the value portion if the prefix is present.

Lastly, similar to searching, all sorting is done asynchronously. This means there won't be any lag, but do be aware that when sorting multiple thousands of items, the sorting logic may run for multiple minutes before completing. Work is ongoing to improve this, but for now just be aware of the general slowness of sorting massive playlists.

&nbsp;

## Customizing the Layout
You can customize the layout of the playlist quite a bit. The easiest way is to get a playlist prefab in scene, go into its hierarchy to the `ScrollView/Viewport/Template` object and fiddle around with that. There are only a few things that is checked for in the template:
* There are 4 gameobjects that are checked for as children of the Template:
    1) A gameobject called `Title` that contains an attached `Text` component
    2) A gameobject called `Url` that contains an attached `Text` component
    3) A gameobject called `Image` or `Poster` that contains an attached `Image` component
    4) A gameobject that contains an attached `Slider` component (no name requirements)
* All of these gameobjects are optional, but at minimum the `Title` and `Slider` recommended for a better user experience.

Once you have customized the structure of the template, you can also adjust the layout of the template entirely. The playlist will automatically fill in horizontal space of the viewport with the content.  
This can be observed by setting the transform setting `Anchors -> Max -> X` to 1 / the number of items per row (for example 1/5, or 0.2). Then click `Update Scene` and you will see the playlist switch to a 5 column layout. You can also adjust the template `Height` for some layout change as well if you want to display larger entries.

These two Template transform settings `Anchors -> Max -> X` and `Height` are the primary controls for getting your desired layout in the playlist.

## Pre-made prefabs

### Classic Playlist Prefab Sample
![Classic Playlist exmaple picture](./Images/ClassicPlaylistSample.png)

### Image Playlist Prefab Sample
![Image Playlist exmaple picture](./Images/ImagePlaylistSample.png)

### Flat Playlist Prefab Sample
![Flat Playlist exmaple picture](./Images/FlatPlaylistSample.png)
