/*using System.Collections.Generic;
using System.Collections;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("VFX Settings")]
    public GameObject[] slashEffectPrefabs;
    public Transform effectSpawnPoint;

    [Header("Fast Ground Combo Settings")]
    public List<float> fastGroundComboCoolDowns = new List<float>
    {
        0.3f, 0.3f, 0.3f, 0.3f
    };

    public float totalGroundComboTime = 1.5f;

    [Header("Movement & Collision")]
    public float forwardMoveSpeed = 5f;
    public LayerMask enemyLayer = -1;
    public LayerMask wallLayer = -1;

    // ������Ʈ ����
    Animator anim;
    PlayerController playerController;
    PlayerSpinAttack playerSpinAttack;
    PlayerAIrAttack playerAIrAttack;
    public Weapon weapon;
    public SwordStyle currentStyle;
    Rigidbody rigid;

    // Ȧ�� ����� �޺� ���� ����
    int attackIndex = 0;
    public bool isAttacking = false;
    public bool isFireReady = true;
    bool isHoldingAttack = false; // ��ư�� ������ �ִ��� üũ

    float comboStartTime = 0f;
    Coroutine groundComboCoroutine;

    // �浹 �� �̵� ����
    bool isMovingForward = false;
    Vector3 moveStartPos;
    Vector3 moveTargetPos;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();
        playerSpinAttack = GetComponent<PlayerSpinAttack>();
        playerAIrAttack = GetComponent<PlayerAIrAttack>();  
        weapon = GetComponentInChildren<Weapon>();
        rigid = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        //HandleGroundAttackInput();

        // ���� �� �浹 üũ
        if (isMovingForward)
            CheckMovementCollision();
    }
    void HandleGroundAttackInput()
    {
        // �ٸ� ���� ���̸� ���� ���� �Ұ�
        if (playerSpinAttack != null && playerSpinAttack.isSpinAttack()) return;
        if (playerAIrAttack != null && playerAIrAttack.isAirAttacking) return;

        // ���󿡼��� ���� ���� ����
        if (playerController.inAir || playerController.isDodging) return;

        // ���콺 �Է� üũ
        bool mouseDown = Input.GetMouseButtonDown(0);
        bool mouseHold = Input.GetMouseButton(0);
        bool mouseUp = Input.GetMouseButtonUp(0);

        if (mouseDown && !isAttacking && isFireReady)
        {
            // ���� ���� ���� ����
            StartFastGroundCombo();
        }
        else if (mouseHold && isAttacking)
        {
            // ��� ������ ������ �޺� ����
            isHoldingAttack = true;
        }
        else if (mouseUp && isAttacking)
        {
            // ��ư�� ������ ���� ���� �� ����
            isHoldingAttack = false;
        }
    }

    void StartFastGroundCombo()
    {
        // 목표 방향으로 회전
        RotateToTarget();

        isAttacking = true;
        isHoldingAttack = true;
        attackIndex = 0;
        comboStartTime = Time.time;

        // ���� Ʈ���ŵ� ����
        ResetAllGroundAttackTriggers();

        // ���� �޺� �ڷ�ƾ ����
        groundComboCoroutine = StartCoroutine(FastGroundComboRoutine());

        isFireReady = false;
    }
    IEnumerator FastGroundComboRoutine()
    {
        while (attackIndex < currentStyle.comboTriggers.Count && isAttacking)
        {
            // ���� ���� ����
            ExecuteCurrentGroundAttack();

            // ���� ��ȯ ���
            float currentCooldown = attackIndex < fastGroundComboCoolDowns.Count ?
                fastGroundComboCoolDowns[attackIndex]
                : 0.3f;

            yield return new WaitForSeconds(currentCooldown);

            // Ȧ�� üũ - ������ ��� ����
            if (!Input.GetMouseButton(0))
            {
                break;
            }

            // ������ �����̸� ����
            if (attackIndex >= currentStyle.comboTriggers.Count - 1)
            {
                break;
            }

            attackIndex++;
        }

        // �޺� ����
        EndCombo();
    }
    void ExecuteCurrentGroundAttack()
    {
        if (currentStyle == null || attackIndex >= currentStyle.comboTriggers.Count) return;

        // �� ���ݸ��� ���� ������
        RotateToTarget();

        // �ִϸ��̼� Ʈ����
        anim.SetTrigger(currentStyle.comboTriggers[attackIndex]);

        // ����Ʈ ����
        CreateSlashEffect();

        // ���� ���� �̵� (�浹 ���� ����)
        PerformForwardMovement();

        // ��Ʈ�ڽ� ����
        StartCoroutine(FastGroundHitBoxTiming());
    }
    IEnumerator FastGroundHitBoxTiming()
    {
        // �� ���� ����
        yield return new WaitForSeconds(0.05f);

        // ��Ʈ�ڽ� Ȱ��ȭ
        EnableHitBox();

        // �� ª�� Ȱ�� �ð�
        yield return new WaitForSeconds(0.25f);

        // ��Ʈ�ڽ� ��Ȱ��ȭ
        DisableHitBox();
    }
    void RotateToTarget()
    {
        Vector3 lookDirection;

        // ���� ���� Ȯ��
        if (playerController.GetComponent<PlayerLockOn>() != null)
        {
            var lockOn = playerController.GetComponent<PlayerLockOn>();
            if (lockOn.isLockOn && lockOn.currentTarget != null)
            {
                // ���� ���̸� ���µ� �� �������� ��� ȸ��
                lookDirection = (lockOn.currentTarget.position - transform.position).normalized;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
                return;
            }
        }

        // �Ϲ� ���¸� ���콺 �������� ȸ��
        playerController.RotateToMouseDirection();
    }
    void PerformForwardMovement()
    {
        if (currentStyle == null || attackIndex >= currentStyle.forwardDistances.Count) return;

        float moveDistance = currentStyle.forwardDistances[attackIndex];
        if (moveDistance <= 0f) return;

        // �̵� ���� ��ġ�� ��ǥ ��ġ ����
        moveStartPos = transform.position;
        moveTargetPos = moveStartPos + transform.forward * moveDistance;

        // �浹 üũ�� ���� ����ĳ��Ʈ
        if (Physics.Raycast(moveStartPos, transform.forward, out RaycastHit hit, moveDistance, wallLayer))
        {
            // ���� ������ �� �ձ����� �̵�
            moveTargetPos = hit.point - transform.forward * 0.1f;
        }

        // �ε巯�� �̵� ����
        StartCoroutine(ForwardMovementWithCollision(moveTargetPos, 0.15f));
    }
    IEnumerator ForwardMovementWithCollision(Vector3 targetPos, float duration)
    {
        isMovingForward = true;
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration && isMovingForward)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, progress);
            transform.position = newPos;

            yield return null;
        }

        isMovingForward = false;
        transform.position = targetPos;
    }
    void CheckMovementCollision()
    {
        // ���� 0.5f �Ÿ����� �� �浹 üũ
        if (Physics.Raycast(transform.position, transform.forward, 0.5f, enemyLayer))
        {
            // ���� �浹�ϸ� �̵� ���� (�ִϸ��̼��� ���)
            isMovingForward = false;
        }
    }
    public void EndCombo()
    {
        // �޺� �� �ð� üũ
        float totalTime = Time.time - comboStartTime;

        // �ڷ�ƾ ����
        if (groundComboCoroutine != null)
        {
            StopCoroutine(groundComboCoroutine);
            groundComboCoroutine = null;
        }

        isAttacking = false;
        isHoldingAttack = false;
        attackIndex = 0;
        isMovingForward = false;

        // ��� Ʈ���� ����
        ResetAllGroundAttackTriggers();

        isFireReady = true;
    }
    public void CancelAttack()
    {
        if (!isAttacking) return;

        // �ڷ�ƾ ����
        if (groundComboCoroutine != null)
        {
            StopCoroutine(groundComboCoroutine);
            groundComboCoroutine = null;
        }

        isAttacking = false;
        isHoldingAttack = false;
        attackIndex = 0;
        isMovingForward = false;

        // ��� Ʈ���� ����
        ResetAllGroundAttackTriggers();

        // Ʈ����, ��Ʈ�ڽ� ����
        if (weapon != null)
            weapon.DisableHitBox();

        isFireReady = true;

    }
    void ResetAllGroundAttackTriggers()
    {
        if (anim == null || currentStyle == null) return;

        foreach (string trigger in currentStyle.comboTriggers)
        {
            anim.ResetTrigger(trigger);
        }
    }
    public void EnableHitBox()
    {
        if (weapon != null)
            weapon.EnableHitBox();
    }

    public void DisableHitBox()
    {
        if (weapon != null)
            weapon.DisableHitBox();
    }
    void CreateSlashEffect()
    {
        if (currentStyle != null && slashEffectPrefabs.Length > 0)
        {
            int effectIndex = attackIndex % slashEffectPrefabs.Length;
            GameObject effect = Instantiate(slashEffectPrefabs[effectIndex],
                effectSpawnPoint.position, effectSpawnPoint.rotation);
            effect.transform.rotation = transform.rotation;
        }
    
    }
    public void StartGroundAttackFromManager()
    {
        if (!CanAttack()) return;
        StartFastGroundCombo(); // ���� private �޼��� ȣ��
    }
    public bool IsAnyAttacking()
    {
        bool spinAttacking = (playerSpinAttack != null && playerSpinAttack.isSpinAttack());
        bool airAttacking = (playerAIrAttack != null && playerAIrAttack.isAirAttacking);
        return isAttacking || spinAttacking || airAttacking;
    }
    public bool IsGroundAttacking() => isAttacking;

    public bool CanAttack()
    {
        return !IsAnyAttacking() && isFireReady && !playerController.isDodging &&
               !playerController.isSkillCasting;
    }

}
*/
