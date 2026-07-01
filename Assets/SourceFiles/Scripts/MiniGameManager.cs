using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

[RequireComponent(typeof(ArenaBallService))]
public class MiniGameManager : MonoBehaviour
{
    private readonly struct SceneCombatantBinding
    {
        public SceneCombatantBinding(GameObject sceneRoot, GameObject actorRoot, ArenaBallAttachmentPoints attachmentPoints)
        {
            SceneRoot = sceneRoot;
            ActorRoot = actorRoot;
            AttachmentPoints = attachmentPoints;
        }

        public GameObject SceneRoot { get; }
        public GameObject ActorRoot { get; }
        public ArenaBallAttachmentPoints AttachmentPoints { get; }
    }

    public static MiniGameManager Instance { get; private set; }

    private const string PlayerSceneRootName = "PlayerRobotScene";
    private const string BotSceneRootName = "AIRobotScene";

    [Header("Scene References")]
    [SerializeField] private Transform arenaRoot;
    [SerializeField] private Transform playFieldRoot;
    [SerializeField] private Collider playFieldSurfaceCollider;
    [SerializeField] private Light arenaDirectionalLight;
    [SerializeField] private ArenaHudView hudView;
    [SerializeField] private Collider[] invisibleWalls;

    [SerializeField] private float combatantGroundClearance = 0.16f;
    [SerializeField] private float ballGroundClearance = 0.06f;
    [SerializeField] private float ballBobbingAmount = 0.12f;
    [SerializeField] private float ballVisualRadius = 0.34f;

    public ArenaCombatant PlayerCombatant { get; private set; }
    public ArenaCombatant BotCombatant { get; private set; }
    public Transform CurrentLooseBallTransform => ballService != null ? ballService.CurrentLooseBallTransform : null;

    private readonly Vector3 playerArenaSpawn = new Vector3(-6.88f, 7.2f, -7.29f);
    private readonly Vector3 botArenaSpawn = new Vector3(7.41f, 6.78f, -2.91f);
    private bool scenePrepared;
    private ArenaLayout arenaLayout;
    private ArenaBallService ballService;

