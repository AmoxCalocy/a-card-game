using System;

namespace OneManJourney.Data
{
    public enum CardType
    {
        Attack,
        Defense,
        Strategy,
        Tactic,
        Logistics
    }

    public enum RarityTier
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public enum StatusEffectType
    {
        None,
        Bleed,
        Armor,
        Morale,
        Disease,
        Fatigue,
        Encircled,
        Plunder
    }

    public enum EnemyIntentType
    {
        Attack,
        Defend,
        Plunder
    }

    public enum EventResolutionType
    {
        Combat,
        SkillCheck,
        PayResource,
        SacrificeCard
    }

    public enum DisasterEventType
    {
        None,
        Plague,
        BanditRaid,
        NaturalDisaster
    }

    public enum CompanionRole
    {
        Damage,
        Guard,
        Control,
        Support,
        Diplomacy
    }

    public enum RelicTriggerType
    {
        Passive,
        BattleStart,
        TurnStart,
        EventResolved,
        OneShot
    }

    public enum RelicModifierType
    {
        Energy,
        Draw,
        ResourceGain,
        StatusPotency,
        CostReduction
    }

    public enum ResourceType
    {
        Food,
        Wealth,
        Reputation,
        MedicalSupplies,
        BuildingMaterials,
        Intel,
        DraftOrder,
        Crisis
    }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Type;
        public int Amount;
    }
}
