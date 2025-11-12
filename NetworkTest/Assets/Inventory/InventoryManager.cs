using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Cinemachine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public InventoryFilterType currentFilter { get; private set; } = InventoryFilterType.All; // 기본값: 전체

    private int slotCapacity = 42;
    public List<RelicData> inventorySlots;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;
    public bool IsFocused { get; private set; } = false;
    public bool IsUIActiveAndFocused => IsUIOpen && IsFocused;

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
        // ★★★ 2. 인게임 클릭 시 포커스 상실 로직 (닫기 아님) ★★★
        if (IsFocused && Input.GetMouseButtonDown(0))
        {
            // 마우스 커서가 UI 요소 위에 있는지 확인
            // IsPointerOverGameObject()는 EventSystem이 null이 아닐 때만 호출해야 안전
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (!isOverUI)
            {
                // UI 밖에 클릭했다면 포커스 상실 (UI는 열린 상태 유지)
                SetFocusState(false);
            }
        }

        if (playerInput == null) return;

        // I, O 키를 누르면 포커스 획득/상실 로직으로 연결
        if (playerInput.GetInventoryToggle())
        {
            ToggleSmallInventory();
        }

        if (playerInput.GetFullInventoryToggle())
        {
            ToggleFullInventory();
        }


        if (playerInput.GetEscape())
        {
            CloseAllInventories(); // ESC는 완전히 닫음
        }
    }


    /// <summary>
    /// 탭 키 입력 처리: 작은 인벤토리 창(슬롯만)을 토글합니다.
    /// </summary>
    public void ToggleSmallInventory()
    {
        if (smallInventoryUI == null) return;

        // 전체 창이 켜져 있으면 끄기
        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            fullInventoryUI.SetActive(false);
            // 전체 창이 닫히면 포커스 상태도 초기화
            if (IsFocused) SetFocusState(false);
        }

        // UI가 현재 열려있는지 확인
        bool currentActive = smallInventoryUI.activeSelf;

        if (currentActive && IsFocused)
        {
            // 열려있고 포커스가 있었다면: 포커스 상실 (UI는 그대로 둠)
            smallInventoryUI.SetActive(false);
            SetFocusState(false);
        }
        else if (!currentActive)
        {
            // 닫혀있었다면: UI를 열고 포커스 획득
            smallInventoryUI.SetActive(true);
            SetFocusState(true);
        }


        else // 열려있지만 포커스가 없었다면: 포커스 다시 획득
        {
            SetFocusState(true);
        }

    }

    /// <summary>
    /// O 키 입력 처리: 전체 인벤토리 창(슬롯 + 장비/스탯)을 토글합니다.
    /// </summary>
    public void ToggleFullInventory()
    {
        if (fullInventoryUI == null) return;
        if (smallInventoryUI != null && smallInventoryUI.activeSelf)
        {
            smallInventoryUI.SetActive(false);
            if (IsFocused) SetFocusState(false);
        }

        bool currentActive = fullInventoryUI.activeSelf;

        if (!currentActive)
        {
            fullInventoryUI.SetActive(true);
            SetFocusState(true);
        }
        else // UI가 현재 열려있는 상태
        {
            SetFocusState(!IsFocused);
        }
    }

    /// <summary>
    /// ESC 키 입력 처리: 모든 인벤토리 창을 닫고 커서를 잠급니다.
    /// </summary>
    public void CloseAllInventories()
    {
        if (!IsUIOpen) return;

        if (smallInventoryUI != null) smallInventoryUI.SetActive(false);
        if (fullInventoryUI != null) fullInventoryUI.SetActive(false);

        SetFocusState(false);
    }


    /// <summary>
    /// UI 활성화 여부에 따라 커서 상태와 입력 이벤트를 설정합니다.
    /// </summary>
    private void SetPlayerInputState(bool uiHasFocus) // 매개변수 이름을 uiHasFocus로 변경
    {
        // 1. 커서 상태 제어 (포커스가 있으면 커서 해제)
        if (uiHasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 2. 플레이어 제어 컴포넌트 비활성화/활성화 (포커스가 있으면 비활성화)
        if (characterMove != null)
        {
            characterMove.enabled = !uiHasFocus;
        }

        if (cameraController != null)
        {
            cameraController.enabled = !uiHasFocus;
        }

        // 3. 이벤트 발생 (WeaponController에게 포커스 상태 전달)
        OnInventoryToggle?.Invoke(uiHasFocus);
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

    public void SetFilter(InventoryFilterType newFilter)
    {
        if (currentFilter != newFilter)
        {
            currentFilter = newFilter;
            Debug.Log($"[InventoryManager] 필터 변경: {currentFilter}");
            OnInventoryChanged?.Invoke();
        }
    }

    public bool IsCombatInputBlockedByUI()
    {
        return IsUIActiveAndFocused;
    }

    private void SetFocusState(bool isFocused)
    {
        if (this.IsFocused == isFocused) return;
        this.IsFocused = isFocused;

        Debug.Log($"[InventoryManager] 포커스 변경: {(isFocused ? "획득" : "상실(인게임 포커스)")}");

        // 1. 커서 상태 제어
        if (isFocused)
        {
            Cursor.lockState = CursorLockMode.None; // 마우스 포커스 획득
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 마우스 포커스 상실(인게임 복귀)
            Cursor.visible = false;
        }

        if (characterMove != null)
        {
            characterMove.enabled = !isFocused;
        }

        if (cameraController != null)
        {
            cameraController.enabled = !isFocused;
        }

        // 3. 이벤트 발생 (WeaponController에게 포커스 상태 전달)
        OnInventoryToggle?.Invoke(isFocused);
    }

    public List<RelicData> GetFilteredInventory()
    {
        // InventoryManager.cs의 slotCapacity 변수를 사용합니다.
        int capacity = slotCapacity;

        // 1. '전체' 필터 시에는 null 슬롯을 포함한 전체 리스트의 복사본을 반환
        if (currentFilter == InventoryFilterType.All)
        {
            // 리스트를 복사하여 반환 (원본 데이터 보호)
            List<RelicData> allSlotsCopy = new List<RelicData>(inventorySlots);
            return allSlotsCopy;
        }

        // 2. 필터링 로직
        List<RelicData> filteredList = new List<RelicData>();

        foreach (RelicData item in inventorySlots)
        {
            if (item == null) continue; // 빈 슬롯은 필터링에서 제외

            // RelicData.cs의 itemTypeEnum을 사용합니다.
            bool match = currentFilter switch
            {
                InventoryFilterType.Weapon => item.itemTypeEnum == ItemType.Weapon,
                InventoryFilterType.Relic => item.itemTypeEnum == ItemType.Artifact,
                // 장비: 무기, 유물, 장신구 모두 포함
                InventoryFilterType.Equipment => item.itemTypeEnum == ItemType.Weapon || item.itemTypeEnum == ItemType.Artifact || item.itemTypeEnum == ItemType.Accessory,
                // 장신구: ItemType.Accessory로 명확화
                InventoryFilterType.Accessory => item.itemTypeEnum == ItemType.Accessory,
                // 기타: 위에 해당되지 않는 모든 것을 포함
                InventoryFilterType.Etc => item.itemTypeEnum != ItemType.Weapon
                                       && item.itemTypeEnum != ItemType.Artifact
                                       && item.itemTypeEnum != ItemType.Accessory,
                _ => false,
            };

            if (match)
            {
                filteredList.Add(item);
            }
        }

        // 3. UI 바인딩을 위해 필터링된 리스트 뒤에 null을 채워줍니다.
        while (filteredList.Count < capacity)
        {
            filteredList.Add(null);
        }

        // 최종적으로 인벤토리의 총 슬롯 개수까지만 반환하도록 자릅니다.
        return filteredList.GetRange(0, Mathf.Min(filteredList.Count, capacity));
    }
}