using System.Collections.Generic;
using OneManJourney.Data;

namespace OneManJourney.Runtime
{
    public readonly struct GameContextInitializedEvent
    {
        public GameContextInitializedEvent(int chapter, int nodeIndex, int nodesVisited, int crisisValue, int cardPoolCount, int eventPoolCount)
        {
            Chapter = chapter;
            NodeIndex = nodeIndex;
            NodesVisited = nodesVisited;
            CrisisValue = crisisValue;
            CardPoolCount = cardPoolCount;
            EventPoolCount = eventPoolCount;
        }

        public int Chapter { get; }
        public int NodeIndex { get; }
        public int NodesVisited { get; }
        public int CrisisValue { get; }
        public int CardPoolCount { get; }
        public int EventPoolCount { get; }
    }

    public readonly struct ResourceChangedEvent
    {
        public ResourceChangedEvent(ResourceType resourceType, int previousValue, int currentValue)
        {
            ResourceType = resourceType;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Delta = currentValue - previousValue;
        }

        public ResourceType ResourceType { get; }
        public int PreviousValue { get; }
        public int CurrentValue { get; }
        public int Delta { get; }
    }

    public readonly struct CardDrawnEvent
    {
        public CardDrawnEvent(CardConfig card, int remainingCardPoolCount)
        {
            Card = card;
            RemainingCardPoolCount = remainingCardPoolCount;
        }

        public CardConfig Card { get; }
        public int RemainingCardPoolCount { get; }
    }

    public readonly struct NodeSelectedEvent
    {
        public NodeSelectedEvent(
            int previousChapter,
            int previousNodeIndex,
            int previousNodesVisited,
            int currentChapter,
            int currentNodeIndex,
            int currentNodesVisited)
        {
            PreviousChapter = previousChapter;
            PreviousNodeIndex = previousNodeIndex;
            PreviousNodesVisited = previousNodesVisited;
            CurrentChapter = currentChapter;
            CurrentNodeIndex = currentNodeIndex;
            CurrentNodesVisited = currentNodesVisited;
        }

        public int PreviousChapter { get; }
        public int PreviousNodeIndex { get; }
        public int PreviousNodesVisited { get; }
        public int CurrentChapter { get; }
        public int CurrentNodeIndex { get; }
        public int CurrentNodesVisited { get; }
    }

    public readonly struct JourneyMapGeneratedEvent
    {
        public JourneyMapGeneratedEvent(JourneyMap map)
        {
            Map = map;
            Seed = map?.Seed ?? 0;
            NodeCount = map?.Nodes.Count ?? 0;
            RouteCount = map?.RouteCount ?? 0;
            BranchingNodeCount = map?.BranchingNodeCount ?? 0;
            BattleNodeCount = map?.GetTypeCount(JourneyNodeType.Battle) ?? 0;
            EventNodeCount = map?.GetTypeCount(JourneyNodeType.Event) ?? 0;
            SupplyNodeCount = map?.GetTypeCount(JourneyNodeType.Supply) ?? 0;
            BossNodeCount = map?.GetTypeCount(JourneyNodeType.Boss) ?? 0;
        }

        public JourneyMap Map { get; }
        public int Seed { get; }
        public int NodeCount { get; }
        public int RouteCount { get; }
        public int BranchingNodeCount { get; }
        public int BattleNodeCount { get; }
        public int EventNodeCount { get; }
        public int SupplyNodeCount { get; }
        public int BossNodeCount { get; }
    }

    public enum JourneyAdvanceBlockReason
    {
        None = 0,
        MissingMap,
        MissingCurrentNode,
        MissingTargetNode,
        MissingBattleEncounterConfig,
        InvalidPath,
        EncounterAlreadyActive,
        EncounterNotActive,
        InsufficientFood
    }

    public readonly struct BattleEncounterPreparedEvent
    {
        public BattleEncounterPreparedEvent(int nodeId, JourneyNodeType nodeType, int encounterSeed, IReadOnlyList<EnemyConfig> enemyQueue)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            EncounterSeed = encounterSeed;
            EnemyQueue = enemyQueue;
            EnemyCount = enemyQueue?.Count ?? 0;
        }

