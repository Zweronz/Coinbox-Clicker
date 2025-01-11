using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;

public class WeightedList<T> : IEnumerable<T>
{
    public WeightedList(Random rand = null)
    {
        this.rand = rand ?? new Random();
    }

    public WeightedList(ICollection<WeightedListItem<T>> listItems, Random rand = null)
    {
        this.rand = rand ?? new Random();

        foreach (WeightedListItem<T> item in listItems)
        {
            list.Add(item.item);
            weights.Add(item.weight);
        }
            
        Recalculate();
    }

    public WeightErrorHandlingType BadWeightErrorHandling { get; set; } = WeightErrorHandlingType.SetWeightToOne;

    public T Next()
    {
        if (Count == 0) 
        {
            return default;
        }

        int nextInt = rand.Next(Count);

        if (areAllProbabilitiesIdentical)
        {
            return list[nextInt];
        }

        int nextProbability = rand.Next(totalWeight);
        return (nextProbability < probabilities[nextInt]) ? list[nextInt] : list[alias[nextInt]];
    }

    public T NextThenRemove()
    {
        if (Count == 0) 
        {
            return default;
        }

        int nextInt = rand.Next(Count);

        if (areAllProbabilitiesIdentical)
        {
            return list[nextInt];
        }

        int nextProbability = rand.Next(totalWeight);

        if (nextProbability < probabilities[nextInt])
        {
            T t = list[nextInt];
            list.RemoveAt(nextInt);

            Recalculate();

            return t;
        }
        else
        {
            T t = list[alias[nextInt]];
            list.RemoveAt(alias[nextInt]);

            Recalculate();

            return t;
        }
    }

    public void AddWeightToAll(int weight)
    {
        if (weight + minWeight <= 0 && BadWeightErrorHandling == WeightErrorHandlingType.ThrowExceptionOnAdd)
        {
            throw new ArgumentException($"Subtracting {-1 * weight} from all items would set weight to non-positive for at least one element.");
        }

        for (int i = 0; i < Count; i++)
        {
            weights[i] = FixWeight(weights[i] + weight);
        }

        Recalculate();
    }

    public void SubtractWeightFromAll(int weight) => AddWeightToAll(weight * -1);

    public void SetWeightOfAll(int weight)
    {
        if (weight <= 0 && BadWeightErrorHandling == WeightErrorHandlingType.ThrowExceptionOnAdd)
        {
            throw new ArgumentException("Weight cannot be non-positive.");
        }

        for (int i = 0; i < Count; i++)
        {
            weights[i] = FixWeight(weight);
        }
            
        Recalculate();
    }

    public int TotalWeight => totalWeight;

    public int MinWeight => minWeight;

    public int MaxWeight => maxWeight;

    public IReadOnlyList<T> Items => list.AsReadOnly();

    public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

    public void Add(T item, int weight)
    {
        list.Add(item);
        weights.Add(FixWeight(weight));

        Recalculate();
    }

    public void Add(ICollection<WeightedListItem<T>> listItems)
    {
        foreach (WeightedListItem<T> listItem in listItems)
        {
            list.Add(listItem.item);
            weights.Add(FixWeight(listItem.weight));
        }

        Recalculate();
    }

    public void Clear()
    {
        list.Clear();
        weights.Clear();

        Recalculate();
    }

    public void Contains(T item) => list.Contains(item);

    public int IndexOf(T item) => list.IndexOf(item);

    public void Insert(int index, T item, int weight)
    {
        list.Insert(index, item);
        weights.Insert(index, FixWeight(weight));

        Recalculate();
    }

    public void Remove(T item)
    {
        int index = IndexOf(item);
        RemoveAt(index);

        Recalculate();
    }

    public void RemoveAt(int index)
    {
        list.RemoveAt(index);
        weights.RemoveAt(index);

        Recalculate();
    }

    public T this[int index] => list[index];

    public int Count => list.Count;

    public void SetWeight(T item, int newWeight) => SetWeightAtIndex(IndexOf(item), FixWeight(newWeight));

