using System;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace mask_utils
{
    class MaskUtils : MonoBehaviour
    {
        /*Vectrosity line drawing utils*/
        VectorLine blobLine;
        VectorPoints blobPoints;

        /* Blob data used for display*/
        Color[] blobPointColors;
        Vector2[] blobPointsPos;

        /* Step 1 here*/

        /// <summary>
        /// /* Place initialization logic here*/
        /// </summary>
        void Start()
        {
            /* Step 2 here*/
        }

        /// <summary>
        /// /* Place update functionality here*/
        /// </summary>
        void Update()
        {
            /* Step 3 here*/
        }

        /// <summary>
        /// /* Place depth image Processing logic here*/
        /// </summary>
        /// <param name="depth"></param>
        void ProcessImage(PXCMImage depth)
        {
            /* Step 4 here*/
        }

        /// <summary>
        /// /* Place cleanup location here*/
        /// </summary>
        void OnDisable()
        {
            /* Step 5 here*/
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
