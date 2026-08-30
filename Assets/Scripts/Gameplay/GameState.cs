namespace SnakeSnack.Gameplay
{
    /// <summary>The five states of a game (GDD §2, §4.1 and §4.4).</summary>
    public enum GameState
    {
        /// <summary>
        /// Snake laid down, oriented, <b>still</b>. No tick is played until an applicable direction
        /// has been pressed (§4.1): nobody dies while the player reads the screen.
        /// </summary>
        Waiting,

        /// <summary>The game is running: the snake moves one cell per tick.</summary>
        Running,

        /// <summary>
        /// Paused. ⚠ No tick is played — <see cref="SnakeSnack.Rules.InputQueue.Tick"/> throws if it
        /// is called while paused, precisely so a snake cannot move out of the player's sight.
        /// </summary>
        Paused,

        /// <summary>Death against a wall or against its own body. Space restarts (§2).</summary>
        Dead,

        /// <summary>
        /// The snake fills the grid: not one free cell left, so no apple to place (§4.4).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>This state is out of human reach</b> — 312 apples on the default grid — and must
        /// exist all the same. Without it, the apple draw runs on an empty interval on the last tick
        /// of a perfect game: the game breaks or freezes, precisely in the one situation no test
        /// session will ever reach. Same screen and same one-key restart as death, with a distinct
        /// label.
        /// </remarks>
        Won
    }
}
