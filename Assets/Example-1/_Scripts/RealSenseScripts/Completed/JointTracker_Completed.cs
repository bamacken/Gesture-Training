/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using UnityEngine;
using System.Collections;

public class JointTracker_Completed : MonoBehaviour 
{
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

	/* joint / pinch specific goodness*/
	ProjectileDragging pDrag;
	Vector3 thumb_pos;
	Vector3 index_pos;
	float pinch_dist;
	bool isPinching = false;
	bool hasLaunched = false;


    PXCMSenseManager sm = null;
	// Use this for initialization
	void Start () 
	{
		pDrag = gameObject.GetComponent<ProjectileDragging> ();
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
					pDrag.UpdateDrag(isPinching);
				}
				/* Now, release the current frame so we can process the next frame */
				sm.ReleaseFrame();
			}
		}
	}

	/* Displaying current frames hand joints */
	private void TrackJoints(PXCMHandData handOutput)
	{
		if (hasLaunched)return;
		
		//Get hand by time of appearence
		if (handOutput.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			handData.QueryTrackedJoint(PXCMHandData.JointType.JOINT_THUMB_TIP, out ThumbJointData);
			handData.QueryTrackedJoint(PXCMHandData.JointType.JOINT_INDEX_TIP, out IndexJointData);
			
			/* get joint distance */
			thumb_pos = new Vector3(ThumbJointData.positionWorld.x, ThumbJointData.positionWorld.y, ThumbJointData.positionWorld.z);
			index_pos = new Vector3(IndexJointData.positionWorld.x, IndexJointData.positionWorld.y, IndexJointData.positionWorld.z);

			pinch_dist = Vector3.Distance(index_pos, thumb_pos);

			/* Update game based on pinch result*/
			HandlePinchResult(pinch_dist);
		}
	}

	void OnFiredAlert(PXCMHandData.AlertData data)
	{
		if(data.label.ToString() == "ALERT_HAND_CALIBRATED")
		{
			Debug.Log(data.label.ToString ());
		}
	}

	void HandlePinchResult(float val)
	{
		if(pinch_dist < 0.05f)
		{
			isPinching = true;
			if(pDrag != null)
			{
				pDrag.PinchDragging(index_pos);
			}
		}
		
		if(pinch_dist > 0.09f && isPinching)
		{
			if(pDrag.spring != null)
			{
				isPinching = false;
				hasLaunched = true;
				pDrag.spring.enabled = true;
				GetComponent<Rigidbody2D>().isKinematic = false;
			}
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



