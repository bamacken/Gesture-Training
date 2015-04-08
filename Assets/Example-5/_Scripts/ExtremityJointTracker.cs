/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ExtremityJointTracker : MonoBehaviour 
{
	/// The text components that will display the onscreen positions
	public Text msg1;
	public Text msg2;
	public Text msg3;
	public Text msg4;
	public Text msg5;
	public Text msg6;

	/// The Hand Module interface instance
	public PXCMHandModule hand = null;
	
	/// The hand data interface instance
	public PXCMHandData hand_data = null;

	/// The hand data structure
	PXCMHandData.IHand handData;

	/// The hand configuration instance
	private PXCMHandConfiguration hcfg;

	/// The extremity poit data
	private PXCMHandData.ExtremityData extremityPoint;

    PXCMSenseManager sm = null;
	// Use this for initialization
	void Start () 
	{
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
				hcfg.SetTrackingMode(PXCMHandData.TrackingModeType.TRACKING_MODE_EXTREMITIES);
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
				/* Retrieve latest extremity data */
				if(hand_data.Update() == pxcmStatus.PXCM_STATUS_NO_ERROR);
				{
					TrackExtremity(hand_data);
					if (hand_data.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
					{
						if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_LEFTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
						Debug.Log("LeftMost Extremity Position = " + extremityPoint.pointWorld);
					}
				}
				/* Now, release the current frame so we can process the next frame */
				sm.ReleaseFrame();
			}
		}
	}

	/* Capture current frames extremity */
	private void TrackExtremity(PXCMHandData handOutput)
	{
		//Get hand by time of appearence
		if (handOutput.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_LEFTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				Debug.Log("LeftMost Extremity Position = " + extremityPoint.pointWorld);

			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_LEFTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg1.text = "Left Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_RIGHTMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg2.text = "Right Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_TOPMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg3.text = "Top Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_BOTTOMMOST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg4.text = "Bottom Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_CENTER, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg5.text = "Center Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";
			if(handData.QueryExtremityPoint(PXCMHandData.ExtremityType.EXTREMITY_CLOSEST, out extremityPoint) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				msg6.text = "Closest Extremity Point = Vector3(" + extremityPoint.pointWorld.x.ToString("F2") + ", " + extremityPoint.pointWorld.y.ToString("F2") + ", "+ extremityPoint.pointWorld.z.ToString("F2") + ")";

		}
	}

	void OnFiredAlert(PXCMHandData.AlertData data)
	{
		Debug.Log(data.label.ToString ());
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



