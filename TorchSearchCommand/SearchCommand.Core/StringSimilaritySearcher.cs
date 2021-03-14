using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NLog;
using Utils.General;

namespace SearchCommand.Core
{
    public class StringSimilaritySearcher<K> : IStringSearcher<K>
    {
        readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly char[] _splits = {' ', ',', '.', ':', ';', '/', '!', '?', '-'};
        readonly int _thresholdDistance;
        readonly List<string> _keywords;
        readonly Dictionary<K, HashSet<string>> _dictionary;

        public StringSimilaritySearcher(int thresholdDistance)
        {
            _thresholdDistance = thresholdDistance;
            _keywords = new List<string>();
            _dictionary = new Dictionary<K, HashSet<string>>();
        }

        public bool HasAnyKeywords => _keywords.Any();
        public string Keywords => _keywords.ToStringSeq();

        IEnumerable<string> SplitWords(string self)
        {
            return self
                .ToLower()
                .Split(_splits, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.ToLower());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddKeyword(string keyword)
        {
            _keywords.AddRange(SplitWords(keyword));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddDictionaryWord(K key, string word)
        {
            if (!_dictionary.TryGetValue(key, out var words))
            {
                words = new HashSet<string>();
                _dictionary.Add(key, words);
            }

            words.UnionWith(SplitWords(word));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                var sumScore = 0f;
                foreach (var lev in levs)
                foreach (var word in dictionaryWords)
                {
                    var score = CalcSimilarityScore(lev, word);
                    score = (float) Math.Pow(score, 2);
                    sumScore += score;
                }

                scores[key] = sumScore;
                Log.Trace($"'{string.Join(" ", _keywords)}', '{string.Join(" ", dictionaryWords)}' -> {sumScore}");
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

        public K[] OrderSimilarWords(int limit)
        {
            return CalcSimilarity()
                .OrderByDescending(md => md.Similarity)
                .Where(md => md.Similarity > 0)
                .Take(limit)
                .Select(md => md.Key)
                .ToArray();
        }
    }
}