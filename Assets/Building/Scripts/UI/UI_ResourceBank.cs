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
        // setup the resource UI
        LinkedBank.IterateCurrentResources((ConstructionResource.EType resourceType, int resourceAmount) =>
        {
            var newResourceUI = GameObject.Instantiate(ResourceUIPrefab, ResourceUIRoot);
            var newResourceUILogic = newResourceUI.GetComponent<UI_ResourceBank_Element>();

            newResourceUILogic.Configure(resourceType, resourceAmount);
            UIMap[resourceType] = newResourceUILogic;
        });

        LinkedBank.OnResourceAmountChanged.AddListener(OnResourceAmountChanged);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnResourceAmountChanged(ConstructionResource.EType resourceType, int resourceAmount)
    {
        UIMap[resourceType].UpdateResourceAmount(resourceAmount);
    }
}
