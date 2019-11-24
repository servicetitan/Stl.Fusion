using System.Collections.Generic;
using System.Collections.Immutable;
using Stl.Collections;
using Stl.Text;

namespace Stl.ImmutableModel.Indexing
{
    public interface IHasChangeHistory
    {
        (object? BaseState, object? CurrentState, IEnumerable<(Key Key, DictionaryEntryChangeType ChangeType, object? Value)> Changes) GetChangeHistory();
        void DiscardChangeHistory();
    }

    public interface IHasChangeHistory<T> : IHasChangeHistory
    {
        new (object? BaseState, object? CurrentState, ImmutableDictionary<Key, (DictionaryEntryChangeType ChangeType, T Value)> Changes) GetChangeHistory();  
    }
}
