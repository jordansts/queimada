using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [SerializeField] private float combatantGroundClearance = 0.16f;
    [SerializeField] private float ballGroundClearance = 0.06f;
    [SerializeField] private float ballBobbingAmount = 0.12f;
    [SerializeField] private float ballVisualRadius = 0.34f;
    [SerializeField] private float ballVisualScale = 0.72f;
    [SerializeField] private float arenaHorizontalScale = 1.45f;
    [SerializeField] private float boundaryWallHeight = 3.5f;
    [SerializeField] private float boundaryWallThickness = 0.6f;
    [SerializeField] private float boundaryInset = 0.15f;
    [SerializeField] private float arenaDirectionalLightIntensity = 1.35f;
    [SerializeField] private Color arenaDirectionalLightColor = new Color(1f, 0.956f, 0.875f, 1f);
    [SerializeField] private Vector3 arenaDirectionalLightEuler = new Vector3(48f, -32f, 0f);
    [SerializeField] private Color arenaAmbientLightColor = new Color(0.7f, 0.73f, 0.78f, 1f);
    [SerializeField] private float arenaAmbientIntensity = 0.85f;

    public ArenaCombatant PlayerCombatant { get; private set; }
    public ArenaCombatant BotCombatant { get; private set; }
    public Transform CurrentLooseBallTransform => ballService != null ? ballService.CurrentLooseBallTransform : null;
    public float BallVisualScale => ballVisualScale;

    private readonly Vector3 playerArenaSpawn = new Vector3(-6.88f, 7.2f, -7.29f);
    private readonly Vector3 botArenaSpawn = new Vector3(7.41f, 6.78f, -2.91f);
    private readonly Color crosshairColor = new Color(0.35f, 0.95f, 1f, 0.95f);
    private bool scenePrepared;
    private ArenaLayout arenaLayout;
    private Transform arenaRoot;
    private Transform playFieldRoot;
    private BoxCollider playFieldSurfaceCollider;
    private Transform boundaryRoot;
    private ArenaBallService ballService;

    private struct ArenaLayout
    {
        public bool IsValid;
        public Bounds Bounds;
        public Vector3 PlayerSpawn;
        public Vector3 BotSpawn;
        public Vector3 BallSpawn;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(MiniGameManager));
        DontDestroyOnLoad(managerObject);
        managerObject.AddComponent<MiniGameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ballService = GetOrAddComponent<ArenaBallService>(gameObject);
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
        CacheArenaRoot();
        CachePlayField();
        ExpandArenaHorizontally();
        EnsurePlayFieldSurfaceCollider();
        RebuildPlayFieldBoundaries();
        EnsureArenaLighting();
        RefreshArenaLayout();
        ballService.Configure(ResolveGroundPosition, ballVisualScale, ballVisualRadius, ballBobbingAmount, ballGroundClearance);
        RemoveCollectibles();
        SpawnCombatants();
        SpawnArenaBall(GetArenaBallSpawnPoint());
        EnsureFallbackCamera();
    }

    private void RemoveCollectibles()
    {
        foreach (Pickup pickup in FindObjectsByType<Pickup>())
        {
            Destroy(pickup.gameObject);
        }
    }

    private void SpawnCombatants()
    {
        Quaternion playerRotation = Quaternion.Euler(0f, 35f, 0f);
        Quaternion botRotation = Quaternion.Euler(0f, -145f, 0f);
        GameObject playerInstance;
        GameObject botInstance;

        if (!TryFindSceneCombatants(out playerInstance, out botInstance))
        {
            Debug.LogError("MiniGameManager could not find two scene combatants with CharacterController in GetStarted_Scene.");
            return;
        }

        Vector3 groundedPlayerSpawn = ResolveCombatantSpawnPosition(playerInstance, GetPlayerArenaSpawnPoint());
        Vector3 groundedBotSpawn = ResolveCombatantSpawnPosition(botInstance, GetBotArenaSpawnPoint());

        PlayerCombatant = ConfigurePlayer(playerInstance, groundedPlayerSpawn, playerRotation);
        BotCombatant = ConfigureBot(botInstance, groundedBotSpawn, botRotation);

        if (PlayerCombatant != null && BotCombatant != null)
        {
            PlayerCombatant.SetOpponent(BotCombatant);
            BotCombatant.SetOpponent(PlayerCombatant);
        }
    }

    private bool TryFindSceneCombatants(out GameObject playerObject, out GameObject botObject)
    {
        playerObject = null;
        botObject = null;

        GameObject namedPlayerRoot = FindLoadedSceneObject("PlayerRobotScene");
        GameObject namedBotRoot = FindLoadedSceneObject("AIRobotScene");
        if (namedPlayerRoot != null && namedBotRoot != null)
        {
            playerObject = GetActorRoot(namedPlayerRoot);
            botObject = GetActorRoot(namedBotRoot);
            return playerObject != null && botObject != null && playerObject != botObject;
        }

        CharacterController[] controllers = FindObjectsByType<CharacterController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (controllers == null || controllers.Length == 0)
        {
            return false;
        }

        GameObject[] candidates = new GameObject[controllers.Length];
        int candidateCount = 0;

        foreach (CharacterController controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            GameObject actorRoot = GetActorRoot(controller.gameObject);
            if (actorRoot == null)
            {
                continue;
            }

            if (!actorRoot.scene.IsValid() || !actorRoot.scene.isLoaded)
            {
                continue;
            }

            if (actorRoot == gameObject)
            {
                continue;
            }

            bool alreadyAdded = false;
            for (int i = 0; i < candidateCount; i++)
            {
                if (candidates[i] == actorRoot)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (alreadyAdded)
            {
                continue;
            }

            candidates[candidateCount++] = actorRoot;
        }

        if (candidateCount < 2)
        {
            return false;
        }

        float bestPlayerScore = float.MaxValue;
        int playerIndex = -1;
        Vector3 desiredPlayerSpawn = GetPlayerArenaSpawnPoint();

        for (int i = 0; i < candidateCount; i++)
        {
            GameObject candidate = candidates[i];
            float score = Vector3.SqrMagnitude(candidate.transform.position - desiredPlayerSpawn);
            if (GetComponentInSelfOrChildren<PlayerInput>(candidate) != null)
            {
                score -= 100000f;
            }

            if (score < bestPlayerScore)
            {
                bestPlayerScore = score;
                playerIndex = i;
            }
        }

        if (playerIndex < 0)
        {
            return false;
        }

        float bestBotScore = float.MaxValue;
        int botIndex = -1;
        Vector3 desiredBotSpawn = GetBotArenaSpawnPoint();

        for (int i = 0; i < candidateCount; i++)
        {
            if (i == playerIndex)
            {
                continue;
            }

            GameObject candidate = candidates[i];
            float score = Vector3.SqrMagnitude(candidate.transform.position - desiredBotSpawn);
            if (GetComponentInSelfOrChildren<PlayerInput>(candidate) != null)
            {
                score += 100000f;
            }

            if (score < bestBotScore)
            {
                bestBotScore = score;
                botIndex = i;
            }
        }

        if (botIndex < 0)
        {
            return false;
        }

        playerObject = candidates[playerIndex];
        botObject = candidates[botIndex];
        return true;
    }

    private ArenaCombatant ConfigurePlayer(GameObject playerObject, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject sceneRoot = GetSceneInstanceRoot(playerObject);
        sceneRoot.name = "PlayerRobot";
        sceneRoot.tag = "Player";

        GameObject actorRoot = GetActorRoot(sceneRoot);
        actorRoot.tag = "Player";

        SetComponentEnabled<RespawnPlayer>(actorRoot, false);
        ConfigureGroundMask(actorRoot);
        GetOrAddComponent<ArenaRuntimeRig>(actorRoot).Initialize(sceneRoot.transform, true);

        ArenaCombatant combatant = PrepareCombatant(actorRoot, "Player", true, spawnPosition, spawnRotation);
        GetOrAddThrowClipPlayer(actorRoot).Initialize(actorRoot.transform);
        GetOrAddComponent<ArenaPlayerActionAnimator>(actorRoot);
        GetOrAddComponent<ArenaPlayerShooter>(actorRoot).Initialize(combatant);

        return combatant;
    }

    private ArenaCombatant ConfigureBot(GameObject botObject, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject sceneRoot = GetSceneInstanceRoot(botObject);
        sceneRoot.name = "AIRobot";
        sceneRoot.tag = "Untagged";

        GameObject actorRoot = GetActorRoot(sceneRoot);
        actorRoot.tag = "Untagged";

        SetComponentEnabled<RespawnPlayer>(actorRoot, false);
        ConfigureGroundMask(actorRoot);
        GetOrAddComponent<ArenaRuntimeRig>(actorRoot).Initialize(sceneRoot.transform, false);

        ArenaCombatant combatant = PrepareCombatant(actorRoot, "AI Robot", false, spawnPosition, spawnRotation);
        GetOrAddThrowClipPlayer(actorRoot).Initialize(actorRoot.transform);
        GetOrAddComponent<ArenaBotController>(actorRoot).Initialize(combatant);

        return combatant;
    }
    private ArenaCombatant PrepareCombatant(GameObject actor, string displayName, bool isPlayerControlled, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        ArenaKnockbackMotor knockbackMotor = GetOrAddComponent<ArenaKnockbackMotor>(actor);
        ArenaCombatant combatant = GetOrAddComponent<ArenaCombatant>(actor);
        PositionCombatantOnGround(actor, spawnPosition);
        Vector3 alignedSpawnPosition = actor.transform.position;
        combatant.Initialize(displayName, isPlayerControlled, alignedSpawnPosition, spawnRotation, knockbackMotor);

        Transform heldBallAnchor = BuildHeldBallAnchor(actor.transform);
        Transform throwOrigin = BuildThrowOrigin(actor.transform);
        combatant.SetAttachmentPoints(heldBallAnchor, throwOrigin);

        return combatant;
    }

    private Transform BuildHeldBallAnchor(Transform root)
    {
        Transform existingWeapon = FindChildRecursive(root, "ArenaWeapon");
        if (existingWeapon != null)
        {
            Destroy(existingWeapon.gameObject);
        }

        Transform existingHeldBallAnchor = FindChildRecursive(root, "HeldBallAnchor");
        if (existingHeldBallAnchor != null)
        {
            return existingHeldBallAnchor;
        }

        Transform hand = FindChildRecursive(root, "Right_Hand");
        if (hand == null)
        {
            hand = FindChildRecursive(root, "RightHand");
        }
        if (hand == null)
        {
            hand = FindChildRecursive(root, "B-hand.R");
        }
        if (hand == null)
        {
            hand = root;
        }

        GameObject heldBallAnchor = new GameObject("HeldBallAnchor");
        heldBallAnchor.transform.SetParent(hand, false);
        heldBallAnchor.transform.localPosition = new Vector3(0.02f, 0.01f, 0.16f);
        heldBallAnchor.transform.localRotation = Quaternion.identity;

        return heldBallAnchor.transform;
    }

    private Transform BuildThrowOrigin(Transform root)
    {
        Transform existingThrowOrigin = FindChildRecursive(root, "ThrowOrigin");
        if (existingThrowOrigin != null)
        {
            return existingThrowOrigin;
        }

        GameObject throwOrigin = new GameObject("ThrowOrigin");
        throwOrigin.transform.SetParent(root, false);
        throwOrigin.transform.localPosition = new Vector3(0.18f, 1.22f, 0.42f);
        throwOrigin.transform.localRotation = Quaternion.identity;

        return throwOrigin.transform;
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

    private void EnsureFallbackCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject fallbackCamera = new GameObject("FallbackMainCamera", typeof(Camera), typeof(AudioListener));
        fallbackCamera.tag = "MainCamera";
        fallbackCamera.transform.position = new Vector3(0f, 10f, -14f);
        fallbackCamera.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
    }

    private void EnsureArenaLighting()
    {
        Light arenaLight = FindArenaDirectionalLight();
        if (arenaLight == null)
        {
            GameObject lightObject = new GameObject("ArenaDirectionalLight");
            arenaLight = lightObject.AddComponent<Light>();
            arenaLight.type = LightType.Directional;
        }

        arenaLight.type = LightType.Directional;
        arenaLight.color = arenaDirectionalLightColor;
        arenaLight.intensity = arenaDirectionalLightIntensity;
        arenaLight.shadows = LightShadows.Soft;
        arenaLight.transform.rotation = Quaternion.Euler(arenaDirectionalLightEuler);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = arenaAmbientLightColor;
        RenderSettings.ambientIntensity = arenaAmbientIntensity;
    }

    private Light FindArenaDirectionalLight()
    {
        Light fallbackDirectional = null;

        foreach (Light lightComponent in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (lightComponent == null)
            {
                continue;
            }

            if (lightComponent.type != LightType.Directional)
            {
                continue;
            }

            if (lightComponent.name == "Directional Light" || lightComponent.name == "ArenaDirectionalLight")
            {
                return lightComponent;
            }

            if (fallbackDirectional == null)
            {
                fallbackDirectional = lightComponent;
            }
        }

        return fallbackDirectional;
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

    public void RegisterPickupCollected(Pickup pickup)
    {
    }

    public void RegisterRespawn()
    {
    }

    public string GetHudText()
    {
        string enemyStatus = BotCombatant != null && BotCombatant.gameObject.activeSelf ? "ativo" : "desativado";
        return $"F arremessa, mouse direito defende, Ctrl rola, espaco faz double jump. Pegue a bola e cause dano.  Debug: F2 inimigo {enemyStatus}, F3 recuperar bola.";
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        string ballStatus = "Ball: in play";
        if (PlayerCombatant != null && PlayerCombatant.HasBall)
        {
            ballStatus = "Ball: player";
        }
        else if (BotCombatant != null && BotCombatant.HasBall)
        {
            ballStatus = "Ball: AI";
        }
        else if (CurrentLooseBallTransform != null)
        {
            ballStatus = "Ball: floor";
        }

        GUI.Box(new Rect(20f, 20f, 1100f, 64f), $"F throw   RMB defend   Ctrl roll   Space double jump   F2 enemy toggle   F3 recover ball   {ballStatus}", style);
        DrawHealthBar(new Rect(20f, 94f, 320f, 28f), "Player", PlayerCombatant, new Color(0.25f, 0.9f, 1f));
        DrawHealthBar(new Rect(Screen.width - 340f, 94f, 320f, 28f), "AI", BotCombatant, new Color(1f, 0.4f, 0.2f));

        DrawCrosshair();
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
#if ENABLE_INPUT_SYSTEM
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
#else
        return key switch
        {
            Key.F2 => Input.GetKeyDown(KeyCode.F2),
            Key.F3 => Input.GetKeyDown(KeyCode.F3),
            _ => false
        };
#endif
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

    private void DrawCrosshair()
    {
        Color previousColor = GUI.color;
        GUI.color = crosshairColor;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float gap = 7f;
        float armLength = 12f;
        float thickness = 2.5f;
        float dotSize = 4f;

        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - gap - armLength, thickness, armLength), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY + gap, thickness, armLength), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - gap - armLength, centerY - thickness * 0.5f, armLength, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness * 0.5f, armLength, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize), Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
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

    private static void SetComponentEnabled<T>(GameObject target, bool enabled) where T : Behaviour
    {
        T component = GetComponentInSelfOrChildren<T>(target);
        if (component != null)
        {
            component.enabled = enabled;
        }
    }

    private static void ConfigureGroundMask(GameObject target)
    {
        ThirdPersonController controller = GetComponentInSelfOrChildren<ThirdPersonController>(target);
        if (controller != null)
        {
            controller.GroundLayers = ~0;
        }
    }

    private static ArenaThrowClipPlayer GetOrAddThrowClipPlayer(GameObject target)
    {
        return GetOrAddComponent<ArenaThrowClipPlayer>(target);
    }

    private void DrawHealthBar(Rect rect, string label, ArenaCombatant combatant, Color fillColor)
    {
        GUI.Box(rect, string.Empty);
        if (combatant == null)
        {
            return;
        }

        float padding = 3f;
        Rect fillRect = new Rect(
            rect.x + padding,
            rect.y + padding,
            (rect.width - padding * 2f) * combatant.HealthNormalized,
            rect.height - padding * 2f);

        Color previousColor = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(rect, $"{label} HP {Mathf.CeilToInt(combatant.CurrentHealth)}/{Mathf.CeilToInt(combatant.MaxHealth)}", new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        });
        GUI.color = previousColor;
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

    private static GameObject GetSceneInstanceRoot(GameObject instanceRoot)
    {
        Transform root = instanceRoot.transform.root;
        return root != null ? root.gameObject : instanceRoot;
    }

    private static GameObject FindLoadedSceneObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        foreach (GameObject rootObject in activeScene.GetRootGameObjects())
        {
            if (rootObject != null && rootObject.name == objectName)
            {
                return rootObject;
            }
        }

        return null;
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
            arenaBounds = new Bounds(Vector3.zero, new Vector3(24f, 4f, 24f));
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

    private void CacheArenaRoot()
    {
        if (arenaRoot != null)
        {
            return;
        }

        GameObject arenaObject = GameObject.Find("Arena");
        arenaRoot = arenaObject != null ? arenaObject.transform : null;
    }

    private void CachePlayField()
    {
        if (playFieldRoot != null)
        {
            return;
        }

        GameObject playFieldObject = GameObject.Find("PlayField");
        playFieldRoot = playFieldObject != null ? playFieldObject.transform : null;
    }

    private void ExpandArenaHorizontally()
    {
        if (arenaRoot == null)
        {
            return;
        }

        if (arenaHorizontalScale <= 0f || Mathf.Approximately(arenaHorizontalScale, 1f))
        {
            return;
        }

        Vector3 localScale = arenaRoot.localScale;
        arenaRoot.localScale = new Vector3(
            localScale.x * arenaHorizontalScale,
            localScale.y,
            localScale.z * arenaHorizontalScale);
    }

    private void EnsurePlayFieldSurfaceCollider()
    {
        if (playFieldRoot == null)
        {
            return;
        }

        if (!TryCalculateBounds(playFieldRoot, out Bounds worldBounds))
        {
            return;
        }

        playFieldSurfaceCollider = playFieldRoot.GetComponent<BoxCollider>();
        if (playFieldSurfaceCollider == null)
        {
            playFieldSurfaceCollider = playFieldRoot.gameObject.AddComponent<BoxCollider>();
        }

        Vector3 localCenter = playFieldRoot.InverseTransformPoint(worldBounds.center);
        Vector3 lossyScale = playFieldRoot.lossyScale;
        float scaleX = Mathf.Approximately(lossyScale.x, 0f) ? 1f : Mathf.Abs(lossyScale.x);
        float scaleY = Mathf.Approximately(lossyScale.y, 0f) ? 1f : Mathf.Abs(lossyScale.y);
        float scaleZ = Mathf.Approximately(lossyScale.z, 0f) ? 1f : Mathf.Abs(lossyScale.z);
        Vector3 localSize = new Vector3(
            worldBounds.size.x / scaleX,
            worldBounds.size.y / scaleY,
            worldBounds.size.z / scaleZ);

        playFieldSurfaceCollider.center = localCenter;
        playFieldSurfaceCollider.size = localSize;
        playFieldSurfaceCollider.isTrigger = false;
    }

    private void RebuildPlayFieldBoundaries()
    {
        if (!TryGetPlayFieldBounds(out Bounds bounds))
        {
            return;
        }

        if (boundaryRoot != null)
        {
            Destroy(boundaryRoot.gameObject);
        }

        boundaryRoot = new GameObject("PlayFieldBoundaries").transform;
        boundaryRoot.SetParent(playFieldRoot, false);

        float wallCenterY = bounds.max.y + boundaryWallHeight * 0.5f;
        float innerWidth = Mathf.Max(0.1f, bounds.size.x - boundaryInset * 2f);
        float innerDepth = Mathf.Max(0.1f, bounds.size.z - boundaryInset * 2f);

        CreateBoundaryWall("NorthWall",
            new Vector3(bounds.center.x, wallCenterY, bounds.max.z + boundaryWallThickness * 0.5f),
            new Vector3(innerWidth, boundaryWallHeight, boundaryWallThickness));
        CreateBoundaryWall("SouthWall",
            new Vector3(bounds.center.x, wallCenterY, bounds.min.z - boundaryWallThickness * 0.5f),
            new Vector3(innerWidth, boundaryWallHeight, boundaryWallThickness));
        CreateBoundaryWall("EastWall",
            new Vector3(bounds.max.x + boundaryWallThickness * 0.5f, wallCenterY, bounds.center.z),
            new Vector3(boundaryWallThickness, boundaryWallHeight, innerDepth));
        CreateBoundaryWall("WestWall",
            new Vector3(bounds.min.x - boundaryWallThickness * 0.5f, wallCenterY, bounds.center.z),
            new Vector3(boundaryWallThickness, boundaryWallHeight, innerDepth));
    }

    private void CreateBoundaryWall(string wallName, Vector3 worldPosition, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(boundaryRoot, false);
        wall.transform.position = worldPosition;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;
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

    public GameObject CreateArenaBallVisualInstance(bool includePickupCollider, string objectName)
    {
        return ballService != null
            ? ballService.CreateArenaBallVisualInstance(includePickupCollider, objectName)
            : null;
    }

}
