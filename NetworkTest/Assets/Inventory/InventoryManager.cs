using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Cinemachine;

public class InventoryManager : MonoBehaviour
{
    public CharacterMove playerCharacterMove;

    public static InventoryManager Instance;
    public InventoryFilterType currentFilter { get; private set; } = InventoryFilterType.All; // 기본값: 전체

    private int slotCapacity = 60;

    public List<InventorySlot> inventorySlots;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;
    public bool IsFocused { get; private set; } = false;
    public bool IsUIActiveAndFocused => IsUIOpen && IsFocused;

    [Header("UI Reference")]
    [SerializeField] private GameObject smallInventoryUI;
    [SerializeField] private GameObject fullInventoryUI;

    [Header("Loot Settings")]
    [SerializeField] private GameObject genericLootPrefab;

    [Header("Player Control References")]
    [SerializeField] private PlayerInputs playerInput;
    [SerializeField] private CharacterMove characterMove;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private WeaponController weaponController;

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

        inventorySlots = new List<InventorySlot>();
        for (int i = 0; i < slotCapacity; i++)
        {
            // ▼▼▼ [수정] 슬롯 생성 시 자신의 '진짜' 인덱스를 할당합니다. ▼▼▼
            inventorySlots.Add(new InventorySlot() { slotIndex = i });
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        }
    }

    void Update()
    {
        if (IsFocused && Input.GetMouseButtonDown(0))
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (!isOverUI)
            {
                SetFocusState(false);
            }
        }

        if (playerInput == null) return;

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
            CloseAllInventories();
        }
    }

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
    public void ToggleFullInventory()
    {
        if (fullInventoryUI == null) return;
        if (smallInventoryUI != null && smallInventoryUI.activeSelf)
        {
            smallInventoryUI.SetActive(false);
            if (IsFocused) SetFocusState(false);
        }

        // UI가 현재 열려있는지 확인
        bool currentActive = fullInventoryUI.activeSelf;

        if (currentActive && IsFocused)
        {
            // 2. [변경됨] 열려있고 포커스가 있었다면: UI를 닫고 포커스 상실
            fullInventoryUI.SetActive(false);
            SetFocusState(false);
        }
        else if (!currentActive)
        {
            // 3. 닫혀있었다면: UI를 열고 포커스 획득
            fullInventoryUI.SetActive(true);
            SetFocusState(true);
        }
        else // 4. 열려있지만 포커스가 없었다면: 포커스 다시 획득
        {
            SetFocusState(true);
        }
    }
    public void CloseAllInventories()
    {
        if (!IsUIOpen) return;

        if (smallInventoryUI != null) smallInventoryUI.SetActive(false);
        if (fullInventoryUI != null) fullInventoryUI.SetActive(false);

        SetFocusState(false);
    }
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


    public bool AddItem(RelicData itemToAdd)
    {
        if (itemToAdd.maxStack > 1)
        {
            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventorySlots[i];
                if (slot.item != null &&
                    slot.item.itemID == itemToAdd.itemID &&
                    slot.quantity < slot.item.maxStack)
                {
                    slot.AddQuantity(1);
                    OnInventoryChanged?.Invoke();
                    Debug.Log($"{itemToAdd.itemName}을(를) {i + 1}번 슬롯에 스택했습니다. (현재: {slot.quantity}개)");
                    return true;
                }
            }
        }

        int emptySlotIndex = FindNextEmptySlot();

        if (emptySlotIndex == -1)
        {
            Debug.Log("인벤토리가 꽉 찼습니다.");
            return false;
        }

        inventorySlots[emptySlotIndex].item = itemToAdd;
        inventorySlots[emptySlotIndex].quantity = 1;

        OnInventoryChanged?.Invoke();
        Debug.Log($"{itemToAdd.itemName}을(를) {emptySlotIndex + 1}번 슬롯에 새로 추가했습니다.");
        return true;
    }

    public int FindNextEmptySlot()
    {
        for (int i = 0; i < slotCapacity; i++)
        {
            if (inventorySlots[i].item == null)
            {
                return i;
            }
        }
        return -1;
    }

    public void RemoveItemFromSlot(int slotIndex, int amountToRemove = 1)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity || inventorySlots[slotIndex].item == null)
        {
            return;
        }

        InventorySlot slot = inventorySlots[slotIndex];
        slot.quantity -= amountToRemove;

        if (slot.quantity <= 0)
        {
            slot.ClearSlot();
        }

        OnInventoryChanged?.Invoke();
    }

    public void SwapItems(int indexA, int indexB)
    {
        // 1. A와 B의 '데이터'를 통째로 바꿉니다.
        InventorySlot temp = inventorySlots[indexA];
        inventorySlots[indexA] = inventorySlots[indexB];
        inventorySlots[indexB] = temp;

        inventorySlots[indexA].slotIndex = indexA;
        inventorySlots[indexB].slotIndex = indexB;

        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity)
        {
            Debug.LogError($"[InventoryManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }
        inventorySlots[slotIndex].ClearSlot();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void DropItem(int slotIndex)
    {
        InventorySlot slotToDrop = inventorySlots[slotIndex];
        if (slotToDrop.item == null) return;

        RelicData itemToDrop = slotToDrop.item;

        if (genericLootPrefab != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            Vector3 dropPosition;

            if (player != null)
            {
                dropPosition = player.transform.position + (player.transform.forward * 1f);
            }
            else
            {
                dropPosition = Camera.main.transform.position + (Camera.main.transform.forward * 1f);
            }

            GameObject orbInstance = Instantiate(genericLootPrefab, dropPosition, Quaternion.identity);

            ItemPickup pickupScript = orbInstance.GetComponent<ItemPickup>();
            if (pickupScript != null)
            {
                pickupScript.itemData = itemToDrop;
            }
            LootOrbVisuals visualScript = orbInstance.GetComponent<LootOrbVisuals>();
            if (visualScript != null)
            {
                visualScript.Initialize(itemToDrop.grade);
            }

            RemoveItemFromSlot(slotIndex, 1);

            Debug.Log($"{itemToDrop.itemName}을(를) 바닥에 1개 버렸습니다. (남은 수량: {slotToDrop.quantity})");
        }
        else
        {
            Debug.LogWarning("InventoryManager에 genericLootPrefab이 설정되지 않아 아이템을 버릴 수 없습니다.");
        }
    }

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

        if (isFocused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (isFocused)
        {
            if (characterMove != null)
            {
                characterMove.StopAllActions();
            }
        }

        OnInventoryToggle?.Invoke(isFocused);
    }

    public List<InventorySlot> GetFilteredInventory()
    {
        int capacity = slotCapacity;

        // [수정] 'All' 필터도 '복사본'을 반환하도록 변경 (데이터 안정성)
        if (currentFilter == InventoryFilterType.All)
        {
            return new List<InventorySlot>(inventorySlots);
        }

        List<InventorySlot> filteredList = new List<InventorySlot>();

        foreach (InventorySlot slot in inventorySlots)
        {
            RelicData item = slot.item;
            if (item == null) continue;

            ItemType itemType = item.itemTypeEnum; // 편의를 위해

            bool match = currentFilter switch
            {
                InventoryFilterType.Weapon => itemType == ItemType.Weapon,

                // '장비' 탭 = 헬멧, 갑옷, 하의, 신발 등
                InventoryFilterType.Equipment => itemType == ItemType.Equipment,

                // '장신구' 탭 = 얼굴, 목걸이
                InventoryFilterType.Accessory => itemType == ItemType.Accessory,

                // '유물' 탭 = Artifact
                InventoryFilterType.Relic => itemType == ItemType.Artifact,

                // '기타' 탭 = 위 4가지를 제외한 모든 것
                InventoryFilterType.Etc => itemType != ItemType.Weapon &&
                                           itemType != ItemType.Equipment &&
                                           itemType != ItemType.Accessory &&
                                           itemType != ItemType.Artifact,
                _ => false,
            };

            if (match)
            {
                filteredList.Add(slot);
            }
        }

        while (filteredList.Count < capacity)
        {
            // 필터링된 리스트의 나머지(빈 칸)를 채우는 가짜 슬롯
            filteredList.Add(new InventorySlot() { slotIndex = -1 });
        }

        return filteredList;
    }
}