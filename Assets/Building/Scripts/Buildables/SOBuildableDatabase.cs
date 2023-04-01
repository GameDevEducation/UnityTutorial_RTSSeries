using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Buildables Database", fileName = "BuildablesDB")]
public class SOBuildableDatabase : ScriptableObject
{
    [field: SerializeField] public List<SOBuildableObjectBase> AllBuildables { get; private set; } = new();
}
