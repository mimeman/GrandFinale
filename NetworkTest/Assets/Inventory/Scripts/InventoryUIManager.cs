using DG.Tweening;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Small_Inventory_UI와 FullInventory_Inventory_UI 모두를 관리하는
/// 루트 싱글톤 매니저입니다.
/// </summary>
public class InventoryUIManager : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public static InventoryUIManager Instance { get; private set; }

    private PlayerStats playerStats;

    [Header("--- [Common] UI Panel Roots ---")]
    [SerializeField] private GameObject smallInventoryPanel;
    [SerializeField] private GameObject fullInventoryPanel;

    // --- [Small Inventory UI] ---
    [Header("--- [Small UI] References ---")]
    [SerializeField] private RectTransform small_RectTransform;
    [SerializeField] private RectTransform small_HeaderBarRect;
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
    [SerializeField] private RectTransform full_RectTransform;
    [SerializeField] private Image full_DragIcon;

    [Header("[Full UI] Header - Stats & Tabs")]
    [SerializeField] private TMP_Text full_StatsTextMidLeft;
    [SerializeField] private TMP_Text full_StatsTextMidRight;
    [SerializeField] private TMP_Text full_StatsTextMidBottom;
    [SerializeField] private Button full_InventoryButton;
    [SerializeField] private Button full_ArtifactsButton;

    [SerializeField] private TMP_Text full_ButtonSelectText;
    [SerializeField] private GameObject midInventoryPanel;
    [SerializeField] private GameObject midArtifactsPanel;
    [SerializeField] private GameObject full_InventoryDisplayPanel;
    [SerializeField] private GameObject full_ArtifactsDisplayPanel;
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
    [SerializeField] private Button equipmentInfoButton;
    [SerializeField] private Button playerInfoButton;

    [Header("[Left Info] Panels")]
    [SerializeField] private GameObject equipmentInfoPanel;
    [SerializeField] private GameObject statsInfoPanel;
    [SerializeField] private GameObject itemInfoPanel;

    [Header("[Left Info] Stats_Info Contents")]
    [SerializeField] private TMP_Text statsInfoRightText;
    [SerializeField] private TMP_Text statsInfoLeftText;

    [Header("[Left Info] Item_Info Contents")]
    [SerializeField] private Image itemInfoImage;
    [SerializeField] private TMP_Text itemInfoNameText;
    [SerializeField] private TMP_Text itemInfoDescriptionText;
    [SerializeField] private TMP_Text itemInfoHeadText;

    [SerializeField] private TMPro.TMP_InputField full_SearchInputField;

    [Header("[Left Info] Container")]
    [SerializeField] private CanvasGroup equipmentCanvasGroup;
    [SerializeField] private CanvasGroup statsCanvasGroup;
    [SerializeField] private CanvasGroup itemCanvasGroup;
    [SerializeField] private float panelAnimDuration = 0.2f;

    private CanvasGroup currentLeftPanelCanvasGroup;
    private RelicData currentDisplayedItem;

    private Vector2 dragOffset;
    private Coroutine fadeCoroutine;
    private CanvasGroup currentTooltipCanvasGroup;
    private float currentTargetAlpha = 0f;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, -50f);
    private const float FadeDuration = 0.2f;

    [SerializeField] private CanvasGroup midInventoryGroup;
    [SerializeField] private CanvasGroup midArtifactsGroup;
    [SerializeField] private float tabFlipDuration = 0.5f;
    [SerializeField] private float horizontalSlideDistance = 10f;

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

        // PlayerStats의 이벤트 구독
        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdateAllStatsFromPlayer;
            UpdateAllStatsFromPlayer();
        }

        if (midInventoryPanel && midInventoryGroup == null)
            midInventoryGroup = midInventoryPanel.GetComponent<CanvasGroup>();
        if (midArtifactsPanel && midArtifactsGroup == null)
            midArtifactsGroup = midArtifactsPanel.GetComponent<CanvasGroup>();

        // 초기 설정: 유물 탭은 숨깁니다.
        if (midArtifactsGroup)
        {
            midArtifactsGroup.alpha = 0f;
            midArtifactsGroup.blocksRaycasts = false;
            // midArtifactsPanel.SetActive(false); // OnArtifactsTabClick에서 처리
        }
        // 인벤토리 탭은 보이게 설정
        if (midInventoryGroup)
        {
            midInventoryGroup.alpha = 1f;
            midInventoryGroup.blocksRaycasts = true;
        }

        if (equipmentCanvasGroup == null) equipmentCanvasGroup = equipmentInfoPanel.GetComponent<CanvasGroup>();
        if (statsCanvasGroup == null) statsCanvasGroup = statsInfoPanel.GetComponent<CanvasGroup>();
        if (itemCanvasGroup == null) itemCanvasGroup = itemInfoPanel.GetComponent<CanvasGroup>();

        // 모든 패널을 활성화 상태로 두고, 투명하게 만듭니다.
        if (equipmentInfoPanel) equipmentInfoPanel.SetActive(true);
        if (statsInfoPanel) statsInfoPanel.SetActive(true);
        if (itemInfoPanel) itemInfoPanel.SetActive(true);

        if (statsCanvasGroup) { statsCanvasGroup.alpha = 0; statsCanvasGroup.blocksRaycasts = false; }
        if (itemCanvasGroup) { itemCanvasGroup.alpha = 0; itemCanvasGroup.blocksRaycasts = false; }

        // 장비 정보 패널을 기본값으로 설정
        if (equipmentCanvasGroup)
        {
            equipmentCanvasGroup.alpha = 1;
            equipmentCanvasGroup.blocksRaycasts = true;
            currentLeftPanelCanvasGroup = equipmentCanvasGroup;
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
            ShowEquipmentInfoPanel(); // 좌측 탭 (버튼 상태 업데이트)
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

        UpdatePlayerStatsUI(
            (int)playerStats.CurrentHealth,
            (int)playerStats.CurrentMaxHealth,
            (int)playerStats.CurrentDefense,
            (int)playerStats.CurrentRunSpeed,
            (int)playerStats.CurrentPower,
            playerStats.CurrentLevel
        );

        UpdateCurrency(playerStats.CurrentCurrency);
    }

    #region --- [Full UI] Header Functions ---

    public void UpdatePlayerStatsUI(int health, int maxHealth, int defense, int speed, int power, int level)
    {
        if (full_StatsTextMidLeft)
            full_StatsTextMidLeft.text = $"HEALTH : {health} / {maxHealth}\nDEFENSE : {defense}";
        if (full_StatsTextMidRight)
            full_StatsTextMidRight.text = $"SPEED : {speed}\nPOWER : {power}";
        if (full_StatsTextMidBottom)
            full_StatsTextMidBottom.text = $"레벨 : {level}";

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
        if (midInventoryGroup == null || midArtifactsGroup == null) return;

        // 유물 UI가 현재 활성화일 때만 애니메이션 실행
        if (midArtifactsPanel.activeSelf)
        {
            RectTransform inventoryRect = midInventoryPanel.GetComponent<RectTransform>();
            RectTransform artifactsRect = midArtifactsPanel.GetComponent<RectTransform>();

            // 1. 유물 패널 숨기기 (Fade Out + 오른쪽으로 살짝 이동)
            midArtifactsGroup.blocksRaycasts = false;
            midArtifactsGroup.DOFade(0f, tabFlipDuration);
            artifactsRect.DOAnchorPos(artifactsRect.anchoredPosition + new Vector2(horizontalSlideDistance, 0), tabFlipDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    midArtifactsPanel.SetActive(false);

                    // 2. 인벤토리 패널 나타내기 (왼쪽에서 살짝 이동하며 Fade In)
                    midInventoryPanel.SetActive(true);
                    midInventoryGroup.alpha = 0f;

                    // 시작 위치: 왼쪽으로 살짝 이동 (복귀는 중앙)
                    inventoryRect.anchoredPosition -= new Vector2(horizontalSlideDistance, 0);

                    midInventoryGroup.DOFade(1f, tabFlipDuration);
                    inventoryRect.DOAnchorPos(inventoryRect.anchoredPosition + new Vector2(horizontalSlideDistance, 0), tabFlipDuration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            midInventoryGroup.blocksRaycasts = true;
                        });
                });
        }
        else
        {
            midInventoryPanel.SetActive(true);
            midArtifactsPanel.SetActive(false);
        }

        if (full_InventoryButton) full_InventoryButton.interactable = false;
        if (full_ArtifactsButton) full_ArtifactsButton.interactable = true;

        if (full_ButtonSelectText)
            full_ButtonSelectText.text = "인벤토리";
    }

    public void OnArtifactsTabClick()
    {
        if (midInventoryGroup == null || midArtifactsGroup == null) return;

        // 인벤토리 UI가 현재 활성화일 때만 애니메이션 실행
        if (midInventoryPanel.activeSelf)
        {
            RectTransform inventoryRect = midInventoryPanel.GetComponent<RectTransform>();
            RectTransform artifactsRect = midArtifactsPanel.GetComponent<RectTransform>();

            // 1. 인벤토리 패널 숨기기 (Fade Out + 왼쪽으로 살짝 이동)
            midInventoryGroup.blocksRaycasts = false;
            midInventoryGroup.DOFade(0f, tabFlipDuration);
            inventoryRect.DOAnchorPos(inventoryRect.anchoredPosition - new Vector2(horizontalSlideDistance, 0), tabFlipDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    midInventoryPanel.SetActive(false);

                    // 2. 유물 패널 나타내기 (오른쪽에서 살짝 이동하며 Fade In)
                    midArtifactsPanel.SetActive(true);
                    midArtifactsGroup.alpha = 0f;

                    // 시작 위치: 오른쪽으로 살짝 이동 (복귀는 중앙)
                    artifactsRect.anchoredPosition += new Vector2(horizontalSlideDistance, 0);

                    midArtifactsGroup.DOFade(1f, tabFlipDuration);
                    artifactsRect.DOAnchorPos(artifactsRect.anchoredPosition - new Vector2(horizontalSlideDistance, 0), tabFlipDuration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            midArtifactsGroup.blocksRaycasts = true;
                        });
                });
        }
        else
        {
            midInventoryPanel.SetActive(false);
            midArtifactsPanel.SetActive(true);
        }

        if (full_InventoryButton) full_InventoryButton.interactable = true;
        if (full_ArtifactsButton) full_ArtifactsButton.interactable = false;

        if (full_ButtonSelectText)
            full_ButtonSelectText.text = "유물";
    }

    #endregion

    #region --- [NEW] Full UI Left_Info Panel Functions ---

    public void OnEquipmentInfoButtonClick()
    {
        ShowEquipmentInfoPanel();
    }


    public void OnPlayerInfoButtonClick()
    {
        ShowPlayerInfoPanel();
    }


    public void OnBackgroundClick()
    {
        if (currentLeftPanelCanvasGroup == itemCanvasGroup)
        {
            ShowEquipmentInfoPanel();
        }
    }

    private void AnimateLeftPanelSwitch(CanvasGroup panelToShow)
    {
        if (panelToShow == null || panelToShow == currentLeftPanelCanvasGroup)
            return;

        CanvasGroup panelToHide = currentLeftPanelCanvasGroup;

        // 1. 숨길 패널 (Fade Out + Scale Down)
        if (panelToHide != null)
        {
            panelToHide.blocksRaycasts = false;
            panelToHide.DOFade(0f, panelAnimDuration).SetEase(Ease.InSine);
            panelToHide.transform.DOScale(0.8f, panelAnimDuration).SetEase(Ease.InSine);
        }

        // 2. 보여줄 패널 (Fade In + Scale Up Overshoot)
        panelToShow.blocksRaycasts = true;
        panelToShow.alpha = 0f;
        panelToShow.transform.localScale = Vector3.one * 0.8f;

        panelToShow.DOFade(1f, panelAnimDuration).SetEase(Ease.OutSine);
        panelToShow.transform.DOScale(1f, panelAnimDuration).SetEase(Ease.OutBack);

        // 3. 현재 활성 패널 업데이트
        currentLeftPanelCanvasGroup = panelToShow;
    }


    private void ShowEquipmentInfoPanel()
    {
        AnimateLeftPanelSwitch(equipmentCanvasGroup);

        if (equipmentInfoButton) equipmentInfoButton.interactable = false;
        if (playerInfoButton) playerInfoButton.interactable = true;
    }


    private void ShowPlayerInfoPanel()
    {
        AnimateLeftPanelSwitch(statsCanvasGroup);

        if (equipmentInfoButton) equipmentInfoButton.interactable = true;
        if (playerInfoButton) playerInfoButton.interactable = false;
    }


    private void ShowItemInfoPanel(RelicData item)
    {
        if (itemInfoPanel == null || item == null) return;

        // 현재 패널의 CanvasGroup 확보
        CanvasGroup itemCG = itemInfoPanel.GetComponent<CanvasGroup>();
        if (itemCG == null) itemCG = itemInfoPanel.AddComponent<CanvasGroup>();

        // ----------------------------------------------------
        // L1: [전환 애니메이션 로직] 아이템이 변경되었는지 확인
        // ----------------------------------------------------
        bool itemChanged = currentDisplayedItem != item;

        // (A) 패널이 처음 나타나는 경우 (다른 패널에서 전환)
        if (currentLeftPanelCanvasGroup != itemCanvasGroup)
        {
            AnimateLeftPanelSwitch(itemCanvasGroup); // 기존 오버슈트 애니메이션 실행
        }
        // (B) 패널은 켜져 있는데 내용만 바뀌는 경우 (퀵 크로스 페이드)
        else if (itemChanged)
        {
            // 1. 투명도를 낮추어 내용이 사라지는 것처럼 만듭니다. (Fade Out)
            itemCG.DOFade(0.5f, 0.1f) // 0.1초 동안 50% 투명도로 페이드
                .OnComplete(() =>
                {
                    // 2. 내용 변경
                    UpdateItemInfoContent(item);

                    // 3. 다시 불투명하게 만듭니다. (Fade In)
                    itemCG.DOFade(1f, 0.1f); // 0.1초 동안 100% 투명도로 페이드 인
                });
        }
        // (C) 패널이 이미 켜져 있고 아이템도 동일한 경우 (변화 없음)
        else
        {
            // 아무것도 하지 않음
        }

        // ----------------------------------------------------

        // 버튼 인터랙션 설정
        if (equipmentInfoButton) equipmentInfoButton.interactable = true;
        if (playerInfoButton) playerInfoButton.interactable = true;

        // 아이템 정보 업데이트 (애니메이션 없는 경우 또는 OnComplete에서 호출)
        if (currentLeftPanelCanvasGroup != itemCanvasGroup || !itemChanged)
        {
            // (A)나 (C)의 경우 즉시 업데이트 (애니메이션이 시작되기 전이거나, 변경 사항이 없을 때)
            UpdateItemInfoContent(item);
        }

        // 현재 표시 중인 아이템 업데이트
        currentDisplayedItem = item;
    }

    private void UpdateItemInfoContent(RelicData item)
    {
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
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left || smallInventoryPanel == null || !smallInventoryPanel.activeSelf)
        {
            return;
        }

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
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left || smallInventoryPanel == null || !smallInventoryPanel.activeSelf)
        {
            return;
        }

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


    public void UpdateDetails(RelicData item)
    {
        if (smallInventoryPanel != null && smallInventoryPanel.activeSelf)
        {
            return;
        }

        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
            ShowItemInfoPanel(item);
        }
    }


    public void ClearDetails()
    {
        if (fullInventoryPanel != null && fullInventoryPanel.activeSelf)
        {
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
            UpdateTooltipContent(item, full_TooltipTitleText, full_TooltipItemKindText, full_TooltipItemImage, full_TooltipGradeText, full_TooltipGradeImage);
        }
        else if (smallInventoryPanel != null && smallInventoryPanel.activeSelf)
        {
            currentPanel = small_TooltipPanel;
            currentRect = small_TooltipRect;
            InitializeTooltip(currentPanel, out currentTooltipCanvasGroup);
            UpdateTooltipContent(item, small_TooltipTitleText, small_TooltipItemKindText, small_TooltipItemImage, small_TooltipGradeText, null);
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

    private void UpdateTooltipContent(RelicData item, TMP_Text title, TMP_Text kind, Image iconImg, TMP_Text gradeText, Image gradeImg)
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
            Color color;
            gradeText.text = item.grade;
            gradeText.color = ColorUtility.TryParseHtmlString(GetGradeColor(item.grade), out color) ? color : Color.white;
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
        if (full_TooltipDescriptionText) full_TooltipDescriptionText.text = detailsBuilder.ToString();
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