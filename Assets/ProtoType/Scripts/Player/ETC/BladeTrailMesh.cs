using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 검 궤적을 "폴리곤(메시)"로 그려주는 간단 트레일
/// - 기본 공격(지상/공중/제자리)에 사용
/// - 러시/이동형은 기존 SlashVFXController.DirectionalBurst 유지
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BladeTrailMesh : MonoBehaviour
{
    [Header("References")]
    public Transform swordTip;      // 검 끝
    public Transform swordBase;     // 손잡이 쪽 기준점(블레이드 시작)
    public Transform rootOrGrip;    // 플레이어 루트(전역 이동 보정용)

    [Header("Visual")]
    public Material material;       // Unlit/Particles Additive 류 권장
    public float lifetime = 0.35f;  // 세그먼트 생존 시간
    public Gradient colorOverLife;  // 알파/색 변화 (없으면 흰색 페이드)

    [Header("Sampling")]
    public float distanceThreshold = 0.015f; // 상대이동 최소 거리(스윙 감지)
    public float angleThresholdDeg = 2.0f;   // 각속도 임계값(보조)
    public float sampleStep = 0.02f;         // 포인트 간 최소 간격(누적 과밀 방지)
    public int maxSamples = 128;           // 최대 샘플 보관 개수

    // 내부 상태
    struct Sample { public Vector3 tip, bas; public float time; }
    private readonly List<Sample> _samples = new List<Sample>(256);

    private Mesh _mesh;
    private MeshFilter _mf;
    private MeshRenderer _mr;

    private bool _armed;
    private Vector3 _prevTip, _prevBase, _prevRoot;
    private Quaternion _prevRot;
    private float _accumStep;

    private static readonly Color _white = Color.white;

    // ============== 외부에서 호출 ==============
    public void Arm()
    {
        _armed = true;
        _accumStep = 0f;
        CachePrev();
    }

    public void Disarm()
    {
        _armed = false;
        // 남아있는 세그먼트는 lifetime 동안 자연 페이드 후 사라짐
    }

    // ============== Unity lifecycle ==============
    private void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        if (_mesh == null) { _mesh = new Mesh { name = "BladeTrailMesh" }; _mesh.MarkDynamic(); }
        _mf.sharedMesh = _mesh;

        if (material != null) _mr.sharedMaterial = material;
        if (colorOverLife == null || colorOverLife.colorKeys.Length == 0)
        {
            colorOverLife = new Gradient
            {
                colorKeys = new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            };
        }
        CachePrev();
    }

    private void OnDisable()
    {
        _samples.Clear();
        _mesh.Clear();
    }

    private void LateUpdate()
    {
        float now = Time.time;

        // 1) 샘플링 (무장 시에만)
        if (_armed && swordTip && swordBase)
        {
            Vector3 tip = swordTip.position;
            Vector3 bas = swordBase.position;
            Vector3 root = rootOrGrip ? rootOrGrip.position : Vector3.zero;

            // 상대 이동 계산
            Vector3 dTip = tip - _prevTip;
            Vector3 dBase = bas - _prevBase;
            Vector3 dRoot = rootOrGrip ? (root - _prevRoot) : Vector3.zero;

            Vector3 rel = (dTip + dBase) * 0.5f - dRoot; // 평균 이동에서 전역이동 제거
            float ang = rootOrGrip ? Quaternion.Angle(_prevRot, rootOrGrip.rotation) : 0f;

            _accumStep += rel.magnitude;

            bool pass = (rel.magnitude >= distanceThreshold || ang >= angleThresholdDeg) && _accumStep >= sampleStep;
            if (pass)
            {
                PushSample(tip, bas, now);
                _accumStep = 0f;
            }

            _prevTip = tip; _prevBase = bas; _prevRoot = root; _prevRot = rootOrGrip ? rootOrGrip.rotation : _prevRot;
        }

        // 2) 만료 제거
        PruneExpired(now);

        // 3) 메시 재구성
        RebuildMesh(now);
    }

    private void CachePrev()
    {
        if (swordTip) _prevTip = swordTip.position;
        if (swordBase) _prevBase = swordBase.position;
        if (rootOrGrip) { _prevRoot = rootOrGrip.position; _prevRot = rootOrGrip.rotation; }
    }

    private void PushSample(Vector3 tip, Vector3 bas, float t)
    {
        if (_samples.Count >= maxSamples) _samples.RemoveAt(0);
        _samples.Add(new Sample { tip = tip, bas = bas, time = t });
    }

    private void PruneExpired(float now)
    {
        float cutoff = now - lifetime;
        int removeCount = 0;
        for (int i = 0; i < _samples.Count; i++)
        {
            if (_samples[i].time < cutoff) removeCount++;
            else break;
        }
        if (removeCount > 0) _samples.RemoveRange(0, removeCount);
    }

    private void RebuildMesh(float now)
    {
        _mesh.Clear();

        int segs = _samples.Count - 1;
        if (segs <= 0) return;

        // 각 세그먼트 당 4 버텍스(quad), 6 인덱스
        int vcount = segs * 4;
        int icount = segs * 6;

        var verts = new Vector3[vcount];
        var cols = new Color[vcount];
        var uvs = new Vector2[vcount];
        var tris = new int[icount];

        float totalLen = 0f;
        for (int i = 1; i < _samples.Count; i++)
            totalLen += ((_samples[i].tip + _samples[i].bas) * 0.5f - (_samples[i - 1].tip + _samples[i - 1].bas) * 0.5f).magnitude;

        float accumLen = 0f;
        int vi = 0; int ti = 0;
        for (int i = 1; i < _samples.Count; i++)
        {
            var a = _samples[i - 1];
            var b = _samples[i];

            // quad: [a.bas, a.tip, b.bas, b.tip]
            verts[vi + 0] = a.bas;
            verts[vi + 1] = a.tip;
            verts[vi + 2] = b.bas;
            verts[vi + 3] = b.tip;

            // 나이 보정(알파): 최신=1, 오래됨=0
            float ageA = Mathf.InverseLerp(now - lifetime, now, a.time);
            float ageB = Mathf.InverseLerp(now - lifetime, now, b.time);
            Color ca = colorOverLife.Evaluate(ageA);
            Color cb = colorOverLife.Evaluate(ageB);
            cols[vi + 0] = cols[vi + 1] = ca;
            cols[vi + 2] = cols[vi + 3] = cb;

            // UV: U=0(base)/1(tip), V는 궤적 진행(0~1)
            float segLen = (((a.tip + a.bas) * 0.5f) - ((b.tip + b.bas) * 0.5f)).magnitude;
            float v0 = (totalLen <= 0.0001f) ? 0f : (accumLen / totalLen);
            float v1 = (totalLen <= 0.0001f) ? 1f : ((accumLen + segLen) / totalLen);
            uvs[vi + 0] = new Vector2(0f, v0);
            uvs[vi + 1] = new Vector2(1f, v0);
            uvs[vi + 2] = new Vector2(0f, v1);
            uvs[vi + 3] = new Vector2(1f, v1);
            accumLen += segLen;

            // 인덱스
            tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 1; tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 2; tris[ti + 4] = vi + 1; tris[ti + 5] = vi + 3;
            vi += 4; ti += 6;
        }

        _mesh.SetVertices(verts);
        _mesh.SetColors(cols);
        _mesh.SetUVs(0, uvs);
        _mesh.SetTriangles(tris, 0, true);
        _mesh.RecalculateBounds();
    }
}
