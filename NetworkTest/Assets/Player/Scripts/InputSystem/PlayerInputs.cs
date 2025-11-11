using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInputs : MonoBehaviour
{
    private InventoryManager inventoryManager;

    [SerializeField] private OptionKeyData keyData;

    private float horizontalInput = 0f;
    private float verticalInput = 0f;
    private float bending = 0f;

    // Movement
    public float GetAxisHorizontal()
    {
        return horizontalInput;
    }
    public float GetAxisVertical()
    {
        return verticalInput;
    }

    public float GetBending()
    {
        return bending;
    }

    private float CalculateAxis(float current, float raw)
    {
        float finalRaw = (raw != 0) ? raw : 0;

        return Mathf.MoveTowards(current, finalRaw, 10 * Time.deltaTime);
    }

    public bool GetMoveLeft()
    {
        return Input.GetKey(keyData.m_KeyMoveLeft);
    }
    public bool GetMoveRight()
    {
        return Input.GetKey(keyData.m_KeyMoveRight);
    }
    public bool GetMoveUp()
    {
        return Input.GetKey(keyData.m_KeyMoveUp);
    }
    public bool GetMoveDown()
    {
        return Input.GetKey(keyData.m_KeyMoveDown);
    }
    public bool GetJump()
    {
        return Input.GetKeyDown(keyData.m_KeyJump);
    }
    public bool GetSprint()
    {
        return Input.GetKey(keyData.m_KeySprint);
    }
    public bool GetCrouch()
    {
        return Input.GetKeyDown(keyData.m_KeyCrouch);
    }

    // Attack
    public bool GetAttack()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsCombatInputBlockedByUI()) return false;
        Debug.Log("전투 입력 차단됨!");
        return Input.GetKey(keyData.m_KeyAttack);
    }
    public bool GetAimed()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsCombatInputBlockedByUI()) return false;
        Debug.Log("전투 입력 차단됨!");

        return Input.GetKey(keyData.m_KeyAimed);
    }
    public bool GetReload()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsCombatInputBlockedByUI()) return false;

        return Input.GetKeyDown(keyData.m_KeyReload);
    }

    // Accessable
    public bool GetSlot0()
    {
        return Input.GetKeyDown(keyData.m_KeyUnArmed);
    }
    public bool GetSlot1()
    {
        return Input.GetKeyDown(keyData.m_KeySlot1);
    }
    public bool GetSlot2()
    {
        return Input.GetKeyDown(keyData.m_KeySlot2);
    }
    public bool GetSlot3()
    {
        return Input.GetKeyDown(keyData.m_KeySlot3);
    }
    public bool GetSlot4()
    {
        return Input.GetKeyDown(keyData.m_KeySlot4);
    }

    // Interact
    public bool GetInteract()
    {
        return Input.GetKeyDown(keyData.m_KeyInteract);
    }
    public bool GetInventory()
    {
        return Input.GetKeyDown(keyData.m_KeyInventory);
    }

    public bool GetInventoryToggle() // I 키 (작은 인벤토리)
    {
        return Input.GetKeyDown(keyData.m_KeyInventory);
    }

    public bool GetFullInventoryToggle() // O 키 (전체 인벤토리)
    {
        return Input.GetKeyDown(keyData.m_KeyFullInventory);
    }

    // UI
    public bool GetEscape()
    {
        return Input.GetKeyDown(keyData.m_KeyEscape);
    }
    public bool GetChatOpen()
    {
        return Input.GetKeyDown(keyData.m_KeyChat);
    }

    void Start()
    {
        if (OptionDataManager.Instance)
            keyData = OptionDataManager.Instance.OptionData.m_keyData;

        inventoryManager = InventoryManager.Instance;

        if (EventSystem.current == null)
        {
            Debug.LogError("[PlayerInputs] 씬에 EventSystem이 없습니다! UI 입력 차단이 작동하지 않습니다.");
        }
    }

    void Update()
    {
        // Axis Raw
        float horizontalRaw = Input.GetKey(keyData.m_KeyMoveLeft) ? -1 : Input.GetKey(keyData.m_KeyMoveRight) ? 1 : 0;
        float verticalRaw = Input.GetKey(keyData.m_KeyMoveDown) ? -1 : Input.GetKey(keyData.m_KeyMoveUp) ? 1 : 0;

        horizontalInput = CalculateAxis(horizontalInput, horizontalRaw);
        verticalInput = CalculateAxis(verticalInput, verticalRaw);

        // Bending
        float bendingRaw = Input.GetKey(keyData.m_BendingRight) ? -1 : Input.GetKey(keyData.m_BendingLeft) ? 1 : 0;
        bending = CalculateAxis(bending, bendingRaw);

        if (null == OptionDataManager.Instance)
            return;

        if (keyData != OptionDataManager.Instance.OptionData.m_keyData)
            keyData = OptionDataManager.Instance.OptionData.m_keyData;

    }
}

[Serializable]
public class OptionKeyData
{
    [Header("Movement")]
    public KeyCode m_KeyMoveLeft;
    public KeyCode m_KeyMoveRight;
    public KeyCode m_KeyMoveUp;
    public KeyCode m_KeyMoveDown;
    public KeyCode m_KeyJump;
    public KeyCode m_KeySprint;
    public KeyCode m_KeyCrouch;
    public KeyCode m_BendingRight;
    public KeyCode m_BendingLeft;

    [Header("Attack")]
    public KeyCode m_KeyAttack;
    public KeyCode m_KeyAimed;

    [Header("Accessable")]
    public KeyCode m_KeyUnArmed;
    public KeyCode m_KeySlot1;
    public KeyCode m_KeySlot2;
    public KeyCode m_KeySlot3;
    public KeyCode m_KeySlot4;

    [Header("Interaction")]
    public KeyCode m_KeyInteract;
    public KeyCode m_KeyInventory;
    public KeyCode m_KeyReload;

    [Header("UI")]
    public KeyCode m_KeyEscape;
    public KeyCode m_KeyChat;
    public KeyCode m_KeyFullInventory;
}