using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Formats.MapTerrain;
using RA2YR.Presentation;
using RA2YR.Simulation;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public sealed class UnitySyntheticSkirmishBootstrap : MonoBehaviour
    {
        private readonly Dictionary<EntityId, GameObject> entityObjects = new Dictionary<EntityId, GameObject>();
        private readonly Dictionary<CellCoordinate, GameObject> terrainObjects = new Dictionary<CellCoordinate, GameObject>();
        private readonly Dictionary<bool, Material> voxelMaterials = new Dictionary<bool, Material>();
        private Sprite unitSprite;
        private Texture2D unitTexture;
        private Camera playCamera;
        private Vector3 dragStart;
        private bool dragging;
        private bool attackMoveMode;
        private bool paused;
        private float simulationAccumulator;
        private PresentationSnapshot previousPresentation;
        private ExternalLegacyVisualProvider externalVisualProvider;
        private ExternalLegacyVisualStatus externalVisualStatus;
        private Material terrainMaterial;
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
        public int ExternalObjectCount => externalObjectCount;
        public int SyntheticObjectFallbackCount => syntheticObjectFallbackCount;
        public PresentationSnapshot LastPresentation { get; private set; }

        public static UnitySyntheticSkirmishBootstrap CreateSynthetic(string name = "RA2YRSyntheticSkirmish")
        {
            GameObject root = new GameObject(name);
            return root.AddComponent<UnitySyntheticSkirmishBootstrap>();
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
            BuildCamera();
            ConfigureExternalVisuals();
            BuildProceduralArt();
            BuildTerrain();
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
                return;
            }

            HumanPlaytestVisualProfile profile = new HumanPlaytestVisualProfile(
                HumanPlaytestVisualMode.ExternalLegacyPreferred,
                configurationPath,
                artImagePolicy: HumanPlaytestArtImagePolicy.ExplicitOrSectionIdentifier);
            externalVisualProvider = ExternalLegacyVisualProvider.Create(profile, projectRoot);
            externalVisualStatus = externalVisualProvider.Status;
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
            if (!Runtime.World.Registry.IsAlive(entity) || !Runtime.HumanUnits.Contains(entity)) return false;
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
            return Client.SubmitCommand(CommandKind.Attack, null, target, Runtime.Tick);
        }

        public ClientCommandResult IssueAttackMove(CellCoordinate cell)
        {
            return Client.SubmitCommand(CommandKind.AttackMove, cell, null, Runtime.Tick);
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
            playCamera = Camera.main;
            if (playCamera == null)
            {
                GameObject cameraObject = new GameObject("SyntheticSkirmishCamera");
                playCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            playCamera.orthographic = true;
            playCamera.orthographicSize = 13f;
            playCamera.transform.position = new Vector3(14f, 11f, -20f);
            playCamera.transform.rotation = Quaternion.identity;
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
            IsometricProjectionProfile projection = new IsometricProjectionProfile(
                Runtime.Config.Width / 2, Runtime.Config.Height / 2, 2, 1, 0);
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
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            if (horizontal != 0f || vertical != 0f) playCamera.transform.position += new Vector3(horizontal, vertical, 0f) * (8f * Time.unscaledDeltaTime);
            float wheel = Input.mouseScrollDelta.y;
            if (Math.Abs(wheel) > 0.01f) playCamera.orthographicSize = Mathf.Clamp(playCamera.orthographicSize - wheel, 5f, 24f);
        }

        private void HandleHumanInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.R)) RestartMatch();
            if (Input.GetKeyDown(KeyCode.A)) attackMoveMode = true;
            if (Input.GetKeyDown(KeyCode.M)) SetSelectedAutonomy(AutonomyMode.Manual);
            if (Input.GetKeyDown(KeyCode.T)) SetSelectedAutonomy(AutonomyMode.Assisted);
            if (Input.GetKeyDown(KeyCode.O)) SetSelectedAutonomy(AutonomyMode.Automatic);
            if (Input.GetKeyDown(KeyCode.S)) Client.SubmitCommand(CommandKind.Stop, null, null, Runtime.Tick);
            if (Input.GetKeyDown(KeyCode.H)) Client.SubmitCommand(CommandKind.Hold, null, null, Runtime.Tick);
            if (Input.GetKeyDown(KeyCode.P)) QueueProduction();
            if (Input.GetMouseButtonDown(0)) { dragging = true; dragStart = MouseWorld(); }
            if (Input.GetMouseButtonUp(0) && dragging)
            {
                Vector3 end = MouseWorld();
                dragging = false;
                if (Vector2.Distance(new Vector2(dragStart.x, dragStart.y), new Vector2(end.x, end.y)) > 0.4f) SelectBox(dragStart, end, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                else SelectAtCell(ToCell(end), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }
            if (Input.GetMouseButtonDown(1))
            {
                CellCoordinate cell = ToCell(MouseWorld());
                EntityId enemy = FindEnemyAt(cell);
                if (enemy.IsValid) IssueAttack(enemy);
                else if (attackMoveMode) { IssueAttackMove(cell); attackMoveMode = false; }
                else IssueMove(cell);
            }
        }

        private Vector3 MouseWorld()
        {
            Vector3 point = playCamera.ScreenToWorldPoint(Input.mousePosition);
            return new Vector3(point.x, point.y, 0f);
        }

        private CellCoordinate ToCell(Vector3 world) => new CellCoordinate(Mathf.Clamp(Mathf.RoundToInt(world.x), 0, Runtime.Config.Width - 1), Mathf.Clamp(Mathf.RoundToInt(world.y), 0, Runtime.Config.Height - 1));

        private EntityId FindEnemyAt(CellCoordinate cell)
        {
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            return snapshot.Entities.Where(x => x.Owner.Value != Runtime.HumanPlayer.Value && x.Kind == HumanPlaytestEntityKind.Unit && x.X == cell.X && x.Y == cell.Y).Select(x => x.Entity).FirstOrDefault();
        }

        private void SelectAtCell(CellCoordinate cell, bool additive)
        {
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            HumanPlaytestEntitySnapshot candidate = snapshot.Entities.Where(x => x.Owner.Value == Runtime.HumanPlayer.Value && x.Kind == HumanPlaytestEntityKind.Unit && x.X == cell.X && x.Y == cell.Y).OrderBy(x => x.Entity).FirstOrDefault();
            if (candidate.Entity.IsValid) SelectSingle(candidate.Entity, additive);
            else if (!additive) Client.SetSelection(new SelectionState(Array.Empty<EntityId>()));
        }

        private void SelectBox(Vector3 start, Vector3 end, bool additive)
        {
            float minX = Mathf.Min(start.x, end.x); float maxX = Mathf.Max(start.x, end.x); float minY = Mathf.Min(start.y, end.y); float maxY = Mathf.Max(start.y, end.y);
            HumanPlaytestSnapshot snapshot = Runtime.CaptureSnapshot();
            IEnumerable<EntityId> hits = snapshot.Entities.Where(x => x.Owner.Value == Runtime.HumanPlayer.Value && x.Kind == HumanPlaytestEntityKind.Unit && x.X >= minX && x.X <= maxX && x.Y >= minY && x.Y <= maxY).Select(x => x.Entity);
            IEnumerable<EntityId> ids = additive ? Client.Selection.Entities.Concat(hits) : hits;
            SelectionResult result = SelectionService.Replace(ids, new SelectionPolicy(256));
            if (result.IsSuccess) Client.SetSelection(result.Selection);
        }

        private bool TryApplyExternalVisual(GameObject target, HumanPlaytestEntitySnapshot entity)
        {
            if (target == null || externalVisualProvider == null || !externalVisualProvider.IsAvailable) return false;
            bool enemy = entity.Owner.Value != Runtime.HumanPlayer.Value;
            HumanPlaytestVisualRole role = RoleFor(entity.Kind, enemy);
            Mesh mesh;
            if (externalVisualProvider.TryGetVoxelMesh(role, out mesh) && mesh != null)
            {
                Material material = GetVoxelMaterial(enemy);
                if (material == null) return false;
                MeshFilter filter = target.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = target.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
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

        private Material GetVoxelMaterial(bool enemy)
        {
            Material material;
            if (voxelMaterials.TryGetValue(enemy, out material) && material != null) return material;
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) return null;
            material = new Material(shader)
            {
                name = enemy ? "ExternalLegacyEnemyMaterial" : "ExternalLegacyHumanMaterial",
                color = enemy ? new Color(0.85f, 0.18f, 0.16f, 1f) : new Color(0.18f, 0.55f, 0.95f, 1f)
            };
            voxelMaterials[enemy] = material;
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
                    if (!external)
                    {
                        SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
                        renderer.sprite = unitSprite;
                        syntheticObjectFallbackCount = checked(syntheticObjectFallbackCount + 1);
                    }
                    else externalObjectCount = checked(externalObjectCount + 1);
                    entityObjects[entity.Entity] = target;
                    Client.RegisterPickTarget(new UnityPickTarget(entity.Entity, new CellCoordinate(entity.X, entity.Y), target.name, entity.Entity.Index));
                }
                target.transform.position = new Vector3(entity.X, entity.Y, entity.Kind == HumanPlaytestEntityKind.Unit ? 0f : 0.5f);
                target.transform.localScale = new Vector3(entity.Kind == HumanPlaytestEntityKind.Unit ? 0.72f : 1.15f, entity.Kind == HumanPlaytestEntityKind.Unit ? 0.72f : 1.15f, 1f);
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
            bool useExternal = externalVisualProvider != null && externalVisualProvider.IsAvailable;
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
            IVisualAssetProvider[] providers = useExternal
                ? new IVisualAssetProvider[] { externalVisualProvider, new SyntheticProvider() }
                : new IVisualAssetProvider[] { new SyntheticProvider() };
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
            GUILayout.Label("LMB select/drag, Shift add, RMB move/attack, A attack-move, S stop, H hold, WASD/arrows pan, wheel zoom.");
            GUILayout.EndArea();
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
            foreach (Material material in voxelMaterials.Values) DestroyObject(material);
            voxelMaterials.Clear();
            if (externalVisualProvider != null) externalVisualProvider.Dispose();
            externalVisualProvider = null;
        }

        private static void DestroyObject(UnityEngine.Object target)
        { if (target == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(target); else UnityEngine.Object.DestroyImmediate(target); }

        private sealed class SyntheticProvider : IVisualAssetProvider
        {
            public string ProviderId => "synthetic-playtest";
            public VisualAssetProviderResult Resolve(VisualAssetId assetId) => new VisualAssetProviderResult(VisualAssetProviderResolutionStatus.Resolved, ProviderId, assetId);
        }
    }

    internal static class SyntheticColorExtensions
    {
        public static Color WithAlpha(this Color color, float alpha) { color.a = alpha; return color; }
    }
}
