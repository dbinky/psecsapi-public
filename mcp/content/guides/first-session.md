# First Session Guide

This guide is for AI agents working with a brand-new PSECS player. Follow these steps in order during the player's first session.

## Prerequisites
The player must have created a corporation. If they haven't, help them create one with `psecs_create_corp` first.

## Step 1: Explore Your Starting Sector

Run `psecs_explore_sector` with your fleet ID. Explain to the player:

> "You're at a **Nexus** — this is a commerce hub where you can buy ships, browse the market, and store cargo. But there are no resources to mine here — you need to travel outward to find resource-bearing sectors."

Note the conduits available — you'll use one of these to travel in the next step.

## Step 2: Allocate Research to 100%

Use `psecs_allocate_research` to allocate all available research capacity. This is critical:

> "Research ticks process every minute. Unallocated capacity is wasted time. **Always keep research at 100% allocation.**"

Help the player pick an initial research target. If unsure, suggest a Tier 1 technology in their preferred discipline, or a balanced default like Physical or Mechanical.

## Step 3: Transit a Short Conduit

Pick the shortest conduit from the scan results and use `psecs_navigate` to travel. Explain:

> "Traveling through conduits expands your map. Each new sector you visit is permanently added to your known universe. Short conduits mean faster transit — pick those first."

## Step 4: Explore the New Sector

Run `psecs_explore_sector` again. Describe what you find:
- **Sector type** (StarSystem, Nebula, Rubble, Void, etc.)
- **Resources available** (if any — Nebula and Rubble have sector-wide resources, StarSystems have orbital resources)
- **Conduits** for further travel
- **Other fleets** (potential threats or trading partners)

## Step 5: Build an HTML Dashboard

Now that you have fleet data, sector information, and research status, build a visual dashboard for the player. Include:

- **Fleet location and status** — where is the fleet, what's it doing
- **Research progress** — what's being researched, allocation percentages, time estimates
- **Resource inventory** — what resources have been collected
- **Known map** — sectors visited, conduit connections between them

Update the dashboard as you gather more data throughout the session. Visual presentation makes the game much more engaging and easier to understand.

## Step 6: Find Resources and Start Extracting

If the current sector has resources, start extracting with `psecs_mine_resource`. If not, keep exploring and transiting until you find a resource-bearing sector:
- **Nebula** sectors have gas and ore (sector-wide, no orbital needed)
- **Rubble** sectors have metals, ore, and gemstones (sector-wide)
- **StarSystem** sectors have resources on specific orbital bodies (planets, asteroid belts)

Once you find resources, explain what you're mining and why it matters for the player's goals.

## After the First Session

Once the player has explored a few sectors, started research, and begun extracting, they're past the onboarding phase. From here:
- Keep research at 100% allocation at all times
- Explore new sectors to find higher-quality resources
- Check the Nexus Market for trading opportunities
- Work toward manufacturing once research unlocks blueprints
- Update the dashboard each session with new information
