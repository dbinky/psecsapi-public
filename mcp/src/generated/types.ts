// Auto-generated from openapi.json — do not edit by hand
// Run: npm run generate

export interface ActiveProjectModel {
  targetId?: string | null;
  targetName?: string | null;
  targetType?: string | null;
  currentPoints?: number;
  requiredPoints?: number;
  allocationPercent?: number;
  progressPercent?: number;
  nextTickAt?: string;
  startedAt?: string;
}

export interface ApiStakeInfoResponseModel {
  stakedTokens?: number;
  availableTokens?: number;
  rateLimit?: number;
  cooldownEndsAt?: string | null;
}

export interface ApplicationListItem {
  id?: string | null;
  technologyId?: string | null;
  name?: string | null;
  description?: string | null;
  type?: string | null;
  researchCost?: number;
  prerequisites?: string[] | null;
  isResearched?: boolean;
  isVisible?: boolean;
  instanceCount?: number;
  modifier?: ModifierSummary;
  blueprint?: BlueprintSummary;
}

export interface AssignCombatScriptRequest {
  scriptId: string;
}

export interface AsteroidBeltDetailResponseModel {
  metallicComp?: number;
  radioComp?: number;
  rareComp?: number;
  granularity?: number;
  density?: number;
}

export interface AuthResponse {
  accessToken?: string | null;
  refreshToken?: string | null;
  userId?: string | null;
  displayName?: string | null;
}

export interface BasicSectorScanResponseModel {
  entityId?: string;
  name?: string | null;
  type?: string | null;
  createTimestamp?: string;
  spawnedByUserId?: string | null;
  conduits?: ConduitResponseModel[] | null;
  orbitals?: Record<string, string> | null;
}

export type BlackHoleSectorResponseModel = Record<string, unknown>;

export interface BlueprintCapabilityModel {
  type?: string | null;
  baseValue?: number;
  qualitySource?: string | null;
}

export interface BlueprintDetailResponseModel {
  blueprintId?: string | null;
  outputType?: string | null;
  outputId?: string | null;
  baseWorkUnits?: number;
  inputResources?: BlueprintInputResourceModel[] | null;
  inputComponents?: BlueprintInputComponentModel[] | null;
  qualityProperties?: Record<string, number> | null;
  capabilities?: BlueprintCapabilityModel[] | null;
}

export interface BlueprintInputComponentModel {
  label?: string | null;
  componentType?: string | null;
  quantity?: number;
  propertyMapping?: Record<string, string> | null;
}

export interface BlueprintInputResourceModel {
  label?: string | null;
  qualifier?: string | null;
  value?: string | null;
  quantity?: number;
  propertyMapping?: Record<string, string> | null;
  inputKind?: string | null;
  alloyDefinitionId?: string | null;
  role?: string | null;
}

export interface BlueprintSummary {
  blueprintId?: string | null;
  outputType?: string | null;
  outputId?: string | null;
}

export interface BuildOrderResponseModel {
  success?: boolean;
  errorMessage?: string | null;
  orderNumber?: number;
  totalWorkUnits?: number;
  queuePosition?: number;
  estimatedMinutes?: number;
  buildFee?: number;
}

export interface CargoHoldModel {
  cargoModuleId?: string;
  moduleName?: string | null;
  capacity?: number;
  used?: number;
  available?: number;
  contents?: CargoItemModel[] | null;
}

export interface CargoInspectResponseModel {
  assetId?: string;
  assetType?: string | null;
  name?: string | null;
  quantity?: number;
  mass?: number;
  resourceProperties?: Record<string, number> | null;
  rawResourceId?: string | null;
  componentQualities?: Record<string, number> | null;
  tier?: number | null;
  category?: string | null;
  definitionId?: string | null;
  slotType?: string | null;
  moduleCapabilities?: ModuleCapabilityResponseModel[] | null;
  moduleRequirements?: ModuleRequirementResponseModel[] | null;
  alloyProperties?: Record<string, number> | null;
}

