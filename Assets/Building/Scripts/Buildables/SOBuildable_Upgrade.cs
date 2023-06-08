using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Buildables/Upgrade", fileName = "BLD_Upgrade")]
public class SOBuildable_Upgrade : SOBuildableObjectBase
{
#if UNITY_EDITOR
    protected override void InitialiseDefaults()
    {
        BuildableType = EType.Upgrade;
        Name = "Upgrade";
        Description = Name;
        BuildTime = 5f;
        QueueSizeLimit = 1;
        GlobalBuildLimit = 1;
    }
#endif // UNITY_EDITOR
}
