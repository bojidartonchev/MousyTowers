Plugin Version: 2.02
Unity Version: 2017 1.*

# Smooth Sync
Performs interpolation and extrapolation in order to make your objects smooth and more accurate over the network.
Highly configurable, only send what you need. Optionally compress floats to further reduce bandwidth. 
Customizable interpolation and extrapolation settings depending on your game's needs.
Comes with a fully functional example scene.
The full source code is provided so you can see everything with detailed comments!

Supports Windows, OSX, Linux, iOS, Android, Windows Phone, Xbox, PlayStation, Nintendo. If Unity runs it, it'll run!


## Step 1 - Drag and Drop

1. Put the SmoothSync script onto any parent networked object that you want to be smoother. 
2. It will automatically sync the object it is on. 
     *In order to sync a child object, you must have two instances of 
   SmoothSync on the parent. Set childObjectToSync on one of them to point to the child you want to sync, and leave 
   it blank on the other one to sync the parent. You cannot sync children without syncing the parent.
3. It is now smoothy synced across the network. 

## Step 2 - Tweak to Your Needs

Now that it is on your networked object:

1. Read the comments corresponding to the variables to tweak the smoothness to your game's
specific needs.
2. Reduce your bandwidth by only sending position, rotation, and velocity variables that you need.


# How it Works

Unlike the NetworkTransform script provided by Unity, the SmoothSync script stores a list of network States to interpolate 
between. This allows for a more accurate and smoother representation of synced objects.