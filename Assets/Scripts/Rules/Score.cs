namespace SnakeSnack.Rules
{
    /// <summary>
    /// The score of the current game and the best of every game (GDD §4.5).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The best score rises DURING the game</b>, as soon as the current score passes it — not
    /// on death. The score is monotonically increasing: waiting for the end would display a best
    /// score lower than the score shown right next to it, which reads as a display defect rather
    /// than a rule. And the best score of a tab closed mid-game would be lost.
    ///
    /// <para>⚠ <b>"Best beaten" is judged against the best score from BEFORE the game</b>, never
    /// against <see cref="Best"/>: that one has just been raised by the current score, so comparing
    /// them would always be false. It is this predicate that triggers the "new best" line on the end
    /// screen, without which two equal numbers side by side look like a bug (§4.5).</para>
    ///
    /// <para>Persistent reading and writing live outside <c>Rules/</c>: this class receives the
    /// known best score at construction and has no idea where it came from.</para>
    /// </remarks>
    public sealed class Score
    {
        private int _points;
        private int _best;
        private int _bestBeforeTheGame;

        /// <param name="knownBest">
        /// Best score read at startup. ⚠ <b>Normalised, never rejected</b>: the game must not refuse
        /// to start over a counter (§4.5), and in WebGL that storage can disappear or come back
        /// damaged.
        /// </param>
        public Score(int knownBest = 0)
        {
            _best = NormaliseBest(knownBest);
            _bestBeforeTheGame = _best;
            _points = 0;
        }

        /// <summary>Apples eaten in the current game, +1 per apple, nothing else (§4.5).</summary>
        public int Points
        {
            get { return _points; }
        }

        /// <summary>The highest score ever reached, current game included.</summary>
        public int Best
        {
            get { return _best; }
        }

        /// <summary>
        /// True as soon as the current game has passed the best score it found when it started.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Matching the best score does not beat it.</b> A player who exactly repeats their
        /// personal best does see two identical numbers, with no "new best" line: they beat nothing.
        /// This is the only case where the two numbers being equal is not the mark of a fresh best,
        /// and it is written on purpose rather than deduced from a comparison with
        /// <see cref="Best"/>.
        /// </remarks>
        public bool BestBeaten
        {
            get { return _points > _bestBeforeTheGame; }
        }

        /// <summary>
        /// Resets the score to zero for a new game. The best score survives.
        /// </summary>
        public void NewGame()
        {
            _points = 0;
            _bestBeforeTheGame = _best;
        }

        /// <summary>
        /// Counts one apple eaten.
        /// </summary>
        /// <returns>
        /// True if the best score has just gone up a notch — that is the signal which triggers the
        /// persistent write, and it is returned here so the caller does not have to compare the best
        /// score with a previous value it would have had to remember on its own side.
        /// </returns>
        public bool CountApple()
        {
            _points++;

            if (_points > _best)
            {
                _best = _points;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The usable best score from a value that came out of storage.
        /// </summary>
        /// <remarks>
        /// ⚠ A missing or damaged best score <b>restarts from zero with no blocking error</b>
        /// (§4.5). A negative value is not a possible score: it comes from corrupted storage or from
        /// a key written by something else, and letting it through would show "Best -1" on screen.
        /// </remarks>
        public static int NormaliseBest(int value)
        {
            return value < 0 ? 0 : value;
        }

        /// <summary>
        /// Snake length for this score (§4.5: length equals <c>3 + score</c>).
        /// </summary>
        /// <remarks>
        /// That equality is the reason the game does <b>not</b> display length: it would be a second
        /// number to read for the same information. It is written here so it can be checked by a
        /// test rather than recalled in a comment.
        /// </remarks>
        public static int SnakeLength(int points)
        {
            return Grid.InitialLength + NormaliseBest(points);
        }
    }
}
