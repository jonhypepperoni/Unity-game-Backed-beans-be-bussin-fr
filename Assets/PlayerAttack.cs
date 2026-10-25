using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("COMBO SETTINGS")]
    public float comboResetTime = 2.0f;
    public float minAttackInterval = 0.7f;

    [System.Serializable]
    public class AttackSettings
    {
        public float damage = 10f;
        public float startupTime = 0.06f;
        public float activeTime = 0.1f;
        public float recoveryTime = 0.34f;
        public float enemyPush = 0.5f;
    }

    [Header("ATTACK 1")]
    public AttackSettings attack1 = new AttackSettings
    {
        damage = 10f,
        enemyPush = 0.5f
    };

    [Header("ATTACK 2")]
    public AttackSettings attack2 = new AttackSettings
    {
        damage = 10f,
        enemyPush = 0.5f
    };

    [Header("ATTACK 3")]
    public AttackSettings attack3 = new AttackSettings
    {
        damage = 12f,
        startupTime = 0.08f,
        activeTime = 0.12f,
        recoveryTime = 0.4f,
        enemyPush = 0.6f
    };

    [Header("ATTACK 4")]
    public AttackSettings attack4 = new AttackSettings
    {
        damage = 14f,
        startupTime = 0.05f,
        activeTime = 0.1f,
        recoveryTime = 0.35f,
        enemyPush = 0.7f
    };

    [Header("ATTACK 5 (FINISHER)")]
    public AttackSettings attack5 = new AttackSettings
    {
        damage = 25f,
        startupTime = 0.12f,
        activeTime = 0.16f,
        recoveryTime = 0.5f,
        enemyPush = 1.2f
    };

    [Header("HIT DETECTION")]
    public float attackRange = 1.2f;
    public float hitboxRadius = 0.6f;
    public float hitboxForwardOffset = 0.8f;
    public LayerMask enemyLayer;

    [Header("MOVEMENT ON HIT")]
    public float hitMoveDuration = 0.15f;

    [Header("VISUAL SETTINGS")]
    public Transform visual;
    public float punchDistance = 0.35f;
    public float punchRotationAmount = 35f;
    public float rotationSmoothing = 20f;
    public float positionSmoothing = 22f;

    [Header("MOVEMENT LOCK")]
    public float movementLockDuration = 1.0f;

    private enum ComboPhase
    {
        Idle,
        Startup,
        Active,
        Recovery
    }

    private CharacterController controller;
    private PlayerMovement playerMovement;

    private Quaternion originalVisualRotation;
    private Vector3 originalVisualLocalPosition;
    private bool hasVisual;

    private int comboIndex = -1;
    private ComboPhase phase = ComboPhase.Idle;
    private float phaseTimer;
    private float idleTimer;
    private bool bufferedClick;
    private float lastAttackStartTime = -999f;

    private readonly HashSet<Health> hitThisAttack =
        new HashSet<Health>();

    private Vector3 pendingPlayerMoveDir;
    private float pendingPlayerMoveRemaining;
    private float pendingPlayerMoveSpeed;

    private float movementLockTimer;

    public bool IsMovementLocked => movementLockTimer > 0f;

    private static readonly float[] PunchDistanceScale =
        { 1f, 1f, 1.3f, 1.15f, 2.4f };

    private static readonly float[] PunchPitchScale =
        { 0.8f, 0.8f, 1.4f, 1f, 1.8f };

    private static readonly float[] PunchRollScale =
        { 0.35f, 0.35f, 0f, 0.45f, 0f };

    private static readonly float[] PunchRollSign =
        { 1f, -1f, 0f, 1f, 0f };

    private AttackSettings[] attacks;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        if (visual == null && playerMovement != null)
        {
            visual = playerMovement.visual;
        }

        if (visual != null)
        {
            originalVisualRotation = visual.localRotation;
            originalVisualLocalPosition = visual.localPosition;
            hasVisual = true;
        }

        attacks = new[]
        {
            attack1,
            attack2,
            attack3,
            attack4,
            attack5
        };
    }

    void Update()
    {
        if (movementLockTimer > 0f)
        {
            movementLockTimer -= Time.deltaTime;

            if (movementLockTimer < 0f)
                movementLockTimer = 0f;
        }

        HandleInput();
        UpdateAttackState();
        UpdateVisual();
        ApplyPendingPlayerMovement();
    }

    private void HandleInput()
    {
        bool clicked =
            Input.GetButtonDown("Fire1") ||
            Input.GetMouseButtonDown(0);

        if (clicked)
        {
            bufferedClick = true;
        }
    }

    private bool CanStartAttackNow()
    {
        return Time.time - lastAttackStartTime >= minAttackInterval;
    }

    private void StartAttack(int index)
    {
        comboIndex = index;
        phase = ComboPhase.Startup;
        phaseTimer = 0f;
        bufferedClick = false;
        lastAttackStartTime = Time.time;
        hitThisAttack.Clear();

        movementLockTimer = movementLockDuration;
    }

    public void CancelAttackForJump()
    {
        if (phase == ComboPhase.Idle)
            return;

        phase = ComboPhase.Idle;
        comboIndex = -1;
        bufferedClick = false;
        idleTimer = 0f;
        hitThisAttack.Clear();

        movementLockTimer = 0f;
    }

    private void UpdateAttackState()
    {
        if (phase == ComboPhase.Idle)
        {
            idleTimer += Time.deltaTime;

            if (bufferedClick && CanStartAttackNow())
            {
                int nextIndex =
                    (comboIndex < 0 || idleTimer > comboResetTime)
                    ? 0
                    : (comboIndex + 1) % attacks.Length;

                StartAttack(nextIndex);
            }

            return;
        }

        AttackSettings current = attacks[comboIndex];
        phaseTimer += Time.deltaTime;

        switch (phase)
        {
            case ComboPhase.Startup:

                if (phaseTimer >= current.startupTime)
                {
                    phase = ComboPhase.Active;
                    phaseTimer = 0f;
                }

                break;

            case ComboPhase.Active:

                DoHitDetection(current);

                if (phaseTimer >= current.activeTime)
                {
                    phase = ComboPhase.Recovery;
                    phaseTimer = 0f;
                }

                break;

            case ComboPhase.Recovery:

                if (bufferedClick && CanStartAttackNow())
                {
                    int next = (comboIndex + 1) % attacks.Length;

                    StartAttack(next);
                    return;
                }

                if (phaseTimer >= current.recoveryTime)
                {
                    phase = ComboPhase.Idle;
                    idleTimer = 0f;
                }

                break;
        }
    }

    private void DoHitDetection(AttackSettings current)
    {
        Vector3 center = GetHitboxCenter();

        Collider[] hits = Physics.OverlapSphere(
            center,
            hitboxRadius,
            enemyLayer
        );

        foreach (Collider col in hits)
        {
            Health health = col.GetComponent<Health>();

            if (health == null)
            {
                health = col.GetComponentInParent<Health>();
            }

            if (health == null || hitThisAttack.Contains(health))
                continue;

            hitThisAttack.Add(health);
            ApplyHit(health, current);
        }
    }

    private void ApplyHit(Health target, AttackSettings current)
    {
        target.TakeDamage(current.damage, gameObject);

        Debug.Log(
            $"Attack {comboIndex + 1} hit " +
            $"{target.gameObject.name} for {current.damage} damage"
        );

        Vector3 pushDir = transform.forward;
        pushDir.y = 0f;
        pushDir.Normalize();

        target.ApplyKnockback(
            pushDir,
            current.enemyPush,
            hitMoveDuration
        );

        bool isFinisher =
            comboIndex == attacks.Length - 1;

        if (!isFinisher)
        {
            QueuePlayerFollow(
                pushDir,
                current.enemyPush
            );
        }
    }

    private void QueuePlayerFollow(Vector3 dir, float distance)
    {
        pendingPlayerMoveDir = dir;
        pendingPlayerMoveRemaining = distance;

        pendingPlayerMoveSpeed =
            hitMoveDuration > 0f
                ? distance / hitMoveDuration
                : distance;
    }

    private void ApplyPendingPlayerMovement()
    {
        if (pendingPlayerMoveRemaining <= 0f ||
            controller == null)
            return;

        float step = Mathf.Min(
            pendingPlayerMoveSpeed * Time.deltaTime,
            pendingPlayerMoveRemaining
        );

        controller.Move(
            pendingPlayerMoveDir * step
        );

        pendingPlayerMoveRemaining -= step;
    }

    private Vector3 GetHitboxCenter()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        return transform.position +
               forward * hitboxForwardOffset +
               Vector3.up * 0.8f;
    }

    private void UpdateVisual()
    {
        if (!hasVisual)
            return;

        Quaternion targetRotation =
            originalVisualRotation;

        Vector3 targetLocalPos =
            originalVisualLocalPosition;

        if (phase != ComboPhase.Idle &&
            comboIndex >= 0)
        {
            AttackSettings current =
                attacks[comboIndex];

            float total =
                current.startupTime +
                current.activeTime +
                current.recoveryTime;

            float elapsed = 0f;

            switch (phase)
            {
                case ComboPhase.Startup:
                    elapsed = phaseTimer;
                    break;

                case ComboPhase.Active:
                    elapsed =
                        current.startupTime +
                        phaseTimer;
                    break;

                case ComboPhase.Recovery:
                    elapsed =
                        current.startupTime +
                        current.activeTime +
                        phaseTimer;
                    break;
            }

            float t =
                total > 0f
                    ? Mathf.Clamp01(elapsed / total)
                    : 0f;

            float curve =
                Mathf.Sin(t * Mathf.PI);

            targetLocalPos =
                originalVisualLocalPosition +
                Vector3.forward *
                punchDistance *
                PunchDistanceScale[comboIndex] *
                curve;

            float pitch =
                punchRotationAmount *
                PunchPitchScale[comboIndex] *
                curve;

            float roll =
                punchRotationAmount *
                PunchRollScale[comboIndex] *
                PunchRollSign[comboIndex] *
                curve;

            targetRotation =
                originalVisualRotation *
                Quaternion.Euler(
                    pitch,
                    0f,
                    roll
                );
        }

        visual.localRotation =
            Quaternion.Slerp(
                visual.localRotation,
                targetRotation,
                rotationSmoothing *
                Time.deltaTime
            );

        visual.localPosition =
            Vector3.Lerp(
                visual.localPosition,
                targetLocalPos,
                positionSmoothing *
                Time.deltaTime
            );
    }

    void OnDrawGizmosSelected()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.0001f)
        {
            forward.Normalize();
        }

        Vector3 center =
            transform.position +
            forward * hitboxForwardOffset +
            Vector3.up * 0.8f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            center,
            hitboxRadius
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}