    private struct ArenaLayout
    {
        public bool IsValid;
        public Bounds Bounds;
        public Vector3 PlayerSpawn;
        public Vector3 BotSpawn;
        public Vector3 BallSpawn;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ballService = GetComponent<ArenaBallService>();
        if (ballService == null)
        {
            Debug.LogError("MiniGameManager requires ArenaBallService on the same GameObject.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PrepareArena();
    }

    private void Update()
    {
        HandleDebugShortcuts();
        RefreshHud();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        scenePrepared = false;
        PlayerCombatant = null;
        BotCombatant = null;
        PrepareArena();
    }

    private void PrepareArena()
    {
        if (scenePrepared)
        {
            return;
        }

        scenePrepared = true;
        ValidateSceneReferences();
        RefreshArenaLayout();
        ballService.Configure(ResolveGroundPosition, ballVisualRadius, ballBobbingAmount, ballGroundClearance);
        if (!SpawnCombatants())
        {
            return;
        }

        SpawnArenaBall(GetArenaBallSpawnPoint());
    }

    private bool SpawnCombatants()
    {
        Quaternion playerRotation = Quaternion.Euler(0f, 35f, 0f);
        Quaternion botRotation = Quaternion.Euler(0f, -145f, 0f);
        SceneCombatantBinding playerBinding;
        SceneCombatantBinding botBinding;

        if (!TryResolveSceneCombatants(out playerBinding, out botBinding))
        {
            return false;
        }

        Vector3 groundedPlayerSpawn = ResolveCombatantSpawnPosition(playerBinding.ActorRoot, GetPlayerArenaSpawnPoint());
        Vector3 groundedBotSpawn = ResolveCombatantSpawnPosition(botBinding.ActorRoot, GetBotArenaSpawnPoint());

        PlayerCombatant = ConfigurePlayer(playerBinding, groundedPlayerSpawn, playerRotation);
        BotCombatant = ConfigureBot(botBinding, groundedBotSpawn, botRotation);

        if (PlayerCombatant != null && BotCombatant != null)
        {
            PlayerCombatant.SetOpponent(BotCombatant);
            BotCombatant.SetOpponent(PlayerCombatant);
        }

        return PlayerCombatant != null && BotCombatant != null;
    }

    private bool TryResolveSceneCombatants(out SceneCombatantBinding playerBinding, out SceneCombatantBinding botBinding)
    {
        playerBinding = default;
        botBinding = default;

        GameObject namedPlayerRoot = FindRequiredSceneRoot(PlayerSceneRootName);
        GameObject namedBotRoot = FindRequiredSceneRoot(BotSceneRootName);

        if (namedPlayerRoot == null || namedBotRoot == null)
        {
            Debug.LogError(
                $"MiniGameManager requires '{PlayerSceneRootName}' and '{BotSceneRootName}' roots present in the scene.",
                this);
            return false;
        }

        if (!TryCreateSceneCombatantBinding(namedPlayerRoot, out playerBinding))
        {
            Debug.LogError($"MiniGameManager could not resolve a valid combatant from '{PlayerSceneRootName}'.", namedPlayerRoot);
            return false;
        }

        if (!TryCreateSceneCombatantBinding(namedBotRoot, out botBinding))
        {
            Debug.LogError($"MiniGameManager could not resolve a valid combatant from '{BotSceneRootName}'.", namedBotRoot);
            return false;
        }

        if (playerBinding.ActorRoot == botBinding.ActorRoot)
        {
            Debug.LogError("MiniGameManager resolved the same actor root for player and bot. Fix the scene roots.", this);
            return false;
        }

        return true;
    }

    private ArenaCombatant ConfigurePlayer(SceneCombatantBinding binding, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject sceneRoot = binding.SceneRoot;
        GameObject actorRoot = binding.ActorRoot;
        actorRoot.tag = "Player";

        ConfigureGroundMask(actorRoot);
        EnsureGameplayComponent<ArenaRuntimeRig>(actorRoot).Initialize(sceneRoot.transform, true);

        ArenaCombatant combatant = PrepareCombatant(actorRoot, binding.AttachmentPoints, "Player", true, spawnPosition, spawnRotation);
        EnsureThrowClipPlayer(actorRoot).Initialize();
        EnsureGameplayComponent<ArenaPlayerShooter>(actorRoot).Initialize(combatant);

        return combatant;
    }

    private ArenaCombatant ConfigureBot(SceneCombatantBinding binding, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject sceneRoot = binding.SceneRoot;
        GameObject actorRoot = binding.ActorRoot;
        actorRoot.tag = "Untagged";

        ConfigureGroundMask(actorRoot);
        EnsureGameplayComponent<ArenaRuntimeRig>(actorRoot).Initialize(sceneRoot.transform, false);

        ArenaCombatant combatant = PrepareCombatant(actorRoot, binding.AttachmentPoints, "AI Robot", false, spawnPosition, spawnRotation);
        EnsureThrowClipPlayer(actorRoot).Initialize();
        EnsureGameplayComponent<ArenaBotController>(actorRoot).Initialize(combatant);

        return combatant;
    }
    private ArenaCombatant PrepareCombatant(
        GameObject actor,
        ArenaBallAttachmentPoints attachmentPoints,
        string displayName,
        bool isPlayerControlled,
        Vector3 spawnPosition,
        Quaternion spawnRotation)
    {
        ArenaKnockbackMotor knockbackMotor = EnsureGameplayComponent<ArenaKnockbackMotor>(actor);
        ArenaCombatant combatant = EnsureGameplayComponent<ArenaCombatant>(actor);
        PositionCombatantOnGround(actor, spawnPosition);
        Vector3 alignedSpawnPosition = actor.transform.position;
        combatant.Initialize(displayName, isPlayerControlled, alignedSpawnPosition, spawnRotation, knockbackMotor);

        combatant.SetAttachmentPoints(
            attachmentPoints != null ? attachmentPoints.HeldBallAnchor : null,
            attachmentPoints != null ? attachmentPoints.ThrowOrigin : null,
            attachmentPoints != null ? attachmentPoints.HeldBallVisual : null);

        return combatant;
    }

    private ArenaBallAttachmentPoints ResolveBallAttachmentPoints(Transform root)
    {
        ArenaBallAttachmentPoints attachmentPoints = root.GetComponentInChildren<ArenaBallAttachmentPoints>(true);
        if (attachmentPoints != null &&
            attachmentPoints.HeldBallAnchor != null &&
            attachmentPoints.ThrowOrigin != null &&
            attachmentPoints.HeldBallVisual != null)
        {
            return attachmentPoints;
        }

        Debug.LogError(
            $"MiniGameManager could not find valid ArenaBallAttachmentPoints for '{root.name}'. " +
            "Fix the prefab/scene setup instead of relying on runtime reconstruction.",
            root);
        return null;
    }

    private bool TryCreateSceneCombatantBinding(GameObject sceneRoot, out SceneCombatantBinding binding)
    {
        binding = default;
        if (sceneRoot == null)
        {
            return false;
        }

        GameObject actorRoot = GetActorRoot(sceneRoot);
        ArenaBallAttachmentPoints attachmentPoints = ResolveBallAttachmentPoints(sceneRoot.transform);
        if (actorRoot == null || attachmentPoints == null)
        {
            return false;
        }

        binding = new SceneCombatantBinding(sceneRoot, actorRoot, attachmentPoints);
        return true;
    }

    private Vector3 ResolveCombatantSpawnPosition(GameObject actorPrefab, Vector3 desiredPosition)
    {
        Vector3 groundedPosition = ResolveGroundPosition(desiredPosition);
        CharacterController controller = actorPrefab.GetComponentInChildren<CharacterController>(true);
        if (controller == null)
        {
            return groundedPosition;
        }

        float controllerBottomOffset = controller.center.y - (controller.height * 0.5f);
        return groundedPosition - Vector3.up * controllerBottomOffset + Vector3.up * combatantGroundClearance;
    }

    private void PositionCombatantOnGround(GameObject actor, Vector3 desiredPosition)
    {
        Vector3 groundedPosition = ResolveGroundPosition(desiredPosition);
        CharacterController controller = actor.GetComponent<CharacterController>();
        if (controller == null)
        {
            actor.transform.position = groundedPosition;
            return;
        }

        float controllerBottomOffset = controller.center.y - (controller.height * 0.5f);
        Vector3 correctedPosition = new Vector3(
            groundedPosition.x,
            groundedPosition.y - controllerBottomOffset + combatantGroundClearance,
            groundedPosition.z);

        actor.transform.position = correctedPosition;
        AlignCombatantVisualToGround(actor, groundedPosition.y + 0.02f);
    }

    private void AlignCombatantVisualToGround(GameObject actor, float targetGroundY)
    {
        Renderer[] renderers = actor.GetComponentsInChildren<Renderer>(true);
        float visualBottom = float.MaxValue;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            visualBottom = Mathf.Min(visualBottom, renderer.bounds.min.y);
        }

        if (visualBottom == float.MaxValue)
        {
            return;
        }

        float liftAmount = targetGroundY - visualBottom;
        if (liftAmount > 0f)
        {
            actor.transform.position += Vector3.up * liftAmount;
        }
    }

