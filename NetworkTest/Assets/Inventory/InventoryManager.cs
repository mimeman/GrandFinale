using UnityEngine;
using System.Collections.Generic;
using System;
using Cinemachine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private int slotCapacity = 8;
    public List<RelicData> inventorySlots;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;

    [Header("UI Reference")]
    // [SerializeField] private GameObject inventoryUI; // L18: 단일 UI 필드 제거
    [SerializeField] private GameObject smallInventoryUI; // L19: 작은 인벤토리 UI (I 키)
    [SerializeField] private GameObject fullInventoryUI;  // L20: 전체 인벤토리 UI (O 키)

    [Header("Loot Settings")]
    [SerializeField] private GameObject genericLootPrefab;

    [Header("Player Control References")]
    [SerializeField] private PlayerInputs playerInput;    // PlayerInputs.cs
    [SerializeField] private CharacterMove characterMove;  // CharacterMove.cs (이동 제어) - NOTE: 제어 로직은 InputHandler로 이동됨
    [SerializeField] private CameraController cameraController; // 카메라 회전 제어 - NOTE: 제어 로직은 InputHandler로 이동됨
    [SerializeField] private InputHandler inputHandler;  // L27: InputHandler 참조 유지
    [SerializeField] private WeaponController weaponController; // 무기 발사 제어 - NOTE: 제어 로직은 InputHandler로 이동됨

    // 현재 인벤토리/UI가 열려있는지 확인
    public bool IsUIOpen => (smallInventoryUI != null && smallInventoryUI.activeSelf) ||
                            (fullInventoryUI != null && fullInventoryUI.activeSelf);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. RelicData 리스트로 초기화
        inventorySlots = new List<RelicData>();
        for (int i = 0; i < slotCapacity; i++)
        {
            inventorySlots.Add(null);
        }
    }

    void Update()
    {
        if (inputHandler == null) return;

        // L61: I 키 입력 감지 (작은 인벤토리)
        if (inputHandler.GetInventoryToggle())
        {
            ToggleSmallInventory();
        }

        // L66: O 키 입력 감지 (전체 인벤토리 - InputHandler에 GetFullCharacterToggle()이 있다고 가정)
        if (inputHandler.GetFullCharacterToggle())
        {
            ToggleFullInventory();
        }

        // L71: ESC 키 입력 감지 (모든 인벤토리 닫기)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllInventories();
        }
    }

    // L77: 기존 ToggleInventory를 CloseAllInventories로 대체하고, ToggleSmall/FullInventory를 사용합니다.

    /// <summary>
    /// I 키 입력 처리: 작은 인벤토리 창(슬롯만)을 토글합니다.
    /// </summary>
    public void ToggleSmallInventory()
    {
        if (smallInventoryUI == null) return;

        // 1. 전체 창이 켜져 있으면 끄기
        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            fullInventoryUI.SetActive(false);
        }

        // 2. 작은 창 토글
        bool shouldBeActive = !smallInventoryUI.activeSelf;
        smallInventoryUI.SetActive(shouldBeActive);

        // 3. 커서 및 입력 상태 설정
        SetPlayerInputState(shouldBeActive);
    }

    /// <summary>
    /// O 키 입력 처리: 전체 인벤토리 창(슬롯 + 장비/스탯)을 토글합니다.
    /// </summary>
    public void ToggleFullInventory()
    {
        if (fullInventoryUI == null) return;

        // 1. 작은 창이 켜져 있으면 끄기
        if (smallInventoryUI != null && smallInventoryUI.activeSelf)
        {
            smallInventoryUI.SetActive(false);
        }

        // 2. 전체 창 토글
        bool shouldBeActive = !fullInventoryUI.activeSelf;
        fullInventoryUI.SetActive(shouldBeActive);

        // 3. 커서 및 입력 상태 설정
        SetPlayerInputState(shouldBeActive);
    }

    /// <summary>
    /// ESC 키 입력 처리: 모든 인벤토리 창을 닫고 커서를 잠급니다.
    /// </summary>
    public void CloseAllInventories()
    {
        if (!IsUIOpen) return;

        if (smallInventoryUI != null) smallInventoryUI.SetActive(false);
        if (fullInventoryUI != null) fullInventoryUI.SetActive(false);

        // 커서 및 입력 상태 설정 (비활성화 상태)
        SetPlayerInputState(false);
    }


    /// <summary>
    /// UI 활성화 여부에 따라 커서 상태와 입력 이벤트를 설정합니다.
    /// </summary>
    private void SetPlayerInputState(bool uiIsActive)
    {
        if (uiIsActive)
        {
            Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 커서 잠금 (게임 입력 상태로 복귀)
            Cursor.visible = false;
        }

        // L159: InputHandler에서 입력 차단을 처리하도록 이벤트 호출
        OnInventoryToggle?.Invoke(uiIsActive);

        // NOTE: 이전 ToggleInventory의 스크립트 활성화/비활성화 로직은 
        // InputHandler의 OnInventoryToggle을 구독하는 다른 스크립트로 이동하는 것을 권장합니다.
        // InputHandler는 마우스 입력을 제어하며, 이동 스크립트(CharacterMove, CameraController 등)는 
        // InventoryManager의 상태 변화에 따라 직접 활성화/비활성화 할 수도 있습니다. 
        // 여기서는 InputHandler에 위임하는 것이 일관성을 높입니다.
    }


    /// <summary>
    /// RelicData 아이템을 인벤토리에 추가합니다.
    /// </summary>
    public bool AddItem(RelicData itemToAdd) // 3. ItemData -> RelicData
    {
        int emptySlotIndex = FindNextEmptySlot();

        if (emptySlotIndex == -1)
        {
            Debug.Log("인벤토리가 꽉 찼습니다.");
            return false;
        }

        inventorySlots[emptySlotIndex] = itemToAdd;
        OnInventoryChanged?.Invoke();
        Debug.Log(itemToAdd.itemName + "을(를) " + (emptySlotIndex + 1) + "번 슬롯에 추가했습니다.");
        return true;
    }

    public int FindNextEmptySlot()
    {
        for (int i = 0; i < slotCapacity; i++)
        {
            if (inventorySlots[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    // 4. RelicData를 교체하도록 수정
    public void SwapItems(int indexA, int indexB)
    {
        RelicData temp = inventorySlots[indexA];
        inventorySlots[indexA] = inventorySlots[indexB];
        inventorySlots[indexB] = temp;
        OnInventoryChanged?.Invoke();
    }

    // L215: RelicData를 제거하도록 수정 + bool 반환 추가 (CS0029 에러 방지)
    public bool RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity)
        {
            Debug.LogError($"[InventoryManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        inventorySlots[slotIndex] = null;
        OnInventoryChanged?.Invoke();
        return true; // 제거 성공
    }


    public void DropItem(int slotIndex)
    {
        RelicData itemToDrop = inventorySlots[slotIndex];
        if (itemToDrop == null) return;

        // 1. (중요!) RelicData에 dropPrefab이 설정되어 있는지 확인
        if (genericLootPrefab != null)
        {
            // 2. 플레이어 위치 찾기 (임시로 "Player" 태그 사용)
            GameObject player = GameObject.FindWithTag("Player");
            Vector3 dropPosition;

            if (player != null)
            {
                // 플레이어 1미터 앞에 생성
                dropPosition = player.transform.position + (player.transform.forward * 1f);
            }
            else
            {
                // 플레이어를 못 찾으면 카메라 1미터 앞에 생성 (안전 장치)
                dropPosition = Camera.main.transform.position + (Camera.main.transform.forward * 1f);
            }

            GameObject orbInstance = Instantiate(genericLootPrefab, dropPosition, Quaternion.identity);

            ItemPickup pickupScript = orbInstance.GetComponent<ItemPickup>();
            if (pickupScript != null)
            {
                pickupScript.itemData = itemToDrop; // [중요] 이 구체가 어떤 아이템인지 설정
            }
            LootOrbVisuals visualScript = orbInstance.GetComponent<LootOrbVisuals>();
            if (visualScript != null)
            {
                visualScript.Initialize(itemToDrop.grade);
            }

            // 4. 인벤토리에서 아이템 제거
            RemoveItem(slotIndex); // (이 함수는 OnInventoryChanged를 호출함)
            Debug.Log(itemToDrop.itemName + "을(를) 바닥에 버렸습니다.");
        }
        else
        {
            Debug.LogWarning("InventoryManager에 genericLootPrefab이 설정되지 않아 아이템을 버릴 수 없습니다.");
        }
    }

    /// <summary>
    /// 인벤토리 변경 이벤트를 외부에 알립니다. (이벤트 직접 호출 방지용)
    /// </summary>
    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}