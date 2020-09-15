using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Compass : MonoBehaviour
{
    public Transform toFollow;

    private void Update()
    {
        transform.LookAt(toFollow);
    }
}
