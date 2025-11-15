using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Small_Inventory_UI와 FullInventory_Inventory_UI 모두를 관리하는
/// 루트 싱글톤 매니저입니다. (InventoryUI_Root에 부착)
/// </summary>
public class InventoryUIManager : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public static InventoryUIManager Instance { get; private set; }

    private PlayerStats playerStats;

    [Header("--- [Common] UI Panel Roots ---")]
    [Tooltip("Small_Inventory_UI의 최상위 GameObject")]
    [SerializeField] private GameObject smallInventoryPanel;
    [Tooltip("FullInventory_Inventory_UI의 최상위 GameObject")]
    [SerializeField] private GameObject fullInventoryPanel;

    // --- [Small Inventory UI] ---
    [Header("--- [Small UI] References ---")]
    [Tooltip("Small_Inventory_UI의 RectTransform")]
    [SerializeField] private RectTransform small_RectTransform;
    [Tooltip("Small_Inventory_UI의 드래그 영역 (Left_Head_Bar)")]
    [SerializeField] private RectTransform small_HeaderBarRect;
    [Tooltip("Small_Inventory_UI의 드래그 아이콘 (DragIcon)")]
    [SerializeField] private Image small_DragIcon;

    [Header("[Small UI] Item Tooltip (Left_Head_Bar)")]
    [SerializeField] private GameObject small_TooltipPanel;
    [SerializeField] private RectTransform small_TooltipRect;
    [SerializeField] private TextMeshProUGUI small_TooltipTitleText;
    [SerializeField] private TextMeshProUGUI small_TooltipItemKindText;
    [SerializeField] private Image small_TooltipItemImage;
    [SerializeField] private TextMeshProUGUI small_TooltipGradeText;
    [SerializeField] private TextMeshProUGUI small_TooltipDescriptionText;


    // --- [Full Inventory UI] ---
    [Header("--- [Full UI] References ---")]
    [Tooltip("FullInventory_Inventory_UI의 RectTransform")]
    [SerializeField] private RectTransform full_RectTransform;

    [Tooltip("FullInventory_Inventory_UI의 드래그 아이콘 (별도 지정 필요)")]
    [SerializeField] private Image full_DragIcon;

    [Header("[Full UI] Header - Stats & Tabs")]
    [SerializeField] private TMP_Text full_StatsTextMidLeft;
    [SerializeField] private TMP_Text full_StatsTextMidRight;
    [SerializeField] private TMP_Text full_StatsTextMidBottom;
    [SerializeField] private Button full_InventoryButton;
    [SerializeField] private Button full_ArtifactsButton;

    [Tooltip("중앙 탭 선택 텍스트 (Button_Sellect_Text)")]
    [SerializeField] private TMP_Text full_ButtonSelectText
        ; 
    [Tooltip("인벤토리 메인 컨텐츠 패널 (Mid)")]
    [SerializeField] private GameObject midInventoryPanel;  

    [Tooltip("유물 메인 컨텐츠 패널 (Mid_Artifacts)")]
    [SerializeField] private GameObject midArtifactsPanel;
    [SerializeField] private GameObject full_InventoryDisplayPanel; // 인벤토리 슬롯 패널
    [SerializeField] private GameObject full_ArtifactsDisplayPanel; // 유물 슬롯 패널
    [SerializeField] private TMP_Text full_CashText;

    [Header("[Full UI] Item Tooltip (별도 툴팁 패널)")]
    [SerializeField] private GameObject full_TooltipPanel;
    [SerializeField] private RectTransform full_TooltipRect;
    [SerializeField] private TextMeshProUGUI full_TooltipTitleText;
    [SerializeField] private TextMeshProUGUI full_TooltipItemKindText;
    [SerializeField] private Image full_TooltipItemImage;
    [SerializeField] private Image full_TooltipGradeImage;
    [SerializeField] private TextMeshProUGUI full_TooltipGradeText;
    [SerializeField] private TextMeshProUGUI full_TooltipDescriptionText;

    // --- [NEW] Full UI Left Info Panel ---
    [Header("--- [Full UI] Left Info Panel ---")]
    [Header("[Left Info] Buttons")]
    [Tooltip("장비 정보 버튼 (Equipment_Change_Button)")]
    [SerializeField] private Button equipmentInfoButton;

    [Tooltip("플레이어 정보 버튼 (State_Change_Button)")]
    [SerializeField] private Button playerInfoButton;

    [Header("[Left Info] Panels")]
    [Tooltip("장비 정보 패널 (Equipment_Info)")]
    [SerializeField] private GameObject equipmentInfoPanel;

    [Tooltip("스탯 정보 패널 (Stats_Info)")]
    [SerializeField] private GameObject statsInfoPanel;

    [Tooltip("아이템 정보 패널 (Item_Info)")]
    [SerializeField] private GameObject itemInfoPanel;

    [Header("[Left Info] Stats_Info Contents")]
    [Tooltip("스탯 우측 텍스트 (Stats_Right_Text)")]
    [SerializeField] private TMP_Text statsInfoRightText;
    [Tooltip("스탯 좌측 텍스트 (Stats_Left_Text)")]
    [SerializeField] private TMP_Text statsInfoLeftText;

    [Header("[Left Info] Item_Info Contents")]
    [Tooltip("아이템 이미지 (Item_Image)")]
    [SerializeField] private Image itemInfoImage;
    [Tooltip("아이템 이름 (Item_Name_Text)")]
    [SerializeField] private TMP_Text itemInfoNameText;
    [Tooltip("아이템 설명 (Text (TMP))")]
    [SerializeField] private TMP_Text itemInfoDescriptionText;
    [Tooltip("아이템 정보 헤더 텍스트 (Head_Item_Info_Text)")]
    [SerializeField] private TMP_Text itemInfoHeadText;

    [Tooltip("Full UI의 검색 입력 필드 (Search Text의 부모)")]
    [SerializeField] private TMPro.TMP_InputField full_SearchInputField; 

    // --- [Common Logic] ---
    private Vector2 dragOffset;
    private Coroutine fadeCoroutine;
    private CanvasGroup currentTooltipCanvasGroup;
    private float currentTargetAlpha = 0f;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, -50f);
    private const float FadeDuration = 0.2f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeTooltip(small_TooltipPanel, out _);
        InitializeTooltip(full_TooltipPanel, out _);
    }

    private void InitializeTooltip(GameObject panel, out CanvasGroup canvasGroup)
    {
        canvasGroup = null;
        if (panel == null) return;
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        panel.SetActive(false);
    }

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();

        // 2. PlayerStats의 이벤트 구독 (신호 받기 설정)
        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdateAllStatsFromPlayer;
            // 3. 게임 시작 시 UI 즉시 1회 업데이트
            UpdateAllStatsFromPlayer();
        }
        else
        {
            Debug.LogError("[InventoryUIManager] 씬에서 PlayerStats를 찾을 수 없습니다!");

        }
        // Full UI - Header 탭 버튼
        if (full_InventoryButton != null)
            full_InventoryButton.onClick.AddListener(OnInventoryTabClick);
        if (full_ArtifactsButton != null)
            full_ArtifactsButton.onClick.AddListener(OnArtifactsTabClick);

        // [NEW] Full UI - Left_Info 탭 버튼
        if (equipmentInfoButton != null)
            equipmentInfoButton.onClick.AddListener(OnEquipmentInfoButtonClick);
        if (playerInfoButton != null)
            playerInfoButton.onClick.AddListener(OnPlayerInfoButtonClick);


        // Full UI 초기 탭 상태 설정
        if (fullInventoryPanel != null)
        {
            OnInventoryTabClick(); // 중앙 탭
            ShowEquipmentInfoPanel(); // 좌측 탭
        }
    }
    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdateAllStatsFromPlayer;
        }
    }

    private void UpdateAllStatsFromPlayer()
    {
        if (playerStats == null) return;

        // 1. PlayerStats의 현재 값으로 UI 업데이트 함수 호출
        UpdatePlayerStatsUI(
            (int)playerStats.CurrentHealth,
            (int)playerStats.CurrentMaxHealth,
            (int)playerStats.CurrentDefense,
            (int)playerStats.CurrentRunSpeed, // 'SPEED'를 RunSpeed로 가정
            (int)playerStats.CurrentPower,
            playerStats.CurrentLevel
        );

        // 2. PlayerStats의 현재 재화로 재화 UI 업데이트
        UpdateCurrency(playerStats.CurrentCurrency);
    }

    #region --- [Full UI] Header Functions ---

    public void UpdatePlayerStatsUI(int health, int maxHealth, int defense, int speed, int power, int level)
    {
        // 1. Header 스탯 업데이트
        if (full_StatsTextMidLeft)
            full_StatsTextMidLeft.text = $"HEALTH : {health} / {maxHealth}\nDEFENSE : {defense}";
        if (full_StatsTextMidRight)
            full_StatsTextMidRight.text = $"SPEED : {speed}\nPOWER : {power}";
        if (full_StatsTextMidBottom)
            full_StatsTextMidBottom.text = $"레벨 : {level}";

        // 2. Left_Info > Stats_Info 패널 스탯 업데이트
        if (statsInfoLeftText)
            statsInfoLeftText.text = $"체력 : {maxHealth}\n방어력 : {defense}\n공격력 : {power}";
        if (statsInfoRightText)
            statsInfoRightText.text = $"스피드 : {speed}\n보유 능력1 : 없음\n보유 능력2 : 없음";
    }

    public void UpdateCurrency(int amount)
    {
        if (full_CashText)
            full_CashText.text = $"보유 금액 : {amount}";
    }

    public void OnInventoryTabClick()
    {
        if (midInventoryPanel) midInventoryPanel.SetActive(true);
        if (midArtifactsPanel) midArtifactsPanel.SetActive(false);

        if (full_InventoryButton) full_InventoryButton.interactable = false;
        if (full_ArtifactsButton) full_ArtifactsButton.interactable = true;

        if (full_ButtonSelectText)
            full_ButtonSelectText.text = "인벤토리";
    }
    public void OnArtifactsTabClick()
    {
        if (midInventoryPanel) midInventoryPanel.SetActive(false); 
        if (midArtifactsPanel) midArtifactsPanel.SetActive(true);

        if (full_InventoryButton) full_InventoryButton.interactable = true;
        if (full_ArtifactsButton) full_ArtifactsButton.interactable = false;

        if (full_ButtonSelectText)
            full_ButtonSelectText.text = "유물";
    }

    #endregion

    #region --- [NEW] Full UI Left_Info Panel Functions ---

    /// <summary>
    /// '장비 정보' 버튼 클릭 시 (Head_Button_Info > Equipment_Change_Button)
    /// </summary>
    public void OnEquipmentInfoButtonClick()
    {
        ShowEquipmentInfoPanel();
    }

    /// <summary>
    /// '플레이어 정보' 버튼 클릭 시 (Head_Button_Info > State_Change_Button)
    /// </summary>
    public void OnPlayerInfoButtonClick()
    {
        ShowPlayerInfoPanel();
    }

    /// <summary>
    /// 인벤토리 배경(Full_Back_Image) 클릭 시
    /// </summary>
    public void OnBackgroundClick()
    {
        // 아이템 정보(Item_Info) 패널이 켜져 있을 때만 닫고 기본(장비)창으로 복귀
        if (itemInfoPanel != null && itemInfoPanel.activeSelf)
        {
            ShowEquipmentInfoPanel();
        }
    }

    /// <summary>
    /// (기본) 장비 정보 패널을 켭니다.
    /// </summary>
    private void ShowEquipmentInfoPanel()
    {
        if (equipmentInfoPanel) equipmentInfoPanel.SetActive(true);
        if (statsInfoPanel) statsInfoPanel.SetActive(false);
        if (itemInfoPanel) itemInfoPanel.SetActive(false);

        if (equipmentInfoButton) equipmentInfoButton.interactable = false;
        if (playerInfoButton) playerInfoButton.interactable = true;
    }

    /// <summary>
    /// 플레이어 스탯 정보 패널을 켭니다.
    /// </summary>
    private void ShowPlayerInfoPanel()
    {
        if (equipmentInfoPanel) equipmentInfoPanel.SetActive(false);
        if (statsInfoPanel) statsInfoPanel.SetActive(true);
        if (itemInfoPanel) itemInfoPanel.SetActive(false);

        if (equipmentInfoButton) equipmentInfoButton.interactable = true;
        if (playerInfoButton) playerInfoButton.interactable = false;
    }

    /// <summary>
    /// 아이템 상세 정보 패널을 켭니다. (UpdateDetails에서 호출됨)
    /// </summary>
    private void ShowItemInfoPanel(RelicData item)
    {
        if (equipmentInfoPanel) equipmentInfoPanel.SetActive(false);
        if (statsInfoPanel) statsInfoPanel.SetActive(false);
        if (itemInfoPanel) itemInfoPanel.SetActive(true);

        if (equipmentInfoButton) equipmentInfoButton.interactable = true;
        if (playerInfoButton) playerInfoButton.interactable = true;

        // --- 아이템 정보 채우기 ---
        if (itemInfoHeadText != null)
            itemInfoHeadText.text = "아이템 정보";

        if (itemInfoImage != null)
        {
            Sprite icon = Resources.Load<Sprite>(item.iconPath);
            itemInfoImage.sprite = icon;
            itemInfoImage.enabled = (icon != null);
        }
        if (itemInfoNameText != null)
        {
            itemInfoNameText.text = $"<color={GetGradeColor(item.grade)}>{item.itemName}</color>";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(item.description);
        sb.AppendLine();

        if (item.grantedAbility != null)
        {
            AbilityData ability = item.grantedAbility;
            if (ability.abilityLogicID == "Stat_Add")
            {
                sb.AppendLine($"+{ability.param_ValueA} {ability.param_Key}");
            }
            else
            {
                sb.AppendLine($"능력: {ability.abilityName}");
            }
        }

        if (itemInfoDescriptionText != null)
        {
            itemInfoDescriptionText.text = sb.ToString();
        }
    }


    #endregion

    #region --- [Common] UI Movement ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Full UI가 켜져있으면 드래그 불가
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            return;
        }

        // 2. 좌클릭이 아니거나 Small UI가 닫혀있으면 드래그 불가
        if (eventData.button != PointerEventData.InputButton.Left || smallInventoryPanel == null || !smallInventoryPanel.activeSelf)
        {
            return;
        }

        // --- 여기부터는 Small UI 드래그 로직만 실행됨 ---
        RectTransform currentPanelRect = small_RectTransform;
        RectTransform currentHeaderRect = small_HeaderBarRect;

        if (currentHeaderRect != null && RectTransformUtility.RectangleContainsScreenPoint(currentHeaderRect, eventData.position, eventData.pressEventCamera))
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                currentPanelRect.parent.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            ))
            {
                dragOffset = currentPanelRect.anchoredPosition - localPoint;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 1. Full UI가 켜져있으면 드래그 불가
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            return;
        }

        // 2. 좌클릭이 아니거나 Small UI가 닫혀있으면 드래그 불가
        if (eventData.button != PointerEventData.InputButton.Left || smallInventoryPanel == null || !smallInventoryPanel.activeSelf)
        {
            return;
        }

        // --- 여기부터는 Small UI 드래그 로직만 실행됨 ---
        RectTransform currentPanelRect = small_RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            currentPanelRect.parent.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        ))
        {
            currentPanelRect.anchoredPosition = localPointerPosition + dragOffset;
        }
    }

    #endregion

    #region --- [Common] Drag & Drop Visuals ---

    public void StartDrag(Sprite iconSprite)
    {
        Image currentDragIcon = null;
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            currentDragIcon = full_DragIcon;
        }
        else if (smallInventoryPanel != null && smallInventoryPanel.activeSelf)
        {
            currentDragIcon = small_DragIcon;
        }

        if (currentDragIcon != null)
        {
            currentDragIcon.sprite = iconSprite;
            currentDragIcon.gameObject.SetActive(true);
        }
    }

    public void UpdateDragIcon(Vector2 position)
    {
        Image currentDragIcon = null;
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            currentDragIcon = full_DragIcon;
        }
        else if (smallInventoryPanel != null && smallInventoryPanel.activeSelf)
        {
            currentDragIcon = small_DragIcon;
        }

        if (currentDragIcon != null)
        {
            currentDragIcon.rectTransform.position = position;
        }
    }

    public void EndDrag()
    {
        if (full_DragIcon != null) full_DragIcon.gameObject.SetActive(false);
        if (small_DragIcon != null) small_DragIcon.gameObject.SetActive(false);

        ClearDetails();
    }

    #endregion

    #region --- [Common] Item Details ---

    /// <summary>
    /// Slot_UI에서 좌클릭 시 호출됩니다.
    /// </summary>
    public void UpdateDetails(RelicData item)
    {
        if (smallInventoryPanel != null && smallInventoryPanel.activeSelf)
        {
            return; // Small UI: do nothing on click
        }

        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            ShowItemInfoPanel(item); // Full UI: show the item info panel
        }
    }

    /// <summary>
    /// 드래그가 끝나거나 UI가 닫힐 때 상세정보 패널을 초기화합니다.
    /// </summary>
    public void ClearDetails()
    {
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            // Reset to default view (Equipment Info)
            ShowEquipmentInfoPanel();
        }
    }

    #endregion

    #region --- [Common] Item Tooltip ---

    public void ShowTooltip(RelicData item, Vector3 slotScreenPosition)
    {
        GameObject currentPanel = null;
        RectTransform currentRect = null;

        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            currentPanel = full_TooltipPanel;
            currentRect = full_TooltipRect;
            InitializeTooltip(currentPanel, out currentTooltipCanvasGroup);
            UpdateTooltipContent(item, full_TooltipTitleText, full_TooltipItemKindText, full_TooltipItemImage, full_TooltipGradeText, full_TooltipDescriptionText, full_TooltipGradeImage);
        }
        else if (smallInventoryPanel != null && smallInventoryPanel.activeSelf)
        {
            currentPanel = small_TooltipPanel;
            currentRect = small_TooltipRect;
            InitializeTooltip(currentPanel, out currentTooltipCanvasGroup);
            UpdateTooltipContent(item, small_TooltipTitleText, small_TooltipItemKindText, small_TooltipItemImage, small_TooltipGradeText, small_TooltipDescriptionText, null);
        }

        if (currentPanel == null || currentTooltipCanvasGroup == null) return;

        if (currentTargetAlpha == 1f && currentTooltipCanvasGroup.alpha > 0f) { }
        else
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            currentPanel.SetActive(true);
            currentTargetAlpha = 1f;
            fadeCoroutine = StartCoroutine(FadeTooltip(1f, currentPanel, currentTooltipCanvasGroup));
        }

        RectTransform parentCanvas = currentPanel.transform.parent.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas, Input.mousePosition, null, out Vector2 localPointerPosition))
        {
            if (currentRect != null)
            {
                currentRect.localPosition = localPointerPosition + tooltipOffset;
            }
        }
    }

    public void HideTooltip()
    {
        TryHideTooltip(small_TooltipPanel, small_TooltipRect);
        TryHideTooltip(full_TooltipPanel, full_TooltipRect);
    }

    private void TryHideTooltip(GameObject panel, RectTransform rect)
    {
        if (panel == null || !panel.activeSelf) return;
        InitializeTooltip(panel, out currentTooltipCanvasGroup);
        if (currentTooltipCanvasGroup == null) return;
        if (currentTargetAlpha == 0f) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        currentTargetAlpha = 0f;
        fadeCoroutine = StartCoroutine(FadeTooltip(0f, panel, currentTooltipCanvasGroup));
    }


    private IEnumerator FadeTooltip(float targetAlpha, GameObject panel, CanvasGroup canvasGroup)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < FadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / FadeDuration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        currentTargetAlpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            panel.SetActive(false);
        }
        fadeCoroutine = null;
    }

    private void UpdateTooltipContent(RelicData item, TMP_Text title, TMP_Text kind, Image iconImg, TMP_Text gradeText, TMP_Text desc, Image gradeImg)
    {
        if (title) title.text = $"<color={GetGradeColor(item.grade)}>{item.itemName}</color>";
        if (kind) kind.text = item.itemTypeEnum.ToString();
        if (iconImg)
        {
            Sprite icon = Resources.Load<Sprite>(item.iconPath);
            iconImg.sprite = icon;
            iconImg.enabled = (icon != null);
        }
        if (gradeText)
        {
            gradeText.text = item.grade;
            gradeText.color = ColorUtility.TryParseHtmlString(GetGradeColor(item.grade), out Color color) ? color : Color.white;
        }
        if (gradeImg)
        {
            gradeImg.enabled = false;
        }

        StringBuilder detailsBuilder = new StringBuilder();
        detailsBuilder.AppendLine($"{item.description}\n");
        if (item.grantedAbility != null)
        {
            AbilityData ability = item.grantedAbility;
            if (ability.abilityLogicID == "Stat_Add")
            {
                detailsBuilder.AppendLine($"+{ability.param_ValueA} {ability.param_Key}");
            }
            else { detailsBuilder.AppendLine($"능력: {ability.abilityName}"); }
        }
        if (desc) desc.text = detailsBuilder.ToString();
    }

    #endregion

    public void SetSearchQueryFromUI(string query)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetSearchQuery(query);
        }
    }

    private string GetGradeColor(string grade)
    {
        return grade.ToLower() switch
        {
            "common" => "#FFFFFF",
            "rare" => "#00CCFF",
            "epic" => "#9900FF",
            _ => "#FFFFFF",
        };
    }
}