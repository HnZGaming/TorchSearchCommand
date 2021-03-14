namespace SearchCommand.Core
{
    public interface IStringSearcher<K>
    {
        bool HasAnyKeywords { get; }
        string Keywords { get; }
        void AddKeyword(string keyword);
        void AddDictionaryWord(K key, string word);
        K[] OrderSimilarWords(int limit);
    }
}