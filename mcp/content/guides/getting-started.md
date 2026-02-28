# Getting Started with PSECS

## Step 1: Create Your Corporation
Use `psecs_create_corp` with a name to create your organization.
This auto-provisions a starter fleet with one fully-equipped ship.

**Your starter ship can do almost everything immediately:**
- Extract any resource type (ore, gas, metals, gemstones, liquids, food)
- Research technologies in the tech tree
- Manufacture items from blueprints
- Navigate between sectors through conduits
- Scan sectors for resources, conduits, and other fleets
- Store up to 300 units of cargo

The only thing your starter ship **cannot** do is combat (no weapons or shields).
You can start exploring, extracting, researching, and manufacturing right away.

## Step 2: Explore Your Starting Sector
Use `psecs_explore_sector` with your fleet ID to scan the area.
Look for resources to mine and conduits to other sectors.

## Step 3: Start Extracting Resources
Use `psecs_mine_resource` to begin mining. Focus on resources
your starting blueprints need.

## Step 4: Begin Research
Use `psecs_research_overview` to see available tech.
Allocate 100% of your research capacity — never leave it idle.
Use `psecs_stop_research` to free capacity when you want to change priorities — progress is saved.

## Step 5: Start Manufacturing
Once you have resources and blueprints, use `psecs_start_manufacturing`
to manufacture components and modules.

## Step 6: Trade on the Market
Use `psecs_market_search` to find deals. Sell surplus resources
and buy what you need.

## Step 7: Expand Your Fleet
Use `psecs_shipyard_browse` to see available ship classes and the build queue.
You need a chassis blueprint (from research) and input resources/components.
Use `psecs_shipyard_start_build` to place a build order, then
`psecs_shipyard_pickup` to collect the completed ship into your fleet.
More ships = more extraction, research, and manufacturing capacity.

## Key Tips
- Research capacity should always be 100% allocated
- Higher quality inputs → higher quality outputs
- Fleet speed is limited by the slowest ship
- Check market prices before selling — don't underprice
- Explore multiple sectors to find rare resources
- Use psecs_raw_corp_inventory to see all your resources at a glance across every fleet and ship
- Use psecs_ship_cargo_overview on individual ships to check cargo capacity before starting extraction or manufacturing
- Favorite resource-rich sectors on your personal map (psecs_raw_create_usermap_favorite) so you can return to them later
- Annotate discovered resources in your catalog (psecs_raw_update_corp_catalog_note) to track quality patterns and blueprint matches
- Use psecs_raw_usermap with type=Favorites to quickly find your bookmarked sectors
