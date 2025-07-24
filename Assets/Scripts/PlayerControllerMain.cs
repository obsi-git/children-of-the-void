using UnityEngine;
using System.Collections;
public class PlayerControllerMain : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    bool facingRight = true;
    public float killHeight = -64f;

    [Header("Movement")]
    public float moveSpeed = 7f;
    float HorizontalMovement;

    [Header("Jumping")]
    public float jumpForce = 10f;
    public float lowJumpMultiplier = 6f;
    public float fallMultiplier = 7f;
    bool isGrounded;
    bool isJumping;

    [Header("GroundCheck")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.3f);
    public LayerMask groundLayer;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallGravityMult = 2f;

    [Header("WallCheck")]
    public Transform wallCheckRight;
    public Transform wallCheckLeft;
    public Vector2 wallCheckSize = new Vector2(0.49f, 0.3f);
    public LayerMask wallLayer;
    public float wallRaycastDistance = 0.6f;
    bool isTouchingWall;

    [Header("WallMovement")]
    public float wallSlideSpeed = 2f;
    bool isWallSliding;
    bool wasWallSliding = false;

    [Header("WallJump")]
    public float wallJumpForceX = 12f;
    public float wallJumpForceY = 9f;
    public float wallJumpDuration = 0.3f;

    private bool isWallJumping = false;
    private float wallJumpTimer = 0f;

    [Header("Attack")]
    public float attackDuration = 0.2f;
    public bool isAttacking;
    public Vector2 attackHitboxSize = new Vector2(1.5f, 1f);
    public float attackRangeOffset = 1f;
    public int baseAttackDamage = 2;
    public float voidTier1Multiplier = 1.5f;
    public float voidTier2Multiplier = 2f;

    [Header("DodgeRoll")]
    public float RollSpeed = 12f;
    public float RollDuration = 0.35f;
    public float RollCooldown = 0.8f;
    public float RollTimer = 0f;
    public float RollCooldownTimer = 0;
    public bool isRolling = false;
    public bool canRoll = true;
    public bool isInvincible = false;

    [Header("Dash")]
    public float DashSpeed = 20f;
    public float DashDuration = 0.1f;
    public float DashCooldown = 1.2f;
    public float DashTimer = 0f;
    public float DashCooldownTimer = 0f;
    public bool canDash = true;
    public bool isDashing = false;
    [SerializeField]
    private TrailRenderer DashTrail;

    [Header("AttackCombo")]
    public float ComboResetTime = 0.5f;
    private int ComboStep = 0;
    private float LastAttackTime;

    [Header("Health")]
    public int maxHealth = 5;
    public int currentHealth;
    public int healingPotions = 2;
    public int heal = 2;

    [Header("Respawning")]
    public Vector2 spawnpoint;
    public float respawnDelay = 1.2f;
    private bool isDead = false;

    [Header("HurtFlash")]
    public float flashDuration = 0.2f;
    private float hurtFlashTimer = 0f;
    private bool isHurt = false;
    private Color originalColor;
    public float hurtDuration = 0.2f;
    private float hurtTimer = 0f;

    [Header("VoidEnergy")]
    public int currentVoidEnergy = 0;
    public int maxVoidEnergy = 10;
    public int voidEnergyPerHit = 1;
    public int voidTier1Threshold = 5;
    public int voidTier2Threshold = 10;

    [Header("Debug")]
    public bool showGizmos = true;
    public Color groundCheckColor = Color.green;
    public Color wallCheckColor = Color.blue;
    public Color wallRaycastColor = Color.red;
    public Color attackHitboxColor = Color.red;
    public Color playerBoundsColor = Color.yellow;

    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform attackHitbox;
    private Vector2 wallCheckOriginalOffset;
    private Vector2 attackHitboxOriginalOffset;

    private Vector3 originalScale; // Store the original scale

    void Awake()
    {
        // Store the original local positions
        wallCheckOriginalOffset = wallCheck.localPosition;
        attackHitboxOriginalOffset = attackHitbox.localPosition;
        originalScale = transform.localScale; // Save the initial scale
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        spawnpoint = transform.position;
        originalColor = sr.color;
    }

    void Update()
    {
        if (transform.position.y < killHeight)
        {
            Die();
        }
        if (hurtTimer > 0)
        {
            hurtTimer -= Time.deltaTime;
            return;
        }
        ProcessGravity();
        Move();
        Jump();
        GroundCheck();
        WallCheck();
        ProcessWallSlide();
        Flip();
        Attack();
        WallJump();
        Roll();
        Dash();
        Heal();
        anim.SetFloat("Speed", Mathf.Abs(HorizontalMovement));
        anim.SetBool("isWallSliding", isWallSliding);
        anim.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        anim.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
        if(!isWallSliding) sr.flipX = !facingRight;
    }

    public void Move()
    {
        if (isHurt || isDead || hurtTimer > 0) return;
        HorizontalMovement = Input.GetAxis("Horizontal");
        if (!isRolling && !isDashing) rb.linearVelocity = new Vector2(HorizontalMovement * moveSpeed, rb.linearVelocity.y);
    }

    private void ProcessGravity()
    {
        if (isDashing) rb.gravityScale = 0f;
        if (isWallSliding)
        {
            rb.gravityScale = baseGravity;
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    public void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
        }
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f && !Input.GetKey(KeyCode.Space))
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
            }
            else if (rb.linearVelocity.y < -0.1f)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
            }
        }
    }

    public void Attack()
    {
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        // First click → start combo
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
            {
                ComboStep = 1;
                anim.SetTrigger("attack");
                isAttacking = true;
                LastAttackTime = Time.time;
                StartCoroutine(DoAttack());
                Debug.Log("Attack1 triggered");
            }
            // Second click → combo to second attack
            else if (isAttacking && ComboStep == 1 && Time.time - LastAttackTime <= ComboResetTime && info.IsName("attack"))
            {
                ComboStep = 2;
                anim.ResetTrigger("attack");
                anim.SetTrigger("attack2");
                LastAttackTime = Time.time;
                StartCoroutine(DoAttack());
                Debug.Log("Attack2 triggered");
            }
        }

        // Reset states based on current animation
        if (ComboStep == 1 && !info.IsName("attack"))
        {
            isAttacking = false;
        }
        else if (ComboStep == 2 && !info.IsName("attack2"))
        {
            isAttacking = false;
            ComboStep = 0;
        }

        // Combo expired
        if (isAttacking && Time.time - LastAttackTime > ComboResetTime)
        {
            isAttacking = false;
            ComboStep = 0;
            Debug.Log("Combo reset");
        }
    }

    IEnumerator DoAttack()
    {
        // Calculate hitbox center based on facing direction
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(facingRight ? attackRangeOffset : -attackRangeOffset, 0);
        Collider2D[] EnemiesHit = Physics2D.OverlapBoxAll(
            attackCenter,
            attackHitboxSize,
            0f
        );

        foreach (var enemy in EnemiesHit)
        {
            Debug.Log($"Player attack hit: {enemy.name} (tag: {enemy.tag})");
            if (enemy.CompareTag("Enemy"))
            {
                EnemyBase enemyScript = enemy.GetComponent<EnemyBase>();
                if (enemyScript != null)
                {
                    int tier = getVoidTier();
                    float multiplier = (tier == 1) ? 1.5f : (tier == 2) ? 2f : 1f;
                    int finalDamage = Mathf.RoundToInt(2 * multiplier);

                    Vector2 hitDirection = enemy.transform.position - transform.position;
                    enemyScript.TakeDamage(finalDamage, hitDirection);

                    GainVoid();
                }
            }
            // Also allow damaging the boss (BossController)
            BossController bossScript = enemy.GetComponent<BossController>();
            if (bossScript != null)
            {
                int tier = getVoidTier();
                float multiplier = (tier == 1) ? 1.5f : (tier == 2) ? 2f : 1f;
                int finalDamage = Mathf.RoundToInt(2 * multiplier);
                bossScript.TakeDamage(finalDamage);
                Debug.Log($"Player dealt {finalDamage} damage to boss: {enemy.name}");
            }
        }

        yield return new WaitForSeconds(attackDuration);
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (isHurt || isDead) return;

        int tier = getVoidTier();
        float multiplier = (tier == 1) ? 1.5f : (tier == 2) ? 2f : 1f;
        currentHealth -= Mathf.RoundToInt(damage * multiplier);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // stop existing movement
            rb.linearVelocity = knockback;    // launch player back
            anim.SetTrigger("hurt");
            StartCoroutine(HurtFlash());
            hurtTimer = hurtDuration;
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

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        anim.SetTrigger("die");
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnpoint;
        rb.gravityScale = baseGravity;
        currentHealth = maxHealth;
        anim.ResetTrigger("die");
        anim.Play("Idle");
        isDead = false;
    }

    void Heal()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (healingPotions > 0 && currentHealth < maxHealth && !isDead)
            {
                currentHealth = Mathf.Min(currentHealth + heal, maxHealth);
                healingPotions--;
                Debug.Log("Healed! Health: " + currentHealth);
            }
        }
    }

    private void GroundCheck()
    {
        if (Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        if (isGrounded) isJumping = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = groundCheckColor;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        
        // Wall checks
        Gizmos.color = wallCheckColor;
        if (wallCheckRight != null)
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
        if (wallCheckLeft != null)
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
            
        // Wall raycast visualization
        Gizmos.color = wallRaycastColor;
        float direction = facingRight ? 1f : -1f;
        Vector2 origin = transform.position;
        Vector2 rayDirection = new Vector2(direction, 0f);
        Gizmos.DrawRay(origin, rayDirection * wallRaycastDistance);
        
        // Attack hitbox
        if (attackHitboxSize != Vector2.zero)
        {
            Gizmos.color = attackHitboxColor;
            Gizmos.DrawWireCube(transform.position + (Vector3)attackHitboxSize * 0.5f, attackHitboxSize);
        }
        
        // Player bounds
        Gizmos.color = playerBoundsColor;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>()?.bounds.size ?? Vector3.one);
    }

    private void ProcessWallSlide()
    {
         if (!isGrounded && WallCheck() && HorizontalMovement != 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));

            // Flip sprite to face opposite of wall
            sr.flipX = facingRight ? true : false;
        }
        
        else
        {
            isWallSliding = false;
        }
    }

    private bool WallCheck()
    {
        float direction = facingRight ? 1f : -1f;
        Vector2 origin = transform.position;
        Vector2 rayDirection = new Vector2(direction, 0f);

        RaycastHit2D hit = Physics2D.Raycast(origin, rayDirection, 0.6f, wallLayer);
        isTouchingWall = hit.collider != null;
        return isTouchingWall;
    }
    private void WallJump()
    {
        if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            isWallJumping = true;
            wallJumpTimer = wallJumpDuration;

            facingRight = !facingRight;
            sr.flipX = !facingRight;
            float direction = facingRight ? 1 : -1;
            rb.linearVelocity = new Vector2(wallJumpForceX * direction, wallJumpForceY);
        }

        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }
    }

    private void Flip()
    {
        if (!isWallSliding && HorizontalMovement != 0)
        {
            bool shouldFaceRight = HorizontalMovement > 0;
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                sr.flipX = !facingRight;
                wallCheck.transform.localScale = new Vector2(wallCheck.transform.localScale.x * -1, wallCheck.transform.localScale.y);
                attackHitbox.transform.localScale = new Vector2(attackHitbox.transform.localScale.x * -1, attackHitbox.transform.localScale.y);
            }
        }

    }

    private void Roll()
    {
        if (!canRoll)
        {
            RollCooldownTimer -= Time.deltaTime;
            if (RollCooldownTimer <= 0f)
            {
                canRoll = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isRolling && canRoll && !isAttacking && isGrounded)
        {
            isRolling = true;
            canRoll = false;
            isInvincible = true;
            RollTimer = RollDuration;
            RollCooldownTimer = RollCooldown;

            anim.SetTrigger("roll");
            // Uncomment later on to make enemies not collide with player
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
        }

        if (isRolling)
        {
            RollTimer -= Time.deltaTime;
            float direction = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(RollSpeed * direction, 0);

            if (RollTimer <= 0)
            {
                isRolling = false;
                isInvincible = false;

                //Uncomment to enable collisions again
                Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
            }
        }
    }

    private void Dash()
    {
        if (!canDash)
        {
            DashCooldownTimer -= Time.deltaTime;
            if (DashCooldownTimer <= 0f)
            {
                canDash = true;
            }
        }

        if (Input.GetMouseButtonDown(1) && canDash && !isDashing && !isAttacking && !isDashing)
        {
            isDashing = true;
            canDash = false;
            DashTimer = DashDuration;
            DashCooldownTimer = DashCooldown;

            DashTrail.emitting = true;
            anim.SetTrigger("dash");
        }

        if (isDashing)
        {
            DashTimer -= Time.deltaTime;
            float direction = facingRight ? 1 : -1;
            rb.linearVelocity = new Vector2(direction * DashSpeed, 0);
            if (DashTimer <= 0)
            {
                isDashing = false;
                DashTrail.emitting = false;
                anim.ResetTrigger("dash");
            }
        }
    }

    public void ResetAfterRespawn()
    {
        rb.gravityScale = baseGravity;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentHealth = maxHealth;
        isDead = false;
        anim.ResetTrigger("die");
        anim.Play("Idle");
    }

    public int getVoidTier()
    {
        if (currentVoidEnergy >= 10) return 2;
        else if (currentVoidEnergy >= 5) return 1;
        else return 0;
    }

    public void GainVoid()
    {
        currentVoidEnergy = Mathf.Min(currentVoidEnergy + 1, maxVoidEnergy);
    }

}