export interface CargoItemModel {
  boxedResourceId?: string;
  rawResourceId?: string;
  resourceName?: string | null;
  resourceClass?: string | null;
  quantity?: number;
  assetType?: string | null;
  alloyDefinitionId?: string | null;
  alloyName?: string | null;
  computedProperties?: Record<string, number> | null;
}

export interface CargoItemResponseModel {
  assetId?: string;
  assetType?: string | null;
  name?: string | null;
  quantity?: number;
  mass?: number;
}

export interface CargoTransferResponseModel {
  success?: boolean;
  errorMessage?: string | null;
}

export interface ChassisBlueprintComponentInputModel {
  label?: string | null;
  componentType?: string | null;
  quantity?: number;
}

export interface ChassisBlueprintDetailResponseModel {
  blueprintId?: string | null;
  chassisClass?: string | null;
  baseWorkUnitsPerSlot?: number;
  baseInputResources?: ChassisBlueprintInputModel[] | null;
  baseInputComponents?: ChassisBlueprintComponentInputModel[] | null;
  perInteriorSlotInputResources?: ChassisBlueprintInputModel[] | null;
  perInteriorSlotInputComponents?: ChassisBlueprintComponentInputModel[] | null;
  perExteriorSlotInputResources?: ChassisBlueprintInputModel[] | null;
  perExteriorSlotInputComponents?: ChassisBlueprintComponentInputModel[] | null;
  calculatedTotalResources?: ChassisBlueprintInputModel[] | null;
  calculatedTotalComponents?: ChassisBlueprintComponentInputModel[] | null;
  calculatedTotalWorkUnits?: number | null;
}

export interface ChassisBlueprintInputModel {
  label?: string | null;
  qualifier?: string | null;
  value?: string | null;
  quantity?: number;
}

export interface CloudEventModel {
  specversion?: string | null;
  id?: string | null;
  source?: string | null;
  type?: string | null;
  time?: string;
  datacontenttype?: string | null;
  data?: unknown | null;
}

export interface CloudEventsResponse {
  events?: CloudEventModel[] | null;
  cursor?: string | null;
}

export interface CombatHistoryItemModel {
  combatId?: string;
  opponentCorpId?: string;
  opponentCorpName?: string | null;
  outcome?: string | null;
  timestamp?: string;
  shipLosses?: number;
  shipKills?: number;
}

export interface CombatHistoryResponseModel {
  items?: CombatHistoryItemModel[] | null;
  totalCount?: number;
  page?: number;
  pageSize?: number;
}

export interface CombatScriptListItemModel {
  id?: string;
  name?: string | null;
  created?: string;
  modified?: string;
}

export interface CombatScriptResponseModel {
  id?: string;
  name?: string | null;
  source?: string | null;
  created?: string;
  modified?: string;
}

export interface CombatStatusResponseModel {
  combatId?: string;
  status?: string | null;
}

export interface CombatSummaryResponseModel {
  combatId?: string;
  attackerCorpId?: string;
  defenderCorpId?: string;
  attackerFleetId?: string;
  defenderFleetId?: string;
  outcome?: string | null;
  durationTicks?: number;
  durationSeconds?: number;
  shipsDestroyed?: string[] | null;
  shipsFled?: string[] | null;
  timestamp?: string;
}

export interface CompletedApplicationModel {
  instanceId?: string;
  applicationId?: string | null;
  name?: string | null;
  quality?: number;
  completedAt?: string;
}

export interface ConduitResponseModel {
  entityId?: string;
  originSectorId?: string | null;
  endpointSectorId?: string | null;
  width?: number | null;
  length?: number | null;
}

export interface CorpFleetsResponseModel {
  corpFleets?: string[] | null;
}

export interface CorpInventoryResponseModel {
  totals?: ResourceTotalModel[] | null;
  fleets?: FleetSummaryModel[] | null;
  snapshotTime?: string;
}

export interface CorpProfileResponseModel {
  entityId?: string;
  name?: string | null;
  lastUpdateTimestamp?: string | null;
  createTimestamp?: string;
}

