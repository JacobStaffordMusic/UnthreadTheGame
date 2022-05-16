# UnthreadTheGame
Metroidvania Month Game Jam (May-June 2022)

After you clone the repository you will need to make sure a few packages are installed.

First and foremost, this project is using version 2021.3.2f1, which is currently the latest version of Unity. 

Second, after you've cloned the repository, make sure you can open the project using Unity Hub. 

If you've succeded this far, head to Window->Package Manager and proceed to make sure the following packages are installed:

Universal RP
Cinemachine
Input System (Version 1.3.0)

(all of these should already be installed)

Once these packages are confirmed installed (when you search for it, it will have a "Remove" button instead of "Install"), you can go ahead and push play and the game should work. Simply push play again to stop the gameplay. 

**IMPORTANT TIP**
All of you should change the overall color tint of Unity when in "playmode". You can do this by going to Edit->Preferences->Colors->General->Playmode Tint. Here you choose any color you want (I have green). This will show the entire Unity interface as the tint of choice whenever you are in playmode. We want this so that we can distinguish playmode vs edit mode. The reason this is important is because you CAN make changes while in playmode, however nothing in playmode will save once you exit and return to edit mode. This way you can edit the player movement on the fly. Each time you exit though all the changes will revert back to normal. We use the tint to remind us that nothing will be saved. 

If you're getting stuttering as the player moves around. Be sure to go to Edit->Project Settings->Physics2D and change Simulation Mode from Fixed Update to just Update.

To edit the player movement, click on "Player" in the Hierarchy window (usually the left most window using the base Unity layout). On the right side you'll see the Inspector window. The options in this window will change depending on whatever object you click on that is in the Hierarchy. After clicking player, you can scroll down and see each of the components the player has. Once you scroll down to see "Character Controller 2D (Script)" and "Player Movement (Script)" you have reached the right place. The movement script will show you all the settings that can be changed. Here, each one of you can tweak how movement is conducted to fit what you think is best. Once we all find one that we are comfortable with, we can save those settings and work with that from then on. 