    private void ValidateSceneReferences()
    {
        if (arenaRoot == null)
        {
            Debug.LogError("MiniGameManager requires arenaRoot assigned from the scene.", this);
        }

        if (playFieldRoot == null)
        {
            Debug.LogError("MiniGameManager requires playFieldRoot assigned from the scene.", this);
        }

        if (playFieldSurfaceCollider == null || playFieldSurfaceCollider.isTrigger)
        {
            Debug.LogError("MiniGameManager requires a non-trigger playFieldSurfaceCollider assigned from the scene.", this);
        }

        if (arenaDirectionalLight == null || arenaDirectionalLight.type != LightType.Directional)
        {
            Debug.LogError("MiniGameManager requires a directional light assigned from the scene.", this);
        }

        if (Camera.main == null)
        {
            Debug.LogError("MiniGameManager could not find a Main Camera in the scene.", this);
        }

        if (Camera.main != null && Camera.main.GetComponent<CinemachineBrain>() == null)
        {
            Debug.LogError("Main Camera is missing CinemachineBrain. Configure it in the scene.", Camera.main);
        }

        if (hudView == null)
        {
            Debug.LogError("MiniGameManager requires hudView assigned from the scene.", this);
        }

        if (FindRequiredSceneRoot(PlayerSceneRootName) == null || FindRequiredSceneRoot(BotSceneRootName) == null)
        {
            Debug.LogError(
                $"MiniGameManager requires '{PlayerSceneRootName}' and '{BotSceneRootName}' roots present in the scene.",
                this);
        }

        ValidateInvisibleWalls();
    }

