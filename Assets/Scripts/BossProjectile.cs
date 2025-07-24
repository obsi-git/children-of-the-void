using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public float lifetime = 5f;
    public LayerMask collisionLayers; // Assign: Player, Ground, Walls
    public GameObject explosionEffect; // Optional particle effect
    public Animator explosionAnimator; // Optional Animator
    public string explosionTrigger = "Explode"; // Animation trigger name

    private Rigidbody2D rb;
    private Transform player;
    private bool isExploding = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("Rigidbody2D missing!", this);

        // Find player
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) Debug.LogError("Player not found!", this);

        // Set initial velocity toward player
        if (player != null && rb != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }

        Destroy(gameObject, lifetime); // Auto-destroy after delay
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Skip if already exploding or not a valid collision
        if (isExploding || ((1 << other.gameObject.layer) & collisionLayers.value) == 0) return;

        Explode();
    }

    void Explode()
    {
        isExploding = true;
        rb.linearVelocity = Vector2.zero; // Stop moving

        // Play explosion animation (if assigned)
        if (explosionAnimator != null)
            explosionAnimator.SetTrigger(explosionTrigger);

        // Spawn explosion effect (if assigned)
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Damage player if in explosion radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f, collisionLayers);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<PlayerControllerMain>()?.TakeDamage(damage, Vector2.zero);
                break; // Only damage once
            }
        }

        Destroy(gameObject, 0.5f); // Destroy after animation
    }

    // Visualize explosion radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
