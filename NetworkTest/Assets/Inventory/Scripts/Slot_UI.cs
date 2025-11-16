using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class Slot_UI : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public Image slotIcon;
    public TextMeshProUGUI countText;

    public bool dropSuccessful = false;

    private Coroutine hideTooltipCoroutine;
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    public InventorySlot currentSlot { get; private set; }
    private Coroutine singleClickCoroutine;

    private static readonly string[] EquippableTypes =
    {
        ItemType.Weapon.ToString().ToLower(),
        ItemType.Artifact.ToString().ToLower(),
        ItemType.Accessory.ToString().ToLower(),
        ItemType.Equipment.ToString().ToLower()
    };

    // 슬롯에 아이템 바인딩
    public void SetBoundItem(InventorySlot newSlot)
    {
        currentSlot = newSlot;
        UpdateSlotVisuals();
    }

    // 슬롯 비주얼 업데이트
    void UpdateSlotVisuals()
    {
        if (slotIcon == null) return;

        if (HasValidItem())
        {
            DisplayItemIcon();
            DisplayItemCount();
        }
        else
        {
            HideSlot();
        }
    }

    // 유효한 아이템이 있는지 확인
    private bool HasValidItem()
    {
        return currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1;
    }

    // 아이템 아이콘 표시
    private void DisplayItemIcon()
    {
        Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);

        if (icon != null)
        {
            slotIcon.sprite = icon;
            slotIcon.enabled = true;
        }
        else
        {
            slotIcon.enabled = false;
        }
    }

    // 아이템 개수 표시
    private void DisplayItemCount()
    {
        if (countText != null)
        {
            countText.text = currentSlot.quantity.ToString();
            countText.gameObject.SetActive(true);
            countText.fontSize = 10;
        }
    }

    // 슬롯 숨기기
    private void HideSlot()
    {
        slotIcon.sprite = null;
        slotIcon.enabled = false;

        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }

    // 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasValidItem()) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick(eventData);
        }
    }

    // 왼쪽 클릭 처리
    private void HandleLeftClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 1)
        {
            HandleSingleClickStart();
        }
        else if (eventData.clickCount == 2)
        {
            HandleDoubleClick();
        }
    }

    // 싱글 클릭 시작
    private void HandleSingleClickStart()
    {
        if (singleClickCoroutine != null)
            StopCoroutine(singleClickCoroutine);

        if (IsMainInventoryView())
            singleClickCoroutine = StartCoroutine(HandleSingleClick());
    }

    // 더블 클릭 처리
    private void HandleDoubleClick()
    {
        if (singleClickCoroutine != null)
        {
            StopCoroutine(singleClickCoroutine);
            singleClickCoroutine = null;
        }

        AttemptEquip();

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.ClearDetails();
    }

    // 싱글 클릭 처리
    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f);

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.UpdateDetails(currentSlot.item);

        singleClickCoroutine = null;
    }

    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasValidItem() || InventoryUIManager.Instance == null) return;

        dropSuccessful = false;
        Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);

        if (icon != null)
        {
            InventoryUIManager.Instance.StartDrag(icon);
            slotIcon.enabled = false;
            if (countText != null) countText.gameObject.SetActive(false);
        }
    }

    // 드롭 이벤트 처리
    public void OnDrop(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.slotIndex == -1) return;

        Slot_UI sourceSlot = FindSlotComponent(eventData.pointerDrag);

        if (sourceSlot != null)
        {
            HandleInventorySlotDrop(sourceSlot);
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = FindEquipmentSlotComponent(eventData.pointerDrag);

        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            HandleEquipmentSlotDrop(sourceEquipSlot);
        }
    }

    // 인벤토리 슬롯 드롭 처리
    private void HandleInventorySlotDrop(Slot_UI sourceSlot)
    {
        if (sourceSlot.currentSlot == null || sourceSlot.currentSlot.slotIndex == -1) return;
        if (sourceSlot == this) return;

        InventoryManager.Instance.SwapItems(sourceSlot.currentSlot.slotIndex, currentSlot.slotIndex);
        sourceSlot.dropSuccessful = true;
        dropSuccessful = true;
    }

    // 장비 슬롯 드롭 처리
    private void HandleEquipmentSlotDrop(EquipmentSlot_UI sourceEquipSlot)
    {
        bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);
        if (success)
        {
            sourceEquipSlot.MarkDropSuccessful();
            dropSuccessful = true;
        }
    }

    // Slot_UI 컴포넌트 찾기
    private Slot_UI FindSlotComponent(GameObject obj)
    {
        if (obj == null) return null;

        Slot_UI slot = obj.GetComponent<Slot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInParent<Slot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInChildren<Slot_UI>();
        return slot;
    }

    // EquipmentSlot_UI 컴포넌트 찾기
    private EquipmentSlot_UI FindEquipmentSlotComponent(GameObject obj)
    {
        if (obj == null) return null;

        EquipmentSlot_UI slot = obj.GetComponent<EquipmentSlot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInParent<EquipmentSlot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInChildren<EquipmentSlot_UI>();
        return slot;
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        // 드롭 실패 시 처리
        if (!dropSuccessful)
        {
            if (ShouldDropItem(eventData))
            {
                DropItemOutsideInventory();
            }
            else
            {
                UpdateSlotVisuals();
            }
        }

        dropSuccessful = false;
    }

    // 아이템을 드롭해야 하는지 확인
    private bool ShouldDropItem(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null)
        {
            return !IsInsideInventoryArea(eventData);
        }
        return false;
    }

    // 인벤토리 영역 내부인지 확인
    private bool IsInsideInventoryArea(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null) return false;

        Transform current = eventData.pointerEnter.transform;
        while (current != null)
        {
            if (IsInventoryRelatedObject(current.name))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    // 인벤토리 관련 오브젝트인지 확인
    private bool IsInventoryRelatedObject(string objectName)
    {
        return objectName.Contains("Inventory") ||
               objectName.Contains("Slot") ||
               objectName.Contains("ScrollView") ||
               objectName.Contains("Viewport") ||
               objectName == "FullInventory_Inventory_UI" ||
               objectName == "Small_Inventory_UI";
    }

    // 인벤토리 밖으로 아이템 드롭
    private void DropItemOutsideInventory()
    {
        if (HasValidItem())
        {
            InventoryManager.Instance.DropItem(currentSlot.slotIndex);
            dropSuccessful = true;
        }
    }

    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (HasValidItem() && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    // 드롭 성공 표시
    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    // 장비 착용 시도
    private void AttemptEquip()
    {
        if (!HasValidItem()) return;

        RelicData itemToEquip = currentSlot.item;
        string currentItemTypeString = itemToEquip.itemTypeEnum.ToString().ToLower();

        if (IsEquippableType(currentItemTypeString))
        {
            EquipmentManager.Instance.EquipItemToFirstAvailableSlot(itemToEquip, currentSlot.slotIndex);
        }
    }

    // 장착 가능한 타입인지 확인
    private bool IsEquippableType(string itemTypeString)
    {
        foreach (var type in EquippableTypes)
        {
            if (itemTypeString == type)
            {
                return true;
            }
        }
        return false;
    }

    // 마우스 진입
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (!HasValidItem()) return;

        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentSlot.item));
    }

    // 마우스 이탈
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = null;

        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(0.1f));
    }

    // 툴팁 표시 (딜레이 후)
    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }

    // 툴팁 숨기기 (딜레이 후)
    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.HideTooltip();
        }
        hideTooltipCoroutine = null;
    }

    // 메인 인벤토리 뷰인지 확인
    private bool IsMainInventoryView()
    {
        return InventoryManager.Instance.currentFilter != InventoryFilterType.Relic;
    }
}