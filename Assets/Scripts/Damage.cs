using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Damage : MonoBehaviour
{
    public enum damageType
    {
        bullet,         // Moves forward, hits on enter, optionally pierces
        stationary,     // Hits once on enter (spike trap)
        DOT,            // Ticks damage repeatedly while in trigger (poison cloud, lava)
        melee,          // Persistent, rotates to nearest enemy, hits on cooldown with recoil
        AOE,            // Spawned at position, hits all inside instantly, destroys itself
        rangedWeapon    // Detection sphere, fires projectilePrefab on cooldown
    }

    // ── General ──────────────────────────────────────────────────────────────
    [Header("General Settings")]
    public damageType type;
    public Rigidbody rb;
    [Tooltip("Base damage dealt per hit or tick.")]
    public int damageAmount;
    public bool destroyOnHit;
    public bool canPierce;
    [Tooltip("Tag used to identify valid targets.")]
    public string targetTag = "Enemy";

    // ── Projectile ────────────────────────────────────────────────────────────
    [Header("Projectile / Bullet Settings")]
    [Tooltip("Speed the bullet travels forward (units/sec).")]
    public float bulletSpeed;
    [Tooltip("Seconds before the bullet is auto-destroyed. 0 = never.")]
    public float bulletDestroyTime;

    // ── Cooldown ──────────────────────────────────────────────────────────────
    [Header("Cooldown / Rate Settings")]
    [Tooltip("Seconds between attacks or DOT ticks.")]
    public float damageRate;

    // ── Melee ─────────────────────────────────────────────────────────────────
    [Header("Melee Settings")]
    public Transform pivot;
    [Tooltip("How fast the blade rotates toward the target (deg/sec multiplier).")]
    public float rotationSpeed;
    [Tooltip("Degrees the blade snaps back after each hit.")]
    public float recoilAngle;
    [Tooltip("Angle threshold (degrees) within which the blade can deal damage.")]
    public float meleeHitAngle;
    [Tooltip("Seconds the blade pauses during recoil before re-aiming.")]
    public float recoilDuration;

    // ── Ranged Weapon ─────────────────────────────────────────────────────────
    [Header("Ranged Weapon Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    [Tooltip("If true, spawns the projectile at the target's position (AOE). If false, fires toward the target.")]
    public bool targetAtPosition;

    // ── Detection / Range ─────────────────────────────────────────────────────
    [Header("Detection / Range Settings")]
    [Tooltip("Radius of the SphereCollider used to detect enemies. " +
             "Changing this at runtime also updates the collider and (for DOT) the visual ring.")]
    public float detectionRadius;

    // ── AOE Blast ─────────────────────────────────────────────────────────────
    [Header("AOE Blast Settings")]
    [Tooltip("Radius of the AOE blast sphere collider. " +
             "Changing this at runtime also updates the collider and the visual sphere scale.")]
    public float aoeBlastRadius;

    // ── Internal state ────────────────────────────────────────────────────────
    float actionTimer;
    bool isRecoiling;
    Quaternion targetRotation;
    HashSet<Collider> targetsInRange = new HashSet<Collider>();
    Dictionary<Collider, float> dotTimers = new Dictionary<Collider, float>();
    SphereCollider detectionSphere;

    // ── Bounce power-up ───────────────────────────────────────────────────────
    bool bounceMode = false;
    float bounceAttackRate;

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC UPGRADE API
    // Call these from any buff/upgrade/power-up script to modify weapon stats
    // at runtime without needing a direct reference to a specific weapon class.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Apply a stat upgrade to this weapon. Pass the stat name and new value.
    /// Supported keys: "damage", "rate", "speed", "range", "blastRadius",
    ///                 "piercing", "rotationSpeed", "recoilAngle"
    /// </summary>
    public void ApplyUpgrade(string stat, float value)
    {
        switch (stat.ToLower())
        {
            case "damage":
                damageAmount = Mathf.RoundToInt(value);
                break;
            case "rate":
                damageRate = value;
                break;
            case "speed":
                bulletSpeed = value;
                if (rb != null && type == damageType.bullet)
                    rb.linearVelocity = transform.forward * bulletSpeed;
                break;
            case "range":
                detectionRadius = value;
                ApplyDetectionRadius();
                break;
            case "blastradius":
                aoeBlastRadius = value;
                ApplyAOERadius();
                break;
            case "piercing":
                canPierce = value > 0f;
                break;
            case "rotationspeed":
                rotationSpeed = value;
                break;
            case "recoilangle":
                recoilAngle = value;
                break;
        }
    }

    /// <summary>Enable bounce mode on the StormBlade (hits chain between enemies).</summary>
    public void EnableBounceMode(float newBounceRate = 0.1f)
    {
        bounceMode = true;
        bounceAttackRate = newBounceRate;
    }

    /// <summary>Disable bounce mode.</summary>
    public void DisableBounceMode() { bounceMode = false; }

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (type == damageType.bullet)
        {
            if (rb != null) rb.linearVelocity = transform.forward * bulletSpeed;
            if (bulletDestroyTime > 0) Destroy(gameObject, bulletDestroyTime);
        }
        else if (type == damageType.AOE)
        {
            // Use Physics.OverlapSphere for exact radius control independent of
            // collider or transform scale. Fire immediately then self-destruct.
            ApplyAOERadius();
            Collider[] hits = Physics.OverlapSphere(transform.position, aoeBlastRadius);
            foreach (Collider hit in hits)
            {
                if (hit.isTrigger) continue;
                if (!hit.CompareTag(targetTag)) continue;
                IDamage dmg = hit.GetComponent<IDamage>();
                if (dmg != null) dmg.takeDamage(damageAmount);
            }
            Destroy(gameObject, bulletDestroyTime > 0 ? bulletDestroyTime : 0.5f);
        }

        if (pivot == null) pivot = transform;
        targetRotation = pivot.rotation;

        detectionSphere = GetComponent<SphereCollider>();
        if (detectionSphere != null)
        {
            detectionSphere.radius = detectionRadius;
        }

        // DOT: sync visual ring to detectionRadius
        if (type == damageType.DOT)
            ApplyDetectionRadius();
    }

    void Update()
    {
        if (gamemanager.instance != null && gamemanager.instance.isGameOver) return;

        targetsInRange.RemoveWhere(e => e == null);
        actionTimer += Time.deltaTime;

        if (type == damageType.melee && targetsInRange.Count > 0)
            UpdateMelee();
        else if (type == damageType.rangedWeapon && targetsInRange.Count > 0 && actionTimer >= damageRate)
        {
            actionTimer = 0;
            FireRangedWeapon();
        }
    }

    // ── TRIGGER CALLBACKS ────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        switch (type)
        {
            case damageType.bullet:
            case damageType.stationary:
            case damageType.AOE:
                IDamage dmg = other.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(damageAmount);
                    if (type == damageType.bullet && !canPierce && destroyOnHit)
                        Destroy(gameObject);
                }
                break;

            case damageType.melee:
            case damageType.rangedWeapon:
                if (other.CompareTag(targetTag))
                    targetsInRange.Add(other);
                break;

            case damageType.DOT:
                if (other.CompareTag(targetTag))
                {
                    targetsInRange.Add(other);
                    if (!dotTimers.ContainsKey(other))
                        dotTimers[other] = 0f;
                }
                break;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        // Validate the enemy is actually within the detection sphere radius.
        // Guards against stale entries when OnTriggerExit doesn't fire reliably.
        if (detectionSphere != null)
        {
            float dist = Vector3.Distance(transform.position, other.transform.position);
            float enemyRadius = 0f;
            CapsuleCollider cc = other.GetComponent<CapsuleCollider>();
            if (cc != null) enemyRadius = cc.radius;
            else { SphereCollider sc2 = other.GetComponent<SphereCollider>(); if (sc2 != null) enemyRadius = sc2.radius; }

            float effectiveRange = detectionSphere.radius * transform.lossyScale.x + enemyRadius;
            if (dist > effectiveRange)
            {
                targetsInRange.Remove(other);
                dotTimers.Remove(other);
                return;
            }
        }

        if (type == damageType.melee || type == damageType.rangedWeapon)
        {
            targetsInRange.Add(other);
            return;
        }

        if (type == damageType.DOT)
        {
            targetsInRange.Add(other);
            if (!dotTimers.ContainsKey(other))
                dotTimers[other] = 0f;

            if (Time.time >= dotTimers[other])
            {
                IDamage dmg = other.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(damageAmount);
                    dotTimers[other] = Time.time + damageRate;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            targetsInRange.Remove(other);
            dotTimers.Remove(other);
        }
    }

    // ── MELEE LOGIC ──────────────────────────────────────────────────────────

    void UpdateMelee()
    {
        if (!isRecoiling)
        {
            Collider closest = GetClosestEnemy();
            if (closest == null) return;

            Vector3 dir = closest.transform.position - pivot.position;
            dir.y = 0;
            if (dir == Vector3.zero) return;

            targetRotation = Quaternion.LookRotation(dir);
            pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            float angle = Vector3.Angle(pivot.forward, dir);
            if (angle < meleeHitAngle && actionTimer >= damageRate)
            {
                actionTimer = 0;
                IDamage dmg = closest.GetComponent<IDamage>();
                if (dmg != null)
                    dmg.takeDamage(damageAmount);

                StartCoroutine(MeleeRecoilRoutine(bounceMode ? GetNextEnemy(closest) : null));
            }
        }
        else
        {
            pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
        }
    }

    IEnumerator MeleeRecoilRoutine(Collider bounceTarget)
    {
        isRecoiling = true;

        if (bounceMode && bounceTarget != null)
        {
            // Aim recoil directly at the next enemy for chaining
            Vector3 dir = bounceTarget.transform.position - pivot.position;
            dir.y = 0;
            targetRotation = dir != Vector3.zero ? Quaternion.LookRotation(dir) : pivot.rotation * Quaternion.Euler(0, -recoilAngle, 0);
            actionTimer = damageRate - bounceAttackRate;
        }
        else
        {
            targetRotation = pivot.rotation * Quaternion.Euler(0, -recoilAngle, 0);
        }

        yield return new WaitForSeconds(recoilDuration);
        isRecoiling = false;
    }

    // ── RANGED LOGIC ─────────────────────────────────────────────────────────

    void FireRangedWeapon()
    {
        if (projectilePrefab == null) return;
        Collider target = targetAtPosition ? GetHighestHPEnemy() : GetClosestEnemy();
        if (target == null) return;

        if (targetAtPosition)
        {
            Instantiate(projectilePrefab, target.transform.position, Quaternion.identity);
        }
        else
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector3 dir = target.transform.position - spawnPos;
            dir.y = 0;
            if (dir == Vector3.zero) dir = transform.forward;
            Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        }
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    /// <summary>Apply detectionRadius to the SphereCollider and DOT visual ring.</summary>
    void ApplyDetectionRadius()
    {
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null) sc.radius = detectionRadius;

        Transform ring = transform.Find("CloudRing");
        if (ring != null)
            ring.localScale = new Vector3(detectionRadius * 2f, 0.05f, detectionRadius * 2f);
    }

    /// <summary>Scale the BlastSphere visual child to match aoeBlastRadius.
    /// Creates the child programmatically if it doesn't already exist in the prefab.
    /// </summary>
    void ApplyAOERadius()
    {
        Transform visual = transform.Find("BlastSphere");

        // Create the visual sphere at runtime if it wasn't in the prefab
        if (visual == null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BlastSphere";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localPosition = Vector3.zero;

            // Remove the sphere's own collider so it doesn't interfere with physics
            Collider sc = sphere.GetComponent<Collider>();
            if (sc != null) Destroy(sc);

            // Apply a semi-transparent ice-blue material
            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Standard"));

                // URP transparency — must set ALL of these or it renders opaque
                mat.SetFloat("_Surface", 1f);                    // 0=Opaque, 1=Transparent
                mat.SetFloat("_Blend", 0f);                      // 0=Alpha, 1=Premultiply
                mat.SetFloat("_ZWrite", 0f);                     // disable depth write for transparency
                mat.SetFloat("_AlphaClip", 0f);                  // no alpha clipping
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // In URP _BaseColor carries the alpha; mat.color does NOT work
                mat.SetColor("_BaseColor", new Color(0.5f, 0.85f, 1f, 0.25f)); // ice blue, 25% opacity
                rend.material = mat;
            }

            visual = sphere.transform;
        }

        float d = aoeBlastRadius * 2f;
        visual.localScale = new Vector3(d, d, d);
    }

    Collider GetClosestEnemy()
    {
        Collider closest = null;
        float minD = float.MaxValue;
        foreach (var enemy in targetsInRange)
        {
            if (enemy == null) continue;
            float d = Vector3.Distance(transform.position, enemy.transform.position);
            if (d < minD) { minD = d; closest = enemy; }
        }
        return closest;
    }

    Collider GetNextEnemy(Collider exclude)
    {
        Collider best = null;
        float minD = float.MaxValue;
        foreach (var enemy in targetsInRange)
        {
            if (enemy == null || enemy == exclude) continue;
            float d = Vector3.Distance(transform.position, enemy.transform.position);
            if (d < minD) { minD = d; best = enemy; }
        }
        return best;
    }

    Collider GetHighestHPEnemy()
    {
        Collider best = null;
        float maxHP = float.MinValue;
        foreach (var enemy in targetsInRange)
        {
            if (enemy == null) continue;
            IDamage dmg = enemy.GetComponent<IDamage>();
            float hp = 0;
            if (dmg != null)
            {
                var hpField = dmg.GetType().GetField("hp",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (hpField != null) hp = System.Convert.ToSingle(hpField.GetValue(dmg));
            }
            if (hp > maxHP) { maxHP = hp; best = enemy; }
        }
        return best ?? GetClosestEnemy();
    }
}