    private void RefreshHud()
    {
        if (hudView == null)
        {
            return;
        }

        hudView.Refresh(PlayerCombatant, BotCombatant, CurrentLooseBallTransform);
    }

    public void HandleDefeat(ArenaCombatant victim)
    {
        if (victim == null)
        {
            return;
        }

        if (victim.HasBall)
        {
            SpawnArenaBall(victim.transform.position + Vector3.up * 0.35f, 0.45f);
            victim.RemoveBall();
        }

        victim.Respawn();
    }

    private void HandleDebugShortcuts()
    {
        if (WasDebugKeyPressed(Key.F2))
        {
            ToggleBotCombatant();
        }

        if (WasDebugKeyPressed(Key.F3))
        {
            RecoverBallToPlayer();
        }
    }

    private bool WasDebugKeyPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return key switch
        {
            Key.F2 => keyboard.f2Key.wasPressedThisFrame,
            Key.F3 => keyboard.f3Key.wasPressedThisFrame,
            _ => false
        };
    }

    private void ToggleBotCombatant()
    {
        if (BotCombatant == null)
        {
            return;
        }

        if (BotCombatant.gameObject.activeSelf)
        {
            if (BotCombatant.HasBall)
            {
                RecoverBallToPlayer();
            }

            BotCombatant.gameObject.SetActive(false);
            return;
        }

        BotCombatant.gameObject.SetActive(true);
        BotCombatant.Respawn();
    }

    private void RecoverBallToPlayer()
    {
        if (PlayerCombatant == null)
        {
            return;
        }

        ballService?.ClearLooseBall();

        if (BotCombatant != null && BotCombatant.HasBall)
        {
            BotCombatant.RemoveBall();
        }

        PlayerCombatant.GiveBall();
    }

