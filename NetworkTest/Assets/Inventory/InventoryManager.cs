using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public InventoryFilterType currentFilter { get; private set; } = InventoryFilterType.All;

    private int slotCapacity = 60;

    public List<InventorySlot> inventorySlots;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;
    public bool IsFocused { get; private set; } = false;

    [Header("UI Reference")]
    [SerializeField] private GameObject smallInventoryUI;
    [SerializeField] private GameObject fullInventoryUI;

    [Header("Loot Settings")]
    [SerializeField] private GameObject genericLootPrefab;

    [Header("Player Control References")]
    [SerializeField] private CharacterMove characterMove;
    [SerializeField] private CameraSwitcher cameraSwitcher; // [추가]
    private Animator playerAnimator;

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
            inventorySlots.Add(new InventorySlot() { slotIndex = i });
        }

        // 플레이어 애니메이터 찾기
        if (characterMove != null)
        {
            playerAnimator = characterMove.GetComponent<Animator>();
        }
    }

    void Update()
    {
        // Tab 키: Small 인벤토리 토글
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleSmallInventory();
        }

        // O 키: Full 인벤토리 토글
        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleFullInventory();
        }

        // ESC 키: 인벤토리가 열려있으면 닫기
        if (IsFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllInventories();
        }

        // UI 외부 클릭 시 포커스 해제
        if (IsFocused && Input.GetMouseButtonDown(0))
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (!isOverUI)
            {
                SetFocusState(false);
            }
        }
    }

    public void ToggleSmallInventory()
    {
        if (smallInventoryUI == null) return;

        // 전체 창이 켜져 있으면 끄기
        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            fullInventoryUI.SetActive(false);
            if (IsFocused) SetFocusState(false);
        }

        bool currentActive = smallInventoryUI.activeSelf;

        if (currentActive && IsFocused)
        {
            smallInventoryUI.SetActive(false);
            SetFocusState(false);
        }
        else if (!currentActive)
        {
            smallInventoryUI.SetActive(true);
            SetFocusState(true);
        }
        else
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

        bool currentActive = fullInventoryUI.activeSelf;

        if (currentActive && IsFocused)
        {
            fullInventoryUI.SetActive(false);
            SetFocusState(false);
        }
        else if (!currentActive)
        {
            fullInventoryUI.SetActive(true);
            SetFocusState(true);
        }
        else
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

    private void SetFocusState(bool isFocused)
    {
        if (this.IsFocused == isFocused) return;

        this.IsFocused = isFocused;

        // 커서 제어
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

        // 애니메이션 제어
        if (isFocused)
        {
            StopPlayerAnimation();
        }
        else
        {
            ResumePlayerAnimation();
        }

        OnInventoryToggle?.Invoke(isFocused);
    }

    private void StopPlayerAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f;
        }

        if (characterMove != null)
        {
            characterMove.StopAllActions();
        }

        // 조준 중이면 강제로 해제
        if (cameraSwitcher != null)
        {
            cameraSwitcher.StopAiming();
        }
    }

    private void ResumePlayerAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
        }
    }

    public bool AddItem(RelicData itemToAdd)
    {

        Debug.Log($"[DEBUG STACK] 아이템: {itemToAdd.itemName}, Max Stack: {itemToAdd.maxStack}, Item ID: {itemToAdd.itemID}");
        // 1. 스택 가능한 아이템인지 확인 (maxStack > 1)
        if (itemToAdd.maxStack > 1)
        {
            // 2. 인벤토리를 순회하며 같은 아이템이 있고, 스택이 가득 차지 않았는지 확인
            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventorySlots[i];
                if (slot.item != null &&
                    slot.item.itemID == itemToAdd.itemID &&
                    slot.quantity < slot.item.maxStack)
                {
                    // 3. (스택 성공) 수량을 1 증가시키고 알림
                    slot.AddQuantity(1);
                    OnInventoryChanged?.Invoke();
                    Debug.Log($"{itemToAdd.itemName}을(를) {i + 1}번 슬롯에 스택했습니다. (현재: {slot.quantity}개)");
                    return true;
                }
            }
            // 4. (실패) 같은 아이템을 찾지 못했거나 모두 가득 찼음. -> 다음 단계(빈 슬롯 찾기)로 이동
        }

        // 5. (스택 불가 또는 스택 실패) 빈 슬롯을 찾습니다.
        int emptySlotIndex = FindNextEmptySlot();

        if (emptySlotIndex == -1)
        {
            Debug.Log("인벤토리가 꽉 찼습니다.");
            return false;
        }

        // 6. 빈 슬롯에 아이템 정보와 수량 1을 설정합니다.
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
        return IsFocused;
    }

    public List<InventorySlot> GetFilteredInventory()
    {
        int capacity = slotCapacity;

        if (currentFilter == InventoryFilterType.All)
        {
            return new List<InventorySlot>(inventorySlots);
        }

        List<InventorySlot> filteredList = new List<InventorySlot>();

        foreach (InventorySlot slot in inventorySlots)
        {
            RelicData item = slot.item;
            if (item == null) continue;

            ItemType itemType = item.itemTypeEnum;

            bool match = currentFilter switch
            {
                InventoryFilterType.Weapon => itemType == ItemType.Weapon,
                InventoryFilterType.Equipment => itemType == ItemType.Equipment,
                InventoryFilterType.Accessory => itemType == ItemType.Accessory,
                InventoryFilterType.Relic => itemType == ItemType.Artifact,
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
            filteredList.Add(new InventorySlot() { slotIndex = -1 });
        }

        return filteredList;
    }
}