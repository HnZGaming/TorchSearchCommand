using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace SearchCommand.Search
{
    internal class StringSimilaritySearcher<K>
    {
        readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly char[] _splits = {' ', ',', '.', ':', ';', '/', '!', '?', '-'};
        readonly int _thresholdDistance;
        readonly List<string> _keywords;
        readonly Dictionary<K, List<string>> _dictionary;

        public StringSimilaritySearcher(int thresholdDistance)
        {
            _thresholdDistance = thresholdDistance;
            _keywords = new List<string>();
            _dictionary = new Dictionary<K, List<string>>();
        }

        public bool HasAnyKeywords => _keywords.Any();

        IEnumerable<string> SplitWords(string self)
        {
            return self
                .ToLower()
                .Split(_splits, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.ToLower());
        }

        public void AddKeyword(string keyword)
        {
            _keywords.AddRange(SplitWords(keyword));
        }

        public void AddDictionaryWord(K key, string word)
        {
            if (!_dictionary.TryGetValue(key, out var words))
            {
                words = new List<string>();
                _dictionary.Add(key, words);
            }

            words.AddRange(SplitWords(word));
        }

        public IEnumerable<(K Key, float Similarity)> CalcSimilarity()
        {
            var levs = new List<Levenshtein>();
            foreach (var keyword in _keywords)
            {
                var lev = new Levenshtein(keyword);
                levs.Add(lev);
            }

            var scores = new Dictionary<K, float>();
            foreach (var (key, dictionaryWords) in _dictionary)
            {
                var topScore = 0f;
                foreach (var lev in levs)
                foreach (var word in dictionaryWords)
                {
                    var score = CalcSimilarityScore(lev, word);
                    topScore = Math.Max(topScore, score);
                }

                scores[key] = topScore;
                Log.Trace($"'{string.Join(" ", _keywords)}', '{string.Join(" ", dictionaryWords)}' -> {topScore}");
            }

            return scores.Select(kv => (kv.Key, kv.Value));
        }

        float CalcSimilarityScore(Levenshtein lev, string word)
        {
            if (word == lev.StoredValue)
            {
                return _thresholdDistance;
            }

            if (word.Contains(lev.StoredValue))
            {
                return _thresholdDistance;
            }

            var distance = lev.DistanceFrom(word);
            if (distance > _thresholdDistance) return 0f; // too far

            return _thresholdDistance - distance;
        }
    }
}