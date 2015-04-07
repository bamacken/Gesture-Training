using System;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace mask_utils
{
    class MaskUtils_Completed : MonoBehaviour
    {
        /*Vectrosity line drawing utils*/
        VectorLine[] blobLine;
        VectorPoints blobPoints;

        /* Blob point colors*/
        Color[] blobPointColors;
        List<Vector2> blobPointsPos;  

        /* Initialization flags*/
        private bool isInitBlob = false;
        private bool isInitContour = false;

        /* RealSense interfaces*/
        private PXCMSenseManager sm = null;
        private PXCMBlobExtractor m_blob;
        private PXCMContourExtractor m_contour;

        /* Used to store blob and contour data*/
        PXCMBlobExtractor.BlobData[] blobData;
        List<PXCMPointI32> countourPoints;
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
            /* Set blob points colors*/
            blobPointColors = new Color[12];
            blobPointColors[0] = Color.blue;
            blobPointColors[1] = Color.yellow;
            blobPointColors[2] = Color.yellow;
            blobPointColors[3] = Color.yellow;
            blobPointColors[4] = Color.yellow;
            blobPointColors[5] = Color.green;
            blobPointColors[6] = Color.blue;
            blobPointColors[7] = Color.yellow;
            blobPointColors[8] = Color.yellow;
            blobPointColors[9] = Color.yellow;
            blobPointColors[10] = Color.yellow;
            blobPointColors[11] = Color.green;
            blobLine = new VectorLine[2];
            
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
            int blobCount = m_blob.QueryNumberOfBlobs();

            /* To store all blob points */
            blobPointsPos = new List<Vector2>();

            /* To store all contour points */
            countourPoints = new List<PXCMPointI32>();
            
            for (int i = 0; i < blobCount; i++)
            {
                results = m_blob.QueryBlobData(i, new_image, out blobData[i]);
                if (results == pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    blobPointsPos.Add(new Vector2(blobData[i].centerPoint.x * -1, blobData[i].centerPoint.y * -1));
                    blobPointsPos.Add(new Vector2(blobData[i].topPoint.x * -1, blobData[i].topPoint.y * -1));
                    blobPointsPos.Add(new Vector2(blobData[i].bottomPoint.x * -1, blobData[i].bottomPoint.y * -1));
                    blobPointsPos.Add(new Vector2(blobData[i].leftPoint.x * -1, blobData[i].leftPoint.y * -1));
                    blobPointsPos.Add(new Vector2(blobData[i].rightPoint.x * -1, blobData[i].rightPoint.y * -1));
                    blobPointsPos.Add(new Vector2(blobData[i].closestPoint.x * -1, blobData[i].closestPoint.y * -1));

                    DisplayPoints();

                    results = m_contour.ProcessImage(new_image);
                    if (results == pxcmStatus.PXCM_STATUS_NO_ERROR && m_contour.QueryNumberOfContours() > 0)
                    {
                        results = m_contour.QueryContourData(0, out pointOuter[i]);
                        DisplayContour(pointOuter[i], i);
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

            blobPoints = new VectorPoints("BlobExtremityPoints", blobPointsPos.ToArray(), blobPointColors, null, 5.0f);
            blobPoints.Draw();
        }

        /// <summary>
        /// DisplayContour - Display the contour points, adjust camera position, invert points for correct display.
        /// </summary>
        /// <param name="contour"></param>
        public void DisplayContour(PXCMPointI32[] contour, int blobNumber)
        {
            /* Funky Vectrosity camera flip issue*/
            VectorLine.SetCamera(this.GetComponent<Camera>());
            Camera cam = VectorLine.GetCamera();
            cam.transform.position = new Vector3(cam.transform.position.x * -1, cam.transform.position.y * -1, cam.transform.position.z);

            if (blobLine[blobNumber] != null)
                VectorLine.Destroy(ref blobLine[blobNumber]);

            /* can't be cache since the contour length changes based on whats tracked*/
            Vector2[] splinePoints = new Vector2[contour.Length];
            for(int i = 0; i < contour.Length; i++)
            {
               splinePoints[i] = new Vector2(contour[i].x * -1, contour[i].y * -1);
            }

            blobLine[blobNumber] = new VectorLine("BlobContourPoints", new Vector2[contour.Length], null, 2.0f, LineType.Continuous);
            blobLine[blobNumber].MakeSpline(splinePoints);
            blobLine[blobNumber].Draw();
        }
    }
}
