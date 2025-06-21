using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    public float lowJumpMultiplier = 6f;
    public float fallMultiplier = 7f;
    private Rigidbody2D rb;
    private bool isGrounded;

    private SpriteRenderer sr;

    private Animator anim;

    public bool isJumping;
    public bool isFalling;

    public GameObject attackHitbox;

    public float attackDuration = 0.05f;

    private bool isAttacking = false;
    private float attackTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        attackHitbox = transform.Find("AttackHitbox").gameObject;
        attackHitbox.SetActive(false);
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }

        if (rb.linearVelocity.y > 0.1f && !Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y < -0.1f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;

        }

        if (moveX > 0)
        {
            sr.flipX = false;
        }
        else if (moveX < 0)
        {
            sr.flipX = true;
        }

        anim.SetFloat("Speed", Mathf.Abs(moveX)); // For idle/run
        anim.SetBool("isJumping", rb.linearVelocity.y > 0.1f && !isGrounded);
        anim.SetBool("isFalling", rb.linearVelocity.y < -0.1f && !isGrounded);

        

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            isAttacking = true;
            anim.ResetTrigger("attack");
            anim.SetTrigger("attack");
            attackHitbox.SetActive(true);
        }

        // If no longer in Attack animation, reset
        if (isAttacking && !stateInfo.IsName("Attack"))
        {
            isAttacking = false;
            attackHitbox.SetActive(false);
        }


    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    


}
