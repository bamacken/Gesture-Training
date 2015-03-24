using System;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace DrumBeat
{
    class MultiBlob : MonoBehaviour
    {
        //The audio clip array 
		float beatPlane = 0.80000001f; //arbitrary distace from camera
        bool beatLock = false; //the hand is on the drum or not
        bool beatPlayed = false; //the hand is on the drum or not
		float[][] FrameData;
		int frameCount = 0;
		float threshold = 0.10f;
		AudioSource audioSource;
        public AudioClip[] audioClips;
        private float velocity; // velocity of the hand
        public Vector3 previous; // previous hand position
        public Vector3 current; // current hand position

        public Vector3 topPoint;
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
        #endregion

		BeatNotify beatNotify;
        /// <summary>
        /// Init the pipeline
        /// </summary>
        void Start()
        {
			beatNotify = new BeatNotify ();
			beatNotify.MyProperty = 2;
			beatNotify.MyProperty = 3;
			beatNotify.MyProperty = 1;
			FrameData = new float[2][];
			FrameData[0] = new float[10];
			FrameData[1] = new float[10];

			audioSource = gameObject.GetComponent<AudioSource>();
            blobLine = new VectorLine[_maxBlobToShow];
            pointOuter = new PXCMPointI32[_maxBlobToShow][];
            
            SimplePipeline();
        }

        /// <summary>
        /// Grab the blob data every frame
        /// </summary>
        void Update()
        {
			if(Input.anyKeyDown)
			{
				audioSource.Play();
			}
            if (instance.AcquireFrame(true) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                /* To store all blob points */
                blobPointsPos = new List<Vector2>();

                PXCMCapture.Sample sample = instance.QuerySample();
                if (sample != null && sample.depth != null)
                {
                    PXCMImage.ImageInfo info = sample.depth.QueryInfo();
                    if (blobData != null)
                    {
                        blobData.Update();
                        int numblobs = blobData.QueryNumberOfBlobs();

                        for (int i = 0; i < numblobs; i++)
                        {
                            PXCMBlobData.IBlob pBlob;
                            if (blobData.QueryBlobByAccessOrder(i, PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, out pBlob) == pxcmStatus.PXCM_STATUS_NO_ERROR)
                            {
                                Vector3 centerPoint = pBlob.QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CENTER);
                                topPoint = pBlob.QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_TOP_MOST);
                                Vector3 bottomPoint = pBlob.QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_BOTTOM_MOST);
                                Vector3 leftPoint = pBlob.QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_LEFT_MOST);
                                Vector3 rightPoint = pBlob.QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_RIGHT_MOST);
                                Vector3 closestPoint = pBlob.QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CLOSEST);

                                blobPointsPos.Add(new Vector2(centerPoint.x * -1,  centerPoint.y * -1));
                                blobPointsPos.Add(new Vector2(topPoint.x * -1, topPoint.y * -1));
                                blobPointsPos.Add(new Vector2(bottomPoint.x * -1, bottomPoint.y * -1));
                                blobPointsPos.Add(new Vector2(leftPoint.x * -1, leftPoint.y * -1));
                                blobPointsPos.Add(new Vector2(rightPoint.x * -1, rightPoint.y * -1));
                                blobPointsPos.Add(new Vector2(closestPoint.x * -1, closestPoint.y * -1));
 
                                   

                                DisplayPoints();
                                if (pBlob.QueryContourPoints(0, out pointOuter[i]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
                                {
                                    DisplayContour(pointOuter[i], i);
                                }
                                
                            }
                        }
                    }
                }
                instance.ReleaseFrame();
            }

            Debug.Log(CompareFloats(topPoint.normalized.z, beatPlane).ToString());
            if (CompareFloats(topPoint.normalized.z, beatPlane) > 0.04f)
            {
                //Debug.Log("locked");
                if (!beatPlayed)
                {
                    beatPlayed = true;

                    audioSource.Play();
                }
            }
            else if (CompareFloats(topPoint.normalized.z, beatPlane) < 0.01f)
            {
                beatPlayed = false;
                //Debug.Log("unlocked");
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
                    blobConfiguration.SetMaxDistance(550);
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
        public void DisplayContour(PXCMPointI32[] contour, int blobNumber)
        {
            /* Funky Vectrosity camera flip issue*/
            VectorPoints.SetCamera();
            Camera cam = VectorLine.GetCamera();
            cam.transform.position = new Vector3(cam.transform.position.x * -1, cam.transform.position.y * -1, cam.transform.position.z);

            if (blobLine[blobNumber] != null)
                VectorLine.Destroy(ref blobLine[blobNumber]);

            /* can't be cache since the contour length changes based on what is tracked*/
            Vector2[] splinePoints = new Vector2[contour.Length];
            for (int i = 0; i < contour.Length; i++)
            {
                splinePoints[i] = new Vector2(contour[i].x * -1, contour[i].y * -1);
            }

            blobLine[blobNumber] = new VectorLine("BlobContourPoints", new Vector2[contour.Length], null, 2.0f, LineType.Continuous);
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
}

public class BeatNotify : INotifyPropertyChanged
{
	private int _myProperty;
	public int MyProperty
	{
		get
		{
			return _myProperty;
		}
		set
		{
			//The property changed event will get fired whenever
			//the value changes. The subscriber will do work if the value is
			//1. This way you can keep your business logic outside of the setter
			if(value != _myProperty)
			{
				_myProperty = value;
				NotifyPropertyChanged("MyProperty");
			}
		}
	}
	
	private void NotifyPropertyChanged(string propertyName)
	{
		Debug.Log ("something changed " + propertyName);
		//Raise PropertyChanged event
	}
	public event PropertyChangedEventHandler PropertyChanged;
}