export interface CreateCheckoutRequest {
  quantity?: number;
}

export interface CreateCombatScriptRequest {
  name: string;
  source: string;
}

export interface CreateCorpRequestModel {
  name?: string | null;
}

export interface CreateSaleRequest {
  shipId?: string;
  boxedAssetId?: string;
  price?: number;
  durationDays?: number;
  isAuction?: boolean;
  description?: string | null;
}

export interface CreateSpaceRequestModel {
  sectorsToCreate?: number;
}

export interface DeepScanResultResponseModel {
  entityId?: string;
  name?: string | null;
  type?: string | null;
  class?: string | null;
  order?: string | null;
  propertyAssessments?: Record<string, string> | null;
  propertyValues?: Record<string, number> | null;
}

export interface EngageCombatRequest {
  attackerFleetId: string;
  targetFleetId: string;
}

export interface EngageCombatResponseModel {
  success?: boolean;
  combatId?: string | null;
  errorMessage?: string | null;
}

export interface EnhancedMapStatsResponseModel {
  global?: GlobalMapStats;
  personal?: PersonalMapStats;
}

export interface ExtractionJobStatusResponseModel {
  jobId?: string;
  rawResourceId?: string;
  resourceName?: string | null;
  ratePerMinute?: number;
  quantityLimit?: number | null;
  startTime?: string;
  accumulatedQuantity?: number;
}

export interface FleetDetailResponseModel {
  entityId?: string;
  ownerCorpId?: string;
  name?: string | null;
  lastUpdateTimestamp?: string | null;
  createTimestamp?: string | null;
  sectorId?: string | null;
  ships?: string[] | null;
  status?: string | null;
  queueStatus?: QueueState;
  transitETA?: string | null;
  activeCombatId?: string | null;
  lastCombatTimestamp?: string | null;
  assignedCombatScriptId?: string | null;
}

export interface FleetEnqueueRequestModel {
  conduitId?: string;
}

export interface FleetInventoryResponseModel {
  fleetId?: string;
  fleetName?: string | null;
  totals?: ResourceTotalModel[] | null;
  ships?: ShipSummaryModel[] | null;
  snapshotTime?: string;
}

export interface FleetQuantityModel {
  fleetId?: string;
  fleetName?: string | null;
  quantity?: number;
}

export interface FleetScanResultResponseModel {
  fleetId?: string;
  name?: string | null;
  shipCount?: number;
  shipsByClass?: Record<string, number> | null;
  totalMass?: number | null;
  ships?: ScannedShipResponseModel[] | null;
}

export interface FleetSummaryModel {
  fleetId?: string;
  fleetName?: string | null;
  totalQuantity?: number;
  resourceTypeCount?: number;
}

export interface FleetSummaryResponseModel {
  fleetId?: string;
  name?: string | null;
  shipCount?: number;
}

export interface FleetSurveyResultResponseModel {
  fleets?: FleetSummaryResponseModel[] | null;
}

export interface GlobalMapStats {
  totalSectors?: number;
  sectorsByType?: Record<string, number> | null;
}

export interface InstallModulesRequest {
  boxedModuleIds: string[];
}

export interface LootFieldResponseModel {
  id?: string;
  positionX?: number;
  positionY?: number;
  itemCount?: number;
  isExclusive?: boolean;
  expiresAt?: string;
}

export interface ManufacturingCancelRequestModel {
  jobId: string;
}

export interface ManufacturingJobModel {
  jobId?: string;
  shipId?: string;
  shipName?: string | null;
  blueprintId?: string | null;
  blueprintQuality?: number;
  targetQuantity?: number;
  completedCount?: number;
  currentItemProgressPercent?: number;
  status?: string | null;
  displayName?: string | null;
  outputName?: string | null;
  outputType?: string | null;
  estimatedCompletion?: string | null;
  autoResume?: boolean;
}

export interface ManufacturingPauseRequestModel {
  jobId: string;
}

