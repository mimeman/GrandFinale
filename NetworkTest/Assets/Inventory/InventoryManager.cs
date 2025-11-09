using UnityEngine;
using System.Collections.Generic;
using System;
using Cinemachine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private int slotCapacity = 8;
    // 1. ItemData -> RelicData로 변경
    public List<RelicData> inventorySlots;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;

    [Header("UI Reference")]
    [SerializeField] private GameObject inventoryUI; // 'Inventory info' 오브젝트를 연결할 곳

    [Header("Player Control References")]
    [SerializeField] private PlayerInputs playerInput;     // PlayerInputs.cs
    [SerializeField] private CharacterMove characterMove;   // CharacterMove.cs (이동 제어)
    [SerializeField] private CameraController cameraController; // 카메라 회전 제어 (CameraController, InputHandler 등)
    [SerializeField] private InputHandler inputHandler; 
    [SerializeField] private WeaponController weaponController; // 무기 발사 제어


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
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
        if (playerInput != null && playerInput.GetInventory())
        {
            Debug.Log("인벤토리 키 입력 감지 (by PlayerInputs)!");
            ToggleInventory();
        }
    }
    public void ToggleInventory()
    {
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryManager에 inventoryUI가 연결되지 않았습니다!");
            return;
        }

        bool shouldBeActive = !inventoryUI.activeSelf;

        // 1. 인벤토리 UI 활성화/비활성화
        inventoryUI.SetActive(shouldBeActive);

        // 2. (핵심!) 플레이어 입력 및 제어 스크립트 활성화/비활성화
        // 인벤토리가 켜지면 (= shouldBeActive == true) 스크립트들은 꺼집니다 (!shouldBeActive)
        if (playerInput != null) playerInput.enabled = !shouldBeActive;
        if (characterMove != null) characterMove.enabled = !shouldBeActive;
        if (cameraController != null) cameraController.enabled = !shouldBeActive;
        if (inputHandler != null) inputHandler.enabled = !shouldBeActive;
/*        if (CinemachineCameras != null) CinemachineCameras.enabled = !shouldBeActive;
*/        if (weaponController != null) weaponController.enabled = !shouldBeActive;


        // 3. 커서 제어 (시간 정지 없이 커서만 해제/잠금)
        if (shouldBeActive)
        {
            Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
            Cursor.visible = true; // 커서 보이기 (드래그 앤 드롭 가능)
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 커서 잠금 (게임 입력 상태로 복귀)
            Cursor.visible = false; // 커서 숨기기
        }
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

    // 5. RelicData를 제거하도록 수정
    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity) return;
        inventorySlots[slotIndex] = null;
        OnInventoryChanged?.Invoke();
    }


    public void DropItem(int slotIndex)
    {
        RelicData itemToDrop = inventorySlots[slotIndex];
        if (itemToDrop == null) return;

        // 1. (중요!) RelicData에 dropPrefab이 설정되어 있는지 확인
        if (itemToDrop.dropPrefab != null)
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

            // 3. 3D 모델(dropPrefab)을 월드에 생성(Instantiate)
            Instantiate(itemToDrop.dropPrefab, dropPosition, Quaternion.identity);

            // 4. 인벤토리에서 아이템 제거
            RemoveItem(slotIndex); // (이 함수는 OnInventoryChanged를 호출함)
            Debug.Log(itemToDrop.itemName + "을(를) 바닥에 버렸습니다.");
        }
        else
        {
            Debug.LogWarning(itemToDrop.itemName + "에 dropPrefab이 설정되지 않아 버릴 수 없습니다.");
        }
    }
}