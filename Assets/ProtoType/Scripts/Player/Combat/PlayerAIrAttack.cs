/*using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAIrAttack : MonoBehaviour
{
    [Header("Air Attack Settings")]
    [SerializeField] private float maxAirAttackHeight = 8f;    // 공중 공격 최대 높이
    [SerializeField] private float attackRange = 2.5f;        // 공격 범위

    [Header("Combo Settings")]
    [SerializeField] private List<string> comboTriggers = new List<string> { "AirAttack1", "AirAttack2", "AirAttack3" };
    [SerializeField] private List<float> comboCooldowns = new List<float> { 0.3f, 0.3f, 0.3f };
    [SerializeField] private List<float> forwardDistances = new List<float> { 1f, 0.5f, 0.5f }; // 각 타별 이동량

    [Header("Physics")]
    [SerializeField] private float gravityRestoreDelay = 0.05f;

    [Header("VFX")]
    [SerializeField] private Transform effectSpawnPoint;  // 이펙트 생성 위치
    [SerializeField] private GameObject[] airAttackEffects; // 공중 공격 이펙트들


    // 컴포넌트 참조
    private PlayerController playerController;
    private PlayerLockOn lockOn;
    private Animator anim;
    private Weapon weapon;
    private Rigidbody rigid;

    private Coroutine comboCoroutine;

    // 히트 관리
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    // 콤보 시스템
    private int comboIndex = 0;
    private float comboStartTime = 0f;

    // 공중 공격 상태 관리
    public bool isAirAttacking { get; private set; } = false;
    public bool isAirAttackReady { get; private set; } = true;

    /// <summary>컴포넌트 초기화</summary>
    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        lockOn = GetComponent<PlayerLockOn>();
        weapon = GetComponentInChildren<Weapon>();
    }

    // ========== [공개 인터페이스] ==========

    /// <summary>외부에서 호출: 공중 콤보 시작 (PlayerAttackManager에서 호출)</summary>
    public void StartFastAirCombo()
    {
        if (!CanAirAttack()) return;
        if (!IsWithinAirHeight()) return;

        isAirAttacking = true;
        isAirAttackReady = false;
        comboIndex = 0;
        comboStartTime = Time.time;
        hitEnemies.Clear();

        ResetAllTriggers();
        RotateToTarget();

        comboCoroutine = StartCoroutine(AirComboRoutine());
    }


    /// <summary>공중 공격 가능 여부 확인</summary>
    public bool CanAirAttack()
    {
        return playerController.inAir && !isAirAttacking && isAirAttackReady &&
               !playerController.isDodging && !playerController.isSkillCasting;
    }
    private bool IsWithinAirHeight() => transform.position.y <= maxAirAttackHeight;
    private IEnumerator AirComboRoutine()
    {
        while (comboIndex < comboTriggers.Count)
        {
            ExecuteCurrentCombo();
            float delay = comboIndex < comboCooldowns.Count ? comboCooldowns[comboIndex] : 0.3f;
            yield return new WaitForSeconds(delay);

            comboIndex++;
        }

        EndAirCombo();
    }
    private void ExecuteCurrentCombo()
    {
        if (comboIndex >= comboTriggers.Count) return;

        RotateToTarget();
        anim.SetTrigger(comboTriggers[comboIndex]);
        CreateEffect();
        MoveForward();
        StartCoroutine(HitBoxWindow());
    }
    private IEnumerator HitBoxWindow()
    {
        yield return new WaitForSeconds(0.05f);
        EnableHitBox();
        yield return new WaitForSeconds(0.25f);
        DisableHitBox();
    }
    private void MoveForward()
    {
        if (comboIndex >= forwardDistances.Count) return;
        float distance = forwardDistances[comboIndex];
        if (distance <= 0f) return;

        Vector3 dir = lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null
            ? (lockOn.currentTarget.position - transform.position).normalized
            : transform.forward;

        Vector3 target = transform.position + new Vector3(dir.x, 0, dir.z) * distance;
        target.y = transform.position.y;
        transform.DOMove(target, 0.15f).SetEase(Ease.OutCubic);
    }

    private void RotateToTarget()
    {
        Vector3 dir = lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null
            ? (lockOn.currentTarget.position - transform.position).normalized
            : playerController.GetMouseDirection();

        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = rot;
        }
    }
    private void EndAirCombo()
    {
        if (comboCoroutine != null)
        {
            StopCoroutine(comboCoroutine);
            comboCoroutine = null;
        }

        isAirAttacking = false;
        comboIndex = 0;
        DisableHitBox();
        ResetAllTriggers();
        EndEnemyCombos();
        StartCoroutine(RestoreGravityWithDelay());
        isAirAttackReady = true;

        Debug.Log($"[공중콤보 종료] 총 시간: {Time.time - comboStartTime:F2}s");
    }
    private IEnumerator RestoreGravityWithDelay()
    {
        yield return new WaitForSeconds(0.01f); // 즉시 중력 복구
        StopGravityControl();
    }
    private void StopGravityControl()
    {
        if (rigid != null)
            rigid.useGravity = true;
    }
    public void CancelAirAttack()
    {
        if (!isAirAttacking) return;
        EndAirCombo();
        Debug.Log("공중 공격 취소됨");
    }
    private void CreateEffect()
    {
        if (effectSpawnPoint == null || airAttackEffects.Length == 0) return;
        int idx = comboIndex % airAttackEffects.Length;
        Instantiate(airAttackEffects[idx], effectSpawnPoint.position, transform.rotation);
    }
    private void EnableHitBox() => weapon?.EnableHitBox();
    private void DisableHitBox() => weapon?.DisableHitBox();
    private void ResetAllTriggers()
    {
        if (anim == null) return;
        foreach (var trigger in comboTriggers)
            anim.ResetTrigger(trigger);
    }
    public void OnAirAttackHit(Collider target)
    {
        if (hitEnemies.Contains(target.gameObject)) return;
        hitEnemies.Add(target.gameObject);

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null) return;

        int damage = weapon != null ? weapon.GetDamage() : 10;
        if (comboIndex == comboTriggers.Count - 1)
        {
            damage *= 2;
            enemy.LiftReaction();
        }

        enemy.ApplySkillDamage(damage);
        enemy.StartAirComboHit();
    }

    private void EndEnemyCombos()
    {
        foreach (var go in hitEnemies)
        {
            if (go == null) continue;
            Enemy e = go.GetComponent<Enemy>();
            e?.EndAirCombo();
        }
        hitEnemies.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * maxAirAttackHeight,
            new Vector3(1f, 0.1f, 1f));

        #if UNITY_EDITOR
        if (isAirAttacking)
        {
            Gizmos.color = Color.green;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"Air Combo: {comboIndex + 1}/{comboTriggers.Count}");
        }
        #endif
    }

    
}
*/
