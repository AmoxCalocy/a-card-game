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
}
