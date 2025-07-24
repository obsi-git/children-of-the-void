using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float maxMoveSpeed = 5f;
    private bool facingRight = true;
    private bool canMove = true;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.3f);
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Attack")]
    public float meleeCooldown = 3.5f;
    private float lastMeleeTime = -10f;
    private bool isAttacking = false;
    public int meleeDamage = 2;
    public Vector2 meleeHitboxSize = new Vector2(1.5f, 1f);
    public float meleeRangeOffset = 1f;
    public float meleeAttackDelay = 0.5f;
    public float meleeAttackDuration = 1.0f;
    private bool hasDealtDamage = false; // Track if damage was dealt in current attack

    [Header("Projectile Attack")]
    public GameObject bossProjectilePrefab;
    public Transform projectileSpawnPoint;
    public float rangedCooldown = 8f;
    private float lastRangedTime = -10f;
    public float rangedAttackRange = 4f;

    [Header("Health")]
    public int maxHealth = 20;
    private int currentHealth;
    private bool isDead = false;
    public float flashDuration = 0.2f;
    private bool isHurt = false;
    private Color originalColor;

    [Header("AI")]
    public float detectionRange = 10f;
    public float meleeRange = 2.5f;
    private bool playerInRange = false;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactDamageCooldown = 1.0f;
    private float lastContactDamageTime = -10f;

    [Header("Jump Attack")]
    public float jumpForce = 16f;
    public float jumpAttackCooldown = 6f;
    private float lastJumpAttackTime = -10f;
    public float jumpAttackRange = 7f;
    private bool isJumpAttacking = false;
    public LayerMask environmentLayer;

     [Header("Respawn")]
    public Vector3 originalSpawnPosition; // Assign in Inspector or set in Start()
    private bool isRespawning = false;

    void Start()
    {
        originalSpawnPosition = transform.position; // Save initial spawn position
        InitializeComponents();
        InitializeHealth();
    }

    // Call this method to respawn the boss
    public void RespawnBoss()
    {
        if (isRespawning) return;

        isRespawning = true;
        isDead = false;
        currentHealth = maxHealth;
        transform.position = originalSpawnPosition;

        // Reset animations/state
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // Re-enable components
        GetComponent<Collider2D>().enabled = true;
        sr.enabled = true;
        rb.simulated = true;

        isRespawning = false;
        Debug.Log("Boss respawned!");
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }
    }

    void InitializeHealth()
    {
        currentHealth = maxHealth;
        originalColor = sr.color;
    }

    void Update()
    {
        if (isDead || player == null) return;

        CheckPlayerInRange();
        GroundCheck();

        if (!isAttacking && !isHurt && canMove && !isJumpAttacking)
        {
            HandleMovement();
            TryMelee();
            TryRanged();
            TryJumpAttack();
        }
        else if (isAttacking)
        {
            // Completely stop movement during attack
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void CheckPlayerInRange()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(player.position, transform.position);
        playerInRange = distanceToPlayer <= detectionRange;
    }

    void GroundCheck()
    {
        if (groundCheckPos != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
        }
    }

    void HandleMovement()
    {
        if (!playerInRange || player == null || isAttacking) return;

        float directionToPlayer = player.position.x - transform.position.x;
        float distanceToPlayer = Mathf.Abs(directionToPlayer);

        // Move towards player if not in melee range
        if (distanceToPlayer > meleeRange && distanceToPlayer < detectionRange)
        {
            float targetVelocity = moveSpeed * Mathf.Sign(directionToPlayer);
            rb.linearVelocity = new Vector2(targetVelocity, rb.linearVelocity.y);
            
            // Update animation
            if (anim != null)
            {
                anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            }

            // Flip sprite based on movement direction
            if (directionToPlayer > 0 && !facingRight)
            {
                Flip();
            }
            else if (directionToPlayer < 0 && facingRight)
            {
                Flip();
            }
        }
        else
        {
            // Stop moving when in melee range or too far
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null)
            {
                anim.SetFloat("Speed", 0);
            }
        }
    }

    void TryMelee()
    {
        if (Time.time - lastMeleeTime >= meleeCooldown && IsPlayerInMeleeRange() && !isAttacking)
        {
            StartCoroutine(PerformMeleeAttack());
        }
    }

    IEnumerator PerformMeleeAttack()
    {
        isAttacking = true;
        hasDealtDamage = false;
        lastMeleeTime = Time.time;
        
        if (anim != null)
        {
            anim.SetTrigger("attack");
        }

        // Stop movement during attack
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Wait for attack to land (sync with animation swing)
        yield return new WaitForSeconds(meleeAttackDelay);

        // Deal damage only once per attack
        if (!hasDealtDamage)
        {
            DealMeleeDamage();
        }

        // Wait for the rest of the attack animation to finish
        yield return new WaitForSeconds(meleeAttackDuration - meleeAttackDelay);
        
        isAttacking = false;
        hasDealtDamage = false;
    }

    void DealMeleeDamage()
    {
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(facingRight ? meleeRangeOffset : -meleeRangeOffset, 0);
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, meleeHitboxSize, 0f);

        Debug.Log($"Boss melee attack - Found {hits.Length} colliders in hitbox");
        
        foreach (var hit in hits)
        {
            Debug.Log($"Hit object: {hit.name} with tag: {hit.tag}");
            
            if (hit.CompareTag("Player"))
            {
                Debug.Log("Player hit detected!");
                Vector2 knockback = (hit.transform.position - transform.position).normalized * 8f;
                Debug.Log($"Calculated knockback: {knockback}");
                
                var playerController = hit.GetComponent<PlayerControllerMain>();
                if (playerController != null)
                {
                    Debug.Log("PlayerControllerMain found, calling TakeDamage");
                    playerController.TakeDamage(meleeDamage, knockback);
                    hasDealtDamage = true; // Mark that damage was dealt
                    Debug.Log($"Boss dealt {meleeDamage} damage to player");
                    break; // Only hit once per attack
                }
                else
                {
                    Debug.LogError("PlayerControllerMain component not found on player!");
                }
            }
        }
    }

    void TryRanged()
    {
        if (Time.time - lastRangedTime >= rangedCooldown && IsPlayerInRangedRange() && !isAttacking)
        {
            lastRangedTime = Time.time;
            
            if (anim != null)
            {
                anim.SetTrigger("ranged");
            }
            
            if (bossProjectilePrefab != null && projectileSpawnPoint != null)
            {
                GameObject projectile = Instantiate(bossProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
                Debug.Log("Projectile instantiated: " + projectile.name, projectile);
            }
        }
    }

    bool IsPlayerInMeleeRange()
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, transform.position) < meleeRange;
    }

    bool IsPlayerInRangedRange()
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, transform.position) > rangedAttackRange;
    }

    void TryJumpAttack()
    {
        if (Time.time - lastJumpAttackTime >= jumpAttackCooldown && playerInRange && IsPlayerInJumpAttackRange() && isGrounded)
        {
            StartCoroutine(PerformJumpAttack());
        }
    }

    bool IsPlayerInJumpAttackRange()
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, transform.position) <= jumpAttackRange;
    }

    IEnumerator PerformJumpAttack()
    {
        isJumpAttacking = true;
        lastJumpAttackTime = Time.time;
        canMove = false;
        // Ignore collisions with environment
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(environmentLayer), true);
        if (anim != null)
        {
            anim.SetTrigger("jumpAttack");
        }
        // Calculate jump direction
        Vector2 jumpTarget = player.position;
        Vector2 jumpDir = (jumpTarget - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(jumpDir.x * jumpForce, jumpForce);
        // Wait until boss lands
        yield return new WaitUntil(() => isGrounded);
        // Restore collisions
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(environmentLayer), false);
        isJumpAttacking = false;
        canMove = true;
    }

    // Helper to convert LayerMask to layer number
    int LayerMaskToLayer(LayerMask mask)
    {
        int layerNumber = 0;
        int maskValue = mask.value;
        while (maskValue > 1)
        {
            maskValue = maskValue >> 1;
            layerNumber++;
        }
        return layerNumber;
    }

    public void TakeDamage(int damage)
    {
        if (isHurt || isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go below 0

        Debug.Log($"Boss took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            Debug.Log("Boss defeated!");
        }
        else
        {
            StartCoroutine(HurtFlash());
        }
    }

    IEnumerator HurtFlash()
    {
        isHurt = true;
        sr.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
        isHurt = false;
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        
        if (anim != null)
        {
            anim.SetTrigger("die");
        }
        
        // Destroy the boss GameObject after a short delay to allow death animation
        StartCoroutine(DestroyAfterDelay(2.0f));
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void Flip()
    {
        facingRight = !facingRight;
        sr.flipX = !sr.flipX;
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Melee attack range
        Gizmos.color = Color.red;
        Vector2 meleeCenter = (Vector2)transform.position + new Vector2(facingRight ? meleeRangeOffset : -meleeRangeOffset, 0);
        Gizmos.DrawWireCube(meleeCenter, meleeHitboxSize);

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Melee range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Ranged attack range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
    }

    // Always visible gizmos for easier editing
    void OnDrawGizmos()
    {
        // Ground check
        if (groundCheckPos != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        }

        // Projectile spawn point
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
        }

        // Detection range (always visible)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Melee range (always visible)
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Semi-transparent blue
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Ranged attack range (always visible)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
    }

    // Public methods for external control
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // During jump attack, always deal contact damage (ignore cooldown)
            if (isJumpAttacking)
            {
                var playerController = collision.gameObject.GetComponent<PlayerControllerMain>();
                if (playerController != null && !playerController.isInvincible)
                {
                    Vector2 knockback = (collision.transform.position - transform.position).normalized * 12f;
                    playerController.TakeDamage(contactDamage, knockback);
                    Debug.Log($"Boss jump attack contact damaged player for {contactDamage}");
                }
            }
            else if (Time.time - lastContactDamageTime >= contactDamageCooldown)
            {
                lastContactDamageTime = Time.time;
                var playerController = collision.gameObject.GetComponent<PlayerControllerMain>();
                if (playerController != null && !playerController.isInvincible)
                {
                    // Calculate knockback direction
                    Vector2 knockback = (collision.transform.position - transform.position).normalized * 8f;
                    playerController.TakeDamage(contactDamage, knockback);
                    Debug.Log($"Boss contact damaged player for {contactDamage}");
                }
            }
        }
    }
}