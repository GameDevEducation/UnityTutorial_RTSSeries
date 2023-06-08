using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConstructionResource
{
    public enum EType
    {
        Food,
        Wood,
        Metal,
        Energy
    }

    public EType Type = EType.Food;
    public int Amount = 0;
}
