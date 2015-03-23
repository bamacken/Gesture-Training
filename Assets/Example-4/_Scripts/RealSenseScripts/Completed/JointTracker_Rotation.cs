/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using UnityEngine;
using System.Collections;

public class JointTracker_Rotation: MonoBehaviour 
{
    public enum RotationType
    {
        handleDelta,
        FinderDelta,
        Pinch
    }
    public RotationType rotationType;

    /// Pinch rotation
    float pinch_dist;
    bool isPinching = false;
    Vector3 pinchPos = Vector3.zero;

    ///The object to rotate
    public GameObject logo;

	/// The Sense Manager instance
	//public PXCMSenseManager sm = null;

	/// The Hand Module interface instance
	public PXCMHandModule hand = null;
	
	/// The hand data interface instance
	public PXCMHandData hand_data = null;

	/// The hand data structure
	PXCMHandData.IHand handData;

	/// The hand configuration instance
	private PXCMHandConfiguration hcfg;

	/// The joint data instance for the thumb.
	PXCMHandData.JointData ThumbJointData;

	/// The joint data instance for the index.
	PXCMHandData.JointData IndexJointData;

    /// The joint data instance for the palm center.
    PXCMHandData.JointData PalmCenterJointData;

    PXCMSenseManager sm = null;
	// Use this for initialization
	void Start () 
	{
        rotationType = RotationType.Pinch;

        //sm = PXCMSession.CreateInstance();
		/* Initialize a PXCMSenseManager instance */
		sm = PXCMSenseManager.CreateInstance();
		if (sm != null)
		{
			/* Enable hand tracking and configure the hand module */
			pxcmStatus sts = sm.EnableHand();
			if(sts == pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
				/* Hand module interface instance */
				hand = sm.QueryHand(); 
				/* Hand data interface instance */
				hand_data = hand.CreateOutput();

				// Create hand configuration instance and configure 
				hcfg = hand.CreateActiveConfiguration ();
				hcfg.EnableAllAlerts ();
				hcfg.SubscribeAlert(OnFiredAlert);
				hcfg.EnableNormalizedJoints(true);
				hcfg.ApplyChanges ();
				hcfg.Dispose ();

				/* Initialize the execution pipeline */ 
				if (sm.Init() != pxcmStatus.PXCM_STATUS_NO_ERROR) 
				{
					OnDisable();
				}
			}
		}
	}

	// Update is called once per frame
	void Update () 
	{
		if (sm != null)
		{
			/* Wait until any frame data is available */
			if (sm.AcquireFrame(false, 0) == pxcmStatus.PXCM_STATUS_NO_ERROR) 
			{
				/* Retrieve latest hand data, only update slingshot if ready */
				if(hand_data.Update() == pxcmStatus.PXCM_STATUS_NO_ERROR);
				{
					TrackJoints(hand_data);
				}
				/* Now, release the current frame so we can process the next frame */
				sm.ReleaseFrame();
			}
		}
	}

	/* Displaying current frames hand joints */
	private void TrackJoints(PXCMHandData handOutput)
	{
		//Get hand by time of appearence
		if (handOutput.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			handData.QueryTrackedJoint(PXCMHandData.JointType.JOINT_THUMB_TIP, out ThumbJointData);
            handData.QueryTrackedJoint(PXCMHandData.JointType.JOINT_INDEX_TIP, out IndexJointData);
            handData.QueryTrackedJoint(PXCMHandData.JointType.JOINT_CENTER, out PalmCenterJointData);
			
			/* get joint distance */
			Vector3 thumb = new Vector3(ThumbJointData.positionWorld.x, ThumbJointData.positionWorld.y, ThumbJointData.positionWorld.z);
            Vector3 index = new Vector3(IndexJointData.positionWorld.x, IndexJointData.positionWorld.y, IndexJointData.positionWorld.z);
            Vector3 palm = new Vector3(PalmCenterJointData.positionWorld.x, PalmCenterJointData.positionWorld.y, PalmCenterJointData.positionWorld.z);

            switch (rotationType)
            {
                case RotationType.FinderDelta:
                    //Rotate along y-axis
                    ///Compare the difference between the z values and determine if is great than the threshold
                    float diff = CompareFloats(index.z, palm.z);
                    if (Mathf.Abs(diff) > 0.02f)
                    {
                        //Debug.Log(diff.ToString());
                        //begin to rotate using the difference thershold as a multiplier for rotation speed.
                        logo.transform.Rotate(Time.deltaTime * (diff * 500), 0, 0);

                    }

                    //logo.transform.Rotate(Vector3.up * Time.deltaTime * (diff * 500));

                    //rotate along x-axis
                    diff = CompareFloats(thumb.z, palm.z);
                    if (Mathf.Abs(diff) > 0.02f)
                    {
                        Debug.Log(diff.ToString());
                        //begin to rotate using the difference thershold as a multiplier for rotation speed.
                        logo.transform.Rotate(0, Time.deltaTime * (diff * 500), 0);
                    }
                    break;
                case RotationType.handleDelta:
                    Debug.Log(index.ToString());
                    logo.transform.Rotate(index.y * Time.deltaTime * 750, index.x * Time.deltaTime * 750, 0);
                    break;
                case RotationType.Pinch:
                    pinch_dist = Vector3.Distance(index, thumb);
                    if (HandlePinchResult(pinch_dist))
                    {
                        //set the initial position
                        if(pinchPos == Vector3.zero)
                            pinchPos = index;

                        //get the vector between the initial position and the current index postion
                        Vector3 temp = pinchPos - index;
                        logo.transform.Rotate(-temp.y * Time.deltaTime * 750, -temp.x * Time.deltaTime * 750, -temp.z * Time.deltaTime * 750);
                    }

                    break;
                default:
                    Debug.Log("something went wrong");
                    break;
            }


            
		}
	}

    /// <summary>
    /// Handle pinch rotation
    /// </summary>
    /// <param name="val"></param>
    bool HandlePinchResult(float val)
    {
        if (pinch_dist < 0.05f)
        {
            isPinching = true;
            Debug.Log("pinching");

        }

        if (pinch_dist > 0.08f && isPinching)
        {
            isPinching = false;
            pinchPos = Vector3.zero;
            Debug.Log("pinching not");

        }

        return isPinching;
    }
    /// <summary>
    /// Compare two points and determine differences threshold check
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    float CompareFloats(float a, float b)
    {
        float c = (a > b) ? (a-b) : -(b-a);
        return c;
    }

	void OnFiredAlert(PXCMHandData.AlertData data)
	{
		if(data.label.ToString() == "ALERT_HAND_CALIBRATED")
		{
			Debug.Log(data.label.ToString ());
		}
	}

	void OnDisable() 
	{
		/* Dispose hand data instance*/ 
		if(hand_data != null)
		{
			hand_data.Dispose();
			hand_data = null;
		}
		
		/* Dispose hand module instance*/ 
		if(hand != null)
		{
			hand.Dispose ();
			hand = null;
		}
		
		/* Dispose sense manager instance*/ 
		if (sm != null)
		{
			sm.Dispose();
			sm = null;
		}
	}
}



