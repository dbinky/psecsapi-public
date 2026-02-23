---
title: "Monitoring Loop"
---
# PSECS Monitoring Loop

Instructions for your AI agent to periodically check game state and report on your empire. Copy this into your agent's configuration or reference it as a recurring task.

## Check Frequency

- **Active play**: Every 5-10 minutes
- **Background monitoring**: Every 30 minutes
- **Overnight/AFK**: Every 1-2 hours (if your agent supports scheduled tasks)

## Status Checks

Run through this checklist on each monitoring cycle:

### 1. Corporation Overview
- Current credit balance
- Total fleet count and ship count
- Any alerts or notifications

### 2. Research Progress
- Active research allocations and completion percentages
- Any completed technologies (reallocate capacity immediately)
- Idle research capacity (should always be 0%)

### 3. Manufacturing Status
- Running jobs and progress
- Paused jobs (check why — insufficient resources? cargo full?)
- Completed jobs (clear output from cargo if needed)

### 4. Market Positions
- Active sale listings (any expired or completed?)
- Won auctions needing pickup (collect before storage fees accumulate)
- Outbid auctions (decide whether to re-bid)

### 5. Fleet Status
- Fleet locations and states (Idle, InTransit, Queued)
- Any fleets that have arrived at destination
- Ships currently extracting (check cargo levels)

### 6. Cargo Capacity
- Ships approaching full cargo (>80%)
- Ships with extraction paused due to full cargo
- Opportunities to transfer cargo between ships

## Escalation Rules

### Report Immediately (Alert the Commander)
- Credit balance drops below 100
- Unknown fleet enters a sector where you're mining
- Research capacity drops to 0% (all allocations complete)
- Any combat engagement detected
- Market listing expires without selling

### Handle Autonomously (Act and Report)
- Reallocate completed research to next priority
- Resume auto-paused manufacturing jobs (after clearing cargo)
- Collect completed market purchases
- Transfer cargo between ships when one is near capacity

### Log for Later (Include in Next Summary)
- Extraction progress and resource accumulation
- Market price trends for items you're selling
- New sectors discovered during transit
- Manufacturing job completion ETAs

## Status Report Format

When reporting, use this structure:

```
=== PSECS Status Report ===
Credits: [balance]
Fleets: [count] ([idle/transit/extracting breakdown])

Research: [X]% allocated
  - [Tech name]: [X]% complete (ETA: [time])
  - [Tech name]: [X]% complete (ETA: [time])

Manufacturing: [X] active, [X] paused, [X] complete
  - [Job]: [status] ([X]% complete)

Market: [X] active listings, [X] pending pickup
  - [Listing]: [status]

Alerts: [any issues requiring attention]
Next Actions: [recommended next steps]
===========================
```

## Tips

- Always reallocate research immediately when a technology completes
- Check cargo capacity before starting new extraction runs
- Collect market purchases promptly to avoid storage fees
- Keep a running log of resource quality observations for future reference
- If multiple items need attention, prioritize: research > manufacturing > market > extraction
