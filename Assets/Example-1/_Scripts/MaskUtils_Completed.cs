using System;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace DrumBeat
{
    class MaskUtils_Completed : MonoBehaviour
    {
        /*Vectrosity line drawing utils*/
        VectorLine blobLine;
        VectorPoints blobPoints;

        /* Blob point colors*/
        Color[] blobPointColors;
        Vector2[] blobPointsPos;  

        /* Initialization flags*/
        private bool isInitBlob = false;
        private bool isInitContour = false;

        /* RealSense interfaces*/
        private PXCMSenseManager sm = null;
        private PXCMBlobExtractor m_blob;
        private PXCMContourExtractor m_contour;

        /* Used to store blob and contour data*/
        PXCMBlobExtractor.BlobData[] blobData;
        PXCMPointI32[][] pointOuter;
        PXCMPointI32[][] pointInner;

        /* Used to check return status*/
        pxcmStatus results;

        /* ImageInfo structure used to define the essential properties of an image storage.*/
        PXCMImage.ImageInfo info;

        /* Interface to manage image buffers*/
        PXCMImage new_image;

        void Start()
        {
            sm = mask_utils.PipelineManager.session.CreateSenseManager();
            if (sm == null) return;

            mask_utils.PipelineManager.session.CreateImpl<PXCMBlobExtractor>(out m_blob);
            if (m_blob == null) return;
            
            mask_utils.PipelineManager.session.CreateImpl<PXCMContourExtractor>(out m_contour);
            if (m_contour == null) return;

            /* request depth stream as part of the pipeline*/
            sm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 640, 480);

            /* Initialize the pipeline*/
            if (sm.Init() == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                /* Query available capture devices and make sure we have a valid RS device*/
                PXCMCapture.DeviceInfo dinfo;
                sm.QueryCaptureManager().QueryDevice().QueryDeviceInfo(out dinfo);
                if (dinfo != null && dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM)
                {
                    /* Set the depth stream confidence threshold value - Any depth pixels with a confidence score below the threshold will be set to the low confidence pixel value*/
                    sm.QueryCaptureManager().QueryDevice().SetDepthConfidenceThreshold(10);
                    /* Set the smoothing aggressiveness parameter - High smoothing effect for distances between 850mm to 1000mm bringing good accuracy with moderate sharpness level.*/
                    sm.QueryCaptureManager().QueryDevice().SetIVCAMFilterOption(6);  
                }

                /* The BlobData structure describes the parameters of the detected blob*/
                blobData = new PXCMBlobExtractor.BlobData[2];
                pointOuter = new PXCMPointI32[2][];
                pointInner = new PXCMPointI32[2][];

                /* Set Blob tracking parameters */
                float bSmooth = mask_utils.PipelineManager.BlobSmoothing; 
                m_blob.SetSmoothing(bSmooth);

                float maxDepth = mask_utils.PipelineManager.MaxBlobDepth; 
                m_blob.SetMaxDistance(maxDepth);

                int maxBlobs = mask_utils.PipelineManager.MaxBlobs; 
                m_blob.SetMaxBlobs(maxBlobs);

                /* Set Contour smoothing parameters */
                float contourSmooth = mask_utils.PipelineManager.ContourSmoothing;
                m_contour.SetSmoothing(contourSmooth);
            }
            else
            {
                OnDisable();
            }
        }

        void Update()
        {
            if (sm.AcquireFrame(false, 0) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                /* Query and return available image samples*/
                PXCMCapture.Sample sample = sm.QuerySample();
                if (sample != null && sample.depth != null)
                {
                    /* Query depth image properties*/
                    PXCMImage.ImageInfo info = sample.depth.QueryInfo();
                    if (isInitBlob == false)
                    {
                        /* Initialize the blob extraction algorithm*/
                        m_blob.Init(info);
                        isInitBlob = true;
                    }
                    if (isInitContour == false)
                    {
                        /* Initialize the contour extraction algorithm*/
                        m_contour.Init(info);
                        isInitContour = true;
                    }
                    ProcessImage(sample.depth);
                }
                sm.ReleaseFrame();
            }
        }

        void ProcessImage(PXCMImage depth)
        {
            if (depth == null)
                return;

            /* Returns the image properties. */
            info = depth.QueryInfo();

            /* Set the pixel format*/
            info.format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;

            /* Perform blob extraction on the specified depth image.*/
            m_blob.ProcessImage(depth);

            /* Create an instance of the PXC[M]Image interface to manage image buffer access. */
            new_image = mask_utils.PipelineManager.session.CreateImage(info);

            results = m_blob.QueryBlobData(0, new_image, out blobData[0]);
            if (results == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                /* Get all blob points */
                blobPointsPos = new Vector2[6];
                blobPointsPos[0] = new Vector2(blobData[0].centerPoint.x * -1, blobData[0].centerPoint.y * -1);
                blobPointsPos[1] = new Vector2(blobData[0].topPoint.x * -1, blobData[0].topPoint.y * -1);
                blobPointsPos[2] = new Vector2(blobData[0].bottomPoint.x * -1, blobData[0].bottomPoint.y * -1);
                blobPointsPos[3] = new Vector2(blobData[0].leftPoint.x * -1, blobData[0].leftPoint.y * -1);
                blobPointsPos[4] = new Vector2(blobData[0].rightPoint.x * -1, blobData[0].rightPoint.y * -1);
                blobPointsPos[5] = new Vector2(blobData[0].closestPoint.x * -1, blobData[0].closestPoint.y * -1);

                /* Sert blob points colors*/
                blobPointColors = new Color[6];
                blobPointColors[0] = Color.blue;
                blobPointColors[1] = Color.yellow;
                blobPointColors[2] = Color.yellow;
                blobPointColors[3] = Color.yellow;
                blobPointColors[4] = Color.yellow;
                blobPointColors[5] = Color.green;

                DisplayPoints();

                results = m_contour.ProcessImage(new_image);
                if (results == pxcmStatus.PXCM_STATUS_NO_ERROR && m_contour.QueryNumberOfContours() > 0)
                {
                    /* Retrieve the detected contour points.*/
                    results = m_contour.QueryContourData(0, out pointOuter[0]);
                    if (results == pxcmStatus.PXCM_STATUS_NO_ERROR)
                    {
                        if (pointOuter[0] != null && pointOuter[0].Length > 0)
                            DisplayContour(pointOuter[0], 0);
                    }
                }
            }
            new_image.Dispose();
        }

        /// <summary>
        /// OnDisable - Make sure to release all SDK instances and alocated memory to avoid read access error
        /// </summary>
        void OnDisable()
        {
            if (m_blob != null)
            {
                m_blob.Dispose();
                m_blob = null;
            }

            if (m_contour != null)
            {
                m_contour.Dispose();
                m_contour = null;
            }
            if (sm != null)
            {
                sm.Close();
                sm.Dispose();
                sm = null;
            }
        }

        /// <summary>
        /// DisplayPoints - Display all blob points
        /// </summary>
        public void DisplayPoints()
        {
            if (blobPoints != null)
                VectorPoints.Destroy(ref blobPoints);

            blobPoints = new VectorPoints("BlobExtremityPoints", blobPointsPos, blobPointColors, null, 5.0f);
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
            VectorLine.SetCamera(this.GetComponent<Camera>());
            Camera cam = VectorLine.GetCamera();
            cam.transform.position = new Vector3(cam.transform.position.x * -1, cam.transform.position.y * -1, cam.transform.position.z);

            if (blobLine != null)
                VectorLine.Destroy(ref blobLine);

            /* can't be cache since the contour length changes based on whats tracked*/
            Vector2[] splinePoints = new Vector2[contour.Length];
            for(int i = 0; i < contour.Length; i++)
            {
               splinePoints[i] = new Vector2(contour[i].x * -1, contour[i].y * -1);
            }
            
            blobLine = new VectorLine("BlobContourPoints", new Vector2[contour.Length + 1], null, 2.0f, LineType.Continuous);
            blobLine.MakeSpline(splinePoints);
            blobLine.Draw();
        }
    }
}
