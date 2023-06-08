using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_ResourceBank_Element : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ResourceNameDisplay;
    [SerializeField] TextMeshProUGUI ResourceAmountDisplay;

    public void Configure(ConstructionResource.EType resourceType, int initialAmount)
    {
        ResourceNameDisplay.text = resourceType.ToString();
        ResourceAmountDisplay.text = initialAmount.ToString();
    }

    public void UpdateResourceAmount(int newAmount)
    {
        ResourceAmountDisplay.text = newAmount.ToString();
    }
}
