using System.Collections.Generic;

namespace SH.Framework.Diagrams;

public interface IDataNode
{
    public string Id { get; }
}

public interface IDataNode<T> : IDataNode where T : class, IDataNode<T>, IDataNode
{
    public IEnumerable<T> Dependencies { get; }
}