export interface ManufacturingResumeRequestModel {
  jobId: string;
}

export interface ManufacturingStartRequestModel {
  shipId: string;
  blueprintInstanceId: string;
  quantity?: number;
  displayName?: string | null;
  autoResume?: boolean;
  inputs?: Record<string, string[]> | null;
}

export interface ManufacturingStartResponseModel {
  success?: boolean;
  jobId?: string;
  status?: string | null;
  nextTickAt?: string | null;
  errorMessage?: string | null;
}

export interface ManufacturingStatusResponseModel {
  jobs?: ManufacturingJobModel[] | null;
  totalActive?: number;
  totalPaused?: number;
}

export interface MarketListingItemModel {
  saleId?: string;
  type?: string | null;
  sellerCorpId?: string;
  sellerCorpName?: string | null;
  assetSummary?: string | null;
  price?: number;
  startingPrice?: number | null;
  bidCount?: number;
  createdAt?: string;
  expiresAt?: string;
  timeRemaining?: string | null;
  description?: string | null;
}

export interface MarketListingResponseModel {
  listings?: MarketListingItemModel[] | null;
  page?: number;
  pageSize?: number;
  totalItems?: number;
  totalPages?: number;
}

export interface MaterializationResultResponseModel {
  jobId?: string;
  boxedResourceId?: string;
  rawResourceId?: string;
  resourceName?: string | null;
  materializedQuantity?: number;
}

export interface ModifierSummary {
  operationType?: string | null;
  bonusPercent?: number;
}

export interface ModuleCapabilityDetailResponseModel {
  capabilityType?: string | null;
  value?: number;
}

export interface ModuleCapabilityResponseModel {
  type?: string | null;
  value?: number;
}

export type ModuleCompatability = "Ship" | "Station" | "Factory" | "Harvester" | "Colony";

export interface ModuleInstallResponseModel {
  success?: boolean;
  errorMessage?: string | null;
  installedModuleNames?: string[] | null;
  interiorSlotsUsed?: number;
  exteriorSlotsUsed?: number;
  interiorSlotsAvailable?: number;
  exteriorSlotsAvailable?: number;
}

export interface ModuleRequirementDetailResponseModel {
  requirementType?: string | null;
  value?: number;
}

export interface ModuleRequirementResponseModel {
  type?: string | null;
  value?: number;
}

export interface ModuleUninstallResponseModel {
  success?: boolean;
  errorMessage?: string | null;
  uninstalledModuleNames?: string[] | null;
  boxedModuleIds?: string[] | null;
}

export interface MoveCargoRequest {
  boxedAssetId: string;
  destinationCargoModuleId: string;
}

export interface MyBidsItemModel {
  saleId?: string;
  type?: string | null;
  sellerCorpName?: string | null;
  assetSummary?: string | null;
  currentHighBid?: number;
  bidCount?: number;
  expiresAt?: string;
  timeRemaining?: string | null;
  yourBidAmount?: number;
  bidStatus?: string | null;
}

export interface NebulaSectorResponseModel {
  type?: string | null;
  organicComp?: number;
  metallicComp?: number;
  radioComp?: number;
  h2Comp?: number;
  density?: number;
}

export interface OrbitalDetailResponseModel {
  orbitalPosition?: number;
  type?: string | null;
  asteroidBelt?: AsteroidBeltDetailResponseModel;
  planet?: PlanetDetailResponseModel;
}

export interface OwnedBlueprintModel {
  instanceId?: string;
  blueprintDefinitionId?: string | null;
  applicationId?: string | null;
  quality?: number;
  acquiredAt?: string;
  outputType?: string | null;
  outputName?: string | null;
}

export interface PersonalMapStats {
  totalKnown?: number;
  sectorsByType?: Record<string, number> | null;
  favorites?: number;
}

export interface PickupLootRequest {
  fleetId: string;
  shipId: string;
}

export interface PlaceBidRequest {
  amount?: number;
}

