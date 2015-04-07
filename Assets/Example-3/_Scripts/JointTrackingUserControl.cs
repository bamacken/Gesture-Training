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
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Aeroplane;

[RequireComponent(typeof (AeroplaneController))]
public class JointTrackingUserControl : MonoBehaviour 
{
	#region Unity Aeroplane
	// these max angles are only used on mobile, due to the way pitch and roll input are handled
	public float maxRollAngle = 80;
	public float maxPitchAngle = 80;
	
	// reference to the aeroplane that we're controlling
	private AeroplaneController m_Aeroplane;
	#endregion

	#region RealSense
	/// The Hand Module interface instance
	public PXCMHandModule hand = null;
	
	/// The hand data interface instance
	public PXCMHandData hand_data = null;

	/// The hand data structure
	PXCMHandData.IHand[] handData;

	/// The hand configuration instance
	private PXCMHandConfiguration hcfg;

	/// The joint data instance for the first hand.
	PXCMHandData.JointData JointData_1;

	/// The joint data instance for the second hand.
	PXCMHandData.JointData JointData_2;

	/// The pitch neutral zone position "y"
	private float pitchNeutral = 0f;

	/// The number of hands being tracked
	private int numHands = 0;
	#endregion


    PXCMSenseManager sm = null;
	// Use this for initialization
	void Start () 
	{
		// Set up the reference to the aeroplane controller.
		m_Aeroplane = GetComponent<AeroplaneController>();

		/* Initialize a PXCMSenseManager instance */
		sm = PXCMSenseManager.CreateInstance();
		if (sm != null)
		{
			/* Enable hand tracking and configure the hand module */
			pxcmStatus sts = sm.EnableHand();
			if(sts == pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
				/*init hand data structure*/
				handData = new PXCMHandData.IHand[2];

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

	private void FixedUpdate()
	{
		if(numHands == 2)
		{
			handData[0].QueryNormalizedJoint(PXCMHandData.JointType.JOINT_CENTER, out JointData_1);
			handData[1].QueryNormalizedJoint(PXCMHandData.JointType.JOINT_CENTER, out JointData_2);

			// Pitch center
			if(pitchNeutral == 0) { pitchNeutral = JointData_1.positionWorld.z; }
			//Debug.Log("pitchNeutral = " + pitchNeutral);

			// Roll
			float roll = CompareFloats(JointData_1.positionWorld.y, JointData_2.positionWorld.y, 0.04f);
			//Debug.Log("roll = " + roll);

			// Yaw
			float yaw = CompareFloats(JointData_1.positionWorld.z, JointData_2.positionWorld.z, 0.04f) * -1;
			//Debug.Log("pitch = " + pitch);

			// Break and Pitch
			float pitch = 0;
			bool airBrakes = false;

			// Break and Pitch are on the same axis "z", so handle them together
			if(Mathf.Abs(JointData_1.positionWorld.z - JointData_2.positionWorld.z) < 0.03f) 
			{
				pitch = CompareFloats(pitchNeutral, JointData_2.positionWorld.z, 0.05f) * -1;
				Debug.Log ("pitch = " + pitch);
				airBrakes = (JointData_1.positionWorld.z < 0.5f) ? false : true;
			}

			// Throttle 
			float throttle = airBrakes ? -1 : 1;

			//Debug.Log(CompareFloats(JointData_1.positionWorld.y, JointData_2.positionWorld.y, 0.03f));
			m_Aeroplane.Move(roll, pitch, yaw, throttle, airBrakes);
		}
	}

	/// <summary>
	/// Compare two points and determine differences threshold check
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	float CompareFloats(float a, float b, float thres)
	{
		if (Mathf.Abs (a - b) > thres)
			return (a > b) ? -1 : 1;

		return 0;
	}

	/* Displaying current frames hand joints */
	private void TrackJoints(PXCMHandData handOutput)
	{
		numHands = handOutput.QueryNumberOfHands ();
		for(int i = 0; i < numHands; i++)
		{
			//Get hand by time of appearence
			if (handOutput.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, i, out handData[i]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
			}
		}
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



