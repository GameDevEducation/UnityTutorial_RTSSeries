using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BuilderBase;
using UnityEngine.Events;

public class UI_BuildPanel_Tabbed : MonoBehaviour
{
    [SerializeField] BuilderBase LinkedBuilder;
    [SerializeField] GameObject CategoryUIPrefab;
    [SerializeField] GameObject ItemUIPrefab;
    [SerializeField] Transform CategoryUIRoot;
    [SerializeField] Transform ItemUIRoot;

    SOBuildableObjectBase.EType SelectedCategory = SOBuildableObjectBase.EType.Building;

    Dictionary<SOBuildableObjectBase, UI_BuildableItemPicker> ItemPickerUIMap = new();

    private void Start()
    {
        RefreshUI();

        LinkedBuilder.OnBuildQueued.AddListener(OnBuildQueued);
        LinkedBuilder.OnBuildStarted.AddListener(OnBuildStarted);
        LinkedBuilder.OnBuildPaused.AddListener(OnBuildPaused);
        LinkedBuilder.OnBuildResumed.AddListener(OnBuildResumed);
        LinkedBuilder.OnBuildCancelled.AddListener(OnBuildCancelled);
        LinkedBuilder.OnBuildTicked.AddListener(OnBuildTicked);
        LinkedBuilder.OnBuildCompleted.AddListener(OnBuildCompleted);
    }

    void OnBuildQueued(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildStarted(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildPaused(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildResumed(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildCancelled(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildTicked(BuilderBase.BuildData inBuildData)
    {
        ItemPickerUIMap[inBuildData.ObjectBeingBuilt].SetProgress(inBuildData.CurrentBuildProgress);
    }

    void OnBuildCompleted(BuilderBase.BuildData inBuildData)
    {
    }

    void OnCategorySelected(SOBuildableObjectBase.EType inCategoryType)
    {
        SelectedCategory = inCategoryType;

        RefreshUI();
    }

    void OnBuildableItemSelected(SOBuildableObjectBase inBuildableItem)
    {
        LinkedBuilder.RequestToQueueBuild(inBuildableItem);
    }

    void OnBuildableItemCancelled(SOBuildableObjectBase inBuildableItem)
    {
        if (LinkedBuilder.RequestToCancelBuild(inBuildableItem))
        {
            ItemPickerUIMap[inBuildableItem].ClearProgress();
        }
    }

    void RefreshUI()
    {
        // do we need to spawn the category UI
        if (CategoryUIRoot.childCount == 0)
        {
            var rawCategoryTypes = System.Enum.GetValues(typeof(SOBuildableObjectBase.EType));
            foreach(var rawCategoryType in rawCategoryTypes)
            {
                SOBuildableObjectBase.EType categoryType = (SOBuildableObjectBase.EType)rawCategoryType;

                if (categoryType == SOBuildableObjectBase.EType.NotSet)
                    continue;

                var categoryUI = GameObject.Instantiate(CategoryUIPrefab, CategoryUIRoot);
                var categoryUILogic = categoryUI.GetComponent<UI_CategoryPicker>();

                categoryUILogic.Bind(categoryType);
                categoryUILogic.OnCategorySelected.AddListener(OnCategorySelected);
            }
        }

        ItemPickerUIMap.Clear();

        // clean out existing UI
        for (int childIndex = ItemUIRoot.childCount - 1; childIndex >= 0; childIndex--) 
        { 
            var childGO = ItemUIRoot.GetChild(childIndex).gameObject;
            Destroy(childGO);
        }

        // get the available items to build
        var availableBuildables = LinkedBuilder.GetBuildableItemsForType(SelectedCategory);
        if (availableBuildables == null)
            return;

        // spawn the UI
        foreach(var availableBuildable in availableBuildables)
        {
            var itemUI = GameObject.Instantiate(ItemUIPrefab, ItemUIRoot);
            var itemUILogic = itemUI.GetComponent<UI_BuildableItemPicker>();

            ItemPickerUIMap[availableBuildable] = itemUILogic;

            itemUILogic.Bind(availableBuildable);
            itemUILogic.OnItemSelected.AddListener(OnBuildableItemSelected);
            itemUILogic.OnItemCancelled.AddListener(OnBuildableItemCancelled);
        }
    }
}
