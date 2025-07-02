using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public int maxHealth = 5;
    protected int currentHealth;
    protected Rigidbody2D rb;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float knockbackForce = 10f;


    protected virtual void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void TakeDamage(int amount, Vector2 knockback)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage!");

        rb.AddForce(knockback.normalized * 5f, ForceMode2D.Impulse);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // base behavior — can be extended or replaced
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerControllerMain player = collision.collider.GetComponent<PlayerControllerMain>();
        if (player != null && !player.isInvincible)
        {
            Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
            player.TakeDamage(contactDamage, knockbackDir * knockbackForce);
        }
    }
}
