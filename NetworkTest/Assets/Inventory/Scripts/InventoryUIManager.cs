using UnityEngine;

/// <summary>
/// 인벤토리 UI 총괄 매니저 (슬림화 버전)
/// - 모든 하위 컨트롤러 조율
/// - 외부 인터페이스 제공
/// - 최소한의 책임만 보유
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    #region Singleton

    public static InventoryUIManager Instance { get; private set; }

    #endregion

    #region Serialized Fields - Panel References

    [Header("UI Panel Roots")]
    [SerializeField] private GameObject smallInventoryPanel;
    [SerializeField] private GameObject fullInventoryPanel;

    #endregion

    #region Serialized Fields - Controllers

    [Header("Controllers")]
    [SerializeField] private TooltipController tooltipController;
    [SerializeField] private DragDropHandler dragDropHandler;
    [SerializeField] private TabSwitchController tabSwitchController;
    [SerializeField] private LeftPanelController leftPanelController;
    [SerializeField] private InventoryStatsUI inventoryStatsUI;

    #endregion

    #region Initialization

    void Awake()
    {
        InitializeSingleton();
    }

    void Start()
    {
        ValidateControllers();
    }

    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void ValidateControllers()
    {
        if (tooltipController == null)
        {
            Debug.LogWarning("[InventoryUIManager] TooltipController가 할당되지 않았습니다!");
        }

        if (dragDropHandler == null)
        {
            Debug.LogWarning("[InventoryUIManager] DragDropHandler가 할당되지 않았습니다!");
        }

        if (tabSwitchController == null)
        {
            Debug.LogWarning("[InventoryUIManager] TabSwitchController가 할당되지 않았습니다!");
        }

        if (leftPanelController == null)
        {
            Debug.LogWarning("[InventoryUIManager] LeftPanelController가 할당되지 않았습니다!");
        }

        if (inventoryStatsUI == null)
        {
            Debug.LogWarning("[InventoryUIManager] InventoryStatsUI가 할당되지 않았습니다!");
        }
    }

    #endregion

    #region Public API - Tooltip

    /// <summary>
    /// 툴팁 표시
    /// </summary>
    public void ShowTooltip(RelicData item, Vector3 slotScreenPosition)
    {
        if (tooltipController == null) return;

        InventoryType type = GetCurrentInventoryType();
        tooltipController.ShowTooltip(item, type);
    }

    /// <summary>
    /// 툴팁 숨기기
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipController != null)
        {
            tooltipController.HideTooltip();
        }
    }

    #endregion

    #region Public API - Drag & Drop

    /// <summary>
    /// 아이템 드래그 시작
    /// </summary>
    public void StartDrag(Sprite iconSprite)
    {
        if (dragDropHandler != null)
        {
            dragDropHandler.StartItemDrag(iconSprite);
        }
    }

    /// <summary>
    /// 드래그 아이콘 위치 업데이트
    /// </summary>
    public void UpdateDragIcon(Vector2 position)
    {
        if (dragDropHandler != null)
        {
            dragDropHandler.UpdateDragIconPosition(position);
        }
    }

    /// <summary>
    /// 드래그 종료
    /// </summary>
    public void EndDrag()
    {
        if (dragDropHandler != null)
        {
            dragDropHandler.EndItemDrag();
        }

        // 드래그 종료 시 상세 정보 초기화
        ClearDetails();
    }

    #endregion

    #region Public API - Item Details

    /// <summary>
    /// 아이템 상세 정보 업데이트
    /// </summary>
    public void UpdateDetails(RelicData item)
    {
        // Small Inventory에서는 상세 정보 표시 안 함
        if (IsSmallInventoryActive())
        {
            return;
        }

        if (leftPanelController != null && item != null)
        {
            leftPanelController.ShowItemInfoPanel(item);
        }
    }

    /// <summary>
    /// 상세 정보 초기화
    /// </summary>
    public void ClearDetails()
    {
        if (IsFullInventoryActive() && leftPanelController != null)
        {
            leftPanelController.ShowEquipmentPanel();
        }
    }

    #endregion

    #region Public API - Search

    /// <summary>
    /// 검색 쿼리 설정 (UI Input Field에서 호출)
    /// </summary>
    public void SetSearchQueryFromUI(string query)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetSearchQuery(query);
        }
    }

    #endregion

    #region Helper Methods

    private InventoryType GetCurrentInventoryType()
    {
        return IsFullInventoryActive() ? InventoryType.Full : InventoryType.Small;
    }

    private bool IsSmallInventoryActive()
    {
        return smallInventoryPanel != null && smallInventoryPanel.activeSelf;
    }

    private bool IsFullInventoryActive()
    {
        return fullInventoryPanel != null && fullInventoryPanel.activeSelf;
    }

    #endregion
}