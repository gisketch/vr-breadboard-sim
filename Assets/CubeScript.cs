using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeScript : MonoBehaviour
{
    public void MoveCube()
    {
        transform.position += new Vector3(0, 1, 0);
    }

    public void HoverCube()
    {
        transform.localScale += new Vector3(0.1f, 0.1f, 0.1f);
    }
}
