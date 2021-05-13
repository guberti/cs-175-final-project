# cs-175-final-project
Final project for Harvard's CS 175, by Gavin Uberti and Diego Gutierrez

For our final project we decided to learn how to use Unity by implementing chess. We learned how to program in C#, import assets from the community store, use the scene editor, spawn and destroy scene elements, link scripts to scene elements, update scene element properties, use raycasting to find where our mouse is in the scene, and a lot more.

As firm believers in “learning by doing”, we did not use Unity tutorials that discussed making a full game. Instead, we only looked up individual functions and much smaller chunks, and tried to piece together the rest ourselves. The end result may be slightly less clean as a result of this, but we believe this approach allowed us to learn far more about the engine.

Our first step in developing the game was loading assets from the community store. We got them from user Broken Vector here https://assetstore.unity.com/packages/3d/props/low-poly-chess-pack-50405. We ran into issues with getting input for which square the user was clicking because the board was one object. We fixed this by casting a ray and seeing where/if this intersected the board. Once we got this position it was fairly simple to get the selected square using the square size and the position of the board.

Next, we had to spawn the pieces. Since the number of pieces in chess is not fixed, we could not have a fixed set of 32 game pieces (e.g. what if a pawn was promoted and we needed another queen)? To work around this issue, and to make the setup cleaner in general, we instantiated the chess pieces as prefabs (though the board and end screen were just normal GameObjects). Then in our script BoardManager.cs, we instantiated the pieces as needed.

We used a similar approach for tile highlighting. We created a single tile highlight prefab, which we made a copy of on top of any squares we wished to highlight (this was used to show the possible moves of a piece). Doing it like this also allowed us to easily edit the material properties, color, glow, etc. of the tile highlight in the GUI, instead of hard-coding those factors into our script.

Next, we wished to animate our pieces. While Unity does provide GUI support for animation, we decided it would be more straightforward to use an IEnumerator - a function that uses the yield keyword to be called every iteration through the loop. To move pieces between squares, we started by using Unity’s Lerp command, which works the same way as the one we discussed in class. However, that made the pieces accelerate and decelerate instantly, which looked bad and was not realistic. As a result, we added an S-curve function to smooth out this acceleration.

We took a similar approach for the growing and shrinking animations. We didn’t want pieces to appear and disappear instantly, so we instead used exponential curves and the Lerp function to smooth this out. Our camera movement after each move behaves similarly to this as well, though is a little more sophisticated. We initially wanted to make the camera rotate smoothly around the center of the board, but decided a translate-and-twist move looked better.

Lastly, our graphics setup needed end screens to display the winner of the game when checkmate was achieved. Unity has built-in Canvas objects that serve this purpose, so we used those. We wanted the end screen to smoothly fade in however, so we added an object script to a Canvas Group. Like all our other animations, we didn’t want the fade to be strictly rigid, instead deciding it would be cool if it slowly faded out, never fully completing. To accomplish this, we used another IEnumerator with an exponential function.

Implementing the rules of chess was fairly complicated. Our original implementation involved a 8 by 8 array of Enum values which represented each piece. This implementation was slightly troublesome as we were creating multiple enum types for different states(ex. PAWN, PAWN_MOVED, PAWN_IN_PASSING). We also wanted to highlight the moveable square for a piece which we found a little difficult to do under this implementation.

We eventually transitioned to an implementation which involved a Piece class and a List<Piece> which stored the majority of the game state. Each piece had a function called getValidMoves() which returned a List<Command>. Each Command had an execute(), undo(), and check() function. The execute() and undo() functions are self explanatory. The check() function would make sure that the move was legal, it typically followed the pattern of:

execute();
bool r = inCheck();
undo();
Return r == false;

This ensured that the move wouldn’t put the player in check. When the board control would select a piece it would call the getValidMoves() function to highlight all the available squares that piece could go to legally. If the player clicked on any of those squares the move would be fetched by the square, then it would execute, the game would change the current player, and the command would be added to a stack called notation. ChessGame.undo() would have reversed this process, popping the top Command on the stack, calling Command.undo() and then switching the current side.
