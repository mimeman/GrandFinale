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

    [Tooltip("아이템 수량을 표시할 TextMeshProUGUI 컴포넌트")]
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
        ItemType.Artifact.ToString().ToLower(),    // 유물
        ItemType.Accessory.ToString().ToLower(),   // 장신구
        ItemType.Equipment.ToString().ToLower()    // 장비
    };

    public void SetBoundItem(InventorySlot newSlot)
    {
        currentSlot = newSlot;
        UpdateSlotVisuals();
    }

    void UpdateSlotVisuals()
    {
        if (slotIcon == null)
        {
            return;
        }

        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1)
        {
            RelicData item = currentSlot.item;
            Sprite icon = Resources.Load<Sprite>(item.iconPath);

            if (icon != null)
            {
                slotIcon.sprite = icon;
                slotIcon.enabled = true;
            }
            else
            {
                slotIcon.enabled = false;
            }

            if (countText != null)
            {
                countText.text = currentSlot.quantity.ToString();
                countText.gameObject.SetActive(true);
                countText.fontSize = 10;
            }
        }
        else
        {
            slotIcon.sprite = null;
            slotIcon.enabled = false;

            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    private bool CheckFilterMatch(RelicData item)
    {
        InventoryFilterType currentFilter = InventoryManager.Instance.currentFilter;
        if (currentFilter == InventoryFilterType.All) return true;
        if (item == null) return false;

        ItemType itemType = item.itemTypeEnum;

        return currentFilter switch
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
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.item == null || currentSlot.slotIndex == -1) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount == 1)
            {
                if (singleClickCoroutine != null)
                {
                    StopCoroutine(singleClickCoroutine);
                }
                singleClickCoroutine = StartCoroutine(HandleSingleClick());
            }
            else if (eventData.clickCount == 2)
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
        }
    }

    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f);

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.UpdateDetails(currentSlot.item);

        singleClickCoroutine = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1 && InventoryUIManager.Instance != null)
        {
            dropSuccessful = false;
            Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);
            if (icon != null)
            {
                InventoryUIManager.Instance.StartDrag(icon);
                slotIcon.enabled = false;
                if (countText != null) countText.gameObject.SetActive(false);
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI targetSlot = this;
        if (targetSlot.currentSlot.slotIndex == -1) return;

        Slot_UI sourceInventorySlot = eventData.pointerDrag.GetComponent<Slot_UI>();
        if (sourceInventorySlot != null)
        {
            if (sourceInventorySlot.currentSlot == null || sourceInventorySlot.currentSlot.slotIndex == -1) return;

            if (sourceInventorySlot != targetSlot)
            {
                InventoryManager.Instance.SwapItems(sourceInventorySlot.currentSlot.slotIndex, targetSlot.currentSlot.slotIndex);
                sourceInventorySlot.dropSuccessful = true;
            }
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = eventData.pointerDrag.GetComponent<EquipmentSlot_UI>();
        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);

            if (success)
            {
                sourceEquipSlot.MarkDropSuccessful();
                dropSuccessful = true;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        if (eventData.pointerEnter == null)
        {
            if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1)
            {
                InventoryManager.Instance.DropItem(this.currentSlot.slotIndex);
                dropSuccessful = true;
            }
        }

        if (!dropSuccessful)
        {
            UpdateSlotVisuals();
        }

        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1)
        {
            InventoryUIManager.Instance.UpdateDetails(currentSlot.item);
        }

        dropSuccessful = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1 && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    private void AttemptEquip()
    {
        if (currentSlot == null || currentSlot.item == null || currentSlot.slotIndex == -1) return;

        RelicData itemToEquip = currentSlot.item;
        string currentItemTypeString = itemToEquip.itemTypeEnum.ToString().ToLower();

        bool isEquippable = false;
        foreach (var type in EquippableTypes)
        {
            if (currentItemTypeString == type)
            {
                isEquippable = true;
                break;
            }
        }

        if (isEquippable)
        {
            bool success = EquipmentManager.Instance.EquipItemToFirstAvailableSlot(itemToEquip, this.currentSlot.slotIndex);

            if (!success)
            {
                Debug.Log("장비 장착에 실패했습니다. (빈 슬롯이 없거나 인벤토리가 꽉 참)");
            }
            else
            {
                Debug.Log($"{itemToEquip.itemName} 장착 성공!");
            }
        }
        else
        {
            Debug.Log($"이 아이템({itemToEquip.itemName})은 장착할 수 없습니다. (타입: {itemToEquip.itemTypeEnum})");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (currentSlot == null || currentSlot.item == null || currentSlot.slotIndex == -1) return;
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentSlot.item));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = null;
        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(0.1f));
    }

    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.HideTooltip();
        }
        hideTooltipCoroutine = null;
    }
}