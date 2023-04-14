using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuilderBase : MonoBehaviour
{
    public class BuildData
    {
        public BuilderBase OwningBuilder { get; private set; }
        public SOBuildableObjectBase ObjectBeingBuilt { get; private set; }
        public float CurrentBuildProgress { get; private set; }

        public bool HasStarted => CurrentBuildProgress > 0f;

        public BuildData(BuilderBase owningBuilder, SOBuildableObjectBase objectBeingBuilt)
        {
            OwningBuilder = owningBuilder;
            ObjectBeingBuilt = objectBeingBuilt;
            CurrentBuildProgress = 0f;
        }

        public bool Tick(float deltaTime)
        {
            CurrentBuildProgress = Mathf.Clamp01(CurrentBuildProgress + (deltaTime / ObjectBeingBuilt.BuildTime));

            return CurrentBuildProgress >= 1f;
        }
    }

    [SerializeField] SOBuildableDatabase BuildablesDB;

    [Tooltip("If not empty then can only see these types of buildables")]
    [SerializeField] List<SOBuildableObjectBase.EType> PermittedTypes = new();
    [Tooltip("If not empty then only the listed buildables are supported")]
    [SerializeField] List<SOBuildableObjectBase> OverridePermittedBuildables = new();

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

            bool isFinished = buildData.Tick(deltaTime);
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

        return true;
    }

    public bool RequestToQueueBuild(SOBuildableObjectBase buildable)
    {
        if (!CanBuild(buildable))
            return false;

        BuildData newBuildData = new BuildData(this, buildable);

        BuildsInProgress.Add(newBuildData);

        OnBuildQueued.Invoke(newBuildData);

        return true;
    }

    public bool RequestToCancelBuild(SOBuildableObjectBase buildable)
    {
        if (BuildsInProgress.Count == 0)
            return true;

        if (BuildsInProgress[0].ObjectBeingBuilt == buildable)
            return CancelBuild(BuildsInProgress[0]);

        for (int itemIndex = 0; itemIndex < BuildsInProgress.Count; itemIndex++)
        {
            if (BuildsInProgress[itemIndex].ObjectBeingBuilt == buildable)
            {
                BuildsInProgress.RemoveAt(itemIndex);
                return true;
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
}
