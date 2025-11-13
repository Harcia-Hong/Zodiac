using UnityEngine;

/// <summary>
/// 플레이어 컨트롤러 - 쿼터뷰 기반 XZ 평면 이동 전용
/// 점프 및 Y축 관련 기능 제거
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dodgeSpeed = 12f;
    public float dodgeDuration = 0.3f;

    [Header("Mouse Control")]
    public LayerMask groundLayer = 1; // Ground 레이어 마스크

    float hAxis;
    float vAxis;

    Rigidbody rigid;
    Animator anim;
    PlayerGroundAttack playerGroundAttack;
    PlayerLockOn playerLockOn;
    Camera mainCamera; // 성능 최적화를 위한 캐싱 
    PlayerCombatStateMachine combatStateMachine;

    Vector3 moveVec;
    Vector3 dodgeVec;
    Vector3 mouseWorldPos;

    bool dDown;

    public bool isDodging;
    public bool isSkillCasting = false;
    public bool isSwap;
    public bool isInSwitchMode = false;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        playerGroundAttack = GetComponent<PlayerGroundAttack>();
        playerLockOn = GetComponent<PlayerLockOn>();
        mainCamera = Camera.main; // 캐싱으로 성능 개선
        combatStateMachine = GetComponent<PlayerCombatStateMachine>();
    }

    private void Update()
    {
        // 임시 디버깅용 - timeScale 강제 복구
        if (Time.timeScale == 0f)
        {
            //Debug.LogWarning("timeScale이 0입니다! 1로 복구합니다.");
            Time.timeScale = 1f;
        }

        //Debug.Log("현재 Time.timeScale 값: " + Time.timeScale);
    }

    void LateUpdate()
    {
        GetInput();
        UpdateMouseWorldPosition();
        MoveInput();
        Turn();
        DodgeInput();
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        dDown = Input.GetButtonDown("Dash");
    }

    void MoveInput()
    {
        if (isInSwitchMode) return;

        // 카메라의 forward와 right 방향을 가져옴
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // Y축 값을 0으로 만들어서 XZ 평면에서만 이동하도록 정규화
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 카메라 기준으로 이동 벡터 계산
        moveVec = (cameraForward * vAxis + cameraRight * hAxis).normalized;
        // moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (isDodging)
            moveVec = dodgeVec;

        if (isSwap || isSkillCasting)
            moveVec = Vector3.zero;

        Vector3 velocity = moveVec * moveSpeed;
        velocity.y = rigid.linearVelocity.y;
        rigid.linearVelocity = velocity;

        /*anim.SetFloat("MoveX", hAxis);
        anim.SetFloat("MoveY", vAxis);*/

        anim.SetBool("isRunning", moveVec != Vector3.zero);
    }

    void Turn()
    {
        if (playerGroundAttack != null && playerGroundAttack.isGroundAttacking)
            return;

        if (combatStateMachine != null && combatStateMachine.IsInAction())
        {
            // 공격/스킬 중: 마우스 방향으로 부드럽게 회전
            Vector3 lookDirection = GetMouseDirection();
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 15f
                );
            }
        }
        else if (moveVec != Vector3.zero)
        {
            // 평소: 이동 방향으로 즉시 회전
            transform.LookAt(transform.position + moveVec);
        }
    }

    void UpdateMouseWorldPosition()
    {
        // 마우스 스크린 좌표를 월드 좌표로 변환
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            mouseWorldPos = hit.point;
        else
        {
            // 바닥이 없는 경우 플레이어와 같은 높이 평면에서 계산
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float distance))
                mouseWorldPos = ray.GetPoint(distance);
        }
        }

    public void RotateToMouseDirection()
    {
        Vector3 lookDirection;

        // 락온 상태 확인
        if (playerLockOn != null && playerLockOn.isLockOn && playerLockOn.currentTarget != null)
        {
            // 락온 중이면 락온된 적 방향으로 회전
            lookDirection = (playerLockOn.currentTarget.position - transform.position).normalized;
        }
        else
        {
            // 일반 상태면 마우스 방향으로 회전
            lookDirection = (mouseWorldPos - transform.position).normalized;
        }

        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public Vector3 GetMouseDirection()
    {
        Vector3 direction;

        // 락온 상태 확인
        if (playerLockOn != null && playerLockOn.isLockOn && playerLockOn.currentTarget != null)
        {
            // 락온 중이면 락온된 적 방향 반환
            direction = (playerLockOn.currentTarget.position - transform.position).normalized;
        }
        else
        {
            // 일반 상태면 마우스 방향 반환
            direction = (mouseWorldPos - transform.position).normalized;
        }

        direction.y = 0;
        return direction;
    }

    public Vector3 GetMouseWorldPosition()
    {
        // 락온 중이면 락온된 적의 위치 반환
        if (playerLockOn != null && playerLockOn.isLockOn && playerLockOn.currentTarget != null)
        {
            return playerLockOn.currentTarget.position;
        }

        return mouseWorldPos;
    }

    void DodgeInput()
    {
        if (isInSwitchMode || isDodging) return;

        // 카메라 기준으로 회피 방향도 계산
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 inputVec = (cameraForward * vAxis + cameraRight * hAxis).normalized;

        //Vector3 inputVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (dDown && inputVec != Vector3.zero)
        {
            if (playerGroundAttack != null && playerGroundAttack.isAttacking) // 공격중이면 강제 취소
            {
                playerGroundAttack.CancelGroundAttack();
            }
            dodgeVec = inputVec;
            moveSpeed *= 2;
            anim.SetTrigger("Dodge");
            isDodging = true;

            Invoke("DodgeOut", 0.4f);
        }
    }

    void DodgeOut()
    {
        moveSpeed *= 0.5f;
        isDodging = false;
    }

    public void EnterSwitchMode()
    {
        isInSwitchMode = true;
        moveVec = Vector3.zero;
        rigid.linearVelocity = Vector3.zero;

        if (playerGroundAttack != null && playerGroundAttack.isAttacking)
            playerGroundAttack.CancelGroundAttack();
    }

    public void ExitSwitchMode()
    {
        isInSwitchMode = false;
    }
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // 마우스 위치 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, mouseWorldPos);
            Gizmos.DrawSphere(mouseWorldPos, 0.3f);

            // 락온 상태면 락온 방향도 표시
            if (playerLockOn != null && playerLockOn.isLockOn && playerLockOn.currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, playerLockOn.currentTarget.position);
                Gizmos.DrawSphere(playerLockOn.currentTarget.position, 0.5f);
            }
        }
    }
}
