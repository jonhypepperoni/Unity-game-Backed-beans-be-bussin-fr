using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 6f;
    public float rotationSpeed = 720f;
    public float jumpSpeed = 8f;
    public float jumpButtonGracePeriod = 0.2f;

    [Header("Dive")]
    public float diveSpeed = 18f;
    public float diveDuration = 0.6f;
    public float diveCooldown = 1f;

    [Header("Visual")]
    public Transform visual;
    public float anguloInclinacao = 45f;
    public float suavidadeInclinacao = 10f;

    private CharacterController controller;
    private PlayerAttack playerAttack;

    private float ySpeed;
    private float lastGroundedTime = -999f;
    private float jumpPressedTime = -999f;

    private float cooldownTimer;
    private float diveTimer;
    private Vector3 diveDir;
    private bool beanHidden;

    private enum State
    {
        Air,
        Diving,
        Puddle
    }

    private State state;

    [Header("Puddle Trail")]
    private Vector3 lastPuddlePos;
    public float puddleSpacing = 0.8f;

    private float currentPuddleSpeed;
    private Vector3 puddleBoostDir;
    private Quaternion uprightRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        playerAttack = GetComponent<PlayerAttack>();

        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }

        if (visual != null)
        {
            uprightRotation = visual.localRotation;
        }

        state = State.Air;
        lastPuddlePos = transform.position;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        bool grounded = controller.isGrounded;

        if (state == State.Diving &&
            grounded &&
            ySpeed <= 0f)
        {
            SpawnPuddle();

            if (Input.GetKey(KeyCode.F))
            {
                state = State.Puddle;

                puddleBoostDir = transform.forward;
                puddleBoostDir.y = 0f;
                puddleBoostDir.Normalize();

                currentPuddleSpeed = diveSpeed;

                if (visual != null)
                    visual.gameObject.SetActive(false);

                beanHidden = true;
                lastPuddlePos = transform.position;
            }
            else
            {
                state = State.Air;
            }
        }

        if (state == State.Puddle)
        {
            if (visual != null)
                visual.position = transform.position;

            float dist =
                Vector3.Distance(
                    transform.position,
                    lastPuddlePos
                );

            if (dist >= puddleSpacing &&
                grounded)
            {
                SpawnPuddle();
                lastPuddlePos = transform.position;
            }

            if (currentPuddleSpeed > speed)
            {
                currentPuddleSpeed =
                    Mathf.MoveTowards(
                        currentPuddleSpeed,
                        speed,
                        25f * Time.deltaTime
                    );
            }
        }

        if (state == State.Puddle &&
            !Input.GetKey(KeyCode.F))
        {
            state = State.Air;

            RespawnBean();

            beanHidden = false;
            jumpPressedTime = Time.time;

            if (visual != null)
            {
                visual.gameObject.SetActive(true);
                visual.localRotation = uprightRotation;
            }

            ySpeed = jumpSpeed * 0.5f;
        }

        if (state == State.Diving)
        {
            Dive();
            Tilt(true);
            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (playerAttack != null &&
            playerAttack.IsMovementLocked)
        {
            h = 0f;
            v = 0f;
        }

        Vector3 f = Camera.main.transform.forward;
        Vector3 r = Camera.main.transform.right;

        f.y = 0f;
        r.y = 0f;

        f.Normalize();
        r.Normalize();

        Vector3 move = f * v + r * h;

        float moveSpeed;

        if (state == State.Puddle)
        {
            moveSpeed =
                Mathf.Clamp01(move.magnitude) *
                currentPuddleSpeed;
        }
        else
        {
            moveSpeed =
                Mathf.Clamp01(move.magnitude) *
                speed;
        }

        if (move != Vector3.zero)
            move.Normalize();

        ySpeed += Physics.gravity.y * Time.deltaTime;

        if (grounded)
            lastGroundedTime = Time.time;

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressedTime = Time.time;

            if (playerAttack != null)
                playerAttack.CancelAttackForJump();

            if (beanHidden)
                RespawnBean();
        }

        if (Time.time - lastGroundedTime <=
            jumpButtonGracePeriod)
        {
            ySpeed = -0.5f;

            if (Time.time - jumpPressedTime <=
                jumpButtonGracePeriod)
            {
                ySpeed = jumpSpeed;

                jumpPressedTime = -999f;
                lastGroundedTime = -999f;
            }
        }

        Vector3 velocity;

        if (state == State.Puddle)
        {
            if (currentPuddleSpeed > speed)
            {
                Vector3 targetDir =
                    move != Vector3.zero
                    ? move
                    : puddleBoostDir;

                Vector3 combinedDir =
                    Vector3.Lerp(
                        puddleBoostDir,
                        targetDir,
                        0.4f
                    ).normalized;

                velocity =
                    combinedDir *
                    currentPuddleSpeed;
            }
            else
            {
                velocity =
                    move *
                    moveSpeed;
            }
        }
        else
        {
            velocity =
                move *
                moveSpeed;
        }

        velocity.y = ySpeed;

        controller.Move(
            velocity *
            Time.deltaTime
        );

        if (state != State.Diving &&
            move != Vector3.zero)
        {
            Quaternion rot =
                Quaternion.LookRotation(
                    move,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    rot,
                    rotationSpeed *
                    Time.deltaTime
                );
        }

        Tilt(false);

        if (Input.GetKeyDown(KeyCode.F) &&
            !grounded &&
            cooldownTimer <= 0f &&
            state == State.Air)
        {
            StartDive();
        }
    }

    void StartDive()
    {
        state = State.Diving;

        diveTimer = diveDuration;
        cooldownTimer = diveCooldown;

        diveDir = transform.forward;
        diveDir.y = 0f;
        diveDir.Normalize();

        ySpeed = jumpSpeed * 0.4f;
    }

    void Dive()
    {
        diveTimer -= Time.deltaTime;

        ySpeed +=
            Physics.gravity.y *
            1.5f *
            Time.deltaTime;

        Vector3 move =
            diveDir *
            diveSpeed;

        move.y = ySpeed;

        controller.Move(
            move *
            Time.deltaTime
        );
    }

    void SpawnPuddle()
    {
        PocaImpacto p =
            GetComponent<PocaImpacto>();

        if (p != null)
            p.CriarPoca();
    }

    void Tilt(bool divingState)
    {
        if (visual == null)
            return;

        Quaternion targetRotation;

        if (divingState)
        {
            targetRotation =
                uprightRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    anguloInclinacao
                );
        }
        else
        {
            targetRotation =
                uprightRotation;
        }

        visual.localRotation =
            Quaternion.Lerp(
                visual.localRotation,
                targetRotation,
                suavidadeInclinacao *
                Time.deltaTime
            );
    }

    void RespawnBean()
    {
        beanHidden = false;

        if (visual != null)
        {
            visual.gameObject.SetActive(true);
            visual.localRotation =
                uprightRotation;
        }

        ySpeed =
            jumpSpeed *
            0.5f;
    }
}