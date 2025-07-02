using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

public class PlayerControllerMain : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    bool facingRight = true;

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
    public GameObject attackHitbox;
    public float attackDuration = 0.2f;
    public bool isAttacking;

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
    private int currentHealth;
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

    [Header("VoidEnergy")]
    public int currentVoidEnergy = 0;
    public int maxVoidEnergy = 10;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);
        currentHealth = maxHealth;
        spawnpoint = transform.position;
        originalColor = sr.color;
    }

    void Update()
    {
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
        anim.SetFloat("Speed", Mathf.Abs(HorizontalMovement));
        anim.SetBool("isWallSliding", isWallSliding);
        anim.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        anim.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
    }

    public void Move()
    {
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
        attackHitbox.SetActive(true);

        Collider2D[] EnemiesHit = Physics2D.OverlapBoxAll(
            attackHitbox.transform.position,
            attackHitbox.GetComponent<BoxCollider2D>().size,
            0f
        );

        foreach (var enemy in EnemiesHit)
        {
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
        }

        yield return new WaitForSeconds(attackDuration);
        attackHitbox.SetActive(false);

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
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
            anim.SetTrigger("hurt");
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
        //groundcheck
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
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
            // Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(enemyLayer), true);
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
                //Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(enemyLayer), false);
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

