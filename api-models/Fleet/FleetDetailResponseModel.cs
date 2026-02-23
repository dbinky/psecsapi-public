using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Fleet
{
    public class FleetDetailResponseModel
    {
        public Guid EntityId { get; set; }
        public Guid OwnerCorpId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? LastUpdateTimestamp { get; set; }
        public DateTime? CreateTimestamp { get; set; }
        public Guid? SectorId { get; set; }
        public List<Guid>? Ships { get; set; }
        public string? Status { get; set; }
        public QueueState? QueueStatus { get; set; }
        public DateTime? TransitETA { get; set; }
        public Guid? ActiveCombatId { get; set; }
        public DateTime? LastCombatTimestamp { get; set; }
        public Guid? AssignedCombatScriptId { get; set; }
    }

    public class QueueState
    {
        public Guid ConduitId { get; set; }
        public int QueueWidth { get; set; }
        public int QueueLength { get; set; }
        public int QueuePosition { get; set; }
        public DateTime EnqueuedTimestamp { get; set; }
    }
}
