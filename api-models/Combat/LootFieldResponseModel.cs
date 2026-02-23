namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// A loot field in a sector, created when a ship is destroyed in combat.
    /// </summary>
    public class LootFieldResponseModel
    {
        /// <summary>Unique identifier for this loot field.</summary>
        public Guid Id { get; set; }

        /// <summary>X position in the sector where cargo was dropped.</summary>
        public double PositionX { get; set; }

        /// <summary>Y position in the sector where cargo was dropped.</summary>
        public double PositionY { get; set; }

        /// <summary>Number of items in this loot field.</summary>
        public int ItemCount { get; set; }

        /// <summary>Whether this loot field is currently exclusive to the victor's corp.</summary>
        public bool IsExclusive { get; set; }

        /// <summary>When the loot field expires and despawns (UTC).</summary>
        public DateTime ExpiresAt { get; set; }
    }
}