        public int NodeId { get; }
        public JourneyNodeType NodeType { get; }
        public int EncounterSeed { get; }
        public IReadOnlyList<EnemyConfig> EnemyQueue { get; }
        public int EnemyCount { get; }
    }

    public enum BattleTurnPhase
    {
        None = 0,
        PlayerTurn,
        EnemyTurn
    }

    public readonly struct BattleEnemyIntentView
    {
        public BattleEnemyIntentView(
            int enemyIndex,
            string enemyId,
            string enemyDisplayName,
            EnemyIntentType intentType,
            int intentValue,
            bool isDefeated,
            string summary)
        {
            EnemyIndex = enemyIndex;
            EnemyId = enemyId ?? string.Empty;
            EnemyDisplayName = enemyDisplayName ?? string.Empty;
            IntentType = intentType;
            IntentValue = intentValue;
            IsDefeated = isDefeated;
            Summary = summary ?? string.Empty;
        }

        public int EnemyIndex { get; }
        public string EnemyId { get; }
        public string EnemyDisplayName { get; }
        public EnemyIntentType IntentType { get; }
        public int IntentValue { get; }
        public bool IsDefeated { get; }
        public string Summary { get; }
    }

    public readonly struct BattleEnemyIntentUpdatedEvent
    {
        public BattleEnemyIntentUpdatedEvent(
            int nodeId,
            int turnNumber,
            IReadOnlyList<BattleEnemyIntentView> enemyIntents)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            EnemyIntents = enemyIntents;
            EnemyCount = enemyIntents?.Count ?? 0;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public IReadOnlyList<BattleEnemyIntentView> EnemyIntents { get; }
        public int EnemyCount { get; }
    }

    public readonly struct BattleFlowInitializedEvent
    {
        public BattleFlowInitializedEvent(
            int nodeId,
            JourneyNodeType nodeType,
            int encounterSeed,
            int enemyCount,
            int drawPileCount,
            int handCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            EncounterSeed = encounterSeed;
            EnemyCount = enemyCount;
            DrawPileCount = drawPileCount;
            HandCount = handCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public JourneyNodeType NodeType { get; }
        public int EncounterSeed { get; }
        public int EnemyCount { get; }
        public int DrawPileCount { get; }
        public int HandCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleTurnStartedEvent
    {
        public BattleTurnStartedEvent(
            int nodeId,
            int turnNumber,
            int energy,
            int drawnCardCount,
            int handCount,
            int drawPileCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            Energy = energy;
            DrawnCardCount = drawnCardCount;
            HandCount = handCount;
            DrawPileCount = drawPileCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public int Energy { get; }
        public int DrawnCardCount { get; }
        public int HandCount { get; }
        public int DrawPileCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleCardPlayedEvent
    {
        public BattleCardPlayedEvent(
            int nodeId,
            int turnNumber,
            CardConfig card,
            CardType cardType,
            int cardBaseValue,
            StatusEffectType statusEffect,
            int requestedStatusStacks,
            int energyBefore,
            int energyAfter,
            bool exhausted,
            int damageApplied,
            int armorApplied,
            int healingApplied,
            int cardsDrawnByEffect,
            int statusStacksApplied,
            string effectSummary,
            int handCount,
            int drawPileCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            Card = card;
            CardType = cardType;
            CardBaseValue = cardBaseValue;
            StatusEffect = statusEffect;
            RequestedStatusStacks = requestedStatusStacks;
            EnergyBefore = energyBefore;
            EnergyAfter = energyAfter;
            Exhausted = exhausted;
            DamageApplied = damageApplied;
            ArmorApplied = armorApplied;
            HealingApplied = healingApplied;
            CardsDrawnByEffect = cardsDrawnByEffect;
            StatusStacksApplied = statusStacksApplied;
            EffectSummary = effectSummary ?? string.Empty;
            HandCount = handCount;
            DrawPileCount = drawPileCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public CardConfig Card { get; }
        public CardType CardType { get; }
        public int CardBaseValue { get; }
        public StatusEffectType StatusEffect { get; }
        public int RequestedStatusStacks { get; }
        public int EnergyBefore { get; }
        public int EnergyAfter { get; }
        public bool Exhausted { get; }
        public int DamageApplied { get; }
        public int ArmorApplied { get; }
        public int HealingApplied { get; }
        public int CardsDrawnByEffect { get; }
        public int StatusStacksApplied { get; }
        public string EffectSummary { get; }
        public int HandCount { get; }
        public int DrawPileCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleHandDiscardedEvent
    {
        public BattleHandDiscardedEvent(
            int nodeId,
            int turnNumber,
            int discardedCount,
            int handCount,
            int drawPileCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            DiscardedCount = discardedCount;
            HandCount = handCount;
            DrawPileCount = drawPileCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public int DiscardedCount { get; }
        public int HandCount { get; }
        public int DrawPileCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleEnemyTurnResolvedEvent
    {
        public BattleEnemyTurnResolvedEvent(
            int nodeId,
            int turnNumber,
            int enemyCount,
            int totalDamageToPlayer,
            int totalArmorGained,
            int totalResourcesPlundered,
            string summary,
            int handCount,
            int drawPileCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            EnemyCount = enemyCount;
            TotalDamageToPlayer = totalDamageToPlayer;
            TotalArmorGained = totalArmorGained;
            TotalResourcesPlundered = totalResourcesPlundered;
            Summary = summary ?? string.Empty;
            HandCount = handCount;
            DrawPileCount = drawPileCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public int EnemyCount { get; }
        public int TotalDamageToPlayer { get; }
        public int TotalArmorGained { get; }
        public int TotalResourcesPlundered { get; }
        public string Summary { get; }
        public int HandCount { get; }
        public int DrawPileCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleCardsDrawnEvent
    {
        public BattleCardsDrawnEvent(
            int nodeId,
            int turnNumber,
            int requestedCount,
            int drawnCount,
            int reshuffleCount,
            int handCount,
            int drawPileCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            RequestedCount = requestedCount;
            DrawnCount = drawnCount;
            ReshuffleCount = reshuffleCount;
            HandCount = handCount;
            DrawPileCount = drawPileCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public int RequestedCount { get; }
        public int DrawnCount { get; }
        public int ReshuffleCount { get; }
        public int HandCount { get; }
        public int DrawPileCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleFlowEndedEvent
    {
        public BattleFlowEndedEvent(
            int nodeId,
            int turnNumber,
            string reason,
            int handCount,
            int drawPileCount,
            int discardPileCount,
            int exhaustPileCount)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            Reason = reason ?? string.Empty;
            HandCount = handCount;
            DrawPileCount = drawPileCount;
            DiscardPileCount = discardPileCount;
            ExhaustPileCount = exhaustPileCount;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public string Reason { get; }
        public int HandCount { get; }
        public int DrawPileCount { get; }
        public int DiscardPileCount { get; }
        public int ExhaustPileCount { get; }
    }

    public readonly struct BattleSettledEvent
    {
        public BattleSettledEvent(
            int nodeId,
            int turnNumber,
            bool isVictory,
            IReadOnlyList<ResourceAmount> rewards,
            IReadOnlyList<ResourceAmount> resourcesLost,
            int cardsDiscardedCount,
            bool companionInjured,
            string settlementSummary)
        {
            NodeId = nodeId;
            TurnNumber = turnNumber;
            IsVictory = isVictory;
            Rewards = rewards ?? System.Array.Empty<ResourceAmount>();
            ResourcesLost = resourcesLost ?? System.Array.Empty<ResourceAmount>();
            CardsDiscardedCount = cardsDiscardedCount;
            CompanionInjured = companionInjured;
            SettlementSummary = settlementSummary ?? string.Empty;
        }

        public int NodeId { get; }
        public int TurnNumber { get; }
        public bool IsVictory { get; }
        public IReadOnlyList<ResourceAmount> Rewards { get; }
        public IReadOnlyList<ResourceAmount> ResourcesLost { get; }
        public int CardsDiscardedCount { get; }
        public bool CompanionInjured { get; }
        public string SettlementSummary { get; }
    }

    public readonly struct JourneyNodeEnteredEvent
    {
        public JourneyNodeEnteredEvent(int previousNodeId, int targetNodeId, JourneyNodeType nodeType, string sceneName)
        {
            PreviousNodeId = previousNodeId;
            TargetNodeId = targetNodeId;
            NodeType = nodeType;
            SceneName = sceneName ?? string.Empty;
        }

        public int PreviousNodeId { get; }
        public int TargetNodeId { get; }
        public JourneyNodeType NodeType { get; }
        public string SceneName { get; }
    }

    public readonly struct JourneyNodeCompletedEvent
    {
        public JourneyNodeCompletedEvent(int nodeId, JourneyNodeType nodeType, int foodCost, int foodBefore, int foodAfter)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            FoodCost = foodCost;
            FoodBefore = foodBefore;
            FoodAfter = foodAfter;
        }

        public int NodeId { get; }
        public JourneyNodeType NodeType { get; }
        public int FoodCost { get; }
        public int FoodBefore { get; }
        public int FoodAfter { get; }
    }

    public readonly struct JourneyAdvanceBlockedEvent
    {
        public JourneyAdvanceBlockedEvent(JourneyAdvanceBlockReason reason, string message, int currentNodeId, int foodAmount)
        {
            Reason = reason;
            Message = message ?? string.Empty;
            CurrentNodeId = currentNodeId;
            FoodAmount = foodAmount;
        }

        public JourneyAdvanceBlockReason Reason { get; }
        public string Message { get; }
        public int CurrentNodeId { get; }
        public int FoodAmount { get; }
    }

    public readonly struct CrisisDisasterTriggeredEvent
    {
        public CrisisDisasterTriggeredEvent(
            int crisisValue,
            int triggerThreshold,
            EventConfig disasterEvent,
            DisasterEventType disasterType,
            bool usedFallbackEvent)
        {
            CrisisValue = crisisValue;
            TriggerThreshold = triggerThreshold;
            DisasterEvent = disasterEvent;
            DisasterType = disasterType;
            UsedFallbackEvent = usedFallbackEvent;
        }

        public int CrisisValue { get; }
        public int TriggerThreshold { get; }
        public EventConfig DisasterEvent { get; }
        public DisasterEventType DisasterType { get; }
        public bool UsedFallbackEvent { get; }
    }
}
