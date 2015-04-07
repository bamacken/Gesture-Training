using System;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Aeroplane;

[RequireComponent(typeof (AeroplaneController))]
class MultiBlobUserControl : MonoBehaviour
{
    #region Vectrosity
    /*Vectrosity line drawing utils*/
    VectorLine[] blobLine;
    VectorPoints blobPoints;
    List<Vector2> blobPointsPos;
    /* Used to store blob and contour data*/
    PXCMPointI32[][] pointOuter;
    #endregion

    #region RealSense
    PXCMBlobData blobData = null;
    PXCMBlobConfiguration blobConfiguration = null;
    PXCMSession session = null;
    PXCMSenseManager instance = null;
    int _maxBlobToShow = 2;
	Vector3[] centerPoints; 
	int numblobs = 0;
    #endregion

	#region Unity Aeroplane
	// these max angles are only used on mobile, due to the way pitch and roll input are handled
	public float maxRollAngle = 80;
	public float maxPitchAngle = 80;
	
	// reference to the aeroplane that we're controlling
	private AeroplaneController m_Aeroplane;
    #endregion
	
	
	private void Awake()
	{
		centerPoints = new Vector3[2];
		// Set up the reference to the aeroplane controller.
		m_Aeroplane = GetComponent<AeroplaneController>();
	}

    /// <summary>
    /// Init the pipeline
    /// </summary>
    void Start()
    {
        blobLine = new VectorLine[_maxBlobToShow];
        pointOuter = new PXCMPointI32[_maxBlobToShow][];
        
        SimplePipeline();
    }


	private void FixedUpdate()
	{
		// We have our "controller", let's process the input
		if(numblobs == 2)
		{
			centerPoints[0].Normalize();
			centerPoints[1].Normalize();
			
			// Read input for the pitch, yaw, roll and throttle of the aeroplane.
			float roll = CompareFloats(centerPoints[0].z, centerPoints[1].z);
			float pitch = CompareFloats(centerPoints[0].y, centerPoints[1].y);
			bool airBrakes = CrossPlatformInputManager.GetButton("Fire1");
			
			Debug.Log("Roll = " + roll);
			//Debug.Log("Pitch = " + pitch);
			//Debug.Log("process input");
			
			//Debug.Log ("roll = " + roll);
			//Debug.Log ("pitch = " + pitch);
			// auto throttle up, or down if braking.
			float throttle = airBrakes ? -1 : 1;
			#if MOBILE_INPUT
			AdjustInputForMobileControls(ref roll, ref pitch, ref throttle);
			#endif
			
			// Pass the input to the aeroplane
			m_Aeroplane.Move(roll, pitch, 0, throttle, airBrakes);
		}
		/*
		// Read input for the pitch, yaw, roll and throttle of the aeroplane.
		float roll = CrossPlatformInputManager.GetAxis("Horizontal");
		float pitch = CrossPlatformInputManager.GetAxis("Vertical");
		bool airBrakes = CrossPlatformInputManager.GetButton("Fire1");

		//Debug.Log ("roll = " + roll);
		//Debug.Log ("pitch = " + pitch);
		// auto throttle up, or down if braking.
		float throttle = airBrakes ? -1 : 1;
		#if MOBILE_INPUT
		AdjustInputForMobileControls(ref roll, ref pitch, ref throttle);
		#endif
		// Pass the input to the aeroplane
		m_Aeroplane.Move(roll, pitch, 0, throttle, airBrakes);
		*/
	}


