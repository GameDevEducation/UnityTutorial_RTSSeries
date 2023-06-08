using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceBank : MonoBehaviour
{
    [SerializeField] List<ConstructionResource> StartingResources = new();

    public UnityEvent<ConstructionResource.EType, int> OnResourceAmountChanged = new();

    Dictionary<ConstructionResource.EType, int> ResourceAmounts = new();

    private void Awake()
    {
        // build up the resource map
        foreach(var resource in StartingResources) 
        { 
            int resourceAmount = 0;
            ResourceAmounts.TryGetValue(resource.Type, out resourceAmount);

            resourceAmount += resource.Amount;

            ResourceAmounts[resource.Type] = resourceAmount;
        }
    }

    public void IterateCurrentResources(System.Action<ConstructionResource.EType, int> iteratorFn)
    {
        foreach(var kvp in ResourceAmounts)
        {
            iteratorFn(kvp.Key, kvp.Value);
        }
    }

    public bool AreResourcesAvailable(List<ConstructionResource> resources) 
    {
        foreach(var resource in resources)
        {
            if (!IsResourceAvailable(resource))
                return false;
        }

        return true;
    }

    public bool IsResourceAvailable(ConstructionResource resource)
    {
        return IsResourceAvailable(resource.Type, resource.Amount);
    }

    public bool IsResourceAvailable(ConstructionResource.EType resourceType, int resourceAmount)
    {
        int amountAvailable = 0;
        ResourceAmounts.TryGetValue(resourceType, out amountAvailable);

        return amountAvailable >= resourceAmount;
    }

    public bool RequestResources(List<ConstructionResource> resources) 
    {
        if (!AreResourcesAvailable(resources))
            return false;

        foreach(var resource in resources)
        {
            RequestResource(resource.Type, resource.Amount);
        }

        return true;
    }

    public int RequestResource(ConstructionResource.EType resourceType, int resourceAmount)
    {
        int amountAvailable = 0;
        ResourceAmounts.TryGetValue(resourceType, out amountAvailable);

        int amountSupplied = Mathf.Min(amountAvailable, resourceAmount);
        int newAmountAvailable = amountAvailable - amountSupplied;

        ResourceAmounts[resourceType] = newAmountAvailable;

        OnResourceAmountChanged.Invoke(resourceType, newAmountAvailable);

        return amountSupplied;
    }

    public void AddResources(Dictionary<ConstructionResource.EType, int> resources)
    {
        foreach(var kvp in resources)
        {
            var resourceType = kvp.Key;
            var resourceAmount = kvp.Value;

            int amountAvailable = 0;
            ResourceAmounts.TryGetValue(resourceType, out amountAvailable);
            amountAvailable += resourceAmount;

            ResourceAmounts[resourceType] = amountAvailable;

            OnResourceAmountChanged.Invoke(resourceType, amountAvailable);
        }
    }
}