    private static T EnsureGameplayComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static T GetComponentInSelfOrChildren<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.GetComponentInChildren<T>(true);
    }

    private static void ConfigureGroundMask(GameObject target)
    {
        ThirdPersonController controller = GetComponentInSelfOrChildren<ThirdPersonController>(target);
        if (controller != null)
        {
            controller.GroundLayers = ~0;
        }
    }

    private static ArenaThrowClipPlayer EnsureThrowClipPlayer(GameObject target)
    {
        return EnsureGameplayComponent<ArenaThrowClipPlayer>(target);
    }

    private static GameObject GetActorRoot(GameObject instanceRoot)
    {
        ThirdPersonController controller = instanceRoot.GetComponentInChildren<ThirdPersonController>(true);
        if (controller != null)
        {
            return controller.gameObject;
        }

        CharacterController characterController = instanceRoot.GetComponentInChildren<CharacterController>(true);
        if (characterController != null)
        {
            return characterController.gameObject;
        }

        return instanceRoot;
    }

    private static GameObject FindRequiredSceneRoot(string objectName)
    {
        return GameObject.Find(objectName);
    }

    public static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public void ClaimArenaBall(ArenaBallPickup pickup, ArenaCombatant combatant)
    {
        ballService?.ClaimArenaBall(pickup, combatant);
    }

    public void RegisterLooseArenaBall(ArenaBallPickup pickup)
    {
        ballService?.RegisterLooseArenaBall(pickup);
    }

    public void SpawnArenaBall(Vector3 position, float pickupDelay = 0f)
    {
        ballService?.SpawnArenaBall(position, pickupDelay);
    }

    public ArenaCombatant FindBestBallClaimant(Vector3 ballPosition, float claimRadius)
    {
        ArenaCombatant bestCombatant = null;
        float bestDistance = float.MaxValue;
        EvaluateBallClaimant(PlayerCombatant, ballPosition, claimRadius, ref bestCombatant, ref bestDistance);
        EvaluateBallClaimant(BotCombatant, ballPosition, claimRadius, ref bestCombatant, ref bestDistance);
        return bestCombatant;
    }

    private Vector3 GetArenaBallSpawnPoint()
    {
        return arenaLayout.IsValid ? arenaLayout.BallSpawn : Vector3.Lerp(playerArenaSpawn, botArenaSpawn, 0.5f);
    }

    private void RefreshArenaLayout()
    {
        arenaLayout = BuildArenaLayout();
    }

    private ArenaLayout BuildArenaLayout()
    {
        ArenaLayout layout = default;
        if (!TryGetPlayFieldBounds(out Bounds arenaBounds))
        {
            Debug.LogError("MiniGameManager could not build arena layout because play field bounds are unavailable.", this);
            return layout;
        }

        Vector3 center = arenaBounds.center;
        center.y = arenaBounds.max.y;

        float xOffset = Mathf.Max(4f, arenaBounds.extents.x * 0.32f);
        float zOffset = Mathf.Max(1.5f, arenaBounds.extents.z * 0.12f);

        Vector3 playerSpawn = new Vector3(center.x - xOffset, center.y, center.z - zOffset);
        Vector3 botSpawn = new Vector3(center.x + xOffset, center.y, center.z + zOffset);
        Vector3 ballSpawn = new Vector3(center.x, center.y, center.z);

        layout.IsValid = true;
        layout.Bounds = arenaBounds;
        layout.PlayerSpawn = ResolveGroundPosition(playerSpawn);
        layout.BotSpawn = ResolveGroundPosition(botSpawn);
        layout.BallSpawn = ResolveGroundPosition(ballSpawn);
        return layout;
    }

    private Vector3 GetPlayerArenaSpawnPoint()
    {
        return arenaLayout.IsValid ? arenaLayout.PlayerSpawn : playerArenaSpawn;
    }

    private Vector3 GetBotArenaSpawnPoint()
    {
        return arenaLayout.IsValid ? arenaLayout.BotSpawn : botArenaSpawn;
    }

    private bool TryGetPlayFieldBounds(out Bounds bounds)
    {
        bounds = default;
        if (playFieldSurfaceCollider != null && playFieldSurfaceCollider.enabled)
        {
            bounds = playFieldSurfaceCollider.bounds;
            return true;
        }

        return TryCalculateBounds(playFieldRoot, out bounds);
    }

    private void ValidateInvisibleWalls()
    {
        if (invisibleWalls == null || invisibleWalls.Length < 4)
        {
            Debug.LogError("MiniGameManager requires at least 4 InvisibleWall collider references assigned from the scene.", this);
            return;
        }

        for (int i = 0; i < invisibleWalls.Length; i++)
        {
            Collider wall = invisibleWalls[i];
            if (wall == null || !wall.enabled || wall.isTrigger)
            {
                Debug.LogError($"MiniGameManager has an invalid InvisibleWall reference at index {i}.", this);
            }
        }
    }

    private bool TryCalculateBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private Vector3 ResolveGroundPosition(Vector3 desiredPosition)
    {
        if (!TryGetPlayFieldBounds(out Bounds bounds))
        {
            return desiredPosition;
        }

        float clampedX = Mathf.Clamp(desiredPosition.x, bounds.min.x, bounds.max.x);
        float clampedZ = Mathf.Clamp(desiredPosition.z, bounds.min.z, bounds.max.z);
        return new Vector3(clampedX, bounds.max.y, clampedZ);
    }

    public ArenaProjectile ActivateArenaBallProjectile(string objectName, Vector3 position)
    {
        return ballService != null
            ? ballService.ActivateSceneProjectile(objectName, position)
            : null;
    }

    public void RecycleArenaBallProjectile(ArenaProjectile projectile)
    {
        ballService?.RecycleProjectile(projectile);
    }

    private static void EvaluateBallClaimant(
        ArenaCombatant candidate,
        Vector3 ballPosition,
        float claimRadius,
        ref ArenaCombatant bestCombatant,
        ref float bestDistance)
    {
        if (candidate == null || candidate.HasBall || !candidate.gameObject.activeInHierarchy)
        {
            return;
        }

        float distance = Vector3.Distance(
            Vector3.ProjectOnPlane(candidate.transform.position, Vector3.up),
            Vector3.ProjectOnPlane(ballPosition, Vector3.up));

        if (distance > claimRadius)
        {
            return;
        }

        if (bestCombatant == null || distance < bestDistance - 0.01f)
        {
            bestCombatant = candidate;
            bestDistance = distance;
            return;
        }

        if (Mathf.Abs(distance - bestDistance) <= 0.01f && candidate.IsPlayerControlled && !bestCombatant.IsPlayerControlled)
        {
            bestCombatant = candidate;
            bestDistance = distance;
        }
    }

}
