using System.Collections;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    [Header("Components")]
    public CharacterController characterController;
    public BodyTurnHandler bodyTurnHandler;
    public Animator animator;
    public Transform directionOrienter;
    private PlayerInputs playerInputs;
    private PlayerStats playerStats;
    public PlayerInputs Inputs { get { return playerInputs; } }


    [Header("Colider values")]
    public float crouchColliderHeight = 1f;
    public float normalColliderHeight { get; private set; }
    public float grounCheckDistance;

    private bool _isGrounded;
    public bool isGrounded
    {
        get => _isGrounded;
        set
        {
            if (value == _isGrounded) return;

            _isGrounded = value;

            if (!_isGrounded && currentState != inAirState)
                SetState(inAirState);

            animator.SetBool("isGrounded", value);

            OnGroundedValueChange.Invoke(value);
        }
    }
    public LayerMask groundCheckMask;
    public delegate void isGroundedChange(bool changed);
    public event isGroundedChange OnGroundedValueChange;
    public float edgeFallMoveForce = 1f;
    public float noSlipDistance = .1f;


    [Header("Move values")]

    public float gravity = -9.81f;

    public float crouchSpeed = 1;

    public float jumpHeight = 1f;
    public float walkSpeed => playerStats.CurrentWalkSpeed;
    public float runSpeed => playerStats.CurrentRunSpeed;
    public float sprintSpeed => playerStats.CurrentSprintSpeed;

    [Header("Velocity values")]
    public Vector3 moveVelocity;
    public Vector3 velocity;
    public Vector3 rollVelocity;
    public Vector3 edgeSlipVelocity;

    public StateMachineBase previousState;
    public StateMachineBase currentState;
    public MoveState moveState { get; private set; }
    public CrouchState crouchState { get; private set; }
    public RollState rollState { get; private set; }
    public JumpState jumpState { get; private set; }
    public InAirState inAirState { get; private set; }

    // animator ids
    public int horizontalInputID { get; private set; }
    public int verticalInputID { get; private set; }
    public int walkID { get; private set; }
    public int crouchID { get; private set; }
    public int isGroundID { get; private set; }
    public int sprintID { get; private set; }
    public int rollID { get; private set; }

    IEnumerator colliderSizeChangeCor;

    private void Awake()
    {
        AssighAnimatorIDs();
        colliderSizeChangeCor = ColliderSizeChangeSmooth(false);
        characterController = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
        normalColliderHeight = characterController.height;
        GameManager.Instance.TryGetComponent<PlayerInputs>(out playerInputs);


        moveState = new MoveState(this);
        crouchState = new CrouchState(this);
        rollState = new RollState(this);
        jumpState = new JumpState(this);
        inAirState = new InAirState(this);
    }

    private void Start()
    {
        currentState = moveState;
        SetState(inAirState);
    }

    public void SetState(StateMachineBase state)
    {
        if (currentState != null)
            currentState.OnStateExit();

        previousState = currentState;
        currentState = state;


        bool coliderReduce = currentState == crouchState | currentState == rollState;
        if (colliderSizeChangeCor != null) StopCoroutine(colliderSizeChangeCor);
        colliderSizeChangeCor = ColliderSizeChangeSmooth(coliderReduce);
        StartCoroutine(colliderSizeChangeCor);

        if (currentState != null)
            currentState.OnStateEnter();
    }

    // CharacterMove.cs (Update 함수 수정)

    private void Update()
    {
        if (currentState == null)
            return;

        // InventoryManager 인스턴스 준비 상태 확인
        bool isInvReady = InventoryManager.Instance != null;

        // UI 포커스 상태 확인 (InventoryManager가 준비되었을 때만 체크)
        bool isInputBlocked = isInvReady && InventoryManager.Instance.IsUIActiveAndFocused;

        // 1. GroundCheck는 항상 실행 (물리 상태 유지)
        GroundCheck();

        if (isInputBlocked)
        {
            // 인벤토리에 포커스가 있는 상태 (입력 차단)

            // 공중에 있을 경우, InAirState로 강제 전환하여 중력 적용을 보장합니다.
            if (!isGrounded && currentState != inAirState)
            {
                // SetState(inAirState)를 통해 중력 상태로 진입
                SetState(inAirState);
            }

            // currentState.Tick()을 호출하여 중력, 애니메이션 상태 유지 등 
            // 필수 물리 업데이트를 진행합니다. (Tick 내부에서 입력이 0이므로 이동은 막힘)
            currentState.Tick();
        }
        else // 인벤토리 포커스가 해제된 상태 (정상적인 인게임 입력 복구)
        {
            // 인게임 플레이가 정상적으로 진행될 때의 Tick()을 호출합니다.
            // Tick() 내부에서 PlayerInputs.GetAxis() 등의 실제 입력값을 사용합니다.
            currentState.Tick();
        }
    }

    void GroundCheck()
    {
        RaycastHit hitInfo;

        if (velocity.y <= 0 && Physics.SphereCast(transform.position + characterController.center, characterController.radius + characterController.skinWidth, Vector3.down, out hitInfo, grounCheckDistance, groundCheckMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            Vector3 relativeHitPoint = hitInfo.point - (transform.position + Vector3.right * characterController.center.x + Vector3.forward * characterController.center.z);

            Debug.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * 0.3f, Color.red);

            if (characterController.velocity.y < 0 && relativeHitPoint.magnitude > noSlipDistance && !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, groundCheckMask))
            {
                Vector3 edgeFallMovement = transform.position - hitInfo.point;
                edgeFallMovement.y = 0;
                edgeSlipVelocity += (edgeFallMovement * Time.deltaTime * edgeFallMoveForce);
            }
            else
            {
                edgeSlipVelocity = Vector3.zero;
            }
        }
        else
        {
            isGrounded = false;
            edgeSlipVelocity = Vector3.zero;
        }

    }

    IEnumerator ColliderSizeChangeSmooth(bool reduce)
    {
        var startSize = characterController.height;
        var finalSize = reduce ? crouchColliderHeight : normalColliderHeight;
        var startCenter = characterController.center.y;
        var finalCener = finalSize / 2f;
        float t = 0;

        while (t < 0.3f)
        {
            characterController.height = Mathf.Lerp(startSize, finalSize, t / 0.3f);
            characterController.center = new Vector3(characterController.center.x, Mathf.Lerp(startCenter, finalCener, t / 0.3f), characterController.center.z);
            t += Time.deltaTime;
            yield return null;
        }
        characterController.height = finalSize;
        yield break;
    }

    private void AssighAnimatorIDs()
    {
        horizontalInputID = Animator.StringToHash("x");
        verticalInputID = Animator.StringToHash("y");
        isGroundID = Animator.StringToHash("isGround");
        sprintID = Animator.StringToHash("sprint");
        rollID = Animator.StringToHash("roll");
        walkID = Animator.StringToHash("walk");
        crouchID = Animator.StringToHash("crouch");
    }

}
