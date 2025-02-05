using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BehavioursContainer : BeatmapObjectContainerCollection, CMInput.IBehaviourGridActions
{
    [SerializeField] private GameObject behaviourPrefab;

    [SerializeField] private BehaviourAppearanceSO behaviourAppearanceSo;
    [SerializeField] private TracksManagerRight tracksManagerRight;
    [SerializeField] private CreateBehaviourLanesLabels labels;
    [SerializeField] private CountersPlusController countersPlus;

    public List<MapEvent> AllRotationEvents = new List<MapEvent>();

    private int currentPage = 0;
    private const int maxPage = 5;

    private bool isInitiating = true;
    
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Behaviour;


    public override BeatmapObjectContainer CreateContainer()
    {
        BeatmapObjectContainer con = BeatmapBehaviourContainer.SpawnBeatmapBehaviour(this, null, ref behaviourPrefab);
        return con;
    }

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.BehaviourPassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveBehaviourCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.BehaviourPassedThreshold += DespawnCallback;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        LoadInitialMap.LevelLoadedEvent += OnLevelLoaded;
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.BehaviourPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveBehaviourCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.BehaviourPassedThreshold -= DespawnCallback;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        LoadInitialMap.LevelLoadedEvent -= OnLevelLoaded;
    }

    private void OnLevelLoaded()
    {
        isInitiating = false;
    }
    
    private void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData)) RecycleContainer(objectData);
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }

    public void OnCyclePageUp(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        currentPage++;
        if (!(currentPage < maxPage)) currentPage = 0;

        labels.UpdateLabels(currentPage);
    }

    public void OnCyclePageDown(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        currentPage--;
        if (currentPage < 0) currentPage = (maxPage - 1);

        labels.UpdateLabels(currentPage);
    }

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var behaviour = con as BeatmapBehaviourContainer;
        var behaviourData = obj as MapBehaviour;

        if (behaviour != null && behaviourData != null)
            behaviour.UpdateBehaviour(behaviourData.Type, isInitiating);
    }

    protected override void OnObjectSpawned(BeatmapObject obj)
    {      
        if (obj is MapBehaviour b)
        {
            StartCoroutine(tracksManagerRight.OnBehaviourSpwan(b));
        }


        if (obj is MapEvent e)
        {
            if (e.IsRotationEvent)
                AllRotationEvents.Add(e);
        }
        
        countersPlus.UpdateStatistic(CountersPlusStatistic.Behaviours);
    }

    protected override void OnObjectDelete(BeatmapObject obj)
    {
        if (obj is MapBehaviour b)
        {
            tracksManagerRight.OnBehaviourDelete(b);
        }

        if (obj is MapEvent e)
        {
            if (e.IsRotationEvent)
            {
                AllRotationEvents.Remove(e);
                tracksManagerRight.RefreshTracks();
            }
        }
        countersPlus.UpdateStatistic(CountersPlusStatistic.Behaviours);
    }

    protected override void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj)
    {
    }

    protected override void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj)
    {
    }
}
