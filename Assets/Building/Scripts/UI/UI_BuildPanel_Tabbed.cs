using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BuilderBase;
using UnityEngine.Events;

public class UI_BuildPanel_Tabbed : MonoBehaviour
{
    [SerializeField] BuilderBase DefaultBuilder;
    [SerializeField] GameObject CategoryUIPrefab;
    [SerializeField] GameObject ItemUIPrefab;
    [SerializeField] Transform CategoryUIRoot;
    [SerializeField] Transform ItemUIRoot;

    [Header("Debug")]
    [SerializeField] BuilderBase DEBUG_NewBuilderToSet;
    [SerializeField] bool DEBUG_SetNewBuilder = false;

    BuilderBase LinkedBuilder = null;
    SOBuildableObjectBase.EType SelectedCategory = SOBuildableObjectBase.EType.Building;

    Dictionary<SOBuildableObjectBase, UI_BuildableItemPicker> ItemPickerUIMap = new();

    private void Start()
    {
        SetBuilder(DefaultBuilder);
    }

    private void Update()
    {
        if (DEBUG_SetNewBuilder)
        {
            DEBUG_SetNewBuilder = false;
            SetBuilder(DEBUG_NewBuilderToSet);
        }
    }

    public void SetBuilder(BuilderBase inBuilder)
    {
        if (LinkedBuilder != null)
        {
            // clean out existing category UI
            for (int childIndex = CategoryUIRoot.childCount - 1; childIndex >= 0; childIndex--) 
            { 
                var childGO = CategoryUIRoot.GetChild(childIndex).gameObject;
                Destroy(childGO);
            }

            // remove the listeners
            LinkedBuilder.OnBuildQueued.RemoveListener(OnBuildQueued);
            LinkedBuilder.OnBuildStarted.RemoveListener(OnBuildStarted);
            LinkedBuilder.OnBuildPaused.RemoveListener(OnBuildPaused);
            LinkedBuilder.OnBuildResumed.RemoveListener(OnBuildResumed);
            LinkedBuilder.OnBuildCancelled.RemoveListener(OnBuildCancelled);
            LinkedBuilder.OnBuildTicked.RemoveListener(OnBuildTicked);
            LinkedBuilder.OnBuildCompleted.RemoveListener(OnBuildCompleted);
        }

        LinkedBuilder = inBuilder;

        if (LinkedBuilder != null)
        {
            LinkedBuilder.OnBuildQueued.AddListener(OnBuildQueued);
            LinkedBuilder.OnBuildStarted.AddListener(OnBuildStarted);
            LinkedBuilder.OnBuildPaused.AddListener(OnBuildPaused);
            LinkedBuilder.OnBuildResumed.AddListener(OnBuildResumed);
            LinkedBuilder.OnBuildCancelled.AddListener(OnBuildCancelled);
            LinkedBuilder.OnBuildTicked.AddListener(OnBuildTicked);
            LinkedBuilder.OnBuildCompleted.AddListener(OnBuildCompleted);
        }

        RefreshUI(true);
    }

    void OnBuildQueued(BuilderBase.BuildData inBuildData)
    {
        RefreshItemUI_QueuedInformation(inBuildData.ObjectBeingBuilt);
    }

    void OnBuildStarted(BuilderBase.BuildData inBuildData)
    {
        RefreshItemUI_QueuedInformation(inBuildData.ObjectBeingBuilt);
    }

    void OnBuildPaused(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildResumed(BuilderBase.BuildData inBuildData)
    {
    }

    void OnBuildCancelled(BuilderBase.BuildData inBuildData)
    {
        RefreshItemUI_QueuedInformation(inBuildData.ObjectBeingBuilt);
    }

    void OnBuildTicked(BuilderBase.BuildData inBuildData)
    {
        UI_BuildableItemPicker buildableItemUI = null;

        if (ItemPickerUIMap.TryGetValue(inBuildData.ObjectBeingBuilt, out buildableItemUI))
        {
            buildableItemUI.SetProgress(inBuildData.CurrentBuildProgress);
        }
    }

    void OnBuildCompleted(BuilderBase.BuildData inBuildData)
    {
        RefreshItemUI_QueuedInformation(inBuildData.ObjectBeingBuilt);
    }

    void OnCategorySelected(SOBuildableObjectBase.EType inCategoryType)
    {
        SelectedCategory = inCategoryType;

        RefreshUI(false);
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

    void RefreshItemUI_QueuedInformation(SOBuildableObjectBase inBuildableItem)
    {
        UI_BuildableItemPicker buildableItemUI = null;
        if (ItemPickerUIMap.TryGetValue(inBuildableItem, out buildableItemUI))
        {
            buildableItemUI.SetNumQueued(LinkedBuilder.GetNumberOfItemQueued(inBuildableItem));
        }
    }

    void RefreshUI(bool regenerateCategoryUI)
    {
        ItemPickerUIMap.Clear();

        // clean out existing UI
        for (int childIndex = ItemUIRoot.childCount - 1; childIndex >= 0; childIndex--)
        {
            var childGO = ItemUIRoot.GetChild(childIndex).gameObject;
            Destroy(childGO);
        }

        if (LinkedBuilder == null)
            return;

        // do we need to spawn the category UI
        if (regenerateCategoryUI)
        {
            SOBuildableObjectBase.EType previouslySelectedCategory = SelectedCategory;
            SelectedCategory = SOBuildableObjectBase.EType.NotSet;

            var rawCategoryTypes = System.Enum.GetValues(typeof(SOBuildableObjectBase.EType));
            foreach(var rawCategoryType in rawCategoryTypes)
            {
                SOBuildableObjectBase.EType categoryType = (SOBuildableObjectBase.EType)rawCategoryType;

                if (categoryType == SOBuildableObjectBase.EType.NotSet)
                    continue;

                if (LinkedBuilder.GetBuildableItemsForType(categoryType) == null)
                    continue;

                if (SelectedCategory == SOBuildableObjectBase.EType.NotSet)
                    SelectedCategory = categoryType;

                if (previouslySelectedCategory == categoryType)
                    SelectedCategory = previouslySelectedCategory;

                var categoryUI = GameObject.Instantiate(CategoryUIPrefab, CategoryUIRoot);
                var categoryUILogic = categoryUI.GetComponent<UI_CategoryPicker>();

                categoryUILogic.Bind(categoryType);
                categoryUILogic.OnCategorySelected.AddListener(OnCategorySelected);
            }
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
            itemUILogic.SetNumQueued(LinkedBuilder.GetNumberOfItemQueued(availableBuildable));
        }
    }
}
