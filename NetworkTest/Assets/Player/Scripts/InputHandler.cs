using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // EventSystems 추가

public class InputHandler : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private WeaponPickup weaponPickUp;
    [SerializeField] private BodySlope_Handler bodySlope_Handler;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private BodyTiltInSprint bodyTiltInSprint;

    private PlayerInputs playerInputs;
    private bool isPause = false;

    // ★ 추가된 변수: UI 입력 제어용
    private bool isPointerOverUI = false;
    private bool isInventoryOpen = false;

    // I/O 키 코드 (InventoryManager에서 사용)
    private const KeyCode InventoryKey_Small = KeyCode.I; // I 키
    private const KeyCode InventoryKey_Full = KeyCode.O;  // O 키

    // --- 마우스 우클릭 상태 변수 ---
    private bool isPressing = false;
    private float pressTime = 0f;
    private bool isLongAimTriggered = false;

    private void Start()
    {
        GameManager.OnPauseStateChanged += OnPause;
        weaponController.activeID = 1;
        weaponController.animator.Play("GunPickUp", 1);
        GameManager.Instance.TryGetComponent<PlayerInputs>(out playerInputs);
    }

    void Update()
    {
        if (isPause)
            return;

        // ★ L41: UI 오버 여부 체크 로직 추가
        if (isInventoryOpen)
        {
            // UI가 열려 있을 때만 마우스가 UI 위에 있는지 검사
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }
        else
        {
            isPointerOverUI = false;
        }

        // 마우스 입력 차단 플래그
        bool blockMouseInput = isInventoryOpen && isPointerOverUI;

        TryShoot(); // 좌클릭(총쏘기) 로직 (내부에 blockMouseInput 체크 포함)

        bodySlope_Handler.setInput(playerInputs.GetBending());
        bodyTiltInSprint.SetMouseXMove(Input.GetAxis("Mouse X"));

        // ... (Slot 변경 로직 유지)

        if (Input.GetKeyDown(KeyCode.F) && weaponPickUp != null)
        {
            weaponPickUp.PickupCheck();
        }

        // ----------------------------------------------------------------------------------
        // ★ L88: 우클릭(조준) 입력 차단 로직 적용
        // ----------------------------------------------------------------------------------
        // 1. 마우스 우클릭 시작 감지
        if (Input.GetMouseButtonDown(1))
        {
            if (blockMouseInput)
            {
                return; // UI 조작 중이므로 우클릭 입력 차단
            }

            isPressing = true;
            isLongAimTriggered = false;
            pressTime = 0f;
        }

        // 2. 마우스 우클릭 누르고 있는 동안 처리 (롱클릭 감지)
        if (Input.GetMouseButton(1) && isPressing)
        {
            // blockMouseInput은 Down에서 체크되므로 Down이 막히면 isPressing이 false라 이 블록에 안 들어옴

            pressTime += Time.deltaTime;

            if (!isLongAimTriggered && pressTime >= 0.2f)
            {
                isLongAimTriggered = true;
                cameraSwitcher.StartTpvAim(); // 롱클릭: TPV 조준 시작
            }
        }

        // 3. 마우스 우클릭에서 손을 뗌 (Up) 처리
        if (Input.GetMouseButtonUp(1) && isPressing)
        {
            // 롱클릭 상태에서 손을 뗐다면 조준 중지
            if (isLongAimTriggered)
            {
                cameraSwitcher.StopAiming();
            }
            // 롱클릭이 발동되기 전(0.3초 미만)에 손을 뗐다면 FPV 조준 토글
            else
            {
                cameraSwitcher.ToggleFpvAim();
            }
            isPressing = false;
            isLongAimTriggered = false; // UP 시점에 모두 초기화
        }

        // ★ L137: UI 열림 상태에서 isPressing 강제 해제 (Down이 막혔는데 isPressing이 true일 경우 대비)
        if (blockMouseInput && isPressing)
        {
            cameraSwitcher.StopAiming(); // 조준 강제 종료
            isPressing = false;
            isLongAimTriggered = false;
        }
        // ----------------------------------------------------------------------------------


        if (Input.GetKeyDown(KeyCode.V))
        {
            cameraSwitcher.ViewChange();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponController.GETCurrentWeapon.Reload();
        }
    }

    void OnPause(bool pause)
    {
        isPause = pause;
    }

    void TryShoot()
    {
        // Prevent shooting while sprinting
        if (bodyTiltInSprint.standState.isSprint) return;

        if (!weaponController.GETCurrentWeapon)
            return;

        // ★ L166: UI 입력 차단 플래그 다시 확인
        bool blockMouseInput = isInventoryOpen && isPointerOverUI;

        bool singleshoot = weaponController.GETCurrentWeapon.SingleShoot;
        if (singleshoot && Input.GetMouseButtonDown(0))
        {
            if (blockMouseInput) return; // UI 조작 중이면 발사 차단
            weaponController.StartShoot();
        }
        else if (!singleshoot && Input.GetMouseButton(0))
        {
            if (blockMouseInput) return; // UI 조작 중이면 발사 차단
            weaponController.StartShoot();
        }
    }

    // ★ L181: InventoryManager 연동 함수 추가
    public void OnInventoryToggle(bool open)
    {
        isInventoryOpen = open;
    }

    // ★ L186: InventoryManager 연동 함수 추가
    public bool GetInventoryToggle()
    {
        return Input.GetKeyDown(InventoryKey_Small);
    }

    // ★ L191: InventoryManager 연동 함수 추가
    public bool GetFullCharacterToggle()
    {
        return Input.GetKeyDown(InventoryKey_Full);
    }
}