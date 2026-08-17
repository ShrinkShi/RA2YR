using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Formats.MapTerrain;
using RA2YR.Presentation;
using RA2YR.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RA2YR.UnityIntegration
{
    public sealed class UnitySyntheticSkirmishBootstrap : MonoBehaviour
    {
        private readonly Dictionary<EntityId, GameObject> entityObjects = new Dictionary<EntityId, GameObject>();
        private readonly Dictionary<CellCoordinate, GameObject> terrainObjects = new Dictionary<CellCoordinate, GameObject>();
        private readonly Dictionary<bool, Material> ownerMarkerMaterials = new Dictionary<bool, Material>();
        private Material voxelMaterial;
        private Sprite unitSprite;
        private Texture2D unitTexture;
        private Camera playCamera;
        private UnityHumanPlaytestCameraController cameraController;
        private Vector3 dragStart;
        private bool dragging;
        private bool attackMoveMode;
        private UnityRtsInputController inputController;
        [SerializeField] private bool syntheticMode;
        [SerializeField] private bool strictRealContent = true;
        private string strictRealContentFailure;
        private bool paused;
        private float simulationAccumulator;
        private PresentationSnapshot previousPresentation;
        private ExternalLegacyVisualProvider externalVisualProvider;
        private ExternalLegacyVisualStatus externalVisualStatus;
        private StrictOriginalContentPreflightResult strictPreflight;
        private Material terrainMaterial;
        private IsometricProjectionProfile terrainProjection;
        private int terrainCellCount;
        private int externalObjectCount;
        private int syntheticObjectFallbackCount;

        public HumanPlaytestRuntime Runtime { get; private set; }
        public UnityPresentationWorld PresentationWorld { get; private set; }
        public UnityInteractiveClient Client { get; private set; }
        public Camera PlayCamera => playCamera;
        public bool IsInitialized { get; private set; }
        public bool IsPaused => paused;
        public int TerrainCellCount => terrainCellCount;
        public ExternalLegacyVisualStatus ExternalVisualStatus => externalVisualStatus;
        public StrictOriginalContentPreflightResult StrictPreflight => strictPreflight;
        public int ExternalObjectCount => externalObjectCount;
        public int SyntheticObjectFallbackCount => syntheticObjectFallbackCount;
        public PresentationSnapshot LastPresentation { get; private set; }

        public static UnitySyntheticSkirmishBootstrap CreateSynthetic(string name = "RA2YRSyntheticSkirmish")
        {
            GameObject root = new GameObject(name);
            UnitySyntheticSkirmishBootstrap bootstrap = root.AddComponent<UnitySyntheticSkirmishBootstrap>();
            bootstrap.syntheticMode = true;
            bootstrap.strictRealContent = false;
            return bootstrap;
        }

        private void Awake()
        {
            if (!IsInitialized) Initialize();
        }

        private void Start()
        {
            if (!IsInitialized) Initialize();
            RenderState();
        }

        private void Update()
        {
            if (!IsInitialized) return;
            HandleCameraInput();
            HandleHumanInput();
            if (!paused)
            {
                simulationAccumulator += Time.unscaledDeltaTime;
                while (simulationAccumulator >= 1f / 15f)
                {
                    simulationAccumulator -= 1f / 15f;
                    Runtime.Step();
                }
            }
            RenderState();
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            Runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            // Headless scene loading can spend several frames integrating assets; keep
            // the deterministic smoke-test world at tick zero until the test drives it.
            paused = Application.isBatchMode;
            PresentationWorld = gameObject.GetComponent<UnityPresentationWorld>() ?? gameObject.AddComponent<UnityPresentationWorld>();
            PresentationWorld.Configure(new UnityPresentationWorldPolicy(4096, 1024));
            Client = gameObject.GetComponent<UnityInteractiveClient>() ?? gameObject.AddComponent<UnityInteractiveClient>();
            Client.Configure(new UnityInteractiveClientPolicy(512, 256), new IsometricPointerProfile(64, 32, 1920, 1080), Runtime.CommandQueue);
            inputController = gameObject.GetComponent<UnityRtsInputController>() ?? gameObject.AddComponent<UnityRtsInputController>();
            terrainProjection = CreateTerrainProjection();
            BuildCamera();
            ConfigureExternalVisuals();
            if (syntheticMode)
            {
                BuildProceduralArt();
                BuildTerrain();
            }
            else
            {
                if (strictPreflight == null)
                    strictRealContentFailure = externalVisualProvider == null || externalVisualStatus == null || !externalVisualStatus.IsStrictRealContentReady
                        ? "StrictRealContent requires complete external role presentation; no synthetic visual fallback is permitted."
                        : string.Empty;
            }
            IsInitialized = true;
        }

        private void ConfigureExternalVisuals()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string configurationPath = Path.Combine(projectRoot, "Config", "ExternalContent.local.xml");
            if (!File.Exists(configurationPath))
            {
                externalVisualStatus = new ExternalLegacyVisualStatus(
                    false, false, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    false,
                    HumanPlaytestRemapProfile.SourcePaletteOnly,
                    "SyntheticFallback",
                    "Local external content configuration is not present.",
                    new ExternalVisualRouteDiagnostics(ExternalVisualRouteGateStatus.SourceNotConfigured));
                if (!syntheticMode && strictRealContent)
                {
                    strictPreflight = StrictOriginalContentPreflight.Run(configurationPath, projectRoot, externalVisualStatus, false, false);
                    strictRealContentFailure = strictPreflight.Message;
                }
                return;
            }

            HumanPlaytestVisualProfile profile = new HumanPlaytestVisualProfile(
                syntheticMode ? HumanPlaytestVisualMode.SyntheticOnly : (strictRealContent ? HumanPlaytestVisualMode.StrictRealContent : HumanPlaytestVisualMode.ExternalLegacyPreferred),
                configurationPath,
                artImagePolicy: HumanPlaytestArtImagePolicy.ExplicitOrSectionIdentifier);
            externalVisualProvider = ExternalLegacyVisualProvider.Create(profile, projectRoot);
            externalVisualStatus = externalVisualProvider.Status;
            if (!syntheticMode && strictRealContent)
            {
                strictPreflight = StrictOriginalContentPreflight.Run(configurationPath, projectRoot, externalVisualStatus, false, false);
                strictRealContentFailure = strictPreflight.IsReady ? string.Empty : strictPreflight.Message;
            }
        }

        public void RestartMatch()
        {
            if (!IsInitialized) Initialize();
            foreach (GameObject target in entityObjects.Values) DestroyObject(target);
            entityObjects.Clear();
            Runtime.Reset();
            Client.ClearTargets();
            previousPresentation = null;
            externalObjectCount = 0;
            syntheticObjectFallbackCount = 0;
            paused = Application.isBatchMode;
            simulationAccumulator = 0f;
            RenderState();
        }

        public bool SelectSingle(EntityId entity, bool additive = false)
        {
            if (!Runtime.IsSelectable(entity, Runtime.HumanPlayer)) return false;
            var ids = additive ? Client.Selection.Entities.Concat(new[] { entity }) : new[] { entity };
            SelectionResult result = SelectionService.Replace(ids, new SelectionPolicy(256));
            if (!result.IsSuccess) return false;
            Client.SetSelection(result.Selection);
            return true;
        }

        public ClientCommandResult IssueMove(CellCoordinate cell)
        {
            attackMoveMode = false;
            return Client.SubmitCommand(CommandKind.Move, cell, null, Runtime.Tick);
        }

        public ClientCommandResult IssueAttack(EntityId target)
        {
            attackMoveMode = false;
            return Client.SubmitCommand(CommandKind.Attack, null, target, Runtime.Tick);
        }

        public ClientCommandResult IssueAttackMove(CellCoordinate cell)
        {
            attackMoveMode = false;
            return Client.SubmitCommand(CommandKind.AttackMove, cell, null, Runtime.Tick);
        }

        public ClientCommandResult IssueHarvest(CellCoordinate cell)
        {
            return Client.SubmitCommand(CommandKind.Harvest, cell, null, Runtime.Tick);
        }

        public bool SetSelectedAutonomy(AutonomyMode mode) => Runtime.SetAutonomy(Client.Selection.Entities, mode);
        public bool QueueProduction() => Runtime.QueueProduction();
        public void StepSimulation(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            for (int index = 0; index < ticks; index++) Runtime.Step();
            RenderState();
        }

        private void BuildCamera()
        {
            cameraController = cameraController ?? new UnityHumanPlaytestCameraController();
            playCamera = Camera.main;
            if (playCamera == null)
            {
                GameObject cameraObject = new GameObject("SyntheticSkirmishCamera");
                playCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            playCamera.orthographic = true;
            playCamera.orthographicSize = 13f;
            Vector3 center = MapCellToPresentationPosition(Runtime.Config.Width / 2f, Runtime.Config.Height / 2f);
            playCamera.transform.position = center + new Vector3(0f, 20f, -20f);
            playCamera.transform.LookAt(center, Vector3.up);
        }

        private void BuildProceduralArt()
        {
            unitTexture = MakeTexture(new Color(0.25f, 0.75f, 1f, 1f));
            unitSprite = Sprite.Create(unitTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "SyntheticRuntimePixel";
            texture.SetPixel(0, 0, color);
            texture.Apply(false, false);
            return texture;
        }

        private void BuildTerrain()
        {
            // The external source is not yet proven to provide a complete TMP/theater
            // binding. Keep the map visible with one bounded isometric chunk instead
            // of a square checkerboard or one GameObject per terrain tile.
            var cells = new List<TerrainTilePresentationDescriptor>();
            for (int y = 0; y < Runtime.Config.Height; y++)
                for (int x = 0; x < Runtime.Config.Width; x++)
                    cells.Add(new TerrainTilePresentationDescriptor(
                        x, y, checked((long)y * Runtime.Config.Width + x), 0, 0,
                        null, null, null, 0, 0, null, checked((long)y * Runtime.Config.Width + x)));

            TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(
                cells,
                new TerrainPresentationPolicy(16, 16, checked(Runtime.Config.Width * Runtime.Config.Height), 64));
            IsometricProjectionProfile projection = terrainProjection ?? CreateTerrainProjection();
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader != null)
            {
                terrainMaterial = new Material(shader) { name = "SyntheticIsometricFallbackMaterial", color = new Color(0.20f, 0.34f, 0.22f, 1f) };
            }
            foreach (TerrainChunkDescriptor chunk in composed.Chunks)
            {
                TerrainChunkMeshBuildResult result = PresentationWorld.ApplyTerrainChunk(
                    chunk, projection, new TerrainMeshBuildPolicy(4096, 16384, 24576));
                if (result.IsSuccess)
                {
                    GameObject chunkObject = PresentationWorld.transform.Find("TerrainChunk_" + chunk.StableIdentity)?.gameObject;
                    if (chunkObject != null && terrainMaterial != null)
                        chunkObject.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
                }
            }
            terrainCellCount = cells.Count;
        }

        private void HandleCameraInput()
        {
            Vector2 mouse = Input.mousePosition;
            float horizontal = mouse.x <= 18f ? -1f : mouse.x >= Screen.width - 18f ? 1f : 0f;
            float vertical = mouse.y <= 18f ? -1f : mouse.y >= Screen.height - 18f ? 1f : 0f;
            if (horizontal == 0f) horizontal = (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f) + (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);
            if (vertical == 0f) vertical = (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f) + (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);
            float wheel = Input.GetAxisRaw("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) <= 0.0001f) wheel = Input.mouseScrollDelta.y;
            cameraController.Apply(playCamera, horizontal, vertical, wheel, Time.unscaledDeltaTime);
            if (inputController != null && inputController.State == UnityRtsInputState.CameraRightDrag && Input.GetMouseButton(1))
            {
                Vector2 delta = inputController.UpdateRightDrag(Input.mousePosition);
                cameraController.Apply(playCamera, -delta.x / 64f, -delta.y / 64f, 0f, Time.unscaledDeltaTime);
            }
        }

        private void HandleHumanInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.R)) RestartMatch();
            if (Input.GetKeyDown(KeyCode.M)) SetSelectedAutonomy(AutonomyMode.Manual);
            if (Input.GetKeyDown(KeyCode.T)) SetSelectedAutonomy(AutonomyMode.Assisted);
            if (Input.GetKeyDown(KeyCode.O)) SetSelectedAutonomy(AutonomyMode.Automatic);
            if (Input.GetKeyDown(KeyCode.S)) Client.SubmitCommand(CommandKind.Stop, null, null, Runtime.Tick);
            if (Input.GetKeyDown(KeyCode.H)) Client.SubmitCommand(CommandKind.Hold, null, null, Runtime.Tick);
            if (Input.GetKeyDown(KeyCode.P)) QueueProduction();
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi()) { dragging = true; dragStart = MouseWorld(); inputController.BeginLeft(Input.mousePosition); }
            if (dragging && Input.GetMouseButton(0)) inputController.UpdateLeft(Input.mousePosition);
            if (Input.GetMouseButtonUp(0) && dragging)
            {
                Vector3 end = MouseWorld();
                dragging = false;
                bool wasDrag;
                inputController.EndLeft(Input.mousePosition, out wasDrag);
                bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (wasDrag) SelectBox(dragStart, end, additive);
                else HandleLeftClick(ToCell(end), additive);
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (IsPointerOverUi()) return;
                inputController.BeginRightDrag(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(1) && inputController.State == UnityRtsInputState.CameraRightDrag)
            {
                bool click = Vector2.Distance(inputController.DragStartScreen, Input.mousePosition) < inputController.DragThresholdPixels;
                inputController.EndRightDrag();
                if (click)
                {
                    Client.SetSelection(new SelectionState(System.Array.Empty<EntityId>()));
                    attackMoveMode = false;
                    inputController.Cancel();
                }
            }
        }

        private void HandleLeftClick(CellCoordinate cell, bool additive)
        {
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            HumanPlaytestEntitySnapshot candidate = snapshot.Entities.Where(x => x.Owner.Value == Runtime.HumanPlayer.Value && x.IsSelectable && x.X == cell.X && x.Y == cell.Y).OrderBy(x => x.Entity).FirstOrDefault();
            if (candidate.Entity.IsValid)
            {
                SelectSingle(candidate.Entity, additive);
                return;
            }
            if (Client.Selection.Entities.Count == 0) return;
            EntityId enemy = FindEnemyAt(cell);
            if (enemy.IsValid) IssueAttack(enemy);
            else if (Runtime.IsResourceCell(cell) && Client.Selection.Entities.Any(entity => Runtime.IsControllable(entity, Runtime.HumanPlayer))) IssueHarvest(cell);
                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) IssueAttackMove(cell);
            else IssueMove(cell);
        }

        private bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private Vector3 MouseWorld()
        {
            Ray ray = playCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            return plane.Raycast(ray, out distance) ? ray.GetPoint(distance) : Vector3.zero;
        }

        private CellCoordinate ToCell(Vector3 world)
        {
            IsometricGridPoint candidate;
            IsometricProjectionProfile projection = terrainProjection ?? CreateTerrainProjection();
            if (!projection.TryInverseNearest(world.x, world.z, 0, 0, out candidate))
                return new CellCoordinate(0, 0);
            return new CellCoordinate(Mathf.Clamp((int)candidate.X, 0, Runtime.Config.Width - 1), Mathf.Clamp((int)candidate.Y, 0, Runtime.Config.Height - 1));
        }

        private EntityId FindEnemyAt(CellCoordinate cell)
        {
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            return snapshot.Entities.Where(x => x.Owner.Value != Runtime.HumanPlayer.Value && x.IsEnemy && x.X == cell.X && x.Y == cell.Y).Select(x => x.Entity).FirstOrDefault();
        }

        private void SelectAtCell(CellCoordinate cell, bool additive)
        {
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            HumanPlaytestEntitySnapshot candidate = snapshot.Entities.Where(x => x.Owner.Value == Runtime.HumanPlayer.Value && x.IsSelectable && x.X == cell.X && x.Y == cell.Y).OrderBy(x => x.Entity).FirstOrDefault();
            if (candidate.Entity.IsValid) SelectSingle(candidate.Entity, additive);
            else if (!additive) Client.SetSelection(new SelectionState(Array.Empty<EntityId>()));
        }

        private void SelectBox(Vector3 start, Vector3 end, bool additive)
        {
            CellCoordinate startCell = ToCell(start);
            CellCoordinate endCell = ToCell(end);
            float minX = Mathf.Min(startCell.X, endCell.X); float maxX = Mathf.Max(startCell.X, endCell.X); float minY = Mathf.Min(startCell.Y, endCell.Y); float maxY = Mathf.Max(startCell.Y, endCell.Y);
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            IEnumerable<EntityId> hits = snapshot.Entities.Where(x => x.Owner.Value == Runtime.HumanPlayer.Value && x.IsSelectable && x.X >= minX && x.X <= maxX && x.Y >= minY && x.Y <= maxY).Select(x => x.Entity);
            IEnumerable<EntityId> ids = additive ? Client.Selection.Entities.Concat(hits) : hits;
            SelectionResult result = SelectionService.Replace(ids, new SelectionPolicy(256));
            if (result.IsSuccess) Client.SetSelection(result.Selection);
        }

        private bool TryApplyExternalVisual(GameObject target, HumanPlaytestEntitySnapshot entity)
        {
            if (target == null || externalVisualProvider == null || !externalVisualProvider.IsAvailable) return false;
            bool enemy = entity.Owner.Value != Runtime.HumanPlayer.Value;
            HumanPlaytestVisualRole role = RoleFor(entity.Kind, enemy);
            VxlPresentationAsset presentation;
            if (externalVisualProvider.TryGetVoxelPresentation(role, out presentation) && presentation != null && presentation.Sections.Count > 0)
            {
                Material material = GetVoxelMaterial();
                if (material == null || !presentation.Metrics.IsFiniteAndBounded(VxlPresentationTransformProfile.Default)) return false;
                foreach (VxlPresentationSectionMesh section in presentation.Sections)
                {
                    if (section.Mesh == null) return false;
                    GameObject sectionObject = new GameObject("VxlSection_" + section.SectionOrdinal);
                    sectionObject.transform.SetParent(target.transform, false);
                    MeshFilter filter = sectionObject.AddComponent<MeshFilter>();
                    filter.sharedMesh = section.Mesh;
                    MeshRenderer renderer = sectionObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    ExternalVxlPresentationSectionMarker sectionMarker = sectionObject.AddComponent<ExternalVxlPresentationSectionMarker>();
                    sectionMarker.SectionIdentity = section.SectionIdentity;
                    sectionMarker.SectionOrdinal = section.SectionOrdinal;
                    sectionMarker.HvaApplied = section.HvaApplied;
                }
                target.AddComponent<ExternalVxlPresentationMarker>();
                AddOwnerMarker(target, enemy, Mathf.Max(presentation.Metrics.Bounds.WidthCells, presentation.Metrics.Bounds.DepthCells) * 0.7f);
                return true;
            }

            Sprite sprite;
            if (externalVisualProvider.TryGetSprite(role, out sprite) && sprite != null)
            {
                SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                return true;
            }
            return false;
        }

        private static HumanPlaytestVisualRole RoleFor(HumanPlaytestEntityKind kind, bool enemy)
        {
            switch (kind)
            {
                case HumanPlaytestEntityKind.Harvester: return HumanPlaytestVisualRole.HumanHarvester;
                case HumanPlaytestEntityKind.MainBase: return enemy ? HumanPlaytestVisualRole.EnemyBase : HumanPlaytestVisualRole.HumanBase;
                case HumanPlaytestEntityKind.Refinery: return HumanPlaytestVisualRole.HumanRefinery;
                case HumanPlaytestEntityKind.Factory: return enemy ? HumanPlaytestVisualRole.EnemyFactory : HumanPlaytestVisualRole.HumanFactory;
                case HumanPlaytestEntityKind.Power: return HumanPlaytestVisualRole.HumanPower;
                default: return enemy ? HumanPlaytestVisualRole.EnemyBasicUnit : HumanPlaytestVisualRole.HumanBasicUnit;
            }
        }

        private Material GetVoxelMaterial()
        {
            if (voxelMaterial != null) return voxelMaterial;
            Shader shader = Shader.Find("RA2YR/ExternalLegacyVxlLit");
            if (shader == null) return null;
            voxelMaterial = new Material(shader)
            {
                name = "ExternalLegacyVxlLitMaterial",
                color = Color.white
            };
            return voxelMaterial;
        }

        private void AddOwnerMarker(GameObject target, bool enemy, float radius)
        {
            GameObject markerObject = new GameObject("OwnerMarker");
            markerObject.transform.SetParent(target.transform, false);
            markerObject.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            LineRenderer line = markerObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 16;
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.material = GetOwnerMarkerMaterial(enemy);
            for (int index = 0; index < line.positionCount; index++)
            {
                float angle = (Mathf.PI * 2f * index) / line.positionCount;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private Material GetOwnerMarkerMaterial(bool enemy)
        {
            Material material;
            if (ownerMarkerMaterials.TryGetValue(enemy, out material) && material != null) return material;
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) return null;
            material = new Material(shader)
            {
                name = enemy ? "ExternalLegacyEnemyOwnerMarker" : "ExternalLegacyHumanOwnerMarker",
                color = enemy ? new Color(0.95f, 0.3f, 0.2f, 0.9f) : new Color(0.2f, 0.85f, 1f, 0.9f)
            };
            ownerMarkerMaterials[enemy] = material;
            return material;
        }

        private void RenderState()
        {
            if (!IsInitialized) return;
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot(Client.Selection.Entities);
            var live = new HashSet<EntityId>();
            foreach (HumanPlaytestEntitySnapshot entity in snapshot.Entities)
            {
                live.Add(entity.Entity);
                GameObject target;
                if (!entityObjects.TryGetValue(entity.Entity, out target) || target == null)
                {
                    target = new GameObject("Entity_" + entity.Entity.Index + "_" + entity.Entity.Generation);
                    target.transform.SetParent(transform, false);
                    bool external = TryApplyExternalVisual(target, entity);
                    if (!external && syntheticMode)
                    {
                        SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
                        renderer.sprite = unitSprite;
                        syntheticObjectFallbackCount = checked(syntheticObjectFallbackCount + 1);
                    }
                    else if (external)
                    {
                        externalObjectCount = checked(externalObjectCount + 1);
                    }
                    entityObjects[entity.Entity] = target;
                    Client.RegisterPickTarget(new UnityPickTarget(entity.Entity, new CellCoordinate(entity.X, entity.Y), target.name, entity.Entity.Index));
                }
                target.transform.position = MapCellToPresentationPosition(entity.X, entity.Y);
                ExternalVxlPresentationMarker externalMarker = target.GetComponent<ExternalVxlPresentationMarker>();
                if (externalMarker == null)
                    target.transform.localScale = new Vector3(entity.Kind == HumanPlaytestEntityKind.Unit ? 0.72f : 1.15f, entity.Kind == HumanPlaytestEntityKind.Unit ? 0.72f : 1.15f, 1f);
                else
                    target.transform.localScale = Vector3.one;
                SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
                Color baseColor = entity.Owner.Value == Runtime.HumanPlayer.Value ? new Color(0.2f, 0.65f, 1f, 1f) : new Color(1f, 0.28f, 0.25f, 1f);
                if (entity.Kind == HumanPlaytestEntityKind.Harvester) baseColor = new Color(1f, 0.78f, 0.15f, 1f);
                if (entity.Kind == HumanPlaytestEntityKind.MainBase) baseColor = entity.Owner.Value == Runtime.HumanPlayer.Value ? new Color(0.3f, 0.9f, 0.9f, 1f) : new Color(0.95f, 0.15f, 0.2f, 1f);
                if (sprite != null)
                {
                    bool isExternalSprite = externalVisualProvider != null && externalVisualProvider.IsAvailable && sprite.sprite != unitSprite;
                    sprite.color = isExternalSprite ? Color.white : (Client.Selection.Contains(entity.Entity) ? Color.white : baseColor);
                    sprite.sortingOrder = 1000 - entity.Y;
                }
                MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.sortingOrder = 1000 - entity.Y;
            }
            foreach (EntityId entity in entityObjects.Keys.Where(x => !live.Contains(x)).ToList()) { DestroyObject(entityObjects[entity]); entityObjects.Remove(entity); }
            foreach (KeyValuePair<CellCoordinate, GameObject> tile in terrainObjects)
            {
                bool visible = snapshot.Entities.Any(x => x.Owner.Value == Runtime.HumanPlayer.Value && Math.Abs(x.X - tile.Key.X) + Math.Abs(x.Y - tile.Key.Y) <= 9);
                tile.Value.GetComponent<SpriteRenderer>().color = visible ? tile.Value.GetComponent<SpriteRenderer>().color.WithAlpha(1f) : tile.Value.GetComponent<SpriteRenderer>().color.WithAlpha(0.35f);
            }
            bool useExternal = externalVisualProvider != null &&
                (syntheticMode || !strictRealContent
                    ? externalVisualProvider.IsAvailable
                    : externalVisualStatus != null && externalVisualStatus.IsStrictRealContentReady);
            var descriptors = snapshot.Entities.Select(x =>
            {
                bool enemy = x.Owner.Value != Runtime.HumanPlayer.Value;
                HumanPlaytestVisualRole role = RoleFor(x.Kind, enemy);
                ResolvedLegacyVisual resolved;
                string visualId = useExternal && externalVisualProvider.TryGetResolvedVisual(role, out resolved)
                    ? resolved.VisualAssetId
                    : "synthetic/playtest/" + role;
                return new PresentationEntityDescriptor(x.Entity, new VisualAssetId(visualId), x.Kind == HumanPlaytestEntityKind.Unit ? PresentationRenderPass.Vehicle : PresentationRenderPass.Structure, new PresentationPosition(x.X, x.Y), x.Entity.Index);
            });
            IVisualAssetProvider[] providers = syntheticMode
                ? (useExternal ? new IVisualAssetProvider[] { externalVisualProvider, new SyntheticProvider() } : new IVisualAssetProvider[] { new SyntheticProvider() })
                : (useExternal ? new IVisualAssetProvider[] { externalVisualProvider } : Array.Empty<IVisualAssetProvider>());
            LastPresentation = PresentationSnapshotAssembler.Assemble(Runtime.World.CaptureSnapshot(), descriptors, previousPresentation, providers, new PresentationAssemblyPolicy(4096));
            previousPresentation = LastPresentation;
            Client.SetVisibility(BuildVisibility(snapshot));
            Client.RefreshHud(Runtime.World.CaptureSnapshot(), checked((int)Math.Min(Runtime.Economy.Get(Runtime.HumanPlayer).Balance, int.MaxValue)), false, Client.Selection.Entities.Count == 0 ? "Manual" : snapshot.Entities.Where(x => Client.Selection.Contains(x.Entity)).Select(x => x.Autonomy.ToString()).FirstOrDefault() ?? "Manual");
        }

        private VisibilitySnapshot BuildVisibility(HumanPlaytestSnapshot snapshot)
        {
            var cells = new List<VisibilityCell>();
            for (int y = 0; y < Runtime.Config.Height; y++)
                for (int x = 0; x < Runtime.Config.Width; x++)
                {
                    bool visible = snapshot.Entities.Any(entity => entity.Owner.Value == Runtime.HumanPlayer.Value && Math.Abs(entity.X - x) + Math.Abs(entity.Y - y) <= 9);
                    cells.Add(new VisibilityCell(new CellCoordinate(x, y), visible ? ClientVisibilityState.Visible : ClientVisibilityState.Shrouded, y * Runtime.Config.Width + x));
                }
            VisibilitySnapshotResult result = VisibilitySnapshotBuilder.Build(cells, new VisibilityPolicy(Runtime.Config.Width * Runtime.Config.Height, 64));
            return result.Snapshot;
        }

        private void OnGUI()
        {
            if (!IsInitialized) return;
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot(Client.Selection.Entities);
            GUILayout.BeginArea(new Rect(12f, 12f, 390f, 185f), GUI.skin.box);
            GUILayout.Label("RA2YR Synthetic Skirmish — HUMAN PLAYTEST");
            GUILayout.Label("Credits: " + snapshot.Credits + "   Power: 100/50");
            GUILayout.Label("Selected: " + snapshot.SelectedPlayerUnits + "   Health: " + SelectedHealth(snapshot));
            GUILayout.Label("Mission: " + SelectedMission(snapshot) + "   Autonomy: " + SelectedAutonomy(snapshot));
            GUILayout.Label("Harvester cargo: " + snapshot.HarvesterCargo + "   Queue: " + snapshot.ProductionQueueCount);
            GUILayout.Label("Match: " + snapshot.Status + "   Tick: " + snapshot.Tick + (paused ? "   PAUSED" : string.Empty));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Produce (P)")) QueueProduction();
            if (GUILayout.Button("Manual (M)")) SetSelectedAutonomy(AutonomyMode.Manual);
            if (GUILayout.Button("Assisted (T)")) SetSelectedAutonomy(AutonomyMode.Assisted);
            if (GUILayout.Button("Automatic (O)")) SetSelectedAutonomy(AutonomyMode.Automatic);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(paused ? "Resume (Esc)" : "Pause (Esc)")) paused = !paused;
            if (GUILayout.Button("Restart (R)")) RestartMatch();
            GUILayout.EndHorizontal();
            GUILayout.Label((syntheticMode ? "Synthetic mode" : "StrictRealContent") + (string.IsNullOrEmpty(strictRealContentFailure) ? "" : " FAILED"));
            if (!string.IsNullOrEmpty(strictRealContentFailure)) GUILayout.Label(strictRealContentFailure);
            GUILayout.Label("LMB select/drag, Shift add, LMB ground move/attack/harvest, RMB deselect/cancel, Alt attack-move, S stop, H hold, edge/arrows pan, wheel zoom.");
            GUILayout.EndArea();
            DrawSelectionOverlay(snapshot);
        }

        private void DrawSelectionOverlay(HumanPlaytestSnapshot snapshot)
        {
            if (inputController != null && inputController.IsDragging)
            {
                Rect dragRect = ScreenRect(inputController.DragStartScreen, inputController.DragCurrentScreen);
                Color old = GUI.color;
                GUI.color = new Color(0.25f, 0.85f, 1f, 0.28f);
                GUI.Box(dragRect, GUIContent.none);
                GUI.color = old;
            }
            foreach (HumanPlaytestEntitySnapshot entity in snapshot.Entities.Where(x => Client.Selection.Contains(x.Entity)))
            {
                GameObject target;
                if (!entityObjects.TryGetValue(entity.Entity, out target) || target == null) continue;
                Vector3 screen = playCamera.WorldToScreenPoint(target.transform.position + Vector3.up * 0.55f);
                if (screen.z <= 0f) continue;
                float width = entity.IsStructure ? 42f : 30f;
                Rect marker = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - width * 0.25f, width, 5f);
                Color old = GUI.color;
                GUI.color = new Color(0.2f, 0.95f, 1f, 0.95f);
                GUI.Box(marker, GUIContent.none);
                Rect health = new Rect(marker.x, marker.y - 7f, marker.width * Mathf.Clamp01(entity.MaximumHealth == 0 ? 0f : (float)entity.Health / entity.MaximumHealth), 3f);
                GUI.color = entity.Health * 2 < entity.MaximumHealth ? new Color(1f, 0.25f, 0.15f, 0.95f) : new Color(0.25f, 1f, 0.35f, 0.95f);
                GUI.Box(health, GUIContent.none);
                if (entity.Kind == HumanPlaytestEntityKind.Harvester && entity.Cargo > 0)
                {
                    GUI.color = new Color(1f, 0.8f, 0.15f, 0.95f);
                    GUI.Box(new Rect(marker.x, marker.y + 7f, marker.width * Mathf.Clamp01((float)entity.Cargo / 100f), 3f), GUIContent.none);
                }
                GUI.color = old;
            }
        }

        private static Rect ScreenRect(Vector2 a, Vector2 b)
        {
            float minX = Mathf.Min(a.x, b.x);
            float maxX = Mathf.Max(a.x, b.x);
            float minY = Screen.height - Mathf.Max(a.y, b.y);
            float maxY = Screen.height - Mathf.Min(a.y, b.y);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private int SelectedHealth(HumanPlaytestSnapshot snapshot) => snapshot.Entities.Where(x => Client.Selection.Contains(x.Entity)).Sum(x => x.Health);
        private string SelectedMission(HumanPlaytestSnapshot snapshot) => snapshot.Entities.Where(x => Client.Selection.Contains(x.Entity)).Select(x => x.Mission.ToString()).FirstOrDefault() ?? "None";
        private string SelectedAutonomy(HumanPlaytestSnapshot snapshot) => snapshot.Entities.Where(x => Client.Selection.Contains(x.Entity)).Select(x => x.Autonomy.ToString()).FirstOrDefault() ?? "Manual";

        private void OnDestroy()
        {
            foreach (GameObject target in entityObjects.Values) DestroyObject(target);
            foreach (GameObject tile in terrainObjects.Values) DestroyObject(tile);
            if (unitSprite != null) DestroyObject(unitSprite);
            if (unitTexture != null) DestroyObject(unitTexture);
            if (terrainMaterial != null) DestroyObject(terrainMaterial);
            if (voxelMaterial != null) DestroyObject(voxelMaterial);
            voxelMaterial = null;
            foreach (Material material in ownerMarkerMaterials.Values) DestroyObject(material);
            ownerMarkerMaterials.Clear();
            if (externalVisualProvider != null) externalVisualProvider.Dispose();
            externalVisualProvider = null;
        }

        private static void DestroyObject(UnityEngine.Object target)
        { if (target == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(target); else UnityEngine.Object.DestroyImmediate(target); }

        private Vector3 MapCellToPresentationPosition(float x, float y)
        {
            IsometricProjectionProfile projection = terrainProjection ?? CreateTerrainProjection();
            IsometricFixedPoint point = projection.ProjectFixed(
                checked((long)Mathf.RoundToInt(x)),
                checked((long)Mathf.RoundToInt(y)),
                0,
                0);
            return new Vector3(
                (float)point.LogicalX,
                0f,
                (float)point.LogicalY);
        }

        private IsometricProjectionProfile CreateTerrainProjection()
        {
            return new IsometricProjectionProfile(
                Runtime.Config.Width / 2,
                Runtime.Config.Height / 2,
                2,
                1,
                0);
        }

        private sealed class SyntheticProvider : IVisualAssetProvider
        {
            public string ProviderId => "synthetic-playtest";
            public VisualAssetProviderResult Resolve(VisualAssetId assetId) => new VisualAssetProviderResult(VisualAssetProviderResolutionStatus.Resolved, ProviderId, assetId);
        }
    }

    internal sealed class ExternalVxlPresentationMarker : MonoBehaviour
    {
    }

    internal sealed class ExternalVxlPresentationSectionMarker : MonoBehaviour
    {
        public string SectionIdentity { get; internal set; }
        public int SectionOrdinal { get; internal set; }
        public bool HvaApplied { get; internal set; }
    }

    internal static class SyntheticColorExtensions
    {
        public static Color WithAlpha(this Color color, float alpha) { color.a = alpha; return color; }
    }
}
