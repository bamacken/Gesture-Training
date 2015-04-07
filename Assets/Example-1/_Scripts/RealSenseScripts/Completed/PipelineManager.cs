/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2013 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using UnityEngine;
using System.Collections;

namespace mask_utils
{
    public class PipelineManager : MonoBehaviour
    {
        public static PXCMSession session = null;
		public static float BlobSmoothing = 1f;
		public static float ContourSmoothing = 1f;
		public static float MaxBlobDepth = 500f;
		public static int MaxBlobs = 2;

        /// <summary>
        /// The main entry point for pipeline.
        /// </summary>
        void Start()
        {
            session = PXCMSession.CreateInstance();
        }

		/// <summary>
		/// Raises the disable event.
		/// </summary>
		void OnDisable()
		{
			if (session != null)
			{
				session.Dispose();
			}
		}
	}
}
