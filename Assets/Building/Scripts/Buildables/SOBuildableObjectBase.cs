using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SOBuildableObjectBase : ScriptableObject
{
    public enum EType
    {
        NotSet = 0,

        Building = 1,
        Unit = 101,
        Upgrade = 201
    }

    [field: SerializeField] public EType BuildableType { get; protected set; } = EType.NotSet;
    [field: SerializeField] public string Name { get; protected set; }
    [field: SerializeField] public string Description { get; protected set; }
    [field: SerializeField] public List<ConstructionResource> ResourceCosts { get; protected set; } = new();

    [field: SerializeField] public float BuildTime { get; protected set; }
    [field: SerializeField] public int QueueSizeLimit { get; protected set; }
    [field: SerializeField] public int GlobalBuildLimit { get; protected set; }
    [field: SerializeField] public Sprite UIImage { get; protected set; }

#if UNITY_EDITOR
    private void Reset()
    {
        if (BuildableType == EType.NotSet && !Application.isPlaying)
        {
            InitialiseDefaults();
        }
    }

    protected abstract void InitialiseDefaults();
#endif // UNITY_EDITOR
}
