using UnityEngine;

/// <summary>
/// Natural water floating via Rigidbody buoyancy.
/// Uses physics force — no direct position setting, no detachment.
///
/// Wave math matches Water/WaterSurface Gerstner shader.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FloatingObject : MonoBehaviour
{
    [Header("Wave Settings (sync with Water_Mat)")]
    public float waveHeightScale = 0.3f;
    public GerstnerWave[] waves = new GerstnerWave[]
    {
        new(0.4f, 0.8f, 1.2f, new Vector2(1, 0)),
        new(0.25f, 1.2f, 0.8f, new Vector2(0.7f, 0.7f)),
        new(0.15f, 2.0f, 2.0f, new Vector2(-0.3f, 0.9f)),
        new(0.08f, 3.0f, 1.5f, new Vector2(-0.8f, 0.6f)),
    };

    [Header("Buoyancy Physics")]
    public float buoyancyStrength = 3f;      // spring stiffness (higher = tighter follow)
    public float waterDrag = 1.5f;            // damping ratio

    [Header("Wave Follow")]

    [Header("Flow Drift")]
    public bool enableDrift = true;
    public float driftSpeed = 0.4f;
    public float driftRadius = 10f;

    [Header("Debug")]
    public bool showWaveHeight = true;

    // --- state ---
    private Rigidbody _rb;
    private Vector3 _startPosition;
    private float _driftAngle;
    private float _timeOffset;  // sync with shader _Time.y

    [System.Serializable]
    public struct GerstnerWave
    {
        public float amplitude, frequency, speed;
        public Vector2 direction;
        public GerstnerWave(float amp, float freq, float spd, Vector2 dir)
        {
            amplitude = amp; frequency = freq; speed = spd;
            direction = dir.normalized;
        }
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;
        _rb.drag = 1f;
        _rb.angularDrag = 2f;

        _startPosition = transform.position;
        _driftAngle = GetNetFlowAngle();

        // Sync time with shader: capture offset between C# Time and shader _Time.y
        // In editor, shader _Time.y keeps running; Time.time resets on Play.
        _timeOffset = Time.timeSinceLevelLoad;
    }

    private void FixedUpdate()
    {
        float time = Time.timeSinceLevelLoad;
        Vector3 pos = _rb.position;

        // 1. Horizontal drift (XZ)
        if (enableDrift)
            pos = UpdateDrift(pos, time);

        // 2. Wave height at this position
        float waveY = GerstnerHeight(pos.x, pos.z, time);

        // 3. Spring-damper buoyancy: pulls object toward wave surface
        //    Force = k * displacement - d * velocity  (simple harmonic motion)
        float surfaceY = waveY + 0.2f;               // target float height
        float displacement = surfaceY - pos.y;        // positive = below surface, need to go up
        float damping = waterDrag * 2 * Mathf.Sqrt(buoyancyStrength); // critical damping
        float forceY = buoyancyStrength * displacement - damping * _rb.velocity.y;
        forceY += -Physics.gravity.y;                 // cancel gravity

        _rb.AddForce(Vector3.up * forceY, ForceMode.Acceleration);

        // 4. Tilt with wave normal
        Vector3 normal = GerstnerNormal(pos.x, pos.z, time);
        Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, normal);
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 0.1f));
    }

    // ──────────── drift ────────────

    private float GetNetFlowAngle()
    {
        Vector2 flow = Vector2.zero;
        foreach (var w in waves)
            flow += w.direction * (w.amplitude * w.speed);
        return flow.magnitude < 0.001f ? 0 : Mathf.Atan2(flow.y, flow.x);
    }

    private Vector3 UpdateDrift(Vector3 pos, float time)
    {
        float angle = _driftAngle + Mathf.Sin(time * 0.12f) * 0.5f;
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        pos.x += dir.x * driftSpeed * Time.fixedDeltaTime;
        pos.z += dir.y * driftSpeed * Time.fixedDeltaTime;

        Vector2 offset = new Vector2(pos.x - _startPosition.x, pos.z - _startPosition.z);
        if (offset.magnitude > driftRadius)
        {
            Vector2 push = offset.normalized * (offset.magnitude - driftRadius);
            pos.x -= push.x;
            pos.z -= push.y;
            _driftAngle += Mathf.PI * 0.3f * Time.fixedDeltaTime;
        }
        return pos;
    }

    // ──────────── wave math ────────────

    private float GerstnerHeight(float x, float z, float time)
    {
        Vector2 pos = new Vector2(x, z);
        float h = 0;
        foreach (var w in waves)
        {
            if (w.amplitude < 0.0001f) continue;
            float phase = w.frequency * Vector2.Dot(w.direction, pos) + time * w.speed;
            h += w.amplitude * Mathf.Sin(phase);
        }
        return h * waveHeightScale;
    }

    private Vector3 GerstnerNormal(float x, float z, float time)
    {
        Vector2 pos = new Vector2(x, z);
        float dx = 0, dz = 0;
        foreach (var w in waves)
        {
            if (w.amplitude < 0.0001f) continue;
            float wa = w.frequency * w.amplitude;
            float C = Mathf.Cos(w.frequency * Vector2.Dot(w.direction, pos) + time * w.speed);
            dx += -w.direction.x * wa * C;
            dz += -w.direction.y * wa * C;
        }
        dx *= waveHeightScale;
        dz *= waveHeightScale;
        return new Vector3(-dx, 1, -dz).normalized;
    }

    // ──────────── gizmos ────────────

    private void OnDrawGizmos()
    {
        if (!showWaveHeight) return;
        Vector3 pos = Application.isPlaying ? _rb.position : transform.position;
        float time = Application.isPlaying ? Time.timeSinceLevelLoad : 0;

        float waveY = GerstnerHeight(pos.x, pos.z, time);
        Vector3 surfacePos = new Vector3(pos.x, waveY, pos.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(surfacePos, 0.15f);

        Gizmos.color = Color.green;
        Vector3 n = GerstnerNormal(pos.x, pos.z, time);
        Gizmos.DrawRay(surfacePos, n * 0.5f);

        if (enableDrift && Application.isPlaying)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.2f);
            Gizmos.DrawWireSphere(_startPosition, driftRadius);
        }
    }
}
