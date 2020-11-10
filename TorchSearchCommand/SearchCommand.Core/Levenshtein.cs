namespace SearchCommand.Core
{
    // Modified version of https://github.com/DanHarltey/Fastenshtein/blob/master/src/Fastenshtein/Levenshtein.cs
    // Original license to https://github.com/DanHarltey/Fastenshtein/blob/master/LICENSE

    /// <summary>
    ///     Measures the difference between two strings.
    ///     Uses the Levenshtein string difference algorithm.
    /// </summary>
    public sealed class Levenshtein
    {
        readonly int[] costs;
        /*
         * WARRING this class is performance critical (Speed).
         */

        public string StoredValue { get; }

        /// <summary>
        ///     Creates a new instance with a value to test other values against
        /// </summary>
        /// <param name="value">Value to compare other values to.</param>
        public Levenshtein(string value)
        {
            StoredValue = value;
            // Create matrix row
            costs = new int[StoredValue.Length];
        }

        /// <summary>
        ///     Compares a value to the stored value.
        ///     Not thread safe.
        /// </summary>
        /// <returns>Difference. 0 complete match.</returns>
        public int DistanceFrom(string value)
        {
            if (costs.Length == 0) return value.Length;

            // Add indexing for insertion to first row
            for (var i = 0; i < costs.Length;) costs[i] = ++i;

            for (var i = 0; i < value.Length; i++)
            {
                // cost of the first index
                var cost = i;
                var previousCost = i;

                // cache value for inner loop to avoid index lookup and bonds checking, profiled this is quicker
                var value1Char = value[i];

                for (var j = 0; j < StoredValue.Length; j++)
                {
                    var currentCost = cost;

                    // assigning this here reduces the array reads we do, improvement of the old version
                    cost = costs[j];

                    if (value1Char != StoredValue[j])
                    {
                        if (previousCost < currentCost) currentCost = previousCost;

                        if (cost < currentCost) currentCost = cost;

                        ++currentCost;
                    }

                    /* 
                     * Improvement on the older versions.
                     * Swapping the variables here results in a performance improvement for modern intel CPU’s, but I have no idea why?
                     */
                    costs[j] = currentCost;
                    previousCost = currentCost;
                }
            }

            return costs[costs.Length - 1];
        }
    }
}