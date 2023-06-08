using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ResourceBank : MonoBehaviour
{
    [SerializeField] ResourceBank LinkedBank;
    [SerializeField] Transform ResourceUIRoot;
    [SerializeField] GameObject ResourceUIPrefab;

    Dictionary<ConstructionResource.EType, UI_ResourceBank_Element> UIMap = new();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