    /// <summary>
    /// Grab the blob data every frame
    /// </summary>
	void Update()
    {
        if (instance.AcquireFrame(false) == pxcmStatus.PXCM_STATUS_NO_ERROR)
        {
            /* To store all blob points */
            blobPointsPos = new List<Vector2>();

            PXCMCapture.Sample sample = instance.QuerySample();
            if (sample != null && sample.depth != null)
            {
                if (blobData != null)
                {
                    blobData.Update();
                    numblobs = blobData.QueryNumberOfBlobs();

					if(numblobs == 2)
					{
						PXCMBlobData.IBlob[] pBlob = new PXCMBlobData.IBlob[2];
						if (blobData.QueryBlobByAccessOrder(0, PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, out pBlob[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                    {
	                        centerPoints[0] = pBlob[0].QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CENTER);
	                        blobPointsPos.Add(new Vector2(centerPoints[0].x * 1,  centerPoints[0].y * 1));

							if(pBlob[0].QueryContourPoints(0, out pointOuter[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
							{
								DisplayContour(pointOuter[0], 0, numblobs);
							}
	                    }

						if (blobData.QueryBlobByAccessOrder(1, PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, out pBlob[1]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
						{
							centerPoints[1] = pBlob[1].QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CENTER);
							blobPointsPos.Add(new Vector2(centerPoints[1].x * 1,  centerPoints[1].y * 1));
							
							if(pBlob[1].QueryContourPoints(1, out pointOuter[1]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
							{
								DisplayContour(pointOuter[1], 0, numblobs);
							}
						}
					}
                }
            }
            instance.ReleaseFrame();
        }
    }


    /// <summary>
    /// Compare two points and determine differences threshold check
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    float CompareFloats(float a, float b)
    {
        float c = (a > b) ? -(a - b) : (b - a);
        return c;
    }

    /// <summary>
    /// Using PXCMSenseManager to handle data
    /// </summary>
    public void SimplePipeline()
    {
        session = PXCMSession.CreateInstance();
        if(session != null)
        {
            instance = session.CreateSenseManager();
            if (instance == null)
            {
                Debug.Log("Create SenseManager Failure");
                return;
            }
            pxcmStatus status = instance.EnableBlob();
            PXCMBlobModule blobModule = instance.QueryBlob();

            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR || blobModule == null)
            {
                Debug.Log("Failed Loading Module");
                return;
            }

            blobConfiguration = blobModule.CreateActiveConfiguration();
            blobData = blobModule.CreateOutput();

            if (blobConfiguration != null)
            {
                blobConfiguration.SetSegmentationSmoothing(1.0f);
                //blobConfiguration.SetMaxDistance(550);
                blobConfiguration.SetMaxObjectDepth(100);
                blobConfiguration.SetMaxBlobs(_maxBlobToShow);
                blobConfiguration.SetContourSmoothing(1.0f);
                blobConfiguration.EnableContourExtraction(true);
                blobConfiguration.EnableSegmentationImage(true);
                blobConfiguration.ApplyChanges();
            }
            if (blobData == null)
            {
                Debug.Log("Failed Create Output");
                return;
            }

            PXCMSenseManager.Handler handler = new PXCMSenseManager.Handler();
            handler.onModuleProcessedFrame = new PXCMSenseManager.Handler.OnModuleProcessedFrameDelegate(OnNewFrame);
            
            if (instance.Init(handler) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                PXCMCapture.DeviceInfo dinfo;
                instance.QueryCaptureManager().QueryDevice().QueryDeviceInfo(out dinfo);

                if (dinfo != null && dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM)
                {
                    instance.QueryCaptureManager().QueryDevice().SetMirrorMode(PXCMCapture.Device.MirrorMode.MIRROR_MODE_DISABLED);
                }
                /* Set the depth stream confidence threshold value - Any depth pixels with a confidence score below the threshold will be set to the low confidence pixel value*/
                instance.QueryCaptureManager().QueryDevice().SetDepthConfidenceThreshold(10);
                /* Set the smoothing aggressiveness parameter - High smoothing effect for distances between 850mm to 1000mm bringing good accuracy with moderate sharpness level.*/
                instance.QueryCaptureManager().QueryDevice().SetIVCAMFilterOption(6);  
            }
        }
        else
        {
            Debug.Log("Init Failed");
        }
    }

    

    /// <summary>
    /// OneNewframe
    /// </summary>
    /// <param name="mid"></param>
    /// <param name="module"></param>
    /// <param name="sample"></param>
    /// <returns></returns>
    public pxcmStatus OnNewFrame(Int32 mid, PXCMBase module, PXCMCapture.Sample sample)
    {
        return pxcmStatus.PXCM_STATUS_NO_ERROR;
    }

    /// <summary>
    /// DisplayPoints - Display all blob points
    /// </summary>
    public void DisplayPoints()
    {
        VectorLine.SetCamera();

        // If we already have a blobpoint object with an array of points, simply update the points. Otherwise, create the blobpoint object
        if (blobPoints != null)
            blobPoints.points2 = blobPointsPos.ToArray();
        else
            blobPoints = new VectorPoints("BlobExtremityPoints", blobPointsPos.ToArray(), Color.green, null, 5f);

        //draw the points
        blobPoints.Draw();
    }

    /// <summary>
    /// DisplayContour - Display the contour points, adjust camera position, invert points for correct display.
    /// </summary>
    /// <param name="contour"></param>
    /// <param name="blobNumber"></param>
    public void DisplayContour(PXCMPointI32[] contour, int blobNumber, int blobCount)
    {
        /* Funky Vectrosity camera flip issue*/
        VectorPoints.SetCamera();
        Camera cam = VectorLine.GetCamera();
        cam.transform.position = new Vector3(cam.transform.position.x * -1, cam.transform.position.y * -1, cam.transform.position.z);

		// remove the unsed blob
		if(blobCount < blobLine.Length)
			VectorLine.Destroy(ref blobLine[1]);

		//refresh the current blob
		VectorLine.Destroy(ref blobLine[blobNumber]);

        /* can't be cache since the contour length changes based on what is tracked*/
        Vector2[] splinePoints = new Vector2[contour.Length];
        for (int i = 0; i < contour.Length; i++)
        {
            splinePoints[i] = new Vector2(contour[i].x * -1, contour[i].y * -1);
        }

		blobLine[blobNumber] = new VectorLine("BlobContourPoints", new Vector2[splinePoints.Length], null, 2.0f, LineType.Continuous);
		blobLine[blobNumber].name = "blobLine_" + blobNumber.ToString (); 
        blobLine[blobNumber].MakeSpline(splinePoints);
        blobLine[blobNumber].Draw();
    }

    /// <summary>
    /// dispose of the rs instances
    /// </summary>
    void OnDisable()
    {
        if (blobPoints != null)
            VectorPoints.Destroy(ref blobPoints);

        // Clean Up
        if (blobData != null)
        {
            blobData.Dispose();
        }
        if (blobConfiguration != null)
        {
            blobConfiguration.Dispose();
        }

        if (instance != null)
        {
            instance.Close();
            instance.Dispose();
        }
    }
}
