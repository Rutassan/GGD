using System.Collections.Generic;

public class Inventory
{
    public Dictionary<string, int> Resources { get; private set; }

    public Inventory()
    {
        Resources = new Dictionary<string, int>();
    }

    public void AddItem(string resourceType, int count)
    {
        if (Resources.ContainsKey(resourceType))
        {
            Resources[resourceType] += count;
        }
        else
        {
            Resources.Add(resourceType, count);
        }
    }

    public bool RemoveItem(string resourceType, int count)
    {
        if (Resources.ContainsKey(resourceType) && Resources[resourceType] >= count)
        {
            Resources[resourceType] -= count;
            if (Resources[resourceType] == 0)
            {
                Resources.Remove(resourceType);
            }
            return true;
        }
        return false; // Not enough resources or item not found
    }

    public bool HasItem(string resourceType, int count)
    {
        return Resources.ContainsKey(resourceType) && Resources[resourceType] >= count;
    }
}