export interface PlaceBuildOrderRequest {
  catalogId: string;
  blueprintInstanceId: string;
  selectedInputs: Record<string, string[]>;
}

export interface PlanetDetailResponseModel {
  type?: string | null;
  solidComp?: number;
  solidDetails?: SolidCompDetailsResponseModel;
  liquidComp?: number;
  gasComp?: number;
  atmostphericPressure?: number;
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: unknown;
}

export interface QueueState {
  conduitId?: string;
  queueWidth?: number;
  queueLength?: number;
  queuePosition?: number;
  enqueuedTimestamp?: string;
}

export type RawResourceClass = "Metal" | "Ore" | "Gemstone" | "Gas" | "Liquid" | "Food" | "NonFood" | "Meat" | "Hide" | "Bone" | "Algae" | "Bacteria";

export type RawResourceGroup = "Inorganic" | "Organic";

export type RawResourceOrder = "Ferromagnetic" | "NonFerromagnetic" | "Precious" | "Radioactive" | "NonRadioactive" | "Rare" | "CrystallineGemstone" | "AmorphousGemstone" | "ReactiveGas" | "InertGas" | "Water" | "PetrochemicalLiquid" | "LubricatingFluid" | "Fruit" | "Vegetable" | "Grain" | "HardWood" | "SoftWood" | "Fibrous" | "SmoothMeat" | "TexturedMeat" | "SmoothHide" | "TexturedHide" | "CalcinatedBone" | "ChitinousBone" | "GreenAlgae" | "BlueAlgae" | "RedAlgae" | "AerobicBacteria" | "AnaerobicBacteria";

export type RawResourceType = "Mineral" | "Chemical" | "Flora" | "Fauna" | "Microscopic" | "Energetic";

export interface RefreshRequest {
  userId?: string | null;
  refreshToken?: string | null;
}

export interface RepostSaleRequest {
  price?: number;
  durationDays?: number;
  isAuction?: boolean;
  description?: string | null;
}

export interface ResearchAllocateRequestModel {
  targetId?: string | null;
  percent?: number;
}

export interface ResearchAllocateResponseModel {
  success?: boolean;
  errorMessage?: string | null;
  project?: ActiveProjectModel;
}

export interface ResearchCompletedResponseModel {
  technologies?: string[] | null;
  applications?: CompletedApplicationModel[] | null;
}

export interface ResearchListResponseModel {
  technologies?: TechnologyListItem[] | null;
  applications?: ApplicationListItem[] | null;
}

export interface ResearchModifiersResponseModel {
  modifiers?: Record<string, number> | null;
}

export interface ResearchStatusResponseModel {
  totalCapacity?: number;
  activeProjects?: ActiveProjectModel[] | null;
  totalAllocation?: number;
  availableAllocation?: number;
}

export interface ResearchStopRequestModel {
  targetId?: string | null;
}

export interface ResourceCatalogEntryResponseModel {
  entryId?: string;
  rawResourceId?: string;
  name?: string | null;
  shortNameKey?: string | null;
  group?: RawResourceGroup;
  type?: RawResourceType;
  class?: RawResourceClass;
  order?: RawResourceOrder;
  properties?: {
    OQ?: number;
    PE?: number;
    SR?: number;
    UT?: number;
    MA?: number;
    CN?: number;
    CR?: number;
    HR?: number;
    DR?: number;
    FL?: number;
    FR?: number;
  } | null;
  density?: number;
  sectorId?: string;
  sectorName?: string | null;
  orbitalPosition?: number | null;
  discoveredAt?: string;
  discoveredByUserId?: string | null;
  isFavorite?: boolean;
  note?: string | null;
}

export interface ResourceTotalModel {
  rawResourceId?: string;
  resourceName?: string | null;
  resourceClass?: string | null;
  totalQuantity?: number;
  byFleet?: FleetQuantityModel[] | null;
}

export interface RetrieveSaleRequest {
  shipId?: string;
  cargoModuleId?: string;
}

