using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStatus))]
public class Player : MonoBehaviour
{
    public bool allowMove = true;
    new private Rigidbody2D rigidbody;
    public Animator animator;
    public Transform visualRoot;
    private float inputX, inputY;
    private float facingX = 1f;
    private Vector3 offset;

    //冲刺计算属性
    private float dashTimer = 0.0f;

    void Awake()
    {
        offset = Camera.main.transform.position - transform.position; 
        rigidbody = GetComponent<Rigidbody2D>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (visualRoot == null)
        {
            visualRoot = transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (dashTimer > 0 || !allowMove) return;
        if (Input.GetMouseButtonDown(1))
        {
            Vector2 dashDir = Vector2.zero;

            Camera cam = Camera.main;
            Plane plane = new Plane(Vector3.forward, transform.position);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                dashDir = (hit - transform.position).normalized;
            }
            else throw new Exception("错误的方向");

            rigidbody.velocity = dashDir * PlayerStatus.Instance.dashSpeed;
            dashTimer = PlayerStatus.Instance.dashTime;

            return;
        }

        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        if (inputX < -0.01f)
        {
            facingX = -1f;
        }
        else if (inputX > 0.01f)
        {
            facingX = 1f;
        }
        if (visualRoot != null)
        {
            Vector3 scale = visualRoot.localScale;
            float sign = facingX < 0f ? -1f : 1f;
            scale.x = Mathf.Abs(scale.x) * sign;
            visualRoot.localScale = scale;
        }
        Vector2 input = (inputX * transform.right + inputY * transform.up).normalized;
        rigidbody.velocity = input * PlayerStatus.Instance.GetMoveSpeed();
        if (animator != null)
        {
            if (input != Vector2.zero)
            {
                animator.SetBool("IsMoving", true);
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }
            animator.SetFloat("InputX", facingX);
            animator.SetFloat("InputY", inputY);
        }
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position = transform.position + offset;
        }
    }

    private void FixedUpdate()
    {
        if (dashTimer > 0) dashTimer -= Time.fixedDeltaTime;
    }

    public void StopMove()
    {
        allowMove = false;
        dashTimer = 0.0f;
        rigidbody.velocity = Vector2.zero;
    }

    public void StartMove()
    {
        allowMove = true;
    }
}
