using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Owns Henry's punch and kick timelines plus their animated limb hitboxes.
    /// It exposes attack commands for the combat AI but never chooses or starts
    /// an attack by itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HenryCombatHitboxController : MonoBehaviour
    {
        public const string HurtboxObjectName = "Henry_Hurtbox";

        [Header("References")]
        [SerializeField] private HenryRunAnimationPlayer animationPlayer;
        [SerializeField] private MeleeHitboxRig meleeHitboxRig;
        [SerializeField] private MeleeDamageDealer meleeDamageDealer;
        [SerializeField] private CombatHealth combatHealth;
        [SerializeField] private BoxCollider hurtbox;
        [SerializeField] private CombatHurtbox hurtboxMarker;

        [Header("Damage")]
        [SerializeField, Min(0f)] private float attackDamage = 20f;

        [Header("Punch")]
        [SerializeField, Range(0f, 1f)]
        private float punchHitWindowStart = 0.29f;
        [SerializeField, Range(0f, 1f)]
        private float punchHitWindowEnd = 0.43f;

        [Header("MMA Kick")]
        [SerializeField, Range(0f, 1f)]
        private float mmaKickHitWindowStart = 0.34f;
        [SerializeField, Range(0f, 1f)]
        private float mmaKickHitWindowEnd = 0.58f;

        [Header("Roundhouse Kick")]
        [SerializeField, Range(0f, 1f)]
        private float roundhouseHitWindowStart = 0.29f;
        [SerializeField, Range(0f, 1f)]
        private float roundhouseHitWindowEnd = 0.45f;

        [Header("Debug")]
        [SerializeField] private bool logHits;

        private HenryCombatAttack activeAttack;
        private int attackSequence;
        private bool combatReady;
        private bool attacking;
        private bool hitWindowOpened;
        private bool hitWindowConsumed;
        private int loggedHitCount;
        private bool setupValid;
        private bool setupErrorLogged;

        public bool IsCombatReady => combatReady;
        public bool IsAttacking => attacking;
        public CombatHealth CombatHealth => combatHealth;
        public BoxCollider Hurtbox => hurtbox;
        public CombatHurtbox HurtboxMarker => hurtboxMarker;
        public MeleeHitboxRig MeleeHitboxRig => meleeHitboxRig;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForInitiallyLoadedScene()
        {
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForScene(scene);
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform henry = FindChildRecursive(
                    roots[i].transform,
                    HenryTheftInteractable.HenryObjectName);
                if (henry == null)
                {
                    continue;
                }

                if (henry.GetComponent<HenryCombatHitboxController>() == null)
                {
                    henry.gameObject
                        .AddComponent<HenryCombatHitboxController>();
                }

                return;
            }
        }

        private void Awake()
        {
            EnsureRuntimeSetup();
        }

        private void OnDisable()
        {
            combatReady = false;
            if (hurtbox != null)
            {
                hurtbox.enabled = false;
            }

            CancelAttack(true);
        }

        private void OnDestroy()
        {
            if (combatHealth != null)
            {
                combatHealth.Died -= HandleHenryDied;
            }
        }

        private void LateUpdate()
        {
            if (!attacking || animationPlayer == null)
            {
                return;
            }

            if (!animationPlayer.IsCombatAttackPlaying(activeAttack))
            {
                FinishAttack();
                return;
            }

            float normalizedTime =
                animationPlayer.GetCombatAttackNormalizedTime(activeAttack);
            ResolveActiveAttack(
                out float damage,
                out float hitWindowStart,
                out float hitWindowEnd,
                out MeleeHitboxLimb hitLimb);

            if (!hitWindowOpened &&
                !hitWindowConsumed &&
                normalizedTime >= hitWindowEnd)
            {
                // Never reopen a hit window after its active animation frames.
                hitWindowConsumed = true;
            }
            else if (!hitWindowOpened &&
                     !hitWindowConsumed &&
                     normalizedTime >= hitWindowStart)
            {
                hitWindowOpened = meleeDamageDealer != null &&
                    meleeDamageDealer.BeginHitWindow(
                        hitLimb,
                        damage,
                        attackSequence);
                if (!hitWindowOpened)
                {
                    hitWindowConsumed = true;
                }
            }

            if (hitWindowOpened && normalizedTime < hitWindowEnd)
            {
                int hitCount = meleeDamageDealer.EvaluateHitWindow();
                if (logHits && hitCount > loggedHitCount)
                {
                    Debug.Log(
                        $"[HenryCombat] {activeAttack} hit {hitCount - loggedHitCount} new target(s).",
                        this);
                    loggedHitCount = hitCount;
                }
            }

            if (hitWindowOpened && normalizedTime >= hitWindowEnd)
            {
                meleeDamageDealer.EndHitWindow(attackSequence);
                hitWindowOpened = false;
                hitWindowConsumed = true;
            }
        }

        private void OnValidate()
        {
            attackDamage = Mathf.Max(0f, attackDamage);
            punchHitWindowStart = Mathf.Clamp01(punchHitWindowStart);
            punchHitWindowEnd = Mathf.Clamp(
                punchHitWindowEnd,
                punchHitWindowStart,
                1f);
            mmaKickHitWindowStart = Mathf.Clamp01(mmaKickHitWindowStart);
            mmaKickHitWindowEnd = Mathf.Clamp(
                mmaKickHitWindowEnd,
                mmaKickHitWindowStart,
                1f);
            roundhouseHitWindowStart =
                Mathf.Clamp01(roundhouseHitWindowStart);
            roundhouseHitWindowEnd = Mathf.Clamp(
                roundhouseHitWindowEnd,
                roundhouseHitWindowStart,
                1f);
        }

        public void EnterCombatMode()
        {
            if (!EnsureRuntimeSetup() ||
                combatHealth == null ||
                combatHealth.IsDead)
            {
                combatReady = false;
                if (hurtbox != null)
                {
                    hurtbox.enabled = false;
                }

                return;
            }

            combatReady = true;
            if (hurtbox != null)
            {
                hurtbox.enabled = true;
            }
        }

        public void ExitCombatMode(bool returnToIdle = true)
        {
            combatReady = false;
            if (hurtbox != null)
            {
                hurtbox.enabled = false;
            }

            CancelAttack(returnToIdle);
        }

        public bool TryPlayMmaKick()
        {
            return TryPlayAttack(HenryCombatAttack.MmaKick);
        }

        public bool TryPlayPunch()
        {
            return TryPlayAttack(HenryCombatAttack.Punch);
        }

        public bool TryPlayRoundhouseKick()
        {
            return TryPlayAttack(HenryCombatAttack.RoundhouseKick);
        }

        public bool TryPlayAttack(HenryCombatAttack attack)
        {
            if (!EnsureRuntimeSetup() ||
                !combatReady ||
                attacking ||
                animationPlayer == null ||
                combatHealth == null ||
                combatHealth.IsDead)
            {
                return false;
            }

            if (!animationPlayer.TryPlayCombatAttack(
                    attack,
                    out AnimationState state) ||
                state == null)
            {
                return false;
            }

            attackSequence++;
            activeAttack = attack;
            attacking = true;
            hitWindowOpened = false;
            hitWindowConsumed = false;
            loggedHitCount = 0;
            meleeDamageDealer?.EndHitWindow();
            return true;
        }

        public void CancelAttack(bool returnToIdle)
        {
            if (!attacking && !hitWindowOpened)
            {
                return;
            }

            attackSequence++;
            meleeDamageDealer?.EndHitWindow();
            hitWindowOpened = false;
            hitWindowConsumed = true;
            attacking = false;
            if (returnToIdle &&
                animationPlayer != null &&
                !animationPlayer.IsRunPlaying)
            {
                animationPlayer.PlayIdle();
            }
        }

        private void FinishAttack()
        {
            attackSequence++;
            meleeDamageDealer?.EndHitWindow();
            hitWindowOpened = false;
            hitWindowConsumed = true;
            attacking = false;

            if (combatReady &&
                animationPlayer != null &&
                !animationPlayer.IsRunPlaying &&
                !animationPlayer.IsIdlePlaying)
            {
                animationPlayer.PlayIdle();
            }
        }

        private bool EnsureRuntimeSetup()
        {
            if (animationPlayer == null)
            {
                animationPlayer = GetComponent<HenryRunAnimationPlayer>();
                if (animationPlayer == null)
                {
                    animationPlayer =
                        gameObject.AddComponent<HenryRunAnimationPlayer>();
                }
            }

            bool animationReady = animationPlayer.ConfigureCombatClips();

            if (meleeHitboxRig == null)
            {
                meleeHitboxRig = GetComponent<MeleeHitboxRig>();
                if (meleeHitboxRig == null)
                {
                    meleeHitboxRig =
                        gameObject.AddComponent<MeleeHitboxRig>();
                }
            }

            bool hitboxReady = meleeHitboxRig.ConfigureForHenry() &&
                meleeHitboxRig.TryGetPose(
                    MeleeHitboxLimb.RightHand,
                    out _) &&
                meleeHitboxRig.TryGetPose(
                    MeleeHitboxLimb.RightFoot,
                    out _);

            int playerLayer = LayerMask.NameToLayer("Player");
            LayerMask playerMask = playerLayer >= 0
                ? 1 << playerLayer
                : 0;

            if (meleeDamageDealer == null)
            {
                meleeDamageDealer = GetComponent<MeleeDamageDealer>();
                if (meleeDamageDealer == null)
                {
                    meleeDamageDealer =
                        gameObject.AddComponent<MeleeDamageDealer>();
                }
            }

            meleeDamageDealer.Configure(
                meleeHitboxRig,
                playerMask,
                QueryTriggerInteraction.Collide);

            if (combatHealth == null)
            {
                combatHealth = GetComponent<CombatHealth>();
                if (combatHealth == null)
                {
                    combatHealth = gameObject.AddComponent<CombatHealth>();
                }
            }

            combatHealth.Died -= HandleHenryDied;
            combatHealth.Died += HandleHenryDied;
            bool hurtboxReady = EnsureHenryHurtbox();

            setupValid = animationReady &&
                hitboxReady &&
                playerLayer >= 0 &&
                hurtboxReady;
            if (!setupValid && !setupErrorLogged)
            {
                setupErrorLogged = true;
                Debug.LogError(
                    "[HenryCombat] Combat setup is incomplete. Required: " +
                    "Punch and both kick clips, right-hand/right-foot bones, " +
                    "Player/Enemy layers, damage dealer, health, and hurtbox.",
                    this);
            }

            return setupValid;
        }

        private bool EnsureHenryHurtbox()
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                Debug.LogError(
                    "[HenryCombat] Project is missing the Enemy layer.",
                    this);
                return false;
            }

            Transform hurtboxTransform = FindDirectChild(
                transform,
                HurtboxObjectName);
            if (hurtboxTransform == null)
            {
                GameObject hurtboxObject =
                    new GameObject(HurtboxObjectName);
                hurtboxTransform = hurtboxObject.transform;
                hurtboxTransform.SetParent(transform, false);
            }

            hurtboxTransform.gameObject.layer = enemyLayer;
            hurtbox = hurtboxTransform.GetComponent<BoxCollider>();
            bool newlyCreated = hurtbox == null;
            if (newlyCreated)
            {
                hurtbox = hurtboxTransform.gameObject
                    .AddComponent<BoxCollider>();
            }

            if (newlyCreated)
            {
                BoxCollider bodyCollider = GetComponent<BoxCollider>();
                if (bodyCollider != null)
                {
                    hurtbox.center = bodyCollider.center;
                    hurtbox.size = bodyCollider.size;
                }
                else
                {
                    hurtbox.center =
                        new Vector3(0.018f, 0.790f, -0.016f);
                    hurtbox.size =
                        new Vector3(0.623f, 1.603f, 0.352f);
                }
            }

            hurtbox.isTrigger = true;
            hurtbox.enabled = combatReady;

            hurtboxMarker = hurtboxTransform.GetComponent<CombatHurtbox>();
            if (hurtboxMarker == null)
            {
                hurtboxMarker = hurtboxTransform.gameObject
                    .AddComponent<CombatHurtbox>();
            }

            hurtboxMarker.Configure(hurtbox, combatHealth);
            return hurtbox != null && combatHealth != null;
        }

        private void HandleHenryDied()
        {
            // The encounter director starts Defeated on the next frame so it
            // can resolve a simultaneous KO deterministically. Do not briefly
            // force Idle while that outcome is pending.
            ExitCombatMode(false);
        }

        private void ResolveActiveAttack(
            out float damage,
            out float hitWindowStart,
            out float hitWindowEnd,
            out MeleeHitboxLimb hitLimb)
        {
            damage = attackDamage;
            if (activeAttack == HenryCombatAttack.Punch)
            {
                hitWindowStart = punchHitWindowStart;
                hitWindowEnd = punchHitWindowEnd;
                hitLimb = MeleeHitboxLimb.RightHand;
                return;
            }

            if (activeAttack == HenryCombatAttack.RoundhouseKick)
            {
                hitWindowStart = roundhouseHitWindowStart;
                hitWindowEnd = roundhouseHitWindowEnd;
                hitLimb = MeleeHitboxLimb.RightFoot;
                return;
            }

            hitWindowStart = mmaKickHitWindowStart;
            hitWindowEnd = mmaKickHitWindowEnd;
            hitLimb = MeleeHitboxLimb.RightFoot;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(
                        child.name,
                        childName,
                        StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(
            Transform parent,
            string childName)
        {
            if (parent == null)
            {
                return null;
            }

            if (string.Equals(
                    parent.name,
                    childName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform match = FindChildRecursive(
                    parent.GetChild(i),
                    childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
