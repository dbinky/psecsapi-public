# PSECS Tech Tree

## Structure
The tech tree has 3 layers:
1. **Technologies** — the research targets, organized by tier and discipline
2. **Applications** — unlocked by completing a technology; each is either a **blueprint** (for manufacturing) or a **modifier** (a passive bonus)
3. **Prerequisites** — technologies may require other technologies first; applications require their parent technology

Use `psecs_research_overview` for your current research state, `psecs_allocate_research` to start projects, `psecs_stop_research` to free up capacity, and `psecs_tech_tree_path` to plan prerequisite chains.

## Disciplines
Every technology has a primary discipline (and sometimes a secondary). Use discipline codes to filter the research list (`psecs_raw_research_list` with `primary` or `secondary` query params).

| Code | Discipline | Focus |
|------|-----------|-------|
| **B** | Biology | Organic extraction, bio-matrices, life support, flora/fauna processing |
| **C** | Chemistry | Chemical extraction, alloy synthesis, propellant systems, compound analysis |
| **E** | Energetics | Power generation, energy extraction, capacitors, thermal management |
| **I** | Informatics | Sensors, compute, navigation, targeting, data processing |
| **M** | Mechanics | Structural frames, manufacturing, ship hulls, cargo systems |
| **P** | Physics | Weapons, shields, propulsion, warp mechanics, particle physics |
| **S** | Sociology | Fleet command, crew management, trade networks, coordination |

## Tiers
- **Tier 1**: Basic — foundational technologies, simple extraction modules, starter components
- **Tier 2**: Intermediate — cargo holds, research modules, mineral extraction, basic weapons/armor
- **Tier 3**: Advanced — specialized components (weapon/defense components appear), enhanced modules
- **Tier 4**: Expert — high-efficiency systems, dual weapon/defense specialization
- **Tier 5**: Master — cutting-edge tech, advanced alloys, powerful weapon systems
- **Tier 6**: Elite — top-tier components and modules, strong modifiers
- **Tier 7**: Pinnacle — ultimate capabilities, rare blueprints, pinnacle modules

## Component Categories
Components are manufactured intermediate parts used as inputs for module blueprints. Each tier introduces upgraded versions with higher quality.

| Category | Purpose | Quality Properties |
|----------|---------|-------------------|
| BioMatrices | Organic cultivation systems | Vitality, Responsiveness |
| PowerCells | Energy storage cells | EnergyDensity, Efficiency |
| StructuralFrames | Load-bearing ship frames | Integrity, Flexibility |
| PropellantSystems | Propulsion fuel systems | ThrustRatio, FuelEfficiency |
| Conduits | Power transmission lines | Conductivity, Capacity |
| Circuits | Processing/logic circuits | ComputeCapacity, Stability |
| ThermalManagement | Heat dissipation systems | Dissipation, Tolerance |
| Optics | Sensor and targeting optics | Sensitivity, Resolution |
| WeaponComponents | Weapon subsystems (T3+) | Shielding, Resilience |
| DefenseComponents | Defense subsystems (T3+) | Shielding, Resilience |

Use `psecs_raw_research_components` to browse components with optional `tier` and `category` filters.

## Module Categories
Modules are installed on ships and provide capabilities. Higher-tier modules have better stats and may require more slots.

**Core Ship Systems:**
| Category | Capability | Slot Type |
|----------|-----------|-----------|
| Propulsion | Speed (fleet transit speed) | Internal+External |
| Power | PowerGeneration (powers other modules) | Internal |
| Sensors | Sensors (scanning, detection) | External |
| Compute | ComputeCapacity (operations support) | Internal |
| Cargo | CargoSpace (storage capacity) | Internal |

**Economy & Production:**
| Category | Capability | Slot Type |
|----------|-----------|-----------|
| Extraction | BiologicalExtraction, MineralExtraction, ChemicalExtraction, EnergeticExtraction | External |
| Manufacturing | Manufacturing (production capacity) | Internal |
| Research | Research (research capacity — 1 point per capacity per hour) | Internal |

**Combat & Defense:**
| Category | Capability | Slot Type |
|----------|-----------|-----------|
| Weapons | KineticDamage, EnergyDamage, QuantumDamage | External |
| Shields | EnergyResistance, KineticResistance | External |
| Armor | KineticResistance, EnergyResistance | Internal |
| Defense / DefenseSystems | QuantumResistance, various | Internal |
| Targeting | WeaponRange (targeting range) | External |

**Fleet & Crew:**
| Category | Capability | Slot Type |
|----------|-----------|-----------|
| FleetCommand | FleetCommand (max fleet ships) | Internal+External |
| Navigation | Navigation (route planning) | Internal |
| LifeSupport | CrewMember (crew capacity) | Internal |

Use `psecs_raw_research_modules` to browse modules with optional `tier` and `category` filters.

## Modifiers
Research applications can unlock passive modifiers that boost game systems:
- **Extraction**: BiologicalExtraction, MineralExtraction, ChemicalExtraction, EnergeticExtraction speed bonuses
- **Manufacturing**: ManufacturingSpeed, ManufacturingEfficiency, ManufacturingQuality bonuses
- **Research**: ResearchSpeed bonus
- Modifiers stack across tiers — researching more modifier applications compounds your bonuses

## Strategy

**General:**
- Research earns 1 point per capacity per hour (adjusted by allocation % and ResearchSpeed modifiers). A 25-point tech at capacity 1 takes ~25 hours.
- Always keep research capacity 100% allocated — idle capacity is wasted time
- Prioritize technologies that unlock the applications you need (blueprints for manufacturing chains, modifiers for speed boosts)
- Use `psecs_tech_tree_path` to find the shortest path to a desired technology or application

**By Playstyle:**
- **Industrialist**: Prioritize M (Mechanics) and C (Chemistry) — manufacturing modules, structural frames, alloy synthesis
- **Explorer/Miner**: Prioritize B (Biology) and E (Energetics) — extraction modules and speed modifiers
- **Trader**: Prioritize M (Mechanics) and S (Sociology) — cargo capacity, fleet command, manufacturing for goods to sell
- **Combat**: Prioritize P (Physics) and I (Informatics) — weapons, shields, targeting, sensors
- **Balanced**: Start with extraction + manufacturing basics (T1-T2), then specialize from T3 onward

**Progression Tips:**
- T1-T2: Get extraction and manufacturing modules online — you need resources to build everything else
- T3: Weapon and defense components appear — this is when combat builds start to diverge
- T4+: Modules require multiple component types as inputs — plan your manufacturing chains
- Higher-quality input components produce higher-quality modules — invest in extraction modifier research early
