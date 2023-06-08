using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Buildables/Unit", fileName = "BLD_Unit")]
public class SOBuildable_Unit : SOBuildableObjectBase
{
#if UNITY_EDITOR
    protected override void InitialiseDefaults()
    {
        BuildableType = EType.Unit;
        Name = "Unit";
        Description = Name;
        BuildTime = 5f;
        QueueSizeLimit = 10;
        GlobalBuildLimit = -1;
    }
#endif // UNITY_EDITOR
}