export interface RubbleSectorResponseModel {
  type?: string | null;
  metallicComp?: number;
  radioComp?: number;
  rareComp?: number;
  granularity?: number;
  density?: number;
}

export interface SaleDetailsResponseModel {
  saleId?: string;
  type?: string | null;
  state?: string | null;
  sellerCorpId?: string;
  sellerCorpName?: string | null;
  buyerCorpId?: string | null;
  buyerCorpName?: string | null;
  boxedAssetId?: string;
  assetSummary?: string | null;
  price?: number;
  startingPrice?: number | null;
  bidCount?: number;
  minimumNextBid?: number;
  description?: string | null;
  durationDays?: number;
  createdAt?: string;
  expiresAt?: string;
  pickupWindowEndsAt?: string;
  timeRemaining?: string | null;
  storageFeesPaid?: number;
}

export interface SaleResultResponseModel {
  success?: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
  saleId?: string | null;
  newState?: string | null;
  feesCharged?: number | null;
  creditsTransferred?: number | null;
  expiresAt?: string | null;
  pickupWindowEndsAt?: string | null;
}

export interface ScannedModuleResponseModel {
  name?: string | null;
  capabilities?: ModuleCapabilityDetailResponseModel[] | null;
  interiorSlotsRequired?: number;
  exteriorSlotsRequired?: number;
}

export interface ScannedShipResponseModel {
  shipId?: string;
  class?: string | null;
  mass?: number;
  externalModules?: ScannedModuleResponseModel[] | null;
  internalModules?: ScannedModuleResponseModel[] | null;
}

export type SectorType = "Nebula" | "Void" | "Rubble" | "StarSystem" | "BlackHole" | "Nexus";

export interface SetCatalogNoteRequestModel {
  content?: string | null;
}

export interface SetNoteRequestModel {
  content?: string | null;
}

export interface ShipCatalogEntryResponseModel {
  catalogId?: string | null;
  name?: string | null;
  class?: string | null;
  interiorSlots?: number;
  exteriorSlots?: number;
  totalSlots?: number;
  baseStructurePoints?: number;
  baseHullPoints?: number;
  baseMass?: number;
}

export interface ShipDetailResponseModel {
  entityId?: string;
  ownerCorpId?: string;
  name?: string | null;
  class?: string | null;
  currentStructurePoints?: number | null;
  currentHullPoints?: number | null;
  lastUpdateTimestamp?: string;
  createTimestamp?: string;
  fleetId?: string;
  modules?: TechModuleResponseModel[] | null;
  capabilities?: ModuleCapabilityDetailResponseModel[] | null;
  requirements?: ModuleRequirementDetailResponseModel[] | null;
  requirementsMet?: boolean | null;
  totalInteriorSlots?: number;
  totalExteriorSlots?: number;
  maxStructurePoints?: number;
  maxHullPoints?: number;
  shipMass?: number;
}

export interface ShipInventoryResponseModel {
  shipId?: string;
  shipName?: string | null;
  fleetId?: string;
  fleetName?: string | null;
  cargoHolds?: CargoHoldModel[] | null;
  snapshotTime?: string;
  hasCargoDetails?: boolean;
}

export interface ShipSummaryModel {
  shipId?: string;
  shipName?: string | null;
  totalQuantity?: number;
  resourceTypeCount?: number;
  hasActiveExtraction?: boolean;
}

export interface ShipyardQueueEntryResponseModel {
  orderNumber?: number;
  totalSlots?: number;
  progressPercent?: number;
  estimatedMinutesRemaining?: number;
  isOwnOrder?: boolean;
  placedTimestamp?: string;
}

export interface ShipyardQueueResponseModel {
  currentBuild?: ShipyardQueueEntryResponseModel;
  queuedBuilds?: ShipyardQueueEntryResponseModel[] | null;
  totalQueueDepth?: number;
}

export type SolidCompDetailsResponseModel = Record<string, unknown>;

