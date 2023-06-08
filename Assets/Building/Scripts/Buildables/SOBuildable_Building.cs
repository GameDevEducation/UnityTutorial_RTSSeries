using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Buildables/Building", fileName = "BLD_Building")]
public class SOBuildable_Building : SOBuildableObjectBase
{
#if UNITY_EDITOR
    protected override void InitialiseDefaults()
    {
        BuildableType = EType.Building;
        Name = "Building";
        Description = Name;
        BuildTime = 5f;
        QueueSizeLimit = 1;
        GlobalBuildLimit = -1;
    }
#endif // UNITY_EDITOR
}
