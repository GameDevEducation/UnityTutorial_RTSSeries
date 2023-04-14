using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UI_BuildableItemPicker : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ItemLabel;
    [SerializeField] UnityEngine.UI.Image ItemImage;
    [SerializeField] UnityEngine.UI.Image InProgressIndicator;

    public UnityEvent<SOBuildableObjectBase> OnItemSelected = new();
    public UnityEvent<SOBuildableObjectBase> OnItemCancelled = new();

    public SOBuildableObjectBase ItemSO { get; private set; }

    public void Bind(SOBuildableObjectBase inItemSO)
    {
        ItemSO = inItemSO;
        ItemLabel.text = ItemSO.Name;
        ItemImage.sprite = ItemSO.UIImage;
    }

    public void ClearProgress()
    {
        InProgressIndicator.fillAmount = 0f;
    }

    public void SetProgress(float inAmount)
    {
        InProgressIndicator.fillAmount = 1f - inAmount;
    }

    public void OnButtonSelected(BaseEventData inPointerEventData)
    {
        var pointerEventData = inPointerEventData as PointerEventData;

        if (pointerEventData.button == PointerEventData.InputButton.Left)
            OnItemSelected.Invoke(ItemSO);
        else if (pointerEventData.button == PointerEventData.InputButton.Right)
            OnItemCancelled.Invoke(ItemSO);
    }
}
