using System;
using System.Collections.Generic;
using System.Text;
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
        [Header("Player Combat Stats")]
        [Min(1)]
        [SerializeField] private int _playerMaxHealth = 40;
        [Header("Enemy Intent Rules")]
        [Min(0)]
        [SerializeField] private int _enemyDefendBonusWhenNoBaseDefense = 2;
        [Min(1)]
        [SerializeField] private int _enemyPlunderAmount = 2;
        [Header("Defeat Penalties")]
        [Range(0f, 1f)]
        [SerializeField] private float _defeatWealthLossPercent = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float _defeatFoodLossPercent = 0.2f;
        [Min(1)]
        [SerializeField] private int _defeatCardsLostCount = 1;

        private readonly List<CardConfig> _drawPile = new List<CardConfig>();
        private readonly List<CardConfig> _hand = new List<CardConfig>();
        private readonly List<CardConfig> _discardPile = new List<CardConfig>();
        private readonly List<CardConfig> _exhaustPile = new List<CardConfig>();
        private readonly List<EnemyConfig> _enemyQueue = new List<EnemyConfig>();
        private readonly List<BattleCombatantState> _enemyStates = new List<BattleCombatantState>();
        private readonly List<BattleEnemyIntentView> _enemyIntents = new List<BattleEnemyIntentView>();
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
        private BattleCombatantState _playerState;
        private string _lastCardEffectSummary = string.Empty;
        private string _lastEnemyTurnSummary = "none";

        public bool IsActive => _isActive;
        public int TurnNumber => _turnNumber;
        public int CurrentEnergy => _currentEnergy;
        public int MaxEnergyPerTurn => Mathf.Max(1, _maxEnergyPerTurn);
        public int CardsDrawPerTurn => Mathf.Max(1, _cardsDrawPerTurn);
        public int PlayerMaxHealth => _playerState == null ? Mathf.Max(1, _playerMaxHealth) : _playerState.MaxHealth;
        public int PlayerCurrentHealth => _playerState == null ? PlayerMaxHealth : _playerState.CurrentHealth;
        public int PlayerArmor => _playerState == null ? 0 : _playerState.Armor;
        public string PlayerStatusSummary => _playerState == null ? "none" : _playerState.GetStatusSummary();
        public string LastCardEffectSummary => string.IsNullOrWhiteSpace(_lastCardEffectSummary) ? "none" : _lastCardEffectSummary;
        public string LastEnemyTurnSummary => string.IsNullOrWhiteSpace(_lastEnemyTurnSummary) ? "none" : _lastEnemyTurnSummary;
        public int ActiveNodeId => _activeNodeId;
        public JourneyNodeType ActiveNodeType => _activeNodeType;
        public BattleTurnPhase Phase => _phase;
        public IReadOnlyList<EnemyConfig> EnemyQueue => _enemyQueue;
        public IReadOnlyList<BattleCombatantState> EnemyStates => _enemyStates;
        public IReadOnlyList<BattleEnemyIntentView> EnemyIntents => _enemyIntents;
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

            CardEffectResult result = ExecuteCardEffect(card);
            _lastCardEffectSummary = result.Summary;
            Publish(new BattleCardPlayedEvent(
                _activeNodeId,
                _turnNumber,
                card,
                card.CardType,
                Mathf.Max(0, card.BaseValue),
                card.StatusEffect,
                Mathf.Max(0, card.StatusStacks),
                energyBefore,
                _currentEnergy,
                exhausted,
                result.DamageApplied,
                result.ArmorApplied,
                result.HealingApplied,
                result.CardsDrawn,
                result.StatusStacksApplied,
                result.Summary,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));

            CheckBattleOutcome();

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
            EnemyTurnResolution resolution = ResolveEnemyTurn();
            _lastEnemyTurnSummary = resolution.Summary;
            Publish(new BattleEnemyTurnResolvedEvent(
                _activeNodeId,
                _turnNumber,
                _enemyQueue.Count,
                resolution.TotalDamageToPlayer,
                resolution.TotalArmorGained,
                resolution.TotalResourcesPlundered,
                resolution.Summary,
                _hand.Count,
                _drawPile.Count,
                _discardPile.Count,
                _exhaustPile.Count));

            if (!CheckBattleOutcome())
            {
                BeginPlayerTurn();
            }

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
            InitializeBattleState();
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

        private void InitializeBattleState()
        {
            _playerState = new BattleCombatantState(
                "player",
                "Player",
                Mathf.Max(1, _playerMaxHealth),
                0);

            _enemyStates.Clear();
            for (int i = 0; i < _enemyQueue.Count; i++)
            {
                EnemyConfig enemyConfig = _enemyQueue[i];
                if (enemyConfig == null)
                {
                    _enemyStates.Add(new BattleCombatantState(
                        $"enemy-{i}",
                        $"Enemy {i + 1}",
                        1,
                        0));
                    continue;
                }

                _enemyStates.Add(new BattleCombatantState(
                    enemyConfig.Id,
                    enemyConfig.DisplayName,
                    enemyConfig.MaxHealth,
                    enemyConfig.BaseDefense,
                    enemyConfig));
            }

            _lastCardEffectSummary = "Battle initialized.";
            _lastEnemyTurnSummary = "Awaiting first enemy turn.";
        }

        private CardEffectResult ExecuteCardEffect(CardConfig card)
        {
            int baseValue = Mathf.Max(0, card.BaseValue);
            int requestedStacks = Mathf.Max(0, card.StatusStacks);
            StatusEffectType statusEffect = card.StatusEffect;
            var result = new CardEffectResult();

            switch (card.CardType)
            {
                case CardType.Attack:
                {
                    BattleCombatantState target = GetFirstAliveEnemy();
                    if (target == null)
                    {
                        result.Summary = "Attack had no valid enemy target.";
                        break;
                    }

                    result.DamageApplied = target.ApplyDamage(baseValue);
                    result.StatusStacksApplied = target.AddStatus(statusEffect, requestedStacks);
                    result.Summary = $"Attack -> {FormatCombatantState(target)}; damage={result.DamageApplied}, status+={result.StatusStacksApplied}.";
                    break;
                }
                case CardType.Defense:
                {
                    if (_playerState == null)
                    {
                        result.Summary = "Defense effect skipped because player state is missing.";
                        break;
                    }

                    result.ArmorApplied = _playerState.AddArmor(baseValue);
                    result.StatusStacksApplied = _playerState.AddStatus(statusEffect, requestedStacks);
                    result.Summary = $"Defense -> {FormatCombatantState(_playerState)}; armor+={result.ArmorApplied}, status+={result.StatusStacksApplied}.";
                    break;
                }
                case CardType.Strategy:
                {
                    result.CardsDrawn = DrawCards(baseValue);
                    if (_playerState != null)
                    {
                        result.StatusStacksApplied = _playerState.AddStatus(statusEffect, requestedStacks);
                        result.Summary = $"Strategy -> draw+={result.CardsDrawn}, player={FormatCombatantState(_playerState)}, status+={result.StatusStacksApplied}.";
                    }
                    else
                    {
                        result.Summary = $"Strategy -> draw+={result.CardsDrawn}.";
                    }

                    break;
                }
                case CardType.Logistics:
                {
                    if (_playerState == null)
                    {
                        result.Summary = "Logistics effect skipped because player state is missing.";
                        break;
                    }

                    result.HealingApplied = _playerState.Heal(baseValue);
                    result.StatusStacksApplied = _playerState.AddStatus(statusEffect, requestedStacks);
                    result.Summary = $"Logistics -> {FormatCombatantState(_playerState)}; heal+={result.HealingApplied}, status+={result.StatusStacksApplied}.";
                    break;
                }
                case CardType.Tactic:
                {
                    BattleCombatantState target = GetFirstAliveEnemy();
                    if (target != null && baseValue > 0)
                    {
                        result.DamageApplied = target.ApplyDamage(baseValue);
                    }

                    result.StatusStacksApplied = AddStatusToAliveEnemies(statusEffect, requestedStacks);
                    string targetSummary = target == null ? "none" : FormatCombatantState(target);
                    result.Summary = $"Tactic -> target={targetSummary}, damage={result.DamageApplied}, totalStatusApplied={result.StatusStacksApplied}.";
                    break;
                }
                default:
                {
                    result.Summary = $"Card type {card.CardType} is not mapped to a runtime effect.";
                    break;
                }
            }

            return result;
        }

        private BattleCombatantState GetFirstAliveEnemy()
        {
            for (int i = 0; i < _enemyStates.Count; i++)
            {
                if (!_enemyStates[i].IsDefeated)
                {
                    return _enemyStates[i];
                }
            }

            return null;
        }

        private int AddStatusToAliveEnemies(StatusEffectType statusEffect, int stacks)
        {
            if (statusEffect == StatusEffectType.None || stacks <= 0)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < _enemyStates.Count; i++)
            {
                if (_enemyStates[i].IsDefeated)
                {
                    continue;
                }

                total += _enemyStates[i].AddStatus(statusEffect, stacks);
            }

            return total;
        }

        private static string FormatCombatantState(BattleCombatantState state)
        {
            if (state == null)
            {
                return "null";
            }

            return $"{state.DisplayName}(HP {state.CurrentHealth}/{state.MaxHealth}, Armor {state.Armor}, Status {state.GetStatusSummary()})";
        }

        private void BeginPlayerTurn()
        {
            if (!_isActive)
            {
                return;
            }

            _turnNumber += 1;
            RebuildEnemyIntentPlan();
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

        private bool CheckBattleOutcome()
        {
            if (!_isActive)
            {
                return false;
            }

            if (AllEnemiesDefeated())
            {
                ResolveVictory();
                return true;
            }

            if (IsPlayerDefeated())
            {
                ResolveDefeat();
                return true;
            }

            return false;
        }

        private bool AllEnemiesDefeated()
        {
            for (int i = 0; i < _enemyStates.Count; i++)
            {
                if (!_enemyStates[i].IsDefeated)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPlayerDefeated()
        {
            return _playerState != null && _playerState.IsDefeated;
        }

        private void ResolveVictory()
        {
            var rewardMap = new Dictionary<ResourceType, int>();
            for (int i = 0; i < _enemyStates.Count; i++)
            {
                EnemyConfig enemyConfig = _enemyStates[i].EnemyConfig;
                if (enemyConfig == null)
                {
                    continue;
                }

                IReadOnlyList<ResourceAmount> rewards = enemyConfig.DefeatRewards;
                if (rewards == null || rewards.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < rewards.Count; j++)
                {
                    ResourceAmount reward = rewards[j];
                    if (reward.Amount <= 0)
                    {
                        continue;
                    }

                    int current = rewardMap.TryGetValue(reward.Type, out int existing) ? existing : 0;
                    rewardMap[reward.Type] = current + reward.Amount;
                }
            }

            var rewardList = new List<ResourceAmount>(rewardMap.Count);
            var summaryBuilder = new StringBuilder();
            summaryBuilder.Append("Victory! Rewards: ");
            bool hasRewards = false;
            foreach (KeyValuePair<ResourceType, int> entry in rewardMap)
            {
                if (_context != null)
                {
                    _context.AddResource(entry.Key, entry.Value);
                }

                rewardList.Add(new ResourceAmount { Type = entry.Key, Amount = entry.Value });

                if (hasRewards)
                {
                    summaryBuilder.Append(", ");
                }

                summaryBuilder.Append($"+{entry.Value} {entry.Key}");
                hasRewards = true;
            }

            if (!hasRewards)
            {
                summaryBuilder.Append("none");
            }

            string summary = summaryBuilder.ToString();

            Publish(new BattleSettledEvent(
                _activeNodeId,
                _turnNumber,
                isVictory: true,
                rewardList,
                resourcesLost: System.Array.Empty<ResourceAmount>(),
                cardsDiscardedCount: 0,
                companionInjured: false,
                summary));

            EndBattleFlow("Victory");
        }

        private void ResolveDefeat()
        {
            int wealthLost = 0;
            int foodLost = 0;
            int cardsLostCount = 0;
            var resourcesLostList = new List<ResourceAmount>();

            if (_context != null)
            {
                int currentWealth = _context.GetResource(ResourceType.Wealth);
                if (currentWealth > 0)
                {
                    wealthLost = Mathf.Max(0, Mathf.RoundToInt(currentWealth * _defeatWealthLossPercent));
                    if (wealthLost > 0)
                    {
                        _context.AddResource(ResourceType.Wealth, -wealthLost);
                        resourcesLostList.Add(new ResourceAmount { Type = ResourceType.Wealth, Amount = wealthLost });
                    }
                }

                int currentFood = _context.GetResource(ResourceType.Food);
                if (currentFood > 0)
                {
                    foodLost = Mathf.Max(0, Mathf.RoundToInt(currentFood * _defeatFoodLossPercent));
                    if (foodLost > 0)
                    {
                        _context.AddResource(ResourceType.Food, -foodLost);
                        resourcesLostList.Add(new ResourceAmount { Type = ResourceType.Food, Amount = foodLost });
                    }
                }

                for (int i = 0; i < _defeatCardsLostCount; i++)
                {
                    if (_context.TryRemoveRandomCard(out _))
                    {
                        cardsLostCount++;
                    }
                }
            }

            var summaryBuilder = new StringBuilder();
            summaryBuilder.Append("Defeat! Lost: ");
            bool hasLoss = false;

            if (wealthLost > 0)
            {
                summaryBuilder.Append($"-{wealthLost} Wealth");
                hasLoss = true;
            }

            if (foodLost > 0)
            {
                if (hasLoss) summaryBuilder.Append(", ");
                summaryBuilder.Append($"-{foodLost} Food");
                hasLoss = true;
            }

            if (cardsLostCount > 0)
            {
                if (hasLoss) summaryBuilder.Append(", ");
                summaryBuilder.Append($"-{cardsLostCount} card(s)");
                hasLoss = true;
            }

            if (!hasLoss)
            {
                summaryBuilder.Append("none");
            }

            string summary = summaryBuilder.ToString();

            Publish(new BattleSettledEvent(
                _activeNodeId,
                _turnNumber,
                isVictory: false,
                rewards: System.Array.Empty<ResourceAmount>(),
                resourcesLostList,
                cardsLostCount,
                companionInjured: false,
                summary));

            EndBattleFlow("Defeat");
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
            _enemyStates.Clear();
            _enemyIntents.Clear();
            _playerState = null;
            _lastCardEffectSummary = string.Empty;
            _lastEnemyTurnSummary = string.Empty;
        }

        private void RebuildEnemyIntentPlan()
        {
            _enemyIntents.Clear();
            for (int index = 0; index < _enemyStates.Count; index++)
            {
                BattleCombatantState enemyState = _enemyStates[index];
                EnemyIntentType intentType = SelectEnemyIntent(enemyState, index);
                int intentValue = CalculateIntentValue(enemyState, intentType);
                bool isDefeated = enemyState == null || enemyState.IsDefeated;
                string summary = BuildEnemyIntentSummary(enemyState, intentType, intentValue, isDefeated);
                _enemyIntents.Add(new BattleEnemyIntentView(
                    index,
                    enemyState?.Id,
                    enemyState?.DisplayName,
                    intentType,
                    intentValue,
                    isDefeated,
                    summary));
            }

            Publish(new BattleEnemyIntentUpdatedEvent(
                _activeNodeId,
                _turnNumber,
                new List<BattleEnemyIntentView>(_enemyIntents)));
        }

        private EnemyIntentType SelectEnemyIntent(BattleCombatantState enemyState, int enemyIndex)
        {
            if (enemyState == null || enemyState.IsDefeated)
            {
                return EnemyIntentType.Attack;
            }

            EnemyIntentType primaryIntent = enemyState.EnemyConfig == null
                ? EnemyIntentType.Attack
                : enemyState.EnemyConfig.PrimaryIntent;
            int pattern = Math.Abs(_turnNumber + enemyIndex) % 3;
            return pattern switch
            {
                0 => primaryIntent,
                1 => GetNextIntent(primaryIntent),
                _ => GetPreviousIntent(primaryIntent)
            };
        }

        private int CalculateIntentValue(BattleCombatantState enemyState, EnemyIntentType intentType)
        {
            if (enemyState == null || enemyState.IsDefeated)
            {
                return 0;
            }

            EnemyConfig enemyConfig = enemyState.EnemyConfig;
            int baseAttack = enemyConfig == null ? 0 : Mathf.Max(0, enemyConfig.BaseAttack);
            int baseDefense = enemyConfig == null ? 0 : Mathf.Max(0, enemyConfig.BaseDefense);
            return intentType switch
            {
                EnemyIntentType.Attack => Mathf.Max(1, baseAttack),
                EnemyIntentType.Defend => Mathf.Max(1, baseDefense > 0 ? baseDefense : baseAttack / 2 + _enemyDefendBonusWhenNoBaseDefense),
                EnemyIntentType.Plunder => Mathf.Max(1, _enemyPlunderAmount),
                _ => 1
            };
        }

        private static EnemyIntentType GetNextIntent(EnemyIntentType intentType)
        {
            return intentType switch
            {
                EnemyIntentType.Attack => EnemyIntentType.Defend,
                EnemyIntentType.Defend => EnemyIntentType.Plunder,
                _ => EnemyIntentType.Attack
            };
        }

        private static EnemyIntentType GetPreviousIntent(EnemyIntentType intentType)
        {
            return intentType switch
            {
                EnemyIntentType.Attack => EnemyIntentType.Plunder,
                EnemyIntentType.Defend => EnemyIntentType.Attack,
                _ => EnemyIntentType.Defend
            };
        }

        private static string BuildEnemyIntentSummary(
            BattleCombatantState enemyState,
            EnemyIntentType intentType,
            int intentValue,
            bool isDefeated)
        {
            string enemyName = enemyState == null ? "Unknown Enemy" : enemyState.DisplayName;
            if (isDefeated)
            {
                return $"{enemyName} defeated, no action.";
            }

            return intentType switch
            {
                EnemyIntentType.Attack => $"{enemyName}: Attack for {intentValue}.",
                EnemyIntentType.Defend => $"{enemyName}: Defend for {intentValue} armor.",
                EnemyIntentType.Plunder => $"{enemyName}: Plunder up to {intentValue} resources.",
                _ => $"{enemyName}: Unknown intent."
            };
        }

        private EnemyTurnResolution ResolveEnemyTurn()
        {
            if (_enemyIntents.Count == 0)
            {
                RebuildEnemyIntentPlan();
            }

            int totalDamageToPlayer = 0;
            int totalArmorGained = 0;
            int totalResourcesPlundered = 0;
            var summaryBuilder = new StringBuilder();
            bool hasAction = false;

            for (int index = 0; index < _enemyIntents.Count; index++)
            {
                BattleEnemyIntentView intent = _enemyIntents[index];
                if (intent.IsDefeated || intent.IntentValue <= 0)
                {
                    continue;
                }

                hasAction = true;
                BattleCombatantState enemyState = index >= 0 && index < _enemyStates.Count ? _enemyStates[index] : null;
                int actionDamage = 0;
                int actionArmor = 0;
                int actionPlunder = 0;

                switch (intent.IntentType)
                {
                    case EnemyIntentType.Attack:
                        if (_playerState != null)
                        {
                            actionDamage = _playerState.ApplyDamage(intent.IntentValue);
                            totalDamageToPlayer += actionDamage;
                        }

                        summaryBuilder.Append($"{intent.EnemyDisplayName} attacked for {actionDamage}. ");
                        break;
                    case EnemyIntentType.Defend:
                        if (enemyState != null)
                        {
                            actionArmor = enemyState.AddArmor(intent.IntentValue);
                            totalArmorGained += actionArmor;
                        }

                        summaryBuilder.Append($"{intent.EnemyDisplayName} gained {actionArmor} armor. ");
                        break;
                    case EnemyIntentType.Plunder:
                        actionPlunder = TryPlunderResources(intent.IntentValue);
                        totalResourcesPlundered += actionPlunder;
                        if (_playerState != null)
                        {
                            _playerState.AddStatus(StatusEffectType.Plunder, actionPlunder > 0 ? 1 : 0);
                        }

                        summaryBuilder.Append($"{intent.EnemyDisplayName} plundered {actionPlunder}. ");
                        break;
                }
            }

            if (!hasAction)
            {
                summaryBuilder.Append("No active enemy actions.");
            }

            return new EnemyTurnResolution(
                totalDamageToPlayer,
                totalArmorGained,
                totalResourcesPlundered,
                summaryBuilder.ToString().Trim());
        }

        private int TryPlunderResources(int amount)
        {
            if (_context == null || amount <= 0)
            {
                return 0;
            }

            int remaining = amount;
            int plundered = 0;

            int wealthBefore = _context.GetResource(ResourceType.Wealth);
            if (wealthBefore > 0)
            {
                _context.AddResource(ResourceType.Wealth, -remaining);
                int wealthAfter = _context.GetResource(ResourceType.Wealth);
                int stolenFromWealth = wealthBefore - wealthAfter;
                plundered += stolenFromWealth;
                remaining -= stolenFromWealth;
            }

            if (remaining > 0)
            {
                int foodBefore = _context.GetResource(ResourceType.Food);
                if (foodBefore > 0)
                {
                    _context.AddResource(ResourceType.Food, -remaining);
                    int foodAfter = _context.GetResource(ResourceType.Food);
                    int stolenFromFood = foodBefore - foodAfter;
                    plundered += stolenFromFood;
                }
            }

            return plundered;
        }

        private void Publish<TEvent>(TEvent gameEvent)
        {
            _eventBus?.Publish(gameEvent);
        }

        private struct CardEffectResult
        {
            public int DamageApplied;
            public int ArmorApplied;
            public int HealingApplied;
            public int CardsDrawn;
            public int StatusStacksApplied;
            public string Summary;
        }

        private readonly struct EnemyTurnResolution
        {
            public EnemyTurnResolution(
                int totalDamageToPlayer,
                int totalArmorGained,
                int totalResourcesPlundered,
                string summary)
            {
                TotalDamageToPlayer = totalDamageToPlayer;
                TotalArmorGained = totalArmorGained;
                TotalResourcesPlundered = totalResourcesPlundered;
                Summary = summary ?? string.Empty;
            }

            public int TotalDamageToPlayer { get; }
            public int TotalArmorGained { get; }
            public int TotalResourcesPlundered { get; }
            public string Summary { get; }
        }
    }
}