export interface SpaceSectorResponseModel {
  entityId?: string;
  name?: string | null;
  type?: string | null;
  createTimestamp?: string | null;
  spawnedByUserId?: string | null;
  nebulaDetails?: NebulaSectorResponseModel;
  rubbleDetails?: RubbleSectorResponseModel;
  starSystemDetails?: StarSystemSectorResponseModel;
  blackHoleDetails?: BlackHoleSectorResponseModel;
}

export interface StakeTokensRequestModel {
  amount?: number;
}

export interface StakeTokensResponseModel {
  stakedTokens?: number;
  availableTokens?: number;
  rateLimit?: number;
  accessToken?: string | null;
}

export interface StarDetailResponseModel {
  type?: string | null;
  color?: string | null;
  mass?: number;
  luminosity?: number;
  power?: number;
}

export interface StarSystemSectorResponseModel {
  stars?: StarDetailResponseModel[] | null;
  orbitals?: OrbitalDetailResponseModel[] | null;
}

export interface StartExtractionRequestModel {
  resourceId: string;
  quantityLimit?: number | null;
}

export interface TechModuleResponseModel {
  entityId?: string;
  name?: string | null;
  tier?: number;
  category?: string | null;
  slotType?: string | null;
  compatabilities?: ModuleCompatability[] | null;
  interiorSlotsRequired?: number;
  exteriorSlotsRequired?: number;
  mass?: number;
  isEnabled?: boolean;
  condition?: number;
  capabilities?: ModuleCapabilityDetailResponseModel[] | null;
  requirements?: ModuleRequirementDetailResponseModel[] | null;
}

export interface TechTreeComponentModel {
  componentId?: string | null;
  name?: string | null;
  description?: string | null;
  category?: string | null;
  tier?: number;
  mass?: number;
  qualityProperties?: string[] | null;
}

export interface TechTreeDisciplineModel {
  code?: string | null;
  name?: string | null;
  description?: string | null;
}

export interface TechTreeModuleCapabilityModel {
  type?: string | null;
  baseValue?: number;
}

export interface TechTreeModuleModel {
  moduleId?: string | null;
  name?: string | null;
  description?: string | null;
  category?: string | null;
  tier?: number;
  mass?: number;
  slotType?: string | null;
  interiorSlotsRequired?: number;
  exteriorSlotsRequired?: number;
  baseCapabilities?: TechTreeModuleCapabilityModel[] | null;
  requirements?: TechTreeModuleRequirementModel[] | null;
}

export interface TechTreeModuleRequirementModel {
  type?: string | null;
  value?: number;
}

export interface TechnologyListItem {
  id?: string | null;
  name?: string | null;
  description?: string | null;
  tier?: number;
  primaryDiscipline?: string | null;
  secondaryDiscipline?: string | null;
  researchCost?: number;
  prerequisites?: string[] | null;
  isResearched?: boolean;
  isVisible?: boolean;
}

export interface TransferCargoRequest {
  boxedAssetId: string;
  sourceShipId: string;
  destinationShipId: string;
  destinationCargoModuleId: string;
}

export interface UninstallModulesRequest {
  moduleIds: string[];
  cargoModuleId: string;
}

export interface UnstakeTokensResponseModel {
  stakedTokens?: number;
  availableTokens?: number;
  rateLimit?: number;
  cooldownEndsAt?: string;
  accessToken?: string | null;
}

export interface UpdateCombatScriptRequest {
  name: string;
  source: string;
}

export interface UserMapConduitProfile {
  entityId?: string;
  length?: number;
  width?: number;
}

export interface UserMapSectorModel {
  entityId?: string;
  name?: string | null;
  type?: SectorType;
  conduits?: UserMapConduitProfile[] | null;
  createTimestamp?: string;
  lastMappedTimestamp?: string;
  spawnedByUserId?: string | null;
  isFavorited?: boolean;
  note?: string | null;
  noteTimestamp?: string | null;
}

export interface UserProfileResponseModel {
  entityId?: string | null;
  name?: string | null;
  tokens?: number | null;
  lastUpdated?: string | null;
  ownedCorps?: string[] | null;
}
