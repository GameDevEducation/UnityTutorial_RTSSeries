using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class UI_CategoryPicker : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI CategoryLabel;
    public UnityEvent<SOBuildableObjectBase.EType> OnCategorySelected = new();

    public SOBuildableObjectBase.EType CategoryType { get; private set; } = SOBuildableObjectBase.EType.NotSet;

    public void Bind(SOBuildableObjectBase.EType inCategoryType)
    {
        CategoryType = inCategoryType;
        CategoryLabel.text = CategoryType.ToString();
    }

    public void OnButtonSelected()
    {
        OnCategorySelected.Invoke(CategoryType);
    }
}
