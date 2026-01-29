using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed;
    new private Rigidbody2D rigidbody;
    private Animator animator;
    private float inputX, inputY;
    private Vector3 offset;
    void Start()
    {
        offset = Camera.main.transform.position - transform.position; 
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        Vector2 input = (inputX * transform.right + inputY * transform.up).normalized;
        rigidbody.velocity = input * speed; // Unity 2022: Rigidbody2D uses velocity (linearVelocity is Unity 6+).
        if (input != Vector2.zero)
        {
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
        animator.SetFloat("InputX", inputX);
        animator.SetFloat("InputY", inputY);
        Camera.main.transform.position = transform.position + offset;
    }
}
