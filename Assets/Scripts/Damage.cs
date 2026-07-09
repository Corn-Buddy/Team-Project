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

    [Header("General Settings")]
    public damageType type;
    public Rigidbody rb;
    public int damageAmount;
    public bool destroyOnHit;
    public bool canPierce;

    [Header("Projectile Settings")]
    public int bulletSpeed;
    public float bulletDestroyTime;

    [Header("Time / Cooldown Settings")]
    public float damageRate;

    [Header("Melee Settings")]
    public Transform pivot;
    public float rotationSpeed = 10f;
    public float recoilAngle = 45f;

    [Header("Ranged Weapon Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public bool targetAtPosition;

    [Header("DOT / Cloud Settings")]
    [Tooltip("PoisonCloud only: controls both the SphereCollider radius and the visual disc scale.")]
    public float cloudRadius = 3f;

    [Header("Target")]
    public string targetTag = "Enemy";

    // Internal state
    float actionTimer;
    bool isRecoiling;
    Quaternion targetRotation;
    HashSet<Collider> targetsInRange = new HashSet<Collider>();
    Dictionary<Collider, float> dotTimers = new Dictionary<Collider, float>();
    SphereCollider detectionSphere;  // cached for range validation

    void Start()
    {
        if (type == damageType.bullet)
        {
            if (rb != null) rb.linearVelocity = transform.forward * bulletSpeed;
            if (bulletDestroyTime > 0) Destroy(gameObject, bulletDestroyTime);
        }
        else if (type == damageType.AOE)
        {
            Destroy(gameObject, bulletDestroyTime > 0 ? bulletDestroyTime : 0.2f);
        }

        if (pivot == null) pivot = transform;
        targetRotation = pivot.rotation;
        detectionSphere = GetComponent<SphereCollider>();

        // DOT: sync SphereCollider and visual disc to cloudRadius
        if (type == damageType.DOT)
        {
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc != null) sc.radius = cloudRadius;

            // Find the CloudRing child and scale it to match
            Transform ring = transform.Find("CloudRing");
            if (ring != null)
                ring.localScale = new Vector3(cloudRadius * 2f, 0.05f, cloudRadius * 2f);
        }
    }

    void Update()
    {
        if (gamemanager.instance != null && gamemanager.instance.isGameOver) return;

        // Clean up destroyed enemies
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

        // Validate the enemy is actually within the detection sphere radius
        // This guards against stale entries when the enemy collider is a trigger
        // (OnTriggerExit does not fire reliably for trigger-vs-trigger contacts)
        if (detectionSphere != null)
        {
            float dist = Vector3.Distance(transform.position, other.transform.position);
            // Subtract the enemy's own collider radius so damage triggers when
            // the enemy's edge enters the sphere, not just their center point
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

        // Keep melee and rangedWeapon sets populated continuously
        if (type == damageType.melee || type == damageType.rangedWeapon)
        {
            targetsInRange.Add(other);
            return;
        }

        // DOT ticking
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

            // Deal damage once aimed within 15 degrees and cooldown is ready
            float angle = Vector3.Angle(pivot.forward, dir);
            if (angle < 15f && actionTimer >= damageRate)
            {
                actionTimer = 0;
                IDamage dmg = closest.GetComponent<IDamage>();
                if (dmg != null)
                    dmg.takeDamage(damageAmount);

                StartCoroutine(MeleeRecoilRoutine());
            }
        }
        else
        {
            // Snap back to recoil target rotation
            pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
        }
    }

    IEnumerator MeleeRecoilRoutine()
    {
        isRecoiling = true;
        targetRotation = pivot.rotation * Quaternion.Euler(0, -recoilAngle, 0);
        yield return new WaitForSeconds(0.15f);
        isRecoiling = false;
    }

    // ── RANGED LOGIC ─────────────────────────────────────────────────────────

    void FireRangedWeapon()
    {
        if (projectilePrefab == null) return;
        Collider target = GetClosestEnemy();
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
}
