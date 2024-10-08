using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class decal_shift : MonoBehaviour
{
    public bool PressHereToMove = false;
    public float LookDistance = 10.0f;
    public float Bias = 0.05f;

    private void Update()
    {
        if (PressHereToMove)
        {
            DoMoveDecal();
            PressHereToMove = false;
        }
    }

    private void DoMoveDecal()
    {
        // тут ваши действия
        //Debug.Log("sss");
        // transform.Translate(Vector3.forward * 1.0f);

        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.TransformVector(Vector3.forward), out hit))
        {
            if (hit.distance < LookDistance)
                {
                transform.Translate(Vector3.forward * (hit.distance - Bias));
                //print("Found an object - distance: " + hit.distance);
            }
        }



    }
}