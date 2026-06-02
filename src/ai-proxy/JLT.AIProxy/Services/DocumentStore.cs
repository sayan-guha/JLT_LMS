using System.Collections.Concurrent;

namespace JLT.AIProxy.Services;

public class DocumentStore
{
    private readonly ConcurrentDictionary<string, string> _documents = new();

    public string AddDocument(string content)
    {
        var id = Guid.NewGuid().ToString();
        _documents[id] = content;
        return id;
    }

    public string QueryDocument(string id, string query)
    {
        if (_documents.TryGetValue(id, out var content))
        {
            // For MVP, we'll just return the full text if it's small enough, 
            // or we'd need a real chunking/search strategy here.
            // Returning the first 4000 characters as a naive "chunk" approach for now.
            var resultLength = Math.Min(content.Length, 4000);
            return content.Substring(0, resultLength);
        }
        return "Document not found.";
    }

    public string? GetDocument(string id)
    {
        return _documents.TryGetValue(id, out var content) ? content : null;
    }
}
