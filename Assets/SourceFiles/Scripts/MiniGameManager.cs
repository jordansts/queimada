using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    public ArenaCombatant PlayerCombatant { get; private set; }
    public ArenaCombatant BotCombatant { get; private set; }
    public Transform CurrentLooseBallTransform => currentLooseBall != null ? currentLooseBall.transform : null;

    private readonly Vector3 playerArenaSpawn = new Vector3(-6.88f, 7.2f, -7.29f);
    private readonly Vector3 botArenaSpawn = new Vector3(7.41f, 6.78f, -2.91f);
    private readonly Color crosshairColor = new Color(0.35f, 0.95f, 1f, 0.95f);
    private bool scenePrepared;
    private ArenaBallPickup currentLooseBall;

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
        GameObject playerPrefab = Resources.Load<GameObject>("Bootstrap/PlayerRobot");
        if (playerPrefab == null)
        {
            Debug.LogError("MiniGameManager could not load Resources/Bootstrap/PlayerRobot.prefab");
            return;
        }

        Quaternion playerRotation = Quaternion.Euler(0f, 35f, 0f);
        Quaternion botRotation = Quaternion.Euler(0f, -145f, 0f);

        GameObject playerInstance = Instantiate(playerPrefab, playerArenaSpawn, playerRotation);
        GameObject botInstance = Instantiate(playerPrefab, botArenaSpawn, botRotation);

        PlayerCombatant = ConfigurePlayer(playerInstance, playerArenaSpawn, playerRotation);
        BotCombatant = ConfigureBot(botInstance, botArenaSpawn, botRotation);

        if (PlayerCombatant != null && BotCombatant != null)
        {
            PlayerCombatant.SetOpponent(BotCombatant);
            BotCombatant.SetOpponent(PlayerCombatant);
        }
    }

    private ArenaCombatant ConfigurePlayer(GameObject playerObject, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        playerObject.name = "PlayerRobot";
        playerObject.tag = "Player";

        GameObject actorRoot = GetActorRoot(playerObject);
        actorRoot.tag = "Player";

        SetComponentEnabled<ThirdPersonController>(actorRoot, true);
        SetComponentEnabled<StarterAssetsInputs>(actorRoot, true);
        SetComponentEnabled<PlayerInput>(actorRoot, true);
        SetComponentEnabled<RespawnPlayer>(actorRoot, false);

        ArenaCombatant combatant = PrepareCombatant(actorRoot, "Player", true, spawnPosition, spawnRotation);
        GetOrAddThrowClipPlayer(actorRoot).Initialize(actorRoot.transform);
        GetOrAddComponent<ArenaPlayerActionAnimator>(actorRoot);
        GetOrAddComponent<ArenaPlayerShooter>(actorRoot).Initialize(combatant);

        return combatant;
    }

    private ArenaCombatant ConfigureBot(GameObject botObject, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        botObject.name = "AIRobot";
        botObject.tag = "Untagged";

        GameObject actorRoot = GetActorRoot(botObject);
        actorRoot.tag = "Untagged";

        SetComponentEnabled<ThirdPersonController>(actorRoot, false);
        SetComponentEnabled<StarterAssetsInputs>(actorRoot, false);
        SetComponentEnabled<PlayerInput>(actorRoot, false);
        SetComponentEnabled<RespawnPlayer>(actorRoot, false);

        DisableBotCameraRig(botObject.transform);

        ArenaCombatant combatant = PrepareCombatant(actorRoot, "AI Robot", false, spawnPosition, spawnRotation);
        GetOrAddThrowClipPlayer(actorRoot).Initialize(actorRoot.transform);
        GetOrAddComponent<ArenaBotController>(actorRoot).Initialize(combatant);

        return combatant;
    }
    private ArenaCombatant PrepareCombatant(GameObject actor, string displayName, bool isPlayerControlled, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        ArenaKnockbackMotor knockbackMotor = GetOrAddComponent<ArenaKnockbackMotor>(actor);
        ArenaCombatant combatant = GetOrAddComponent<ArenaCombatant>(actor);
        combatant.Initialize(displayName, isPlayerControlled, spawnPosition, spawnRotation, knockbackMotor);

        Transform throwOrigin = BuildThrowOrigin(actor.transform);
        combatant.SetWeaponMuzzle(throwOrigin);

        return combatant;
    }

    private Transform BuildThrowOrigin(Transform root)
    {
        Transform existingWeapon = FindChildRecursive(root, "ArenaWeapon");
        if (existingWeapon != null)
        {
            Destroy(existingWeapon.gameObject);
        }

        Transform existingThrowOrigin = FindChildRecursive(root, "ThrowOrigin");
        if (existingThrowOrigin != null)
        {
            return existingThrowOrigin;
        }

        Transform hand = FindChildRecursive(root, "Right_Hand");
        if (hand == null)
        {
            hand = root;
        }

        GameObject throwOrigin = new GameObject("ThrowOrigin");
        throwOrigin.transform.SetParent(hand, false);
        throwOrigin.transform.localPosition = new Vector3(0.02f, 0.01f, 0.16f);
        throwOrigin.transform.localRotation = Quaternion.identity;

        return throwOrigin.transform;
    }

    private void DisableBotCameraRig(Transform root)
    {
        foreach (Camera cameraComponent in root.GetComponentsInChildren<Camera>(true))
        {
            cameraComponent.enabled = false;
            cameraComponent.gameObject.tag = "Untagged";
            cameraComponent.gameObject.SetActive(false);
        }

        foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
        {
            listener.enabled = false;
        }

        Transform followCamera = FindChildRecursive(root, "PlayerFollowCamera");
        if (followCamera != null)
        {
            followCamera.gameObject.SetActive(false);
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
        return "F arremessa, mouse direito defende, Ctrl rola, espaco faz double jump. Pegue a bola e cause dano.";
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

        GUI.Box(new Rect(20f, 20f, 760f, 64f), $"F throw   RMB defend   Ctrl roll   Space double jump   {ballStatus}", style);
        DrawHealthBar(new Rect(20f, 94f, 320f, 28f), "Player", PlayerCombatant, new Color(0.25f, 0.9f, 1f));
        DrawHealthBar(new Rect(Screen.width - 340f, 94f, 320f, 28f), "AI", BotCombatant, new Color(1f, 0.4f, 0.2f));

        DrawCrosshair();
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

        ArenaPlayerShooter shooter = PlayerCombatant != null ? PlayerCombatant.GetComponent<ArenaPlayerShooter>() : null;
        Camera mainCamera = Camera.main;
        if (shooter != null && shooter.HasAimPoint && mainCamera != null)
        {
            Vector3 aimScreen = mainCamera.WorldToScreenPoint(shooter.CurrentAimPoint);
            if (aimScreen.z > 0f)
            {
                float aimMarkerSize = 10f;
                float screenY = Screen.height - aimScreen.y;
                Rect markerRect = new Rect(
                    aimScreen.x - aimMarkerSize * 0.5f,
                    screenY - aimMarkerSize * 0.5f,
                    aimMarkerSize,
                    aimMarkerSize);

                GUI.DrawTexture(new Rect(markerRect.x, markerRect.center.y - 1f, markerRect.width, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(markerRect.center.x - 1f, markerRect.y, 2f, markerRect.height), Texture2D.whiteTexture);
            }
        }

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
        if (pickup == null || combatant == null)
        {
            return;
        }

        if (currentLooseBall == pickup)
        {
            currentLooseBall = null;
        }

        combatant.GiveBall();
        Destroy(pickup.gameObject);
    }

    public void SpawnArenaBall(Vector3 position, float pickupDelay = 0f)
    {
        if (currentLooseBall != null)
        {
            Destroy(currentLooseBall.gameObject);
            currentLooseBall = null;
        }

        Vector3 groundedPosition = ResolveBallGroundPosition(position);
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.name = "ArenaBallPickup";
        ballObject.transform.position = groundedPosition;
        ballObject.transform.localScale = Vector3.one * 0.42f;

        SphereCollider collider = ballObject.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
            collider.radius = 0.6f;
        }

        Renderer renderer = ballObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.85f, 0.2f);
        }

        currentLooseBall = ballObject.AddComponent<ArenaBallPickup>();
        currentLooseBall.Initialize(groundedPosition, pickupDelay);
    }

    private Vector3 GetArenaBallSpawnPoint()
    {
        Vector3 midpoint = Vector3.Lerp(playerArenaSpawn, botArenaSpawn, 0.5f);
        midpoint.y = Mathf.Min(playerArenaSpawn.y, botArenaSpawn.y) + 0.45f;
        return midpoint;
    }

    private Vector3 ResolveBallGroundPosition(Vector3 desiredPosition)
    {
        Vector3 rayOrigin = desiredPosition + Vector3.up * 8f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.28f;
        }

        return desiredPosition;
    }
}