    public int GetWeightOf(T item) => GetWeightAtIndex(IndexOf(item));

    public void SetWeightAtIndex(int index, int newWeight)
    {
        weights[index] = FixWeight(newWeight);
        Recalculate();
    }

    public int GetWeightAtIndex(int index) => weights[index];

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("WeightedList<");
        sb.Append(typeof(T).Name);

        sb.Append(">: TotalWeight:");
        sb.Append(TotalWeight);

        sb.Append(", Min:");
        sb.Append(minWeight);

        sb.Append(", Max:");
        sb.Append(maxWeight);

        sb.Append(", Count:");
        sb.Append(Count);

        sb.Append(", {");

        for (int i = 0; i < list.Count; i++)
        {
            sb.Append(list[i].ToString());
            sb.Append(":");

            sb.Append(weights[i].ToString());

            if (i < list.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append("}");

        return sb.ToString();
    }

    private readonly List<T> list = new List<T>();

    private readonly List<int> weights = new List<int>();

    private readonly List<int> probabilities = new List<int>();

    private readonly List<int> alias = new List<int>();
        
    private readonly Random rand;

    private int totalWeight;

    private bool areAllProbabilitiesIdentical = false;

    private int minWeight;
        
    private int maxWeight;

    private void Recalculate()
    {
        totalWeight = 0;
        areAllProbabilitiesIdentical = false;

        minWeight = 0;
        maxWeight = 0;

        bool isFirst = true;

        alias.Clear();
        probabilities.Clear();

        List<int> scaledProbabilityNumerator = new List<int>(Count);

        List<int> small = new List<int>(Count);
        List<int> large = new List<int>(Count);
            
        foreach (int weight in weights)
        {
            if (isFirst)
            {
                minWeight = maxWeight = weight;
                isFirst = false;
            }

            minWeight = (weight < minWeight) ? weight : minWeight;
            maxWeight = (maxWeight < weight) ? weight : maxWeight;

            totalWeight += weight;
            scaledProbabilityNumerator.Add(weight * Count);

            alias.Add(0);
            probabilities.Add(0);
        }

        if (minWeight == maxWeight)
        {
            areAllProbabilitiesIdentical = true;
            return;
        }

        for (int i = 0; i < Count; i++)
        {
            if (scaledProbabilityNumerator[i] < totalWeight)
            {
                small.Add(i);
            }
            else
            {
                large.Add(i);
            }
        }

        while (small.Count > 0 && large.Count > 0)
        {
            int l = small[^1];
            small.RemoveAt(small.Count - 1);

            int g = large[^1];
            large.RemoveAt(large.Count - 1);

            probabilities[l] = scaledProbabilityNumerator[l];
            alias[l] = g;

            int tmp = scaledProbabilityNumerator[g] + scaledProbabilityNumerator[l] - totalWeight;
            scaledProbabilityNumerator[g] = tmp;

            if (tmp < totalWeight)
            {
                small.Add(g);
            }
            else
            {
                large.Add(g);
            }
        }

        while (large.Count > 0)
        {
            int g = large[^1];
                
            large.RemoveAt(large.Count - 1);
            probabilities[g] = totalWeight;
        }
    }

    internal static int FixWeightSetToOne(int weight) => (weight <= 0) ? 1 : weight;

    internal static int FixWeightExceptionOnAdd(int weight) => (weight <= 0) ? throw new ArgumentException("Weight cannot be non-positive") : weight;

    private int FixWeight(int weight) => (BadWeightErrorHandling == WeightErrorHandlingType.ThrowExceptionOnAdd) ? FixWeightExceptionOnAdd(weight) : FixWeightSetToOne(weight);
}

public readonly struct WeightedListItem<T>
{
    internal readonly T item;

    internal readonly int weight;

    public WeightedListItem(T item, int weight)
    {
        this.item = item;
        this.weight = weight;
    }
}

public enum WeightErrorHandlingType
{
    SetWeightToOne,
    ThrowExceptionOnAdd,
}