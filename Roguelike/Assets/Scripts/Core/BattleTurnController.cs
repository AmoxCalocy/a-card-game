using System;
using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BattleTurnController : MonoBehaviour
    {
        [Header("Turn Rules")]
        [Min(1)]
        [SerializeField] private int _maxEnergyPerTurn = 3;
        [Min(1)]
        [SerializeField] private int _cardsDrawPerTurn = 5;

        private readonly List<CardConfig> _drawPile = new List<CardConfig>();
        private readonly List<CardConfig> _hand = new List<CardConfig>();
        private readonly List<CardConfig> _discardPile = new List<CardConfig>();
        private readonly List<CardConfig> _exhaustPile = new List<CardConfig>();
        private readonly List<EnemyConfig> _enemyQueue = new List<EnemyConfig>();
        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _journeyNodeEnteredSubscription;
        private IDisposable _journeyNodeCompletedSubscription;
        private bool _isActive;
        private int _turnNumber;
        private int _currentEnergy;
        private int _activeNodeId = -1;
        private JourneyNodeType _activeNodeType = JourneyNodeType.Battle;
        private BattleTurnPhase _phase = BattleTurnPhase.None;
        private System.Random _random;

        public bool IsActive => _isActive;
        public int TurnNumber => _turnNumber;
        public int CurrentEnergy => _currentEnergy;
        public int MaxEnergyPerTurn => Mathf.Max(1, _maxEnergyPerTurn);
        public int CardsDrawPerTurn => Mathf.Max(1, _cardsDrawPerTurn);
        public int ActiveNodeId => _activeNodeId;
        public JourneyNodeType ActiveNodeType => _activeNodeType;
        public BattleTurnPhase Phase => _phase;
        public IReadOnlyList<EnemyConfig> EnemyQueue => _enemyQueue;
        public IReadOnlyList<CardConfig> DrawPile => _drawPile;
        public IReadOnlyList<CardConfig> Hand => _hand;
        public IReadOnlyList<CardConfig> DiscardPile => _discardPile;
        public IReadOnlyList<CardConfig> ExhaustPile => _exhaustPile;

        private void Awake()
        {
            GameServices.Register(this);
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            GameServices.Unregister<BattleTurnController>();
        }

        private void Update()
        {
            TryBind();
        }

        public bool TryPlayCard(int handIndex, out string message)
        {
            if (!_isActive)
            {
                message = "Battle flow is not active.";
                return false;
            }

            if (_phase != BattleTurnPhase.PlayerTurn)
            {
                message = "Cards can only be played during the player turn.";
                return false;
            }

            if (handIndex < 0 || handIndex >= _hand.Count)
            {
                message = $"Hand index {handIndex} is out of range.";
                return false;
            }

            CardConfig card = _hand[handIndex];
            if (card == null)
            {
                message = $"Card at hand index {handIndex} is null.";
                return false;
            }

            int cost = Mathf.Max(0, card.EnergyCost);
            if (_currentEnergy < cost)
            {
                message = $"Not enough energy to play '{card.DisplayName}'. Current={_currentEnergy}, Cost={cost}.";
                return false;
            }

            int energyBefore = _currentEnergy;
            _currentEnergy -= cost;
            _hand.RemoveAt(handIndex);

            bool exhausted = card.ExhaustOnPlay;
            if (exhausted)
            {
                _exhaustPile.Add(card);
            }
            else
            {
                _discardPile.Add(card);
            }

            Publish(new BattleCardPlayedEvent(
                _activeNodeId,
                _turnNumber,
                card,
                energyBefore,
                _currentEnergy,
                exhausted,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));

            message = string.Empty;
            return true;
        }

        public bool TryPlayFirstCard(out string message)
        {
            if (!_isActive)
            {
                message = "Battle flow is not active.";
                return false;
            }

            if (_phase != BattleTurnPhase.PlayerTurn)
            {
                message = "Cards can only be played during the player turn.";
                return false;
            }

            for (int index = 0; index < _hand.Count; index++)
            {
                CardConfig card = _hand[index];
                if (card == null)
                {
                    continue;
                }

                if (_currentEnergy >= Mathf.Max(0, card.EnergyCost))
                {
                    return TryPlayCard(index, out message);
                }
            }

            message = "No playable card in hand with current energy.";
            return false;
        }

        public bool TryEndPlayerTurn(out string message)
        {
            if (!_isActive)
            {
                message = "Battle flow is not active.";
                return false;
            }

            if (_phase != BattleTurnPhase.PlayerTurn)
            {
                message = "Player turn is not active.";
                return false;
            }

            int discarded = DiscardHand();
            Publish(new BattleHandDiscardedEvent(
                _activeNodeId,
                _turnNumber,
                discarded,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));

            _phase = BattleTurnPhase.EnemyTurn;
            Publish(new BattleEnemyTurnResolvedEvent(
                _activeNodeId,
                _turnNumber,
                _enemyQueue.Count,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));

            BeginPlayerTurn();
            message = string.Empty;
            return true;
        }

        private bool TryBind()
        {
            if (_context == null)
            {
                _context = GameContext.Instance;
                if (_context == null)
                {
                    GameServices.TryResolve(out _context);
                }
            }

            if (_eventBus == null)
            {
                if (_context != null)
                {
                    _eventBus = _context.EventBus;
                }

                if (_eventBus == null)
                {
                    GameServices.TryResolve(out _eventBus);
                }
            }

            if (_eventBus == null)
            {
                return false;
            }

            if (_journeyNodeEnteredSubscription == null)
            {
                _journeyNodeEnteredSubscription = _eventBus.Subscribe<JourneyNodeEnteredEvent>(HandleJourneyNodeEntered);
                _journeyNodeCompletedSubscription = _eventBus.Subscribe<JourneyNodeCompletedEvent>(HandleJourneyNodeCompleted);
            }

            return true;
        }

        private void Unbind()
        {
            _journeyNodeEnteredSubscription?.Dispose();
            _journeyNodeCompletedSubscription?.Dispose();
            _journeyNodeEnteredSubscription = null;
            _journeyNodeCompletedSubscription = null;
            _eventBus = null;
            _context = null;
        }

        private void HandleJourneyNodeEntered(JourneyNodeEnteredEvent evt)
        {
            if (evt.NodeType != JourneyNodeType.Battle && evt.NodeType != JourneyNodeType.Boss)
            {
                EndBattleFlow("Entered non-battle node.");
                return;
            }

            StartBattleFlowFromContext(evt.TargetNodeId, evt.NodeType);
        }

        private void HandleJourneyNodeCompleted(JourneyNodeCompletedEvent evt)
        {
            if (!_isActive || evt.NodeId != _activeNodeId)
            {
                return;
            }

            EndBattleFlow("Journey node completed.");
        }

        private void StartBattleFlowFromContext(int nodeId, JourneyNodeType nodeType)
        {
            if (_context == null)
            {
                return;
            }

            BattleEncounterConfig encounter = _context.ActiveBattleEncounterConfig;
            if (encounter == null || encounter.NodeId != nodeId)
            {
                EndBattleFlow("Battle encounter config missing at node entry.");
                return;
            }

            _isActive = true;
            _activeNodeId = nodeId;
            _activeNodeType = nodeType;
            _phase = BattleTurnPhase.None;
            _turnNumber = 0;
            _currentEnergy = 0;
            _enemyQueue.Clear();
            _enemyQueue.AddRange(encounter.EnemyQueue);
            BuildStartingDeck(encounter.EncounterSeed);
            Publish(new BattleFlowInitializedEvent(
                _activeNodeId,
                _activeNodeType,
                encounter.EncounterSeed,
                _enemyQueue.Count,
                _drawPile.Count,
                _hand.Count,
                _discardPile.Count,
                _exhaustPile.Count));
            BeginPlayerTurn();
        }

        private void BuildStartingDeck(int seed)
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _exhaustPile.Clear();

            if (_context != null)
            {
                for (int i = 0; i < _context.CardPool.Count; i++)
                {
                    CardConfig card = _context.CardPool[i];
                    if (card != null)
                    {
                        _drawPile.Add(card);
                    }
                }
            }

            _random = new System.Random(seed);
            Shuffle(_drawPile);
        }

        private void BeginPlayerTurn()
        {
            if (!_isActive)
            {
                return;
            }

            _turnNumber += 1;
            _phase = BattleTurnPhase.PlayerTurn;
            _currentEnergy = MaxEnergyPerTurn;
            int drawn = DrawCards(CardsDrawPerTurn);
            Publish(new BattleTurnStartedEvent(
                _activeNodeId,
                _turnNumber,
                _currentEnergy,
                drawn,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));
        }

        private int DiscardHand()
        {
            int discarded = _hand.Count;
            for (int index = 0; index < _hand.Count; index++)
            {
                CardConfig card = _hand[index];
                if (card != null)
                {
                    _discardPile.Add(card);
                }
            }

            _hand.Clear();
            return discarded;
        }

        private int DrawCards(int count)
        {
            int requested = Mathf.Max(0, count);
            int drawn = 0;
            int reshuffleCount = 0;
            for (int index = 0; index < requested; index++)
            {
                if (_drawPile.Count == 0)
                {
                    if (!TryReshuffleDiscardIntoDrawPile())
                    {
                        break;
                    }

                    reshuffleCount += 1;
                }

                int drawIndex = _drawPile.Count - 1;
                CardConfig card = _drawPile[drawIndex];
                _drawPile.RemoveAt(drawIndex);
                if (card != null)
                {
                    _hand.Add(card);
                }

                drawn += 1;
            }

            Publish(new BattleCardsDrawnEvent(
                _activeNodeId,
                _turnNumber,
                requested,
                drawn,
                reshuffleCount,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));
            return drawn;
        }

        private bool TryReshuffleDiscardIntoDrawPile()
        {
            if (_discardPile.Count == 0)
            {
                return false;
            }

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
            return true;
        }

        private void Shuffle(List<CardConfig> cards)
        {
            if (cards == null || cards.Count <= 1)
            {
                return;
            }

            if (_random == null)
            {
                _random = new System.Random();
            }

            for (int index = cards.Count - 1; index > 0; index--)
            {
                int swapIndex = _random.Next(0, index + 1);
                (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
            }
        }

        private void EndBattleFlow(string reason)
        {
            if (!_isActive)
            {
                return;
            }

            Publish(new BattleFlowEndedEvent(
                _activeNodeId,
                _turnNumber,
                reason ?? string.Empty,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));

            _isActive = false;
            _turnNumber = 0;
            _currentEnergy = 0;
            _activeNodeId = -1;
            _activeNodeType = JourneyNodeType.Battle;
            _phase = BattleTurnPhase.None;
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _exhaustPile.Clear();
            _enemyQueue.Clear();
        }

        private void Publish<TEvent>(TEvent gameEvent)
        {
            _eventBus?.Publish(gameEvent);
        }
    }
}
