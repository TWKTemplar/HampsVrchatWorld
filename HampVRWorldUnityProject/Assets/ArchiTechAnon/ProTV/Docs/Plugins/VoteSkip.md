# VoteSkip Plugin
System to allow users to collaboratively choose to skip the currently playing video

## You can support me here
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>


## The Quick And Easy
This plugin is pretty self contained. Easiest thing to do is to add the prefab into the scene and attach your desired TV to the regular tv slot.
By default, the `VoteSkip` plugin is considered global. This means that it will calculate the vote skip ratio against all players in the instance. This ratio is adjustable in game via a slider in the vote skip prefab by the instance owner.  
If you want to specify particular areas that players need to be in (such as in front of a screen) to vote skip, there is a `VoteZone` prefab you can add to the scene. It comes with a default box collider, but you can add as many colliders to that game object as you want and place them around your scene. Then simply connect the vote zone gameobject into the `zone` slot on the VoteSkip component.  
Anytime a player enters/exits a colider on the zone the VoteSkip will track the active number of people within the zone's colliders (for example multiple rooms watching the same TV on different screens) and calculate the skip ratio based on that number instead of all players in the world.

NOTE: For the VoteZone colliders, DO NOT LET THEM EGREGIOUSLY OVERLAP. This will duplicate total voter numbers while the players are in the overlapping sections. They return to normal upon leaving the overlap sections, so a small overlap isn't going to hurt much, but needs to be taken into consideration when placing the colliders.

NOTE: For the VoteZone colliders, DO NOT LET THEM OVERLAP EITHER 0,0,0 or ANY player spawn position. This can mess with the Player Trigger Enter/Exit events unexpectedly, leading to incorrect vote counts.