using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuilderBase : MonoBehaviour
{
    public enum ECancelBehaviour
    {
        CancelsInProgress,
        CancelsLastQueued
    }

    public enum ECostMode
    {
        PayOverTime,
        PayUpfront,
    }

    public enum ERefundMode
    {
        Full,
        None
    }

    public class BuildData
    {
        public BuilderBase OwningBuilder { get; private set; }
        public SOBuildableObjectBase ObjectBeingBuilt { get; private set; }
        public float CurrentBuildProgress { get; private set; }

        public bool HasStarted => CurrentBuildProgress > 0f;

        Dictionary<ConstructionResource.EType, int> ResourcesRequired = new();
        public Dictionary<ConstructionResource.EType, int> ResourcesConsumed { get; private set; } = new();
        Dictionary<ConstructionResource.EType, float> FractionalConsumptionPending = new();

        int TotalResourcesRequired = 0;
        int TotalResourcesSupplied = 0;

        public BuildData(BuilderBase owningBuilder, SOBuildableObjectBase objectBeingBuilt)
        {
            OwningBuilder = owningBuilder;
            ObjectBeingBuilt = objectBeingBuilt;
            CurrentBuildProgress = 0f;

            // build up the cost data
            foreach(var resource in objectBeingBuilt.ResourceCosts)
            {
                int amountRequired = 0;
                ResourcesRequired.TryGetValue(resource.Type, out amountRequired);

                amountRequired += resource.Amount;
                TotalResourcesRequired += resource.Amount;

                ResourcesRequired[resource.Type] = amountRequired;
                ResourcesConsumed[resource.Type] = 0;
                FractionalConsumptionPending[resource.Type] = 0;
            }
        }

        public void MarkAllResourcesRequiredAsConsumed()
        {
            foreach(var kvp in ResourcesRequired)
            {
                ResourcesConsumed[kvp.Key] = kvp.Value;
            }

            TotalResourcesSupplied = TotalResourcesRequired;
        }

        public bool Tick(float deltaTime, System.Func<ConstructionResource.EType, int, int> requisitionFn)
        {
            float attemptedProgress = deltaTime / ObjectBeingBuilt.BuildTime;

            if (requisitionFn != null)
            {
                // update the amount we've tried to consume and if possible requisition it
                foreach(var kvp in ResourcesRequired)
                {
                    var resourceType = kvp.Key;
                    var resourceAmount = kvp.Value;

                    if (resourceAmount == 0)
                        continue;

                    if (ResourcesRequired[resourceType] == ResourcesConsumed[resourceType])
                        continue;

                    float newFractionalAmount = FractionalConsumptionPending[resourceType];
                    newFractionalAmount += resourceAmount * attemptedProgress;

                    float maximumRequestable = resourceAmount - ResourcesConsumed[resourceType];
                    if (newFractionalAmount > maximumRequestable)
                        newFractionalAmount = maximumRequestable;

                    // can we attempt to consume the resource?
                    if (newFractionalAmount >= 1)
                    {
                        int amountSupplied = requisitionFn(resourceType, Mathf.FloorToInt(newFractionalAmount));

                        newFractionalAmount -= amountSupplied;
                        TotalResourcesSupplied += amountSupplied;

                        ResourcesConsumed[resourceType] += amountSupplied;
                    }

                    FractionalConsumptionPending[resourceType] = newFractionalAmount;
                }

                CurrentBuildProgress = (float)TotalResourcesSupplied / (float)TotalResourcesRequired;
            }
            else
            {
                CurrentBuildProgress = Mathf.Clamp01(CurrentBuildProgress + attemptedProgress);
            }

            return CurrentBuildProgress >= 1f;
        }
    }

    [SerializeField] SOBuildableDatabase BuildablesDB;
    [SerializeField] ResourceBank LinkedResourceBank;

    [SerializeField] ECostMode CostMode = ECostMode.PayUpfront;
    [SerializeField] ERefundMode RefundMode = ERefundMode.Full;

    [Tooltip("If not empty then can only see these types of buildables")]
    [SerializeField] List<SOBuildableObjectBase.EType> PermittedTypes = new();
    [Tooltip("If not empty then only the listed buildables are supported")]
    [SerializeField] List<SOBuildableObjectBase> OverridePermittedBuildables = new();

    [SerializeField] ECancelBehaviour CancelBehaviour = ECancelBehaviour.CancelsLastQueued;

    Dictionary<SOBuildableObjectBase.EType, List<SOBuildableObjectBase>> AvailableBuildables = new();

    List<BuildData> BuildsInProgress = new();

    bool IsPaused = false;

    [Header("Events")]
    public UnityEvent<BuildData> OnBuildQueued = new();
    public UnityEvent<BuildData> OnBuildStarted = new();
    public UnityEvent<BuildData> OnBuildPaused = new();
    public UnityEvent<BuildData> OnBuildResumed = new();
    public UnityEvent<BuildData> OnBuildCancelled = new();
    public UnityEvent<BuildData> OnBuildTicked = new();
    public UnityEvent<BuildData> OnBuildCompleted = new();

    [Header("Debug Tools")]
    [SerializeField] SOBuildableObjectBase DEBUG_ObjectToBuild;
    [SerializeField] bool DEBUG_LogActions = true;
    [SerializeField] bool DEBUG_TriggerPause;
    [SerializeField] bool DEBUG_TriggerResume;
    [SerializeField] bool DEBUG_CancelBuild;

    private void Awake()
    {
        // are we overriding the buildables?
        if (OverridePermittedBuildables.Count > 0)
        {
            foreach(var buildable in OverridePermittedBuildables)
            {
                List<SOBuildableObjectBase> buildablesOfType;

                if (!AvailableBuildables.TryGetValue(buildable.BuildableType, out buildablesOfType))
                {
                    AvailableBuildables[buildable.BuildableType] = buildablesOfType = new List<SOBuildableObjectBase>();
                }

                buildablesOfType.Add(buildable);
            }
        } // populate using the database
        else
        {
            foreach (var buildable in BuildablesDB.AllBuildables)
            {
                List<SOBuildableObjectBase> buildablesOfType;

                // skip if not permitted type
                if (PermittedTypes.Count > 0 && !PermittedTypes.Contains(buildable.BuildableType))
                    continue;

                if (!AvailableBuildables.TryGetValue(buildable.BuildableType, out buildablesOfType))
                {
                    AvailableBuildables[buildable.BuildableType] = buildablesOfType = new List<SOBuildableObjectBase>();
                }

                buildablesOfType.Add(buildable);
            }
        }
    }

    private void Start()
    {
        OnBuildQueued.AddListener(GlobalBuildManager.Instance.OnBuildRequested);
        OnBuildCancelled.AddListener(GlobalBuildManager.Instance.OnBuildCancelled);
        OnBuildCompleted.AddListener(GlobalBuildManager.Instance.OnBuildCompleted);

        if (DEBUG_LogActions)
        {
            OnBuildQueued.AddListener((BuildData buildData) => {Debug.Log($"Queued build of {buildData.ObjectBeingBuilt.Name}");});
            OnBuildStarted.AddListener((BuildData buildData) => {Debug.Log($"Started build of {buildData.ObjectBeingBuilt.Name}");});
            OnBuildPaused.AddListener((BuildData buildData) => {Debug.Log($"Paused build of {buildData.ObjectBeingBuilt.Name}");});
            OnBuildResumed.AddListener((BuildData buildData) => {Debug.Log($"Resumed build of {buildData.ObjectBeingBuilt.Name}");});
            OnBuildCancelled.AddListener((BuildData buildData) => {Debug.Log($"Cancelled build of {buildData.ObjectBeingBuilt.Name}");});
            OnBuildTicked.AddListener((BuildData buildData) => {Debug.Log($"Ticked build of {buildData.ObjectBeingBuilt.Name}");});
            OnBuildCompleted.AddListener((BuildData buildData) => { Debug.Log($"Completed build of {buildData.ObjectBeingBuilt.Name}");});
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DEBUG_ObjectToBuild)
        {
            RequestToQueueBuild(DEBUG_ObjectToBuild);
            DEBUG_ObjectToBuild = null;
        }
        if (DEBUG_TriggerPause)
        {
            DEBUG_TriggerPause = false;
            Pause();
        }
        if (DEBUG_TriggerResume)
        {
            DEBUG_TriggerResume = false;
            Resume();
        }
        if (DEBUG_CancelBuild && BuildsInProgress.Count > 0)
        {
            CancelBuild(BuildsInProgress[0]);
        }

        if (!IsPaused && BuildsInProgress.Count > 0)
        {
            TickBuilds(Time.deltaTime);
        }
    }

    void TickBuilds(float deltaTime)
    {
        for (int index = 0; index < BuildsInProgress.Count; index++)
        {
            var buildData = BuildsInProgress[index];

            if (!buildData.HasStarted)
                OnBuildStarted.Invoke(buildData);

            bool isFinished = false;
            if (CostMode == ECostMode.PayUpfront)
                isFinished = buildData.Tick(deltaTime, null);
            else
                isFinished = buildData.Tick(deltaTime, RequisitionResourceHelper);
            OnBuildTicked.Invoke(buildData);

            if (isFinished)
            {
                OnBuildCompleted.Invoke(buildData);
                BuildsInProgress.RemoveAt(index);
                --index;
            }
            else
                return;
        }
    }

    int RequisitionResourceHelper(ConstructionResource.EType resourceType, int resourceAmount)
    {
        return LinkedResourceBank.RequestResource(resourceType, resourceAmount);
    }

    public bool CanBuild(SOBuildableObjectBase buildable)
    {
        List<SOBuildableObjectBase> buildablesSubset;
        if (!AvailableBuildables.TryGetValue(buildable.BuildableType, out buildablesSubset))
            return false;
        if (!buildablesSubset.Contains(buildable)) 
            return false;

        if (buildable.QueueSizeLimit > 0)
        {
            int numBeingBuilt = GlobalBuildManager.Instance.GetNumberOfBuildsInProgress(buildable);
            if (numBeingBuilt >= buildable.QueueSizeLimit)
                return false;
        }

        if (buildable.GlobalBuildLimit > 0)
        {
            int numBeingBuiltOrInProgress = GlobalBuildManager.Instance.GetNumberBuilt(buildable) +
                                            GlobalBuildManager.Instance.GetNumberOfBuildsInProgress(buildable);  

            if (numBeingBuiltOrInProgress >= buildable.GlobalBuildLimit)
                return false;
        }

        if (CostMode == ECostMode.PayUpfront)
        {
            if (!LinkedResourceBank.AreResourcesAvailable(buildable.ResourceCosts))
                return false;
        }

        return true;
    }

    public bool RequestToQueueBuild(SOBuildableObjectBase buildable)
    {
        if (!CanBuild(buildable))
            return false;

        if (CostMode == ECostMode.PayUpfront)
        {
            if (!LinkedResourceBank.RequestResources(buildable.ResourceCosts))
                return false;
        }

        BuildData newBuildData = new BuildData(this, buildable);

        if (CostMode == ECostMode.PayUpfront) 
        {
            newBuildData.MarkAllResourcesRequiredAsConsumed();
        }

        BuildsInProgress.Add(newBuildData);

        OnBuildQueued.Invoke(newBuildData);

        return true;
    }

    public bool RequestToCancelBuild(SOBuildableObjectBase buildable)
    {
        if (BuildsInProgress.Count == 0)
            return true;

        if (BuildsInProgress[0].ObjectBeingBuilt == buildable)
        {
            if (CancelBehaviour == ECancelBehaviour.CancelsInProgress)
                return CancelBuild(BuildsInProgress[0]);
            else
            {
                for (int itemIndex = BuildsInProgress.Count - 1; itemIndex >= 0; itemIndex--)
                {
                    if (BuildsInProgress[itemIndex].ObjectBeingBuilt == buildable)
                        return CancelBuild(BuildsInProgress[itemIndex]);
                }
            }
        }

        for (int itemIndex = 0; itemIndex < BuildsInProgress.Count; itemIndex++)
        {
            if (BuildsInProgress[itemIndex].ObjectBeingBuilt == buildable)
            {
                return CancelBuild(BuildsInProgress[itemIndex]);
            }
        }

        return false;
    }

    public bool CancelBuild(BuildData buildData)
    {
        if (!BuildsInProgress.Contains(buildData))
            return false;

        BuildsInProgress.Remove(buildData);
        OnBuildCancelled.Invoke(buildData);

        if (RefundMode == ERefundMode.Full)
            LinkedResourceBank.AddResources(buildData.ResourcesConsumed);

        return true;
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;

        if (BuildsInProgress.Count > 0)
            OnBuildPaused.Invoke(BuildsInProgress[0]);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        IsPaused = false;

        if (BuildsInProgress.Count > 0)
            OnBuildResumed.Invoke(BuildsInProgress[0]);
    }

    public List<SOBuildableObjectBase> GetBuildableItemsForType(SOBuildableObjectBase.EType inType)
    {
        List<SOBuildableObjectBase> outList = null;
        AvailableBuildables.TryGetValue(inType, out outList);

        return outList;
    }

    public int GetNumberOfItemQueued(SOBuildableObjectBase buildable)
    {
        int numQueued = 0;

        foreach(var buildData in BuildsInProgress)
        {
            if (buildData.ObjectBeingBuilt == buildable)
                ++numQueued;
        }

        return numQueued;
    }
}
