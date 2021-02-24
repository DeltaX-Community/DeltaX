using DeltaX.RealTime.Interfaces;
using System.Collections.Generic;
using System.Linq;

class TagChangeTrackerManager
{
    private List<TagChangeTracker> cacheTags = new List<TagChangeTracker>();

    public void AddTagIfNotExist(IRtConnector connector, string tagName, int tagId)
    {
        lock (cacheTags)
        {
            if (!cacheTags.Any(t => t.TagName == tagName))
            {
                var tag = connector.AddTag(tagName, tagName);
                cacheTags.Add(new TagChangeTracker(tagName, tag, tagId));
            }
        }
    }

    public int EnqueueTagsChanged()
    {
        var count = 0;
        foreach (var t in cacheTags)
        {
            if (t.IsChanged())
            {
                count += 1;
                t.AppendLastValue();
            }
        }
        return count;
    }

    public List<TagChangeTracker> GetAllTags()
    {
        return cacheTags.Where(t => t.Enable).ToList();
    }

    public TagChangeTracker GetFirst(string tagName)
    {
        return cacheTags.FirstOrDefault(t => t.Enable && t.TagName == tagName);
    }

    public List<string> GetTopics()
    {
        return cacheTags.Select(t => t.TagName).ToList();
    }
}