using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed; // Speed of the player.

    float hAxis; // Horizontal axis input.
    float vAxis; // Vertical axis input.
    bool wDown; // Boolean to check if the walk button is pressed.

    Vector3 moveVec; // Vector to store movement direction.

    Animator anim; // Reference to the Animator component.

    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        anim = GetComponentInChildren<Animator>(); // Get the Animator component from child objects.
    }

    // Update is called once per frame.
    void Update()
    {
        hAxis = Input.GetAxisRaw("Horizontal"); // Get horizontal input.
        vAxis = Input.GetAxisRaw("Vertical"); // Get vertical input.
        wDown = Input.GetButton("Walk"); // Check if the walk button is held down.

        // Calculate movement vector, normalize it to get a constant speed in all directions.
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        // Move the player by the movement vector, adjusted by speed and walk modifier, and delta time.
        transform.position += moveVec * speed * (wDown ? 0.2f : 1) * Time.deltaTime;

        // Set the "isRun" animation state based on if the movement vector is not zero.
        anim.SetBool("isRun", moveVec != Vector3.zero);

        // Set the "isWalk" animation state based on the wDown boolean.
        anim.SetBool("isWalk", wDown);

        // Rotate the player to face the direction of movement.
        if (moveVec != Vector3.zero)
        {
            transform.LookAt(transform.position + moveVec);
        }
    }
}