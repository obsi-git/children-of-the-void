using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpForce = 8f;
    private bool facingRight = true;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.3f);
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Attack")]
    public GameObject meleeHitbox;
    public float meleeCooldown = 2f;
    private float lastMeleeTime;

    [Header("Projectile Attack")]
    public GameObject explosionProjectilePrefab;
    public Transform projectileSpawnPoint;
    public float rangedCooldown = 4f;
    private float lastRangedTime;

    [Header("Health")]
    public int maxHealth = 20;
    private int currentHealth;
    public float flashDuration = 0.2f;
    private bool isHurt = false;
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        currentHealth = maxHealth;
        originalColor = sr.color;
        lastMeleeTime = -meleeCooldown;
        lastRangedTime = -rangedCooldown;
    }

    void Update()
    {
        GroundCheck();
        MoveTowardsPlayer();

        if (Time.time - lastMeleeTime >= meleeCooldown && IsPlayerInMeleeRange())
            MeleeAttack();

        if (Time.time - lastRangedTime >= rangedCooldown && IsPlayerInRangedRange())
            FireProjectile();
    }

    private void GroundCheck()
    {
        isGrounded = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
    }

    private void MoveTowardsPlayer()
    {
        if (!isGrounded) return;

        float dir = player.position.x - transform.position.x;
        if (Mathf.Abs(dir) > 1f)
        {
            rb.linearVelocity = new Vector2(moveSpeed * Mathf.Sign(dir), rb.linearVelocity.y);
            if (dir > 0 && !facingRight) Flip();
            else if (dir < 0 && facingRight) Flip();

            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            anim.SetFloat("Speed", 0);
        }
    }

    private bool IsPlayerInMeleeRange()
    {
        return Vector2.Distance(player.position, transform.position) < 2.5f;
    }

    private bool IsPlayerInRangedRange()
    {
        return Vector2.Distance(player.position, transform.position) > 3f;
    }

    private void MeleeAttack()
    {
        lastMeleeTime = Time.time;
        anim.SetTrigger("attack");
        Instantiate(meleeHitbox, transform.position + Vector3.right * (facingRight ? 1 : -1), Quaternion.identity);
    }

    private void FireProjectile()
    {
        lastRangedTime = Time.time;
        anim.SetTrigger("ranged");
        Instantiate(explosionProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
    }

    public void TakeDamage(int damage)
    {
        if (isHurt) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
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

    private void Die()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("die");
        this.enabled = false;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        sr.flipX = !sr.flipX;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
