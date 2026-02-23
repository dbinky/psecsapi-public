# PSECS Game Mechanics

## Overview
PSECS (Persistent Space Economy & Commerce Simulator) is a multiplayer space commerce game.
You manage a corporation with fleets of ships, exploring space, mining resources,
researching technology, manufacturing items, and trading on the market.

## Resource Extraction
- Resources spawn in sectors at orbital positions (planets, asteroids, nebulae)
- Ships with extraction modules can mine resources
- Resources are classified by a 4-level taxonomy: Group → Type → Class → Order
  - **Group**: Inorganic, Organic
  - **Type**: Mineral, Chemical, Flora, Fauna, Microscopic, Energetic
  - **Class**: Metal, Ore, Gemstone, Gas, Liquid, Food, NonFood, Meat, Hide, Bone, Algae, Bacteria
  - **Order**: most specific (e.g., Ferromagnetic, Precious, ReactiveGas, HardWood)
- Each resource also has numeric quality properties (OQ, PE, SR, etc.) and a density value
- Blueprint inputs specify a qualifier level (Group/Type/Class/Order) + value to match resources
- Higher quality resources produce higher quality manufactured goods

## Research System
- 7-tier tech tree with multiple disciplines
- Research unlocks: technologies, applications (blueprints + modifiers)
- Research capacity comes from ship modules
- Allocate research as percentages across targets (psecs_allocate_research)
- Stop research to free capacity for new priorities (psecs_stop_research) — progress is preserved
- Research processes on 1-minute tick cycles

## Manufacturing
- Blueprints turn raw resources + components into higher-tier items
- Manufacturing capacity comes from ship modules
- Quality properties flow from inputs to outputs
- Jobs can be paused and resumed
- Manufacturing processes on 1-minute tick cycles

## Market (Nexus Market)
- Two sale types: BuyNow (fixed price) and Auction (competitive bidding)
- Auctions have anti-sniping: 5-minute extension on late bids
- Storage fees: BuyNow charges 1% × price × duration days upfront at listing creation (non-refundable); Auction charges 0.5% × final sale price × duration days, deducted from seller proceeds at completion
- Items are wrapped as boxed assets for trade

## Fleet Navigation
- Fleets move between sectors through conduits (wormholes)
- Fleet speed = slowest ship's speed
- Three states: Idle, Queued (at conduit), InTransit
- Conduits have length (transit time = length / speed)

## Combat
- Fleet vs fleet combat — both fleets must be Idle and in the same non-Nexus sector
- Combat is asynchronous: initiate with psecs_engage_combat, monitor with psecs_combat_status
- Ships without a combat script assigned default to **flee behavior** (move toward grid edge)
- Combat scripts are JavaScript programs that control ship behavior each tick
- Scripts must be created first (psecs_raw_create_corp_scripts), then assigned to a fleet (psecs_raw_update_fleet_combat_script)
- After combat: destroyed ships drop loot fields (victor-exclusive for 1 hour, then public for 24 hours)
- Loot despawns after 24 hours — use psecs_scan_loot to find loot fields and psecs_pickup_loot to collect them
- Combat replays available for 90 days (psecs_raw_combat_replay)
- Read the `psecs://guide/combat-scripting` resource for the full scripting API reference

## Ship Building (Shipyard)
- New ships are built at the Nexus Station shipyard from chassis blueprints
- Use psecs_shipyard_browse to see available ship classes (Scout, Corvette, etc.) and the build queue
- Check blueprint costs with psecs_raw_shipyard_blueprint (pass slot counts for total cost calculation)
- Start a build with psecs_shipyard_start_build, providing a catalog ID, blueprint instance, and input assets
- Monitor the queue with psecs_shipyard_browse — builds process sequentially
- Pick up completed ships with psecs_shipyard_pickup into a fleet
- Ships start empty — install modules from cargo using psecs_ship_manage_modules

## Inventory & Cargo Management
- The inventory system provides a corp-wide aggregate view of all resources across every fleet and ship
- Use psecs_raw_corp_inventory (with your corpId) to see total quantities grouped by resource
- Drill down with psecs_raw_corp_inventory_fleet or psecs_raw_corp_inventory_ship for per-fleet or per-ship breakdowns
- The psecs://state/inventory resource gives a quick snapshot of your corp inventory with strategy hints
- Use psecs_ship_cargo_overview to inspect a specific ship's cargo holds in detail
- Transfer cargo between ships in the same fleet with psecs_cargo_transfer (provide fleetId, sourceShipId, destinationShipId, boxedAssetId, and destinationCargoModuleId)
- Move cargo between holds on the same ship with psecs_cargo_move (provide shipId, boxedAssetId, and destinationCargoModuleId)
- Cargo capacity is limited by cargo module size — monitor usage to avoid extraction or manufacturing pauses

## Personal Map & Resource Catalog
Your corporation automatically tracks two knowledge databases as you explore:

### Personal Map (Known Sectors)
- Every sector you scan is added to your personal map
- Use psecs_raw_usermap to list all known sectors (filter by type: StarSystem, BlackHole, Nebula, Rubble, Void, Nexus, or Favorites)
- Use psecs_raw_usermap_by_sector with a sectorId to get sector details including conduit connections
- Favorite important sectors with psecs_raw_create_usermap_favorite (e.g., resource-rich sectors, trade hubs, strategic chokepoints)
- Remove favorites with psecs_raw_delete_usermap_favorite
- Annotate sectors with personal notes using psecs_raw_update_usermap_note (max 500 characters — record resource quality, danger level, transit routes, etc.)
- Clear notes with psecs_raw_delete_usermap_note

### Resource Catalog (Discovered Resources)
- Every resource you discover via deep-scan is logged in your corp's resource catalog
- Use psecs_raw_corp_catalog to list discovered resources (filter by type: Mineral, Chemical, Flora, Fauna, Microscopic; by class: Metal, Ore, Gemstone, Gas, etc.; or favoritesOnly)
- Use psecs_raw_corp_catalog_by_entry to inspect a specific catalog entry's full details (taxonomy, quality properties, location, discovery timestamp)
- Favorite high-value resources with psecs_raw_create_corp_catalog_favorite for quick access later
- Remove favorites with psecs_raw_delete_corp_catalog_favorite
- Add notes to entries with psecs_raw_update_corp_catalog_note (e.g., "Best OQ in sector", "Use for T3 alloy", "Respawns near orbital 3")
- Clear notes with psecs_raw_delete_corp_catalog_note

### Strategic Use
- Build a knowledge base over time: annotate sectors with resource ratings, mark dangerous sectors, note optimal mining routes
- Favorite your best resource sources so you can return to them after manufacturing depletes your stocks
- Use catalog filtering to find specific resource types/classes when a blueprint requires a particular input
- Combine map notes with catalog data to plan efficient extraction routes across sectors

## Credits & Economy
- Credits are the primary currency
- Earned through market sales
- Spent on manufacturing, market purchases, and ship building
