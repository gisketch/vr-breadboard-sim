using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCube : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float moveHorizontal = Input.GetAxis("MoveHorizontal");
        float moveVertical = Input.GetAxis("MoveVertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        transform.Translate(movement * Time.deltaTime * 5f);

        if (Input.GetKey("joystick button 0"))
        {
            transform.Rotate(new Vector3(0, 45, 0));
        }
        if (Input.GetKey("joystick button 1"))
        {
            transform.Translate(Vector3.up * 3f);
        }
        if (Input.GetKey("joystick button 2"))
        {
            transform.Rotate(Vector3.right * 3f);
        }
        if (Input.GetKey("joystick button 3"))
        {
            transform.localScale += Vector3.one * 3f;
        }

    }
}
