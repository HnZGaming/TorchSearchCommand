using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Utils.General;

namespace SearchCommand.Core
{
    public sealed class RegexStringSearcher<K> : IStringSearcher<K>
    {
        readonly Dictionary<string, Regex> _keywords;
        readonly Dictionary<K, HashSet<string>> _dictionary;

        public RegexStringSearcher()
        {
            _keywords = new Dictionary<string, Regex>();
            _dictionary = new Dictionary<K, HashSet<string>>();
        }

        public bool HasAnyKeywords => _keywords.Any();
        public string Keywords => _keywords.Keys.ToStringSeq();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddKeyword(string keyword)
        {
            _keywords.Add(keyword, new Regex(keyword));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddDictionaryWord(K key, string word)
        {
            if (!_dictionary.TryGetValue(key, out var words))
            {
                words = new HashSet<string>();
                _dictionary.Add(key, words);
            }

            words.Add(word);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public K[] OrderSimilarWords(int limit)
        {
            var matched = new HashSet<K>();

            foreach (var (k, words) in _dictionary)
            {
                if (IsMatchAny(words))
                {
                    matched.Add(k);
                }
            }

            return matched.Take(limit).ToArray();
        }

        bool IsMatchAny(IEnumerable<string> words)
        {
            foreach (var word in words)
            {
                foreach (var (_, regex) in _keywords)
                {
                    if (regex.IsMatch(word))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}