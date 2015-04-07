using UnityEngine;
using System.Collections;

public class JointTracker : MonoBehaviour
{
    /* Place member variables here*/

    ProjectileDragging pDrag;
    Vector3 thumb_pos;
    Vector3 index_pos;
    float pinch_dist;
    bool isPinching = false;
    bool hasLaunched = false;

    void Start()
    {
        pDrag = gameObject.GetComponent<ProjectileDragging>();

        /*Do Initialization here*/
    }

    void Update()
    {
        /* Do processing here*/
    }

    private void TrackJoints(PXCMHandData handOutput)
    {
        if (hasLaunched) return;

        /*Do hand and joint query here*/
    }

    void OnDisable()
    {
        /* Do disposal here*/
    }

    // This is used to pinch and drag the projectile
    void HandlePinchResult(float val)
    {
        if (pinch_dist < 0.05f)
        {
            isPinching = true;
            if (pDrag != null)
            {
                pDrag.PinchDragging(index_pos);
            }
        }

        if (pinch_dist > 0.09f && isPinching)
        {
            if (pDrag.spring != null)
            {
                isPinching = false;
                hasLaunched = true;
                pDrag.spring.enabled = true;
                GetComponent<Rigidbody2D>().isKinematic = false;
            }
        }
    }
}