using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private MeshRenderer mesh;
    [SerializeField] private float health;
    [SerializeField] private float speed;
    [SerializeField] private float slipperyForce;
    [SerializeField] private float airControl;
    [SerializeField] private float maxVelocity;
    [SerializeField] private float fallingForce;
    [SerializeField] private Material[] materials; // 0 blue 1 red;

    public static float currentHealth;

    private Color currentColor;
    private int colorIndex;
    private float movement;
    private bool onGround;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = health;
        currentColor = materials[0].color;
    }

    // Update is called once per frame
    void Update()
    {
        movement = Input.GetAxisRaw("Horizontal");

        if(Input.GetKeyDown(KeyCode.Space))
        {
            SwitchColor();
            FlipBoard();
        }
    }

    void FixedUpdate()
    {
        if(onGround)
        {
            AddForceOnGround();
            AddForceOnMovement(1f);
        }
        else
        {
            AddForceOnMovement(airControl);
            AddFallingForce();
        }

        ClampVelocity();
    }

    void AddForceOnMovement(float f)
    {
        Vector3 forceToAdd = ((0.75f * transform.right) + (0.25f * Vector3.right)) * movement * speed * f;
        rb.AddForce(forceToAdd);
    }

    void AddFallingForce()
    {
        rb.AddForce(Vector3.down * fallingForce);
    }

    void AddForceOnGround()
    {
        float alpha = Mathf.Sin(Mathf.Deg2Rad * transform.rotation.eulerAngles.z);
        rb.AddForce(transform.right * -alpha * slipperyForce);
    }

    void ClampVelocity()
    {
        if(rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity *= 0.9f;
        }
    }

    void FlipBoard()
    {
        float z = transform.rotation.eulerAngles.z;
        if (z < 140f || z > 220f) return;

        rb.AddForce(transform.up * -1f * 80f);
        StopAllCoroutines();
        StartCoroutine(FlipBoardCoroutine());
    }

    IEnumerator FlipBoardCoroutine()
    {
        float R = transform.rotation.eulerAngles.z;
        float flipR = R + 180f;
        float dt = 0;
        while(dt <= 1)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Lerp(R, flipR, dt)));
            dt += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void SwitchColor()
    {
        colorIndex = colorIndex == 0 ? 1 : 0;
        currentColor = materials[colorIndex].color;
        mesh.material = materials[colorIndex];
    }

    void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnTriggerStay(Collider collider)
    {
        Obstacle obs = collider.GetComponent<Obstacle>();
        if(obs != null && obs.CurrentColor != currentColor)
        {
            TakeDamage(1);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if(collision.collider.CompareTag("Floor"))
        {
            onGround = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Floor"))
        {
            onGround = false;
        }
    }
}